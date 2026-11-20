#pragma warning disable 0649
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UltimatePenaltyCup.Core;
using UltimatePenaltyCup.Audio;

namespace UltimatePenaltyCup.UI
{
    public class PauseUI : MonoBehaviour
    {
        [Header("Pause Panel")]
        [SerializeField] private GameObject panelPause;

        [Header("Buttons")]
        [SerializeField] private Button btnResume;
        [SerializeField] private Button btnRestart;
        [SerializeField] private Button btnQuitToMenu;

        [Header("Audio Settings")]
        [SerializeField] private Slider sliderMusic;
        [SerializeField] private Slider sliderSfx;

        public bool IsPaused { get; private set; } = false;

        private void Start()
        {
            if (btnResume != null) btnResume.onClick.AddListener(Resume);
            if (btnRestart != null) btnRestart.onClick.AddListener(RestartMatch);
            if (btnQuitToMenu != null) btnQuitToMenu.onClick.AddListener(QuitToMenu);

            if (sliderMusic != null)
            {
                sliderMusic.value = AudioManager.Instance != null ? AudioManager.Instance.MusicVolume : 0.8f;
                sliderMusic.onValueChanged.AddListener((v) => AudioManager.Instance?.SetMusicVolume(v));
            }

            if (sliderSfx != null)
            {
                sliderSfx.value = AudioManager.Instance != null ? AudioManager.Instance.SfxVolume : 0.9f;
                sliderSfx.onValueChanged.AddListener((v) => AudioManager.Instance?.SetSfxVolume(v));
            }

            if (panelPause != null) panelPause.SetActive(false);
        }

        private void Update()
        {
            bool escapePressed = false;
            if (Keyboard.current != null)
            {
                escapePressed = Keyboard.current.escapeKey.wasPressedThisFrame;
            }
            else
            {
                escapePressed = Input.GetKeyDown(KeyCode.Escape);
            }

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
            IsPaused = true;
            Time.timeScale = 0f;
            if (panelPause != null) panelPause.SetActive(true);
            GameStateManager.Instance?.ChangeState(GameState.Paused);
            AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
        }

        public void Resume()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            if (panelPause != null) panelPause.SetActive(false);
            GameStateManager.Instance?.ChangeState(GameState.Match);
            AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
        }

        public void RestartMatch()
        {
            Time.timeScale = 1f;
            AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
            int currentRound = GameManager.Instance != null ? GameManager.Instance.CurrentSelectedRoundNumber : 1;
            GameManager.Instance?.SelectRoundAndStartMatch(currentRound);
        }

        public void QuitToMenu()
        {
            Time.timeScale = 1f;
            AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
            GameManager.Instance?.LoadMainMenuScene();
        }
    }
}
