using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UltimatePenaltyCup.Economy;
using UltimatePenaltyCup.Gameplay;
using UltimatePenaltyCup.Goalkeeper;
using UltimatePenaltyCup.Levels;
using UltimatePenaltyCup.Player;
using UltimatePenaltyCup.SaveSystem;

namespace UltimatePenaltyCup.Core
{
    /// <summary>
    /// Standalone Windows 1920x1080 real-time performance & functional telemetry runner.
    /// Completely dormant during normal gameplay unless launched with `-runPerfBenchmark1080`.
    /// </summary>
    public class StandalonePerfBenchmarkRunner : MonoBehaviour
    {
        private static bool initialized = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void TryAutoStart()
        {
            if (initialized) return;

            bool hasFlag = false;
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "-runPerfBenchmark1080", StringComparison.OrdinalIgnoreCase))
                {
                    hasFlag = true;
                    break;
                }
            }
            if (!hasFlag) return;

            initialized = true;
            GameObject go = new GameObject("StandalonePerfBenchmarkRunner");
            DontDestroyOnLoad(go);
            go.AddComponent<StandalonePerfBenchmarkRunner>();
        }

        private IEnumerator Start()
        {
            // Force exact 1920x1080 windowed measurement inside the standalone EXE
            Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
            for (int i = 0; i < 15; i++) yield return null;

            int prevVsync = QualitySettings.vSyncCount;
            int prevTarget = Application.targetFrameRate;

            using var drawCallsRec = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            using var setPassRec = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            using var trisRec = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            using var gcAllocRec = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");

            var sb = new StringBuilder();
            sb.AppendLine("====================================================================================");
            sb.AppendLine("ULTIMATE PENALTY CUP 3D - REAL STANDALONE WINDOWS EXE BENCHMARK (1920x1080)");
            sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Executable: {Application.productName} v{Application.version} | Build Debug: {UnityEngine.Debug.isDebugBuild}");
            sb.AppendLine($"Actual Resolution: {Screen.width}x{Screen.height} | FullscreenMode: {Screen.fullScreenMode}");
            sb.AppendLine($"Quality: {(QualitySettings.names != null && QualitySettings.GetQualityLevel() < QualitySettings.names.Length ? QualitySettings.names[QualitySettings.GetQualityLevel()] : QualitySettings.GetQualityLevel().ToString())} | vSync={prevVsync} targetFPS={prevTarget}");
            sb.AppendLine($"GPU: {SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsDeviceType}) | CPU: {SystemInfo.processorType} ({SystemInfo.processorCount} cores)");
            sb.AppendLine("====================================================================================");

            try
            {
                // Keep real production vSync/cap telemetry AND uncapped GPU/CPU headroom metrics cleanly separated!
                // Phase 1: MainMenu 120 frames (uncapped headroom)
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = -1;
                yield return null;

                var menuStats = new FrameStats();
                yield return SampleFrames(120, menuStats, drawCallsRec, setPassRec, trisRec, gcAllocRec);
                sb.AppendLine($"1. MAIN MENU (1920x1080, 120 frames, Scene={SceneManager.GetActiveScene().name}):");
                AppendStatsLine(sb, "   Uncapped", menuStats);

                // Phase 2: Real Scene Transition MainMenu -> MatchScene
                if (SaveManager.Instance != null)
                {
                    var testSave = new SaveData();
                    for (int r = 1; r <= 5; r++) testSave.unlockedRounds.Add(r);
                    SaveManager.Instance.SetTestDataInMemory(testSave);
                }

                var sw = Stopwatch.StartNew();
                GameManager.Instance.SelectRoundAndStartMatch(3);
                while (SceneManager.GetActiveScene().name != GameManager.SCENE_MATCH)
                {
                    yield return null;
                }
                yield return null;
                sw.Stop();
                double sceneLoadMs = sw.Elapsed.TotalMilliseconds;
                sb.AppendLine($"2. SCENE TRANSITION (MainMenu -> MatchScene): {sceneLoadMs:F1} ms");

                // Phase 3: Cycle all 4 Stadium Venues (switch latency + 90 rendered frames each)
                sb.AppendLine("3. STADIUM VENUES (Switch latency + 90 frames each at 1920x1080):");
                StadiumType[] venues = { StadiumType.Cantera, StadiumType.Night, StadiumType.Professional, StadiumType.WorldCup };
                foreach (var v in venues)
                {
                    sw.Restart();
                    StadiumController.Instance?.ApplyVenue(v);
                    yield return null;
                    sw.Stop();
                    double switchMs = sw.Elapsed.TotalMilliseconds;

                    var st = new FrameStats();
                    yield return SampleFrames(90, st, drawCallsRec, setPassRec, trisRec, gcAllocRec);
                    sb.AppendLine($"   {v,-12} | Switch={switchMs,6:F2} ms | AvgFPS={st.AvgFps,6:F1} | 1%Low={st.OnePercentLowFps,6:F1} | MinFPS={st.MinFps,6:F1} | AvgFrame={st.AvgMs,5:F2} ms | MaxSpike={st.MaxMs,5:F2} ms | DrawCalls={st.AvgDrawCalls:F0} | SetPass={st.AvgSetPass:F0} | Tris={st.AvgTris:F0}");
                }

                // Phase 4: 6 Real Full-Animation Shots (RunUp + Ball Flight + Keeper Dives/Saves) at 1920x1080
                PenaltyManager pm = PenaltyManager.Instance;
                pm.PostAttemptDelay = 0.08f;
                pm.SaveConfirmDelay = 0.14f;
                var shotStats = new FrameStats();
                int shotsDone = 0;

                Vector3[] targets =
                {
                    new Vector3(-2.7f, 0.7f, 0f),
                    new Vector3( 2.8f, 1.9f, 0f),
                    new Vector3( 0.0f, 1.2f, 0f),
                    new Vector3(-3.0f, 2.0f, 0f),
                    new Vector3( 2.4f, 0.6f, 0f),
                    new Vector3(-1.4f, 1.5f, 0f)
                };

                for (int i = 0; i < targets.Length; i++)
                {
                    if (pm.IsRoundOver)
                    {
                        pm.InitializeRound();
                        yield return null;
                        pm.PostAttemptDelay = 0.08f;
                    }
                    float waitReady = 0f;
                    while ((!pm.CanAcceptShotInput || !PlayerShooter.Instance.CanShoot) && waitReady < 4f)
                    {
                        waitReady += Time.unscaledDeltaTime;
                        yield return null;
                    }

                    bool resolved = false;
                    void OnDone(ShotResult r, ShotScoreResult s, string msg) => resolved = true;
                    pm.OnAttemptFinished += OnDone;
                    PlayerShooter.Instance.ExecuteAutomatedShot(targets[i], 0.80f, skipRunUp: false);

                    float elapsed = 0f;
                    while (!resolved && elapsed < 5f)
                    {
                        yield return null;
                        float dt = Time.unscaledDeltaTime;
                        elapsed += dt;
                        RecordFrame(dt, shotStats, drawCallsRec, setPassRec, trisRec, gcAllocRec);
                    }
                    pm.OnAttemptFinished -= OnDone;
                    shotsDone++;
                    yield return new WaitForSeconds(0.15f);
                }

                // Phase 5: Also verify real shipping vSync=1 60 FPS lock stability (60 frames)
                QualitySettings.vSyncCount = prevVsync;
                Application.targetFrameRate = prevTarget;
                yield return null;
                var vsyncStats = new FrameStats();
                yield return SampleFrames(60, vsyncStats, drawCallsRec, setPassRec, trisRec, gcAllocRec);

                // Phase 6: Measure Peak WorldCup Stadium (with GPU-instanced crowds + UI toast) across Baja / Media / Alta presets
                sb.AppendLine($"4. REAL FULL-ANIMATION SHOTS + KEEPER DIVES ({shotsDone} shots at 1920x1080):");
                AppendStatsLine(sb, "   ShotAction", shotStats);
                sb.AppendLine("5. SHIPPING VSYNC/TARGET-60 STABILITY (60 frames):");
                AppendStatsLine(sb, "   ShippingCap", vsyncStats);

                sb.AppendLine("6. QUALITY PRESETS ON PEAK WORLDCUP STADIUM + CROWD + UI ALERT (Uncapped 60 frames each at 1920x1080):");
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = -1;
                StadiumController.Instance?.ApplyVenue(StadiumType.WorldCup);
                CrowdManager.Instance?.TriggerReaction(CrowdReactionMode.GoalCelebrate, 1.0f, 3.0f);
                int prevQuality = QualitySettings.GetQualityLevel();
                for (int q = 0; q <= 2; q++)
                {
                    if (q < QualitySettings.names.Length)
                    {
                        QualitySettings.SetQualityLevel(q, true);
                        QualitySettings.vSyncCount = 0;
                        Application.targetFrameRate = -1;
                        CrowdManager.Instance?.SetQualityDensityRefresh();
                        yield return null;
                        yield return null;
                        var qStats = new FrameStats();
                        yield return SampleFrames(60, qStats, drawCallsRec, setPassRec, trisRec, gcAllocRec);
                        int crowdCnt = CrowdManager.Instance != null ? CrowdManager.Instance.ActiveSpectatorCount : 0;
                        sb.AppendLine($"   Preset[{QualitySettings.names[q],-5}] Crowd={crowdCnt,4} | AvgFPS={qStats.AvgFps,6:F1} | 1%Low={qStats.OnePercentLowFps,6:F1} | AvgFrame={qStats.AvgMs,5:F2} ms | SetPass={qStats.AvgSetPass:F0} | Tris={qStats.AvgTris:F0}");
                    }
                }
                QualitySettings.SetQualityLevel(prevQuality, true);

                sb.AppendLine($"7. MEMORY SUMMARY: MonoHeap={GC.GetTotalMemory(false) / (1024f * 1024f):F2} MB | UnityAllocated={Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f):F2} MB | AvgGC/Frame={shotStats.AvgGcKb:F2} KB");
                sb.AppendLine("====================================================================================");

                string outPath = ResolveLogOutputPath();
                Directory.CreateDirectory(Path.GetDirectoryName(outPath));
                File.WriteAllText(outPath, sb.ToString());
                UnityEngine.Debug.Log($"[StandalonePerfBenchmark] Wrote report to {outPath}");
            }
            finally
            {
                QualitySettings.vSyncCount = prevVsync;
                Application.targetFrameRate = prevTarget;
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit(0);
#endif
        }

        private static string ResolveLogOutputPath()
        {
            string cwd = Directory.GetCurrentDirectory();
            string logsDir = Path.Combine(cwd, "Logs");
            if (Directory.Exists(logsDir))
                return Path.Combine(logsDir, "Standalone1080pBenchmark.txt");

            string repoLogs = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "..", "Logs"));
            if (Directory.Exists(repoLogs))
                return Path.Combine(repoLogs, "Standalone1080pBenchmark.txt");

            return Path.Combine(Application.dataPath, "..", "Standalone1080pBenchmark.txt");
        }

        private IEnumerator SampleFrames(int count, FrameStats stats,
            ProfilerRecorder drawRec, ProfilerRecorder setPassRec, ProfilerRecorder trisRec, ProfilerRecorder gcRec)
        {
            for (int i = 0; i < count; i++)
            {
                yield return null;
                RecordFrame(Time.unscaledDeltaTime, stats, drawRec, setPassRec, trisRec, gcRec);
            }
        }

        private static void RecordFrame(float dt, FrameStats stats,
            ProfilerRecorder drawRec, ProfilerRecorder setPassRec, ProfilerRecorder trisRec, ProfilerRecorder gcRec)
        {
            if (dt <= 0.00005f || dt > 0.5f) return;
            stats.dts.Add(dt);
            if (drawRec.Valid) stats.drawCalls += drawRec.LastValue;
            if (setPassRec.Valid) stats.setPass += setPassRec.LastValue;
            if (trisRec.Valid) stats.tris += trisRec.LastValue;
            if (gcRec.Valid) stats.gcBytes += gcRec.LastValue;
            stats.sampleCount++;
        }

        private static void AppendStatsLine(StringBuilder sb, string label, FrameStats st)
        {
            sb.AppendLine($"{label,-14} | AvgFPS={st.AvgFps,6:F1} | 1%Low={st.OnePercentLowFps,6:F1} | MinFPS={st.MinFps,6:F1} | AvgFrame={st.AvgMs,5:F2} ms | MaxSpike={st.MaxMs,5:F2} ms | DrawCalls={st.AvgDrawCalls:F0} | SetPass={st.AvgSetPass:F0} | Tris={st.AvgTris:F0} | GC={st.AvgGcKb:F2} KB/f");
        }

        private class FrameStats
        {
            public readonly List<float> dts = new List<float>(256);
            public long drawCalls;
            public long setPass;
            public long tris;
            public long gcBytes;
            public int sampleCount;

            public float AvgMs
            {
                get
                {
                    if (dts.Count == 0) return 0f;
                    float sum = 0f;
                    for (int i = 0; i < dts.Count; i++) sum += dts[i];
                    return (sum / dts.Count) * 1000f;
                }
            }

            public float MaxMs
            {
                get
                {
                    float max = 0f;
                    for (int i = 0; i < dts.Count; i++) if (dts[i] > max) max = dts[i];
                    return max * 1000f;
                }
            }

            public float AvgFps => AvgMs > 0.001f ? 1000f / AvgMs : 0f;
            public float MinFps => MaxMs > 0.001f ? 1000f / MaxMs : 0f;

            public float OnePercentLowFps
            {
                get
                {
                    if (dts.Count == 0) return 0f;
                    var sorted = new List<float>(dts);
                    sorted.Sort();
                    int idx = Mathf.Clamp(Mathf.FloorToInt((sorted.Count - 1) * 0.99f), 0, sorted.Count - 1);
                    return 1f / Mathf.Max(0.0001f, sorted[idx]);
                }
            }

            public float AvgDrawCalls => sampleCount > 0 ? (float)drawCalls / sampleCount : 0f;
            public float AvgSetPass => sampleCount > 0 ? (float)setPass / sampleCount : 0f;
            public float AvgTris => sampleCount > 0 ? (float)tris / sampleCount : 0f;
            public float AvgGcKb => sampleCount > 0 ? (gcBytes / 1024f) / sampleCount : 0f;
        }
    }
}
