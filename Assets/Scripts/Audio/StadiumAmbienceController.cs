using UnityEngine;
using UltimatePenaltyCup.Core;
using UltimatePenaltyCup.Economy;
using UltimatePenaltyCup.Gameplay;
using UltimatePenaltyCup.Player;

namespace UltimatePenaltyCup.Audio
{
    /// <summary>
    /// Localized 3D stadium ambience & dynamic crowd reaction emitter array positioned across the stands.
    /// </summary>
    public class StadiumAmbienceController : MonoBehaviour
    {
        public static StadiumAmbienceController Instance { get; private set; }

        public int EmitterCount => standSources != null ? standSources.Length : 0;
        public float CurrentAmbientIntensity { get; private set; } = 0.25f;
        public int ReactionAudioTriggerCount { get; private set; } = 0;

        private AudioSource[] standSources;
        private StadiumController stadiumController;
        private bool subscribed = false;
        private float targetExcitementBoost = 0f;
        private float currentExcitementBoost = 0f;

        private static readonly Vector3[] StandEmitterOffsets =
        {
            new Vector3(-16f, 4.5f,  16f), // North-West Stand
            new Vector3( 16f, 4.5f,  16f), // North-East Stand
            new Vector3(-36f, 4.5f, -12f), // West Stand
            new Vector3( 36f, 4.5f, -12f)  // East Stand
        };

        private void Awake()
        {
            Instance = this;
            stadiumController = GetComponent<StadiumController>();
            EnsureLocalizedEmitters();
        }

        private void Start()
        {
            TrySubscribeEvents();
            ApplyVolumeAndVenueLevel();
            StartAmbienceLoops();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void EnsureLocalizedEmitters()
        {
            if (standSources != null && standSources.Length == StandEmitterOffsets.Length) return;
            standSources = new AudioSource[StandEmitterOffsets.Length];

            AudioClip loopClip = AudioManager.Instance != null ? AudioManager.Instance.GetClip(SfxType.CrowdAmbient) : null;
            for (int i = 0; i < StandEmitterOffsets.Length; i++)
            {
                Transform existing = transform.Find($"CrowdAudioSector_{i}");
                GameObject go = existing != null ? existing.gameObject : new GameObject($"CrowdAudioSector_{i}");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = StandEmitterOffsets[i];

                AudioSource src = go.GetComponent<AudioSource>();
                if (src == null) src = go.AddComponent<AudioSource>();
                src.clip = loopClip;
                src.loop = true;
                src.playOnAwake = false;
                src.spatialBlend = 0.85f;
                src.minDistance = 12f;
                src.maxDistance = 85f;
                src.rolloffMode = AudioRolloffMode.Logarithmic;
                src.pitch = 0.95f + i * 0.03f;
                src.priority = 160;
                standSources[i] = src;
            }
        }

        private void StartAmbienceLoops()
        {
            if (standSources == null || AudioManager.Instance == null) return;
            AudioClip loopClip = AudioManager.Instance.GetClip(SfxType.CrowdAmbient);
            if (loopClip == null) return;

            for (int i = 0; i < standSources.Length; i++)
            {
                var src = standSources[i];
                if (src == null) continue;
                src.clip = loopClip;
                if (!src.isPlaying)
                {
                    // Stagger playback phase across sectors so emitters don't phase-lock identically
                    src.time = (loopClip.length * i) / standSources.Length;
                    src.Play();
                }
            }
        }

        private void TrySubscribeEvents()
        {
            if (subscribed) return;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.OnVolumeSettingsChanged += HandleVolumeChanged;
            }
            if (PenaltyManager.Instance != null)
            {
                PenaltyManager.Instance.OnAttemptStarted += HandleAttemptStarted;
                PenaltyManager.Instance.OnAttemptFinished += HandleAttemptFinished;
                PenaltyManager.Instance.OnRoundEndedDetailed += HandleRoundEnded;
                subscribed = true;
            }
            if (PlayerShooter.Instance != null)
            {
                PlayerShooter.Instance.OnShotExecuted += HandleShotFired;
            }
        }

        private void UnsubscribeEvents()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.OnVolumeSettingsChanged -= HandleVolumeChanged;
            }
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
            subscribed = false;
        }

        private void HandleVolumeChanged() => ApplyVolumeAndVenueLevel();

        private void HandleAttemptStarted(int attempt, int max)
        {
            targetExcitementBoost = 0.06f;
        }

        private void HandleShotFired(Vector3 target, float power, bool isHighPower)
        {
            targetExcitementBoost = 0.25f;
        }

        private void HandleAttemptFinished(ShotResult result, ShotScoreResult score, string msg)
        {
            switch (result)
            {
                case ShotResult.GoalNormal:
                case ShotResult.GoalCorner:
                case ShotResult.GoalHighPower:
                    PlayLocalizedReaction(SfxType.CrowdGoalRoar, 0.55f);
                    break;
                case ShotResult.Saved:
                    PlayLocalizedReaction(SfxType.CrowdSaveGasp, 0.40f);
                    PlayLocalizedReaction(SfxType.CrowdApplause, 0.25f, sectorStart: 1);
                    break;
                default:
                    PlayLocalizedReaction(SfxType.CrowdMissGroan, 0.22f);
                    break;
            }
        }

        private void HandleRoundEnded(RoundEndSummary summary)
        {
            if (summary.isVictory)
            {
                PlayLocalizedReaction(summary.isGrandFinal ? SfxType.CrowdGoalRoar : SfxType.CrowdApplause, 0.65f);
            }
        }

        public void PlayLocalizedReaction(SfxType type, float excitementBoost, int sectorStart = 0)
        {
            if (AudioManager.Instance == null || standSources == null) return;
            AudioClip clip = AudioManager.Instance.GetClip(type);
            if (clip == null) return;

            ReactionAudioTriggerCount++;
            targetExcitementBoost = Mathf.Max(targetExcitementBoost, excitementBoost);
            float vol = Mathf.Clamp01(AudioManager.Instance.CrowdVolume * (0.55f + excitementBoost * 0.5f));

            // Play slightly varied reactions across 2 stand sectors rather than identical synchronous mono burst
            int count = 0;
            for (int i = sectorStart; i < standSources.Length && count < 2; i++)
            {
                var src = standSources[i];
                if (src != null)
                {
                    src.pitch = UnityEngine.Random.Range(0.95f, 1.06f);
                    src.PlayOneShot(clip, vol);
                    count++;
                }
            }
        }

        private void Update()
        {
            if (!subscribed && PenaltyManager.Instance != null)
            {
                TrySubscribeEvents();
            }

            targetExcitementBoost = Mathf.Max(0f, targetExcitementBoost - Time.deltaTime * 0.22f);
            currentExcitementBoost = Mathf.Lerp(currentExcitementBoost, targetExcitementBoost, Time.deltaTime * 5f);
            ApplyVolumeAndVenueLevel();
        }

        public void ApplyVolumeAndVenueLevel()
        {
            StadiumType venue = stadiumController != null ? stadiumController.ActiveVenue : StadiumType.Professional;
            float baseVenueFactor = venue switch
            {
                StadiumType.Cantera      => 0.16f,
                StadiumType.Night        => 0.26f,
                StadiumType.Professional => 0.34f,
                StadiumType.WorldCup     => 0.44f,
                _ => 0.24f
            };
            CurrentAmbientIntensity = Mathf.Clamp01(baseVenueFactor + currentExcitementBoost);

            float crowdVol = AudioManager.Instance != null ? AudioManager.Instance.CrowdVolume : 0.85f;
            float finalVol = (Time.timeScale <= 0f ? 0.12f : 1f) * CurrentAmbientIntensity * crowdVol;

            if (standSources != null)
            {
                for (int i = 0; i < standSources.Length; i++)
                {
                    if (standSources[i] != null) standSources[i].volume = finalVol;
                }
            }
        }
    }
}
