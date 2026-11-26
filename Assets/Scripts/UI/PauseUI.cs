#pragma warning disable 0649
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using UltimatePenaltyCup.Core;
using UltimatePenaltyCup.Audio;
using UltimatePenaltyCup.Gameplay;
using UltimatePenaltyCup.Player;

namespace UltimatePenaltyCup.UI
{
    public class PauseUI : MonoBehaviour
    {
        public static PauseUI Instance { get; private set; }

        [Header("Pause Panel")]
        [SerializeField] private GameObject panelPause;
        [SerializeField] private GameObject groupMainActions;
        [SerializeField] private GameObject groupInlineOptions;

        [Header("Buttons")]
        [SerializeField] private Button btnResume;
        [SerializeField] private Button btnRestart;
        [SerializeField] private Button btnOptions;
        [SerializeField] private Button btnBackFromOptions;
        [SerializeField] private Button btnQuitToMenu;

        [Header("Audio Settings")]
        [SerializeField] private Slider sliderMaster;
        [SerializeField] private Slider sliderMusic;
        [SerializeField] private Slider sliderSfx;
        [SerializeField] private TMP_Text txtMasterVal;
        [SerializeField] private TMP_Text txtMusicVal;
        [SerializeField] private TMP_Text txtSfxVal;

        public bool IsPaused { get; private set; } = false;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            NotificationManager.EnsureInstance();
            if (btnResume != null) btnResume.onClick.AddListener(Resume);
            if (btnRestart != null) btnRestart.onClick.AddListener(RequestRestartConfirmation);
            if (btnOptions != null) btnOptions.onClick.AddListener(() => ShowOptionsSubmenu(true));
            if (btnBackFromOptions != null) btnBackFromOptions.onClick.AddListener(() => ShowOptionsSubmenu(false));
            if (btnQuitToMenu != null) btnQuitToMenu.onClick.AddListener(RequestQuitToMenuConfirmation);

            SyncSliders();

            if (sliderMaster != null) sliderMaster.onValueChanged.AddListener((v) =>
            {
                AudioManager.Instance?.SetMasterVolume(v);
                if (txtMasterVal != null) txtMasterVal.text = $"{Mathf.RoundToInt(v * 100)}%";
            });
            if (sliderMusic != null) sliderMusic.onValueChanged.AddListener((v) =>
            {
                AudioManager.Instance?.SetMusicVolume(v);
                if (txtMusicVal != null) txtMusicVal.text = $"{Mathf.RoundToInt(v * 100)}%";
            });
            if (sliderSfx != null) sliderSfx.onValueChanged.AddListener((v) =>
            {
                AudioManager.Instance?.SetSfxVolume(v);
                if (txtSfxVal != null) txtSfxVal.text = $"{Mathf.RoundToInt(v * 100)}%";
            });

            if (panelPause != null) panelPause.SetActive(false);
            ShowOptionsSubmenu(false, silent: true);
        }

        private void SyncSliders()
        {
            float m = AudioManager.Instance != null ? AudioManager.Instance.MasterVolume : 1.0f;
            float mus = AudioManager.Instance != null ? AudioManager.Instance.MusicVolume : 0.8f;
            float sfx = AudioManager.Instance != null ? AudioManager.Instance.SfxVolume : 0.9f;

            if (sliderMaster != null) sliderMaster.SetValueWithoutNotify(m);
            if (sliderMusic != null) sliderMusic.SetValueWithoutNotify(mus);
            if (sliderSfx != null) sliderSfx.SetValueWithoutNotify(sfx);

            if (txtMasterVal != null) txtMasterVal.text = $"{Mathf.RoundToInt(m * 100)}%";
            if (txtMusicVal != null) txtMusicVal.text = $"{Mathf.RoundToInt(mus * 100)}%";
            if (txtSfxVal != null) txtSfxVal.text = $"{Mathf.RoundToInt(sfx * 100)}%";
        }

        private void ShowOptionsSubmenu(bool showOptions, bool silent = false)
        {
            if (!silent) AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
            if (groupMainActions != null) groupMainActions.SetActive(!showOptions);
            if (groupInlineOptions != null) groupInlineOptions.SetActive(showOptions);
            if (showOptions) SyncSliders();
        }

        private void Update()
        {
            if (PenaltyManager.Instance != null && PenaltyManager.Instance.IsRoundOver) return;

            bool escapePressed = Keyboard.current != null
                ? Keyboard.current.escapeKey.wasPressedThisFrame
                : Input.GetKeyDown(KeyCode.Escape);

            if (escapePressed)
            {
                TogglePause();
            }
        }

        public void TogglePause()
        {
            if (IsPaused) Resume();
            else Pause();
        }

        public void Pause()
        {
            if (PenaltyManager.Instance != null && PenaltyManager.Instance.IsRoundOver) return;

            IsPaused = true;
            Time.timeScale = 0f;
            PlayerShooter.Instance?.SetInputLocked(true);
            ShowOptionsSubmenu(false, silent: true);
            if (panelPause != null) panelPause.SetActive(true);
            GameStateManager.Instance?.ChangeState(GameState.Paused);
            AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
        }

        public void Resume()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            if (panelPause != null) panelPause.SetActive(false);

            if (PenaltyManager.Instance != null && !PenaltyManager.Instance.IsRoundOver && !PenaltyManager.Instance.IsEvaluatingShot &&
                PenaltyManager.Instance.CurrentState == MatchAttemptState.Aiming)
            {
                PlayerShooter.Instance?.ResetShooter();
            }

            GameStateManager.Instance?.ChangeState(GameState.Match);
            AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
        }

        public void RequestRestartConfirmation()
        {
            NotificationManager.EnsureInstance().ShowConfirmModal(
                "REINICIAR RONDA",
                "¿Deseas reiniciar la ronda actual desde el primer penal?",
                "REINICIAR",
                "CANCELAR",
                onConfirm: RestartMatch,
                isDestructive: false);
        }

        public void RequestQuitToMenuConfirmation()
        {
            NotificationManager.EnsureInstance().ShowConfirmModal(
                "VOLVER AL MENÚ",
                "Se perderá el avance de esta ronda. ¿Deseas volver al Menú Principal?",
                "SALIR AL MENÚ",
                "CONTINUAR",
                onConfirm: QuitToMenu,
                isDestructive: true);
        }

        public void RestartMatch()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
            int currentRound = GameManager.Instance != null ? GameManager.Instance.CurrentSelectedRoundNumber : 1;
            GameManager.Instance?.SelectRoundAndStartMatch(currentRound);
        }

        public void QuitToMenu()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
            GameManager.Instance?.LoadMainMenuScene();
        }
    }
}
