using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UltimatePenaltyCup.Core;
using UltimatePenaltyCup.Economy;
using UltimatePenaltyCup.Player;

namespace UltimatePenaltyCup.Gameplay
{
    public enum CrowdReactionMode
    {
        Idle = 0,
        Tension = 1,
        GoalCelebrate = 2,
        SaveGasp = 3,
        MissGroan = 4
    }

    /// <summary>
    /// Renders animated, varied spectators across all 4 stadium venues via Graphics.DrawMeshInstanced.
    /// Respects Baja (35%), Media (65%) and Alta (100%) quality presets and reacts dynamically to shots.
    /// </summary>
    public class CrowdManager : MonoBehaviour
    {
        public static CrowdManager Instance { get; private set; }

        public CrowdReactionMode CurrentReactionMode { get; private set; } = CrowdReactionMode.Idle;
        public int ActiveSpectatorCount
        {
            get
            {
                StadiumType targetVenue = stadiumController != null ? stadiumController.ActiveVenue : builtForVenue;
                if (venueCrowds.TryGetValue(targetVenue, out VenueCrowdData d))
                {
                    return Mathf.RoundToInt(d.totalCount * GetQualityDensityMultiplier());
                }
                return 0;
            }
        }
        public int ReactionTriggerCount { get; private set; } = 0;

        private Mesh basicSpectatorMesh;
        private Mesh flagSpectatorMesh;
        private Material crowdMaterial;
        private StadiumController stadiumController;

        private struct VenueCrowdData
        {
            public List<Matrix4x4[]> basicBatches;
            public List<MaterialPropertyBlock> basicBlocks;
            public List<Matrix4x4[]> flagBatches;
            public List<MaterialPropertyBlock> flagBlocks;
            public int totalCount;
        }

        private readonly Dictionary<StadiumType, VenueCrowdData> venueCrowds = new Dictionary<StadiumType, VenueCrowdData>();

        private StadiumType builtForVenue = (StadiumType)(-1);
        private float crowdExcitement = 0.2f;
        private float reactionTimer = 0f;
        private int lastQualityLevel = -1;
        private bool subscribedToMatchEvents = false;

        private static readonly int ShaderTimeId = Shader.PropertyToID("_CrowdTime");
        private static readonly int ShaderExciteId = Shader.PropertyToID("_CrowdExcitement");
        private static readonly int ShaderModeId = Shader.PropertyToID("_CrowdMode");
        private static readonly int ShirtColorId = Shader.PropertyToID("_ShirtColor");
        private static readonly int PantsColorId = Shader.PropertyToID("_PantsColor");
        private static readonly int SkinColorId = Shader.PropertyToID("_SkinColor");
        private static readonly int AnimParamsId = Shader.PropertyToID("_AnimParams");

        private static readonly Color[] ShirtPalette =
        {
            new Color(0.08f, 0.34f, 0.86f), // Royal club blue
            new Color(0.95f, 0.78f, 0.16f), // Gold
            new Color(0.94f, 0.95f, 0.97f), // Crisp white
            new Color(0.84f, 0.20f, 0.22f), // Cardinal red
            new Color(0.12f, 0.56f, 0.34f), // Pitch green
            new Color(0.16f, 0.20f, 0.28f), // Dark navy
            new Color(0.94f, 0.44f, 0.12f), // Vibrant orange
            new Color(0.22f, 0.68f, 0.92f)  // Sky cyan
        };

        private static readonly Color[] SkinPalette =
        {
            new Color(0.92f, 0.76f, 0.64f),
            new Color(0.86f, 0.67f, 0.53f),
            new Color(0.76f, 0.56f, 0.42f),
            new Color(0.56f, 0.38f, 0.27f)
        };

        private static readonly Color[] PantsPalette =
        {
            new Color(0.10f, 0.13f, 0.20f),
            new Color(0.18f, 0.22f, 0.30f),
            new Color(0.26f, 0.24f, 0.22f)
        };

        private void Awake()
        {
            Instance = this;
            stadiumController = GetComponent<StadiumController>();
            InitializeMeshesAndMaterial();
            BuildAllVenueCrowds();
        }

        private void Start()
        {
            TrySubscribeMatchEvents();
            SyncWithStadium();
        }

        private void OnDestroy()
        {
            UnsubscribeMatchEvents();
            if (basicSpectatorMesh != null) Destroy(basicSpectatorMesh);
            if (flagSpectatorMesh != null) Destroy(flagSpectatorMesh);
            if (crowdMaterial != null) Destroy(crowdMaterial);
        }

        private void TrySubscribeMatchEvents()
        {
            if (subscribedToMatchEvents) return;
            if (PenaltyManager.Instance != null)
            {
                PenaltyManager.Instance.OnAttemptStarted += HandleAttemptStarted;
                PenaltyManager.Instance.OnAttemptFinished += HandleAttemptFinished;
                PenaltyManager.Instance.OnRoundEndedDetailed += HandleRoundEnded;
                subscribedToMatchEvents = true;
            }
            if (PlayerShooter.Instance != null)
            {
                PlayerShooter.Instance.OnShotExecuted += HandleShotFired;
            }
        }

        private void UnsubscribeMatchEvents()
        {
            if (PenaltyManager.Instance != null)
            {
                PenaltyManager.Instance.OnAttemptStarted -= HandleAttemptStarted;
                PenaltyManager.Instance.OnAttemptFinished -= HandleAttemptFinished;
                PenaltyManager.Instance.OnRoundEndedDetailed -= HandleRoundEnded;
            }
            if (PlayerShooter.Instance != null)
            {
                PlayerShooter.Instance.OnShotExecuted -= HandleShotFired;
            }
            subscribedToMatchEvents = false;
        }

        private void HandleAttemptStarted(int attempt, int max)
        {
            TriggerReaction(CrowdReactionMode.Idle, excitement: 0.25f, duration: 0f);
        }

        private void HandleShotFired(Vector3 target, float power, bool isHighPower)
        {
            TriggerReaction(CrowdReactionMode.Tension, excitement: 0.72f, duration: 1.5f);
        }

        private void HandleAttemptFinished(ShotResult result, ShotScoreResult score, string msg)
        {
            switch (result)
            {
                case ShotResult.GoalNormal:
                case ShotResult.GoalCorner:
                case ShotResult.GoalHighPower:
                    TriggerReaction(CrowdReactionMode.GoalCelebrate, excitement: 1.0f, duration: 2.2f);
                    break;
                case ShotResult.Saved:
                    TriggerReaction(CrowdReactionMode.SaveGasp, excitement: 0.85f, duration: 1.8f);
                    break;
                default:
                    TriggerReaction(CrowdReactionMode.MissGroan, excitement: 0.75f, duration: 1.8f);
                    break;
            }
        }

        private void HandleRoundEnded(RoundEndSummary summary)
        {
            TriggerReaction(summary.isVictory ? CrowdReactionMode.GoalCelebrate : CrowdReactionMode.MissGroan,
                excitement: summary.isVictory ? 1.0f : 0.65f, duration: 3.5f);
        }

        public void TriggerReaction(CrowdReactionMode mode, float excitement = 0.85f, float duration = 2.0f)
        {
            if (mode != CrowdReactionMode.Idle) ReactionTriggerCount++;
            CurrentReactionMode = mode;
            crowdExcitement = Mathf.Clamp01(excitement);
            reactionTimer = duration;
        }

        public void SyncWithStadium()
        {
            if (stadiumController != null)
            {
                builtForVenue = stadiumController.ActiveVenue;
            }
            else
            {
                builtForVenue = StadiumType.Professional;
            }
        }

        public void SetQualityDensityRefresh()
        {
            lastQualityLevel = QualitySettings.GetQualityLevel();
        }

        private float GetQualityDensityMultiplier()
        {
            int q = QualitySettings.GetQualityLevel();
            return q switch
            {
                0 => 0.35f, // Baja
                1 => 0.65f, // Media
                _ => 1.00f  // Alta
            };
        }

        private void Update()
        {
            if (!subscribedToMatchEvents && PenaltyManager.Instance != null)
            {
                TrySubscribeMatchEvents();
            }

            StadiumType targetVenue = stadiumController != null ? stadiumController.ActiveVenue : StadiumType.Professional;
            if (targetVenue != builtForVenue)
            {
                builtForVenue = targetVenue;
            }

            if (reactionTimer > 0f)
            {
                reactionTimer -= Time.deltaTime;
                if (reactionTimer <= 0f)
                {
                    CurrentReactionMode = CrowdReactionMode.Idle;
                    crowdExcitement = targetVenue == StadiumType.WorldCup ? 0.42f : 0.22f;
                }
            }

            if (crowdMaterial == null || !venueCrowds.TryGetValue(targetVenue, out VenueCrowdData data)) return;

            SubmitDrawCalls(null);
        }

        public void SubmitDrawCalls(Camera targetCamera)
        {
            StadiumType targetVenue = stadiumController != null ? stadiumController.ActiveVenue : builtForVenue;
            if (crowdMaterial == null || !venueCrowds.TryGetValue(targetVenue, out VenueCrowdData data)) return;

            Shader.SetGlobalFloat(ShaderTimeId, Time.time);
            Shader.SetGlobalFloat(ShaderExciteId, crowdExcitement);
            Shader.SetGlobalFloat(ShaderModeId, (float)CurrentReactionMode);

            float density = GetQualityDensityMultiplier();
            RenderBatches(basicSpectatorMesh, data.basicBatches, data.basicBlocks, density, targetCamera);
            RenderBatches(flagSpectatorMesh, data.flagBatches, data.flagBlocks, density, targetCamera);
        }

        private void RenderBatches(Mesh mesh, List<Matrix4x4[]> batches, List<MaterialPropertyBlock> blocks, float density, Camera targetCamera)
        {
            if (mesh == null || batches == null) return;
            for (int b = 0; b < batches.Count; b++)
            {
                Matrix4x4[] mats = batches[b];
                int count = Mathf.Clamp(Mathf.CeilToInt(mats.Length * density), 0, mats.Length);
                if (count <= 0) continue;

                Graphics.DrawMeshInstanced(mesh, 0, crowdMaterial, mats, count, blocks[b],
                    ShadowCastingMode.Off, receiveShadows: false, layer: 0, camera: targetCamera);
            }
        }

        private void InitializeMeshesAndMaterial()
        {
            Shader shader = Shader.Find("UltimatePenaltyCup/CrowdInstanced");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Universal Render Pipeline/Lit");
            }
            crowdMaterial = new Material(shader) { enableInstancing = true };

            basicSpectatorMesh = BuildLowPolySpectatorMesh(withFlag: false);
            flagSpectatorMesh  = BuildLowPolySpectatorMesh(withFlag: true);
        }

        private void BuildAllVenueCrowds()
        {
            venueCrowds.Clear();
            venueCrowds[StadiumType.Cantera]      = GenerateVenueCrowd(StadiumType.Cantera);
            venueCrowds[StadiumType.Night]        = GenerateVenueCrowd(StadiumType.Night);
            venueCrowds[StadiumType.Professional] = GenerateVenueCrowd(StadiumType.Professional);
            venueCrowds[StadiumType.WorldCup]     = GenerateVenueCrowd(StadiumType.WorldCup);
        }

        private VenueCrowdData GenerateVenueCrowd(StadiumType venue)
        {
            var data = new VenueCrowdData
            {
                basicBatches = new List<Matrix4x4[]>(),
                basicBlocks  = new List<MaterialPropertyBlock>(),
                flagBatches  = new List<Matrix4x4[]>(),
                flagBlocks   = new List<MaterialPropertyBlock>(),
                totalCount   = 0
            };

            var basicMats = new List<Matrix4x4>();
            var basicCols = new List<SpectatorVisual>();
            var flagMats  = new List<Matrix4x4>();
            var flagCols  = new List<SpectatorVisual>();

            var rng = new System.Random(1000 + (int)venue * 137);

            // Coordinates strictly match BuildTerracedStand in CharacterAndStadiumUpgrader!
            void AddStandSpectators(Vector3 standOrigin, float standYawDeg, float standWidth, int tiers, float stepH, float stepD, float occupancy, float flagChance)
            {
                Quaternion standRot = Quaternion.Euler(0f, standYawDeg, 0f);
                for (int t = 0; t < tiers; t++)
                {
                    float localY = 1.4f + t * stepH + 0.02f;
                    float localZ = t * stepD + stepD * 0.42f;
                    float spacingX = Mathf.Lerp(1.65f, 0.92f, occupancy);
                    int slots = Mathf.FloorToInt((standWidth - 3.0f) / spacingX);

                    for (int s = 0; s < slots; s++)
                    {
                        float lx = -(standWidth * 0.5f - 1.5f) + s * spacingX + (float)(rng.NextDouble() * 0.25 - 0.12);
                        // Keep concrete stair aisles (at +- 0.25 * width) unobstructed!
                        if (Mathf.Abs(Mathf.Abs(lx) - standWidth * 0.25f) < 1.25f) continue;
                        if (rng.NextDouble() > occupancy) continue;

                        Vector3 localPos = new Vector3(lx, localY, localZ + (float)(rng.NextDouble() * 0.30 - 0.15));
                        Vector3 worldPos = standOrigin + standRot * localPos;
                        // Face toward pitch center (0, 0, -5)
                        Vector3 toPitch = new Vector3(0f, worldPos.y, -6f) - worldPos;
                        toPitch.y = 0f;
                        Quaternion lookRot = toPitch.sqrMagnitude > 0.01f
                            ? Quaternion.LookRotation(toPitch.normalized) * Quaternion.Euler(0f, (float)(rng.NextDouble() * 22f - 11f), 0f)
                            : Quaternion.identity;

                        float hScale = 0.88f + (float)rng.NextDouble() * 0.24f;
                        Matrix4x4 mtx = Matrix4x4.TRS(worldPos, lookRot, new Vector3(hScale, hScale, hScale));

                        var vis = new SpectatorVisual
                        {
                            shirt = ShirtPalette[rng.Next(ShirtPalette.Length)],
                            pants = PantsPalette[rng.Next(PantsPalette.Length)],
                            skin  = SkinPalette[rng.Next(SkinPalette.Length)],
                            phase = (float)(rng.NextDouble() * Mathf.PI * 2f),
                            speed = 0.85f + (float)(rng.NextDouble() * 0.45),
                            style = rng.Next(3)
                        };

                        if (rng.NextDouble() < flagChance)
                        {
                            flagMats.Add(mtx);
                            flagCols.Add(vis);
                        }
                        else
                        {
                            basicMats.Add(mtx);
                            basicCols.Add(vis);
                        }
                    }
                }
            }

            switch (venue)
            {
                case StadiumType.Cantera:
                    AddStandSpectators(new Vector3(0f, 0f, 12.5f), 0f, 32f, 2, 1.0f, 2.2f, occupancy: 0.40f, flagChance: 0.10f);
                    break;

                case StadiumType.Night:
                    AddStandSpectators(new Vector3(0f, 0f, 14f), 0f, 56f, 4, 1.1f, 2.4f, occupancy: 0.62f, flagChance: 0.16f);
                    AddStandSpectators(new Vector3(-38f, 0f, 3f), -90f, 30f, 4, 1.1f, 2.4f, occupancy: 0.50f, flagChance: 0.12f);
                    AddStandSpectators(new Vector3(38f, 0f, 3f), 90f, 30f, 4, 1.1f, 2.4f, occupancy: 0.50f, flagChance: 0.12f);
                    break;

                case StadiumType.Professional:
                    AddStandSpectators(new Vector3(0f, 0f, 14f), 0f, 70f, 5, 1.15f, 2.4f, occupancy: 0.78f, flagChance: 0.20f);
                    AddStandSpectators(new Vector3(-38f, 0f, 3f), -90f, 36f, 5, 1.15f, 2.4f, occupancy: 0.68f, flagChance: 0.15f);
                    AddStandSpectators(new Vector3(38f, 0f, 3f), 90f, 36f, 5, 1.15f, 2.4f, occupancy: 0.68f, flagChance: 0.15f);
                    break;

                case StadiumType.WorldCup:
                    AddStandSpectators(new Vector3(0f, 0f, 14f), 0f, 80f, 6, 1.25f, 2.5f, occupancy: 0.92f, flagChance: 0.25f);
                    AddStandSpectators(new Vector3(-39f, 0f, 4f), -90f, 44f, 6, 1.25f, 2.5f, occupancy: 0.80f, flagChance: 0.22f);
                    AddStandSpectators(new Vector3(39f, 0f, 4f), 90f, 44f, 6, 1.25f, 2.5f, occupancy: 0.80f, flagChance: 0.22f);
                    break;
            }

            PackBatches(basicMats, basicCols, data.basicBatches, data.basicBlocks);
            PackBatches(flagMats, flagCols, data.flagBatches, data.flagBlocks);
            data.totalCount = basicMats.Count + flagMats.Count;
            return data;
        }

        private struct SpectatorVisual
        {
            public Color shirt, pants, skin;
            public float phase, speed;
            public int style;
        }

        private static void PackBatches(List<Matrix4x4> srcMats, List<SpectatorVisual> srcVis,
            List<Matrix4x4[]> outBatches, List<MaterialPropertyBlock> outBlocks)
        {
            const int maxBatch = 1000;
            for (int start = 0; start < srcMats.Count; start += maxBatch)
            {
                int len = Mathf.Min(maxBatch, srcMats.Count - start);
                var arr = new Matrix4x4[len];
                var mpb = new MaterialPropertyBlock();
                var shirts = new Vector4[len];
                var pants = new Vector4[len];
                var skins = new Vector4[len];
                var anims = new Vector4[len];

                for (int i = 0; i < len; i++)
                {
                    arr[i] = srcMats[start + i];
                    var v = srcVis[start + i];
                    shirts[i] = v.shirt;
                    pants[i] = v.pants;
                    skins[i] = v.skin;
                    anims[i] = new Vector4(v.phase, v.speed, v.style, 1f);
                }

                mpb.SetVectorArray(ShirtColorId, shirts);
                mpb.SetVectorArray(PantsColorId, pants);
                mpb.SetVectorArray(SkinColorId, skins);
                mpb.SetVectorArray(AnimParamsId, anims);

                outBatches.Add(arr);
                outBlocks.Add(mpb);
            }
        }
        private static Mesh BuildLowPolySpectatorMesh(bool withFlag)
        {
            var verts = new List<Vector3>();
            var cols  = new List<Color>(); // R=armMask, G=skinMask, B=pantsMask, A=flagMask
            var tris  = new List<int>();

            void AddBox(Vector3 center, Vector3 size, Color mask)
            {
                Vector3 h = size * 0.5f;
                int b = verts.Count;
                Vector3[] p =
                {
                    center + new Vector3(-h.x, -h.y, -h.z), center + new Vector3( h.x, -h.y, -h.z),
                    center + new Vector3( h.x,  h.y, -h.z), center + new Vector3(-h.x,  h.y, -h.z),
                    center + new Vector3(-h.x, -h.y,  h.z), center + new Vector3( h.x, -h.y,  h.z),
                    center + new Vector3( h.x,  h.y,  h.z), center + new Vector3(-h.x,  h.y,  h.z)
                };
                for (int i = 0; i < 8; i++) { verts.Add(p[i]); cols.Add(mask); }
                // Drop unseen bottom face (-Y) to cut spectator triangle count ~17%
                int[] boxTris =
                {
                    0,2,1, 0,3,2, 4,5,6, 4,6,7,
                    2,3,7, 2,7,6,
                    0,4,7, 0,7,3, 1,2,6, 1,6,5
                };
                for (int i = 0; i < boxTris.Length; i++) tris.Add(b + boxTris[i]);
            }

            // Unified Lower Body / Pants seated on tier (B=1)
            AddBox(new Vector3(0f, 0.36f, 0f), new Vector3(0.32f, 0.72f, 0.17f), new Color(0f, 0f, 1f, 0f));

            // Torso / Jersey (0,0,0,0 -> shirt)
            AddBox(new Vector3(0f, 0.96f, 0f), new Vector3(0.38f, 0.50f, 0.22f), new Color(0f, 0f, 0f, 0f));

            // Head (G=1 -> skin)
            AddBox(new Vector3(0f, 1.34f, 0.01f), new Vector3(0.22f, 0.24f, 0.22f), new Color(0f, 1f, 0f, 0f));

            // Left & Right Arms (R=1 -> vertex shader lifts/claps!)
            AddBox(new Vector3(-0.25f, 0.98f, 0.03f), new Vector3(0.11f, 0.44f, 0.12f), new Color(1f, 0f, 0f, 0f));
            AddBox(new Vector3( 0.25f, 0.98f, 0.03f), new Vector3(0.11f, 0.44f, 0.12f), new Color(1f, 0f, 0f, 0f));

            if (withFlag)
            {
                // Handheld waving team flag/scarf banner raised above shoulder (A=1 + R=0.5)
                AddBox(new Vector3(0.29f, 1.48f, 0.08f), new Vector3(0.03f, 0.85f, 0.03f), new Color(0.5f, 0f, 1f, 0f));
                AddBox(new Vector3(0.52f, 1.72f, 0.08f), new Vector3(0.46f, 0.32f, 0.03f), new Color(0.5f, 0f, 0f, 1f));
            }

            Mesh m = new Mesh { name = withFlag ? "SpectatorFlagMesh" : "SpectatorMesh" };
            m.SetVertices(verts);
            m.SetColors(cols);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }
    }
}
