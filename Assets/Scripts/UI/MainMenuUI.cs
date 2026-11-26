#pragma warning disable 0649
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UltimatePenaltyCup.Core;
using UltimatePenaltyCup.Economy;
using UltimatePenaltyCup.Audio;
using UltimatePenaltyCup.SaveSystem;

namespace UltimatePenaltyCup.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Screens / Panels")]
        [SerializeField] private GameObject panelMainMenu;
        [SerializeField] private GameObject panelRoundSelect;
        [SerializeField] private GameObject panelShop;
        [SerializeField] private GameObject panelCustomization;
        [SerializeField] private GameObject panelAchievements;
        [SerializeField] private GameObject panelSettings;

        [Header("Main Menu Buttons")]
        [SerializeField] private Button btnPlay;
        [SerializeField] private Button btnShop;
        [SerializeField] private Button btnCustomization;
        [SerializeField] private Button btnAchievements;
        [SerializeField] private Button btnSettings;
        [SerializeField] private Button btnQuit;

        [Header("Header Info")]
        [SerializeField] private TMP_Text txtCoins;
        [SerializeField] private TMP_Text txtHighScore;

        [Header("Secret Code (Test)")]
        [SerializeField] private TMP_InputField inputSecretCode;
        [SerializeField] private Button btnSubmitCode;
        [SerializeField] private TMP_Text txtCodeFeedback;

        private int displayedCoins = -1;
        private Coroutine coinCountCoroutine;

        private void Start()
        {
            Time.timeScale = 1f;
            NotificationManager.EnsureInstance();
            SetupButtons();
            UpdateCurrencyUI(instant: true);

            if (GameManager.Instance != null && GameManager.Instance.RequestOpenAchievementsOnMenu)
            {
                GameManager.Instance.RequestOpenAchievementsOnMenu = false;
                ShowPanel(panelAchievements);
            }
            else
            {
                ShowPanel(panelMainMenu);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayBgm(BgmType.M01_MainMenu);
            }

            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnCoinsChanged += HandleCoinsChanged;
                CurrencyManager.Instance.OnCodeAttempt += HandleCodeAttemptFeedback;
            }
        }

        private void OnDestroy()
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnCoinsChanged -= HandleCoinsChanged;
                CurrencyManager.Instance.OnCodeAttempt -= HandleCodeAttemptFeedback;
            }
        }

        private void HandleCoinsChanged(int coins, int delta)
        {
            UpdateCurrencyUI();
        }

        private void SetupButtons()
        {
            if (btnPlay != null) btnPlay.onClick.AddListener(() => OpenPanel(panelRoundSelect));
            if (btnShop != null) btnShop.onClick.AddListener(() => OpenPanel(panelShop));
            if (btnCustomization != null) btnCustomization.onClick.AddListener(() => OpenPanel(panelCustomization));
            if (btnAchievements != null) btnAchievements.onClick.AddListener(() => OpenPanel(panelAchievements));
            if (btnSettings != null) btnSettings.onClick.AddListener(() => OpenPanel(panelSettings));
            if (btnQuit != null) btnQuit.onClick.AddListener(RequestQuitConfirmation);

            if (btnSubmitCode != null && inputSecretCode != null)
            {
                btnSubmitCode.onClick.AddListener(OnSubmitSecretCode);
            }
        }

        public void OpenPanel(GameObject targetPanel)
        {
            AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
            ShowPanel(targetPanel);
        }

        public void ReturnToMainMenu()
        {
            AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
            ShowPanel(panelMainMenu);
            UpdateCurrencyUI();
        }

        private void ShowPanel(GameObject activePanel)
        {
            if (panelMainMenu != null) panelMainMenu.SetActive(panelMainMenu == activePanel);
            if (panelRoundSelect != null) panelRoundSelect.SetActive(panelRoundSelect == activePanel);
            if (panelShop != null) panelShop.SetActive(panelShop == activePanel);
            if (panelCustomization != null) panelCustomization.SetActive(panelCustomization == activePanel);
            if (panelAchievements != null) panelAchievements.SetActive(panelAchievements == activePanel);
            if (panelSettings != null) panelSettings.SetActive(panelSettings == activePanel);
        }

        public void UpdateCurrencyUI() => UpdateCurrencyUI(instant: false);

        private void UpdateCurrencyUI(bool instant)
        {
            if (txtCoins != null)
            {
                int targetCoins = CurrencyManager.Instance != null ? CurrencyManager.Instance.Coins : 0;
                if (instant || displayedCoins < 0 || !gameObject.activeInHierarchy)
                {
                    displayedCoins = targetCoins;
                    txtCoins.text = $"{targetCoins:N0}";
                }
                else if (displayedCoins != targetCoins)
                {
                    if (coinCountCoroutine != null) StopCoroutine(coinCountCoroutine);
                    coinCountCoroutine = StartCoroutine(AnimateCoinsRoutine(displayedCoins, targetCoins));
                }
            }

            if (txtHighScore != null && SaveManager.Instance != null && SaveManager.Instance.CurrentData != null)
            {
                txtHighScore.text = $"Récord: {SaveManager.Instance.CurrentData.highScore:N0} pts";
            }
        }

        private System.Collections.IEnumerator AnimateCoinsRoutine(int fromVal, int toVal)
        {
            float elapsed = 0f;
            const float dur = 0.45f;
            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                float u = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / dur), 3f);
                displayedCoins = Mathf.RoundToInt(Mathf.Lerp(fromVal, toVal, u));
                if (txtCoins != null) txtCoins.text = $"{displayedCoins:N0}";
                yield return null;
            }
            displayedCoins = toVal;
            if (txtCoins != null) txtCoins.text = $"{toVal:N0}";
        }

        private void OnSubmitSecretCode()
        {
            if (inputSecretCode == null) return;
            string code = inputSecretCode.text;
            AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
            CurrencyManager.Instance?.TryRedeemCode(code);
            inputSecretCode.text = string.Empty;
        }

        private void HandleCodeAttemptFeedback(string message, bool success)
        {
            NotificationManager.EnsureInstance().ShowToast(
                success ? "CÓDIGO CANJEADO" : "CÓDIGO INVÁLIDO",
                message,
                success ? NotificationKind.Reward : NotificationKind.Warning);

            if (txtCodeFeedback != null)
            {
                txtCodeFeedback.gameObject.SetActive(true);
                txtCodeFeedback.text = message;
                txtCodeFeedback.color = success ? new Color(0.14f, 0.80f, 0.45f) : new Color(0.90f, 0.26f, 0.28f);
                CancelInvoke(nameof(HideCodeFeedback));
                Invoke(nameof(HideCodeFeedback), 3.0f);
            }
        }

        private void HideCodeFeedback()
        {
            if (txtCodeFeedback != null) txtCodeFeedback.gameObject.SetActive(false);
        }

        public void RequestQuitConfirmation()
        {
            AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
            NotificationManager.EnsureInstance().ShowConfirmModal(
                "SALIR DEL JUEGO",
                "¿Deseas cerrar UltimatePenaltyCup3D? Tu progreso está guardado.",
                "SALIR",
                "CANCELAR",
                onConfirm: () => GameManager.Instance?.QuitGame(),
                isDestructive: true);
        }

        private void OnQuitClicked()
        {
            AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
            GameManager.Instance?.QuitGame();
        }
    }
}
