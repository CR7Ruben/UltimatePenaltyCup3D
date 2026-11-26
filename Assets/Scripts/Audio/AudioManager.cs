#pragma warning disable 0649
using System;
using UnityEngine;
using UltimatePenaltyCup.SaveSystem;

namespace UltimatePenaltyCup.Audio
{
    public enum BgmType
    {
        M01_MainMenu,
        M02_Match,
        M03_Victory,
        M04_Defeat
    }

    public enum SfxType
    {
        S01_Kick,
        S02_Goal,
        S03_Save,
        S04_Purchase,
        S05_Unlock,
        S06_ButtonClick,
        PostHit,
        Whistle,
        Miss,
        CrowdAmbient,
        Footstep,
        NetSwish,
        TurfLanding,
        UiConfirm,
        UiError,
        CrowdGoalRoar,
        CrowdSaveGasp,
        CrowdMissGroan,
        CrowdApplause
    }

    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource uiSource;

        [Header("Audio Clips (Optional - procedural fallback if unassigned)")]
        [SerializeField] private AudioClip bgmMenu;
        [SerializeField] private AudioClip bgmMatch;
        [SerializeField] private AudioClip bgmVictory;
        [SerializeField] private AudioClip bgmDefeat;

        [SerializeField] private AudioClip sfxKick;
        [SerializeField] private AudioClip sfxGoal;
        [SerializeField] private AudioClip sfxSave;
        [SerializeField] private AudioClip sfxPurchase;
        [SerializeField] private AudioClip sfxUnlock;
        [SerializeField] private AudioClip sfxButtonClick;
        [SerializeField] private AudioClip sfxPostHit;
        [SerializeField] private AudioClip sfxWhistle;
        [SerializeField] private AudioClip sfxMiss;
        [SerializeField] private AudioClip sfxCrowd;

        private AudioClip procKick, procGoal, procSave, procPurchase, procUnlock, procButtonClick;
        private AudioClip procPostHit, procWhistle, procMiss, procCrowd;
        private AudioClip procFootstep, procNetSwish, procTurfLanding, procUiConfirm, procUiError;
        private AudioClip procCrowdGoalRoar, procCrowdSaveGasp, procCrowdMissGroan, procCrowdApplause;
        private AudioClip procBgmMenu, procBgmMatch, procBgmVictory, procBgmDefeat;

        private static readonly float[] UnlockNotes = { 440f, 554.37f, 659.25f, 880f };
        private static readonly float[] VictoryMelody = { 523.25f, 659.25f, 783.99f, 1046.50f };
        private static readonly float[] DefeatMelody = { 440f, 392f, 349.23f, 329.63f };

        public float MasterVolume { get; private set; } = 1.0f;
        public float MusicVolume { get; private set; } = 0.8f;
        public float SfxVolume { get; private set; } = 0.9f;
        public float CrowdVolume { get; private set; } = 0.85f;
        public float UiVolume { get; private set; } = 0.90f;

        public event Action OnVolumeSettingsChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
            GenerateProceduralAudioClips();
            LoadVolumeSettings();
        }

        private void InitializeAudioSources()
        {
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
            }

            if (uiSource == null)
            {
                uiSource = gameObject.AddComponent<AudioSource>();
                uiSource.loop = false;
                uiSource.playOnAwake = false;
                uiSource.ignoreListenerPause = true;
            }
        }

        public void LoadVolumeSettings()
        {
            if (SaveManager.Instance != null && SaveManager.Instance.CurrentData != null)
            {
                var d = SaveManager.Instance.CurrentData;
                MasterVolume = Mathf.Clamp01(d.masterVolume);
                AudioListener.volume = MasterVolume;
                MusicVolume = Mathf.Clamp01(d.musicVolume);
                SfxVolume = Mathf.Clamp01(d.sfxVolume);
                CrowdVolume = Mathf.Clamp01(d.crowdVolume);
                UiVolume = Mathf.Clamp01(d.uiVolume);
                if (bgmSource != null) bgmSource.volume = MusicVolume;
                if (sfxSource != null) sfxSource.volume = SfxVolume;
                if (uiSource != null) uiSource.volume = UiVolume;
                OnVolumeSettingsChanged?.Invoke();
            }
            else
            {
                SetMasterVolume(1.0f);
                SetMusicVolume(0.8f);
                SetSfxVolume(0.9f);
                SetCrowdVolume(0.85f);
                SetUiVolume(0.90f);
            }
        }

        public void SetMasterVolume(float volume)
        {
            MasterVolume = Mathf.Clamp01(volume);
            AudioListener.volume = MasterVolume;
            if (SaveManager.Instance != null && SaveManager.Instance.CurrentData != null)
            {
                SaveManager.Instance.CurrentData.masterVolume = MasterVolume;
                SaveManager.Instance.Save();
            }
            OnVolumeSettingsChanged?.Invoke();
        }

        public void SetMusicVolume(float volume)
        {
            MusicVolume = Mathf.Clamp01(volume);
            if (bgmSource != null) bgmSource.volume = MusicVolume;
            if (SaveManager.Instance != null && SaveManager.Instance.CurrentData != null)
            {
                SaveManager.Instance.CurrentData.musicVolume = MusicVolume;
                SaveManager.Instance.Save();
            }
            OnVolumeSettingsChanged?.Invoke();
        }

        public void SetSfxVolume(float volume)
        {
            SfxVolume = Mathf.Clamp01(volume);
            if (sfxSource != null) sfxSource.volume = SfxVolume;
            if (SaveManager.Instance != null && SaveManager.Instance.CurrentData != null)
            {
                SaveManager.Instance.CurrentData.sfxVolume = SfxVolume;
                SaveManager.Instance.Save();
            }
            OnVolumeSettingsChanged?.Invoke();
        }

        public void SetCrowdVolume(float volume)
        {
            CrowdVolume = Mathf.Clamp01(volume);
            if (SaveManager.Instance != null && SaveManager.Instance.CurrentData != null)
            {
                SaveManager.Instance.CurrentData.crowdVolume = CrowdVolume;
                SaveManager.Instance.Save();
            }
            OnVolumeSettingsChanged?.Invoke();
        }

        public void SetUiVolume(float volume)
        {
            UiVolume = Mathf.Clamp01(volume);
            if (uiSource != null) uiSource.volume = UiVolume;
            if (SaveManager.Instance != null && SaveManager.Instance.CurrentData != null)
            {
                SaveManager.Instance.CurrentData.uiVolume = UiVolume;
                SaveManager.Instance.Save();
            }
            OnVolumeSettingsChanged?.Invoke();
        }

        public AudioClip GetClip(SfxType type)
        {
            return type switch
            {
                SfxType.S01_Kick        => sfxKick != null ? sfxKick : procKick,
                SfxType.S02_Goal        => sfxGoal != null ? sfxGoal : procGoal,
                SfxType.S03_Save        => sfxSave != null ? sfxSave : procSave,
                SfxType.S04_Purchase    => sfxPurchase != null ? sfxPurchase : procPurchase,
                SfxType.S05_Unlock      => sfxUnlock != null ? sfxUnlock : procUnlock,
                SfxType.S06_ButtonClick => sfxButtonClick != null ? sfxButtonClick : procButtonClick,
                SfxType.PostHit         => sfxPostHit != null ? sfxPostHit : procPostHit,
                SfxType.Whistle         => sfxWhistle != null ? sfxWhistle : procWhistle,
                SfxType.Miss            => sfxMiss != null ? sfxMiss : procMiss,
                SfxType.CrowdAmbient    => sfxCrowd != null ? sfxCrowd : procCrowd,
                SfxType.Footstep        => procFootstep,
                SfxType.NetSwish        => procNetSwish,
                SfxType.TurfLanding     => procTurfLanding,
                SfxType.UiConfirm       => procUiConfirm,
                SfxType.UiError         => procUiError,
                SfxType.CrowdGoalRoar   => procCrowdGoalRoar,
                SfxType.CrowdSaveGasp   => procCrowdSaveGasp,
                SfxType.CrowdMissGroan  => procCrowdMissGroan,
                SfxType.CrowdApplause   => procCrowdApplause,
                _ => null
            };
        }

        public void PlayBgm(BgmType type)
        {
            AudioClip clip = type switch
            {
                BgmType.M01_MainMenu => bgmMenu != null ? bgmMenu : procBgmMenu,
                BgmType.M02_Match    => bgmMatch != null ? bgmMatch : procBgmMatch,
                BgmType.M03_Victory  => bgmVictory != null ? bgmVictory : procBgmVictory,
                BgmType.M04_Defeat   => bgmDefeat != null ? bgmDefeat : procBgmDefeat,
                _ => null
            };

            if (clip != null && bgmSource != null)
            {
                if (bgmSource.clip == clip && bgmSource.isPlaying) return;
                bgmSource.clip = clip;
                bgmSource.volume = MusicVolume;
                bgmSource.Play();
            }
        }

        public void StopBgm()
        {
            if (bgmSource != null) bgmSource.Stop();
        }

        public void PlaySfx(SfxType type)
        {
            AudioClip clip = GetClip(type);
            if (clip == null) return;

            bool isUi = type == SfxType.S06_ButtonClick || type == SfxType.S04_Purchase || type == SfxType.S05_Unlock ||
                        type == SfxType.UiConfirm || type == SfxType.UiError;
            AudioSource src = isUi && uiSource != null ? uiSource : sfxSource;
            if (src == null) return;

            float pitch = type switch
            {
                SfxType.S01_Kick        => UnityEngine.Random.Range(0.92f, 1.08f),
                SfxType.S03_Save        => UnityEngine.Random.Range(0.95f, 1.05f),
                SfxType.S06_ButtonClick => UnityEngine.Random.Range(0.98f, 1.02f),
                SfxType.PostHit         => UnityEngine.Random.Range(0.92f, 1.08f),
                SfxType.Footstep        => UnityEngine.Random.Range(0.90f, 1.12f),
                SfxType.TurfLanding     => UnityEngine.Random.Range(0.92f, 1.06f),
                _ => 1.0f
            };
            float vol = isUi ? UiVolume : SfxVolume;
            src.pitch = pitch;
            src.PlayOneShot(clip, vol);
        }

        #region Procedural Audio Synthesis
        private void GenerateProceduralAudioClips()
        {
            const int sampleRate = 22050;

            procKick = CreateSyntheticClip("Proc_Kick", 0.28f, sampleRate, (t, dur) =>
            {
                float progress = t / dur;
                float freq = Mathf.Lerp(160f, 40f, progress);
                float envelope = Mathf.Pow(1f - progress, 2.5f);
                return Mathf.Sin(2f * Mathf.PI * freq * t) * envelope;
            });

            procGoal = CreateSyntheticClip("Proc_Goal", 1.2f, sampleRate, (t, dur) =>
            {
                float env = Mathf.Sin(Mathf.Clamp01(t / 0.1f) * Mathf.PI * 0.5f) * Mathf.Clamp01((dur - t) / dur);
                float chord = Mathf.Sin(2f * Mathf.PI * 523.25f * t)
                            + Mathf.Sin(2f * Mathf.PI * 659.25f * t)
                            + Mathf.Sin(2f * Mathf.PI * 783.99f * t)
                            + Mathf.Sin(2f * Mathf.PI * 1046.50f * t);
                return chord * 0.22f * env;
            });

            procSave = CreateSyntheticClip("Proc_Save", 0.35f, sampleRate, (t, dur) =>
            {
                float progress = t / dur;
                float env = Mathf.Pow(1f - progress, 3f);
                float slap = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(300f, 60f, progress) * t);
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.35f * env;
                return slap * env * 0.7f + noise;
            });

            procPurchase = CreateSyntheticClip("Proc_Purchase", 0.45f, sampleRate, (t, dur) =>
            {
                float f = t < 0.15f ? 1174.66f : 1567.98f;
                float env = Mathf.Clamp01(1f - (t % 0.15f) / 0.15f);
                return Mathf.Sin(2f * Mathf.PI * f * t) * env;
            });

            procUnlock = CreateSyntheticClip("Proc_Unlock", 0.8f, sampleRate, (t, dur) =>
            {
                float step = Mathf.Floor(t / 0.18f);
                int idx = Mathf.Clamp((int)step, 0, UnlockNotes.Length - 1);
                float noteT = t - step * 0.18f;
                float env = Mathf.Clamp01(1f - (noteT / 0.18f));
                return Mathf.Sin(2f * Mathf.PI * UnlockNotes[idx] * t) * env * 0.75f;
            });

            procButtonClick = CreateSyntheticClip("Proc_ButtonClick", 0.055f, sampleRate, (t, dur) =>
            {
                float env = Mathf.Pow(1f - t / dur, 2f);
                return Mathf.Sin(2f * Mathf.PI * 1050f * t) * env * 0.7f;
            });

            procUiConfirm = CreateSyntheticClip("Proc_UiConfirm", 0.16f, sampleRate, (t, dur) =>
            {
                float freq = t < 0.07f ? 784f : 1046.5f;
                float env = Mathf.Clamp01(1f - (t / dur));
                return Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.6f;
            });

            procUiError = CreateSyntheticClip("Proc_UiError", 0.22f, sampleRate, (t, dur) =>
            {
                float freq = t < 0.11f ? 280f : 210f;
                float env = Mathf.Clamp01(1f - t / dur);
                return Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.55f;
            });

            procPostHit = CreateSyntheticClip("Proc_PostHit", 0.4f, sampleRate, (t, dur) =>
            {
                float env = Mathf.Pow(1f - (t / dur), 4f);
                float ping = Mathf.Sin(2f * Mathf.PI * 880f * t) + Mathf.Sin(2f * Mathf.PI * 1320f * t) * 0.6f;
                return ping * env;
            });

            procWhistle = CreateSyntheticClip("Proc_Whistle", 0.42f, sampleRate, (t, dur) =>
            {
                float env = Mathf.Clamp01(t / 0.03f) * Mathf.Clamp01((dur - t) / 0.05f);
                float trill = Mathf.Sin(2f * Mathf.PI * 38f * t) * 110f;
                float s1 = Mathf.Sin(2f * Mathf.PI * (2750f + trill) * t);
                float s2 = Mathf.Sin(2f * Mathf.PI * (2950f + trill) * t);
                return (s1 + s2) * 0.36f * env;
            });

            procMiss = CreateSyntheticClip("Proc_Miss", 0.45f, sampleRate, (t, dur) =>
            {
                float env = Mathf.Pow(1f - (t / dur), 1.8f);
                return Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(210f, 110f, t / dur) * t) * 0.4f * env;
            });

            procFootstep = CreateFilteredNoiseClip("Proc_Footstep", 0.11f, sampleRate, cutoff: 0.14f, (t, dur) =>
                Mathf.Pow(1f - t / dur, 2.2f) * 0.32f);

            procNetSwish = CreateFilteredNoiseClip("Proc_NetSwish", 0.28f, sampleRate, cutoff: 0.24f, (t, dur) =>
                Mathf.Sin(Mathf.Clamp01(t / 0.05f) * Mathf.PI * 0.5f) * Mathf.Pow(1f - t / dur, 1.5f) * 0.45f);

            procTurfLanding = CreateFilteredNoiseClip("Proc_TurfLanding", 0.24f, sampleRate, cutoff: 0.10f, (t, dur) =>
                Mathf.Pow(1f - t / dur, 2.0f) * 0.50f);

            // Warm filtered seamless ambient crowd murmur loop (4.0s)
            procCrowd = CreateFilteredCrowdLoop("Proc_CrowdAmbientLoop", 4.0f, sampleRate, 0.08f, baseChantFreq: 185f);

            // Warm stadium reaction clips
            procCrowdGoalRoar = CreateFilteredNoiseClip("Proc_CrowdGoalRoar", 2.2f, sampleRate, cutoff: 0.13f, (t, dur) =>
                Mathf.Sin(Mathf.Clamp01(t / 0.18f) * Mathf.PI * 0.5f) * Mathf.Pow(1f - t / dur, 0.85f) * 0.72f);

            procCrowdSaveGasp = CreateFilteredNoiseClip("Proc_CrowdSaveGasp", 1.3f, sampleRate, cutoff: 0.16f, (t, dur) =>
                Mathf.Sin(Mathf.Clamp01(t / 0.08f) * Mathf.PI * 0.5f) * Mathf.Pow(1f - t / dur, 1.3f) * 0.58f);

            procCrowdMissGroan = CreateFilteredNoiseClip("Proc_CrowdMissGroan", 1.4f, sampleRate, cutoff: 0.07f, (t, dur) =>
                Mathf.Sin(Mathf.Clamp01(t / 0.12f) * Mathf.PI * 0.5f) * Mathf.Pow(1f - t / dur, 1.1f) * 0.55f);

            procCrowdApplause = CreateFilteredNoiseClip("Proc_CrowdApplause", 1.5f, sampleRate, cutoff: 0.22f, (t, dur) =>
            {
                float clapPulse = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(t * 28f));
                return clapPulse * Mathf.Sin(Mathf.Clamp01(t / 0.1f) * Mathf.PI * 0.5f) * Mathf.Clamp01(1f - t / dur) * 0.52f;
            });

            procBgmMenu = CreateSyntheticLoop("Proc_BgmMenu", 4.0f, sampleRate, (t) =>
            {
                float beat = (t % 0.5f) / 0.5f;
                float kickEnv = Mathf.Pow(Mathf.Clamp01(1f - beat * 3f), 2f);
                float kick = Mathf.Sin(2f * Mathf.PI * 65f * t) * kickEnv * 0.5f;
                float bassNote = ((int)(t / 1.0f) % 4) switch { 0 => 110f, 1 => 130.81f, 2 => 146.83f, _ => 98f };
                float bass = Mathf.Sin(2f * Mathf.PI * bassNote * t) * 0.20f;
                return kick + bass;
            });

            procBgmMatch = CreateSyntheticLoop("Proc_BgmMatch", 4.0f, sampleRate, (t) =>
            {
                float beat = (t % 0.25f) / 0.25f;
                float kick = Mathf.Sin(2f * Mathf.PI * 75f * t) * Mathf.Pow(Mathf.Clamp01(1f - beat * 4f), 2f) * 0.42f;
                float bassNote = ((int)(t / 0.5f) % 4) switch { 0 => 120f, 1 => 140f, 2 => 160f, _ => 100f };
                float synth = Mathf.Sin(2f * Mathf.PI * bassNote * t) * 0.20f;
                return kick + synth;
            });

            procBgmVictory = CreateSyntheticLoop("Proc_BgmVictory", 3.0f, sampleRate, (t) =>
            {
                int idx = (int)(t / 0.75f) % VictoryMelody.Length;
                float noteT = (t % 0.75f);
                float env = Mathf.Clamp01(1f - noteT / 0.75f);
                return Mathf.Sin(2f * Mathf.PI * VictoryMelody[idx] * t) * env * 0.35f;
            });

            procBgmDefeat = CreateSyntheticLoop("Proc_BgmDefeat", 3.0f, sampleRate, (t) =>
            {
                int idx = (int)(t / 0.75f) % DefeatMelody.Length;
                float noteT = (t % 0.75f);
                float env = Mathf.Clamp01(1f - noteT / 0.75f);
                return Mathf.Sin(2f * Mathf.PI * DefeatMelody[idx] * t) * env * 0.30f;
            });
        }

        private AudioClip CreateSyntheticClip(string name, float duration, int sampleRate, Func<float, float, float> sampleFunc)
        {
            int numSamples = Mathf.FloorToInt(duration * sampleRate);
            float[] data = new float[numSamples];
            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                data[i] = Mathf.Clamp(sampleFunc(t, duration), -1f, 1f);
            }
            AudioClip clip = AudioClip.Create(name, numSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateFilteredNoiseClip(string name, float duration, int sampleRate, float cutoff, Func<float, float, float> envFunc)
        {
            int numSamples = Mathf.FloorToInt(duration * sampleRate);
            float[] data = new float[numSamples];
            float lp = 0f;
            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float white = UnityEngine.Random.value * 2f - 1f;
                lp = Mathf.Lerp(lp, white, cutoff);
                data[i] = Mathf.Clamp(lp * envFunc(t, duration) * 2.2f, -1f, 1f);
            }
            AudioClip clip = AudioClip.Create(name, numSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateFilteredCrowdLoop(string name, float duration, int sampleRate, float cutoff, float baseChantFreq)
        {
            int numSamples = Mathf.FloorToInt(duration * sampleRate);
            float[] data = new float[numSamples];
            float lp = 0f;
            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float white = UnityEngine.Random.value * 2f - 1f;
                lp = Mathf.Lerp(lp, white, cutoff);
                float swell = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * (t / duration) * 2f);
                float distantChant = (Mathf.Sin(2f * Mathf.PI * baseChantFreq * t) + Mathf.Sin(2f * Mathf.PI * baseChantFreq * 1.25f * t)) * 0.045f * swell;
                data[i] = Mathf.Clamp(lp * 0.55f * swell + distantChant, -1f, 1f);
            }
            AudioClip clip = AudioClip.Create(name, numSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateSyntheticLoop(string name, float duration, int sampleRate, Func<float, float> sampleFunc)
        {
            int numSamples = Mathf.FloorToInt(duration * sampleRate);
            float[] data = new float[numSamples];
            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                data[i] = Mathf.Clamp(sampleFunc(t), -1f, 1f);
            }
            AudioClip clip = AudioClip.Create(name, numSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
        #endregion
    }
}
