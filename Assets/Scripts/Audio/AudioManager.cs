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
        CrowdAmbient
    }

    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;

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

        private AudioClip procKick;
        private AudioClip procGoal;
        private AudioClip procSave;
        private AudioClip procPurchase;
        private AudioClip procUnlock;
        private AudioClip procButtonClick;
        private AudioClip procPostHit;
        private AudioClip procWhistle;
        private AudioClip procMiss;
        private AudioClip procCrowd;
        private AudioClip procBgmMenu;
        private AudioClip procBgmMatch;
        private AudioClip procBgmVictory;
        private AudioClip procBgmDefeat;

        public float MasterVolume { get; private set; } = 1.0f;
        public float MusicVolume { get; private set; } = 0.8f;
        public float SfxVolume { get; private set; } = 0.9f;

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
        }

        private void LoadVolumeSettings()
        {
            if (SaveManager.Instance != null && SaveManager.Instance.CurrentData != null)
            {
                MasterVolume = Mathf.Clamp01(SaveManager.Instance.CurrentData.masterVolume);
                AudioListener.volume = MasterVolume;
                MusicVolume = Mathf.Clamp01(SaveManager.Instance.CurrentData.musicVolume);
                SfxVolume = Mathf.Clamp01(SaveManager.Instance.CurrentData.sfxVolume);
                if (bgmSource != null) bgmSource.volume = MusicVolume;
                if (sfxSource != null) sfxSource.volume = SfxVolume;
            }
            else
            {
                SetMasterVolume(1.0f);
                SetMusicVolume(0.8f);
                SetSfxVolume(0.9f);
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
        }

        public void PlayBgm(BgmType type)
        {
            AudioClip clip = null;
            switch (type)
            {
                case BgmType.M01_MainMenu:
                    clip = bgmMenu != null ? bgmMenu : procBgmMenu;
                    break;
                case BgmType.M02_Match:
                    clip = bgmMatch != null ? bgmMatch : procBgmMatch;
                    break;
                case BgmType.M03_Victory:
                    clip = bgmVictory != null ? bgmVictory : procBgmVictory;
                    break;
                case BgmType.M04_Defeat:
                    clip = bgmDefeat != null ? bgmDefeat : procBgmDefeat;
                    break;
            }

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
            AudioClip clip = null;
            float pitch = 1.0f;

            switch (type)
            {
                case SfxType.S01_Kick:
                    clip = sfxKick != null ? sfxKick : procKick;
                    pitch = UnityEngine.Random.Range(0.92f, 1.08f);
                    break;
                case SfxType.S02_Goal:
                    clip = sfxGoal != null ? sfxGoal : procGoal;
                    break;
                case SfxType.S03_Save:
                    clip = sfxSave != null ? sfxSave : procSave;
                    pitch = UnityEngine.Random.Range(0.95f, 1.05f);
                    break;
                case SfxType.S04_Purchase:
                    clip = sfxPurchase != null ? sfxPurchase : procPurchase;
                    break;
                case SfxType.S05_Unlock:
                    clip = sfxUnlock != null ? sfxUnlock : procUnlock;
                    break;
                case SfxType.S06_ButtonClick:
                    clip = sfxButtonClick != null ? sfxButtonClick : procButtonClick;
                    pitch = UnityEngine.Random.Range(0.98f, 1.02f);
                    break;
                case SfxType.PostHit:
                    clip = sfxPostHit != null ? sfxPostHit : procPostHit;
                    pitch = UnityEngine.Random.Range(0.9f, 1.1f);
                    break;
                case SfxType.Whistle:
                    clip = sfxWhistle != null ? sfxWhistle : procWhistle;
                    break;
                case SfxType.Miss:
                    clip = sfxMiss != null ? sfxMiss : procMiss;
                    break;
                case SfxType.CrowdAmbient:
                    clip = sfxCrowd != null ? sfxCrowd : procCrowd;
                    break;
            }

            if (clip != null && sfxSource != null)
            {
                sfxSource.pitch = pitch;
                sfxSource.PlayOneShot(clip, SfxVolume);
            }
        }

        #region Procedural Audio Synthesis
        private void GenerateProceduralAudioClips()
        {
            const int sampleRate = 22050;

            // 1. Kick: Low pitch punch
            procKick = CreateSyntheticClip("Proc_Kick", 0.28f, sampleRate, (t, dur) =>
            {
                float progress = t / dur;
                float freq = Mathf.Lerp(160f, 40f, progress);
                float envelope = Mathf.Pow(1f - progress, 2.5f);
                return Mathf.Sin(2f * Mathf.PI * freq * t) * envelope;
            });

            // 2. Goal: Cheerful chime / applause roar synthesis
            procGoal = CreateSyntheticClip("Proc_Goal", 1.2f, sampleRate, (t, dur) =>
            {
                float env = Mathf.Sin(Mathf.Clamp01(t / 0.1f) * Mathf.PI * 0.5f) * Mathf.Clamp01((dur - t) / dur);
                float chord = Mathf.Sin(2f * Mathf.PI * 523.25f * t) // C5
                            + Mathf.Sin(2f * Mathf.PI * 659.25f * t) // E5
                            + Mathf.Sin(2f * Mathf.PI * 783.99f * t) // G5
                            + Mathf.Sin(2f * Mathf.PI * 1046.50f * t); // C6
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.25f;
                return (chord * 0.25f + noise) * env;
            });

            // 3. Save: Thud deflection
            procSave = CreateSyntheticClip("Proc_Save", 0.35f, sampleRate, (t, dur) =>
            {
                float progress = t / dur;
                float env = Mathf.Pow(1f - progress, 3f);
                float slap = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(300f, 60f, progress) * t);
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.4f * env;
                return slap * env * 0.7f + noise;
            });

            // 4. Purchase: Cash chime
            procPurchase = CreateSyntheticClip("Proc_Purchase", 0.45f, sampleRate, (t, dur) =>
            {
                float f = t < 0.15f ? 1174.66f : 1567.98f; // D6 -> G6
                float env = Mathf.Clamp01(1f - (t % 0.15f) / 0.15f);
                return Mathf.Sin(2f * Mathf.PI * f * t) * env;
            });

            // 5. Unlock: Fanfare Arpeggio
            procUnlock = CreateSyntheticClip("Proc_Unlock", 0.8f, sampleRate, (t, dur) =>
            {
                float step = Mathf.Floor(t / 0.18f);
                float[] notes = { 440f, 554.37f, 659.25f, 880f }; // A4, C#5, E5, A5
                int idx = Mathf.Clamp((int)step, 0, notes.Length - 1);
                float noteT = t - step * 0.18f;
                float env = Mathf.Clamp01(1f - (noteT / 0.18f));
                return Mathf.Sin(2f * Mathf.PI * notes[idx] * t) * env * 0.8f;
            });

            // 6. Button Click: Subtle high tick
            procButtonClick = CreateSyntheticClip("Proc_ButtonClick", 0.06f, sampleRate, (t, dur) =>
            {
                float env = Mathf.Clamp01(1f - t / dur);
                return Mathf.Sin(2f * Mathf.PI * 1100f * t) * env;
            });

            // 7. Post Hit: Metallic ping
            procPostHit = CreateSyntheticClip("Proc_PostHit", 0.4f, sampleRate, (t, dur) =>
            {
                float env = Mathf.Pow(1f - (t / dur), 4f);
                float ping = Mathf.Sin(2f * Mathf.PI * 880f * t) + Mathf.Sin(2f * Mathf.PI * 1320f * t) * 0.6f;
                return ping * env;
            });

            // 7b. Referee Whistle: Dual-tone pea trill
            procWhistle = CreateSyntheticClip("Proc_Whistle", 0.42f, sampleRate, (t, dur) =>
            {
                float env = Mathf.Clamp01(t / 0.03f) * Mathf.Clamp01((dur - t) / 0.05f);
                float trill = Mathf.Sin(2f * Mathf.PI * 38f * t) * 110f;
                float s1 = Mathf.Sin(2f * Mathf.PI * (2750f + trill) * t);
                float s2 = Mathf.Sin(2f * Mathf.PI * (2950f + trill) * t);
                return (s1 + s2) * 0.42f * env;
            });

            // 7c. Miss: Soft whoosh + disappointed crowd sigh
            procMiss = CreateSyntheticClip("Proc_Miss", 0.5f, sampleRate, (t, dur) =>
            {
                float env = Mathf.Pow(1f - (t / dur), 1.8f);
                float low = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(210f, 110f, t / dur) * t) * 0.45f;
                float air = (UnityEngine.Random.value * 2f - 1f) * 0.25f;
                return (low + air) * env;
            });

            // 7d. Crowd Ambient Burst
            procCrowd = CreateSyntheticClip("Proc_Crowd", 1.1f, sampleRate, (t, dur) =>
            {
                float env = Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI);
                float roar = (UnityEngine.Random.value * 2f - 1f) * 0.22f;
                return roar * env;
            });

            // 8. BGM Menu: Uplifting sports loop
            procBgmMenu = CreateSyntheticLoop("Proc_BgmMenu", 4.0f, sampleRate, (t) =>
            {
                float beat = (t % 0.5f) / 0.5f;
                float kickEnv = Mathf.Pow(Mathf.Clamp01(1f - beat * 3f), 2f);
                float kick = Mathf.Sin(2f * Mathf.PI * 65f * t) * kickEnv * 0.6f;
                float bassNote = ((int)(t / 1.0f) % 4) switch { 0 => 110f, 1 => 130.81f, 2 => 146.83f, _ => 98f };
                float bass = Mathf.Sin(2f * Mathf.PI * bassNote * t) * 0.25f;
                return kick + bass;
            });

            // 9. BGM Match: Dynamic sports beat
            procBgmMatch = CreateSyntheticLoop("Proc_BgmMatch", 4.0f, sampleRate, (t) =>
            {
                float beat = (t % 0.25f) / 0.25f;
                float kick = Mathf.Sin(2f * Mathf.PI * 75f * t) * Mathf.Pow(Mathf.Clamp01(1f - beat * 4f), 2f) * 0.5f;
                float hihat = ((t % 0.125f) < 0.03f ? (UnityEngine.Random.value * 2f - 1f) * 0.12f : 0f);
                float bassNote = ((int)(t / 0.5f) % 4) switch { 0 => 120f, 1 => 140f, 2 => 160f, _ => 100f };
                float synth = Mathf.Sin(2f * Mathf.PI * bassNote * t) * 0.25f;
                return kick + hihat + synth;
            });

            // 10. BGM Victory: Fanfare loop
            procBgmVictory = CreateSyntheticLoop("Proc_BgmVictory", 3.0f, sampleRate, (t) =>
            {
                float[] melody = { 523.25f, 659.25f, 783.99f, 1046.50f };
                int idx = (int)(t / 0.75f) % melody.Length;
                float noteT = (t % 0.75f);
                float env = Mathf.Clamp01(1f - noteT / 0.75f);
                return Mathf.Sin(2f * Mathf.PI * melody[idx] * t) * env * 0.4f;
            });

            // 11. BGM Defeat: Minor cadence
            procBgmDefeat = CreateSyntheticLoop("Proc_BgmDefeat", 3.0f, sampleRate, (t) =>
            {
                float[] melody = { 440f, 392f, 349.23f, 329.63f };
                int idx = (int)(t / 0.75f) % melody.Length;
                float noteT = (t % 0.75f);
                float env = Mathf.Clamp01(1f - noteT / 0.75f);
                return Mathf.Sin(2f * Mathf.PI * melody[idx] * t) * env * 0.35f;
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
