#pragma warning disable 0649
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UltimatePenaltyCup.Audio;
using UltimatePenaltyCup.Economy;
using UltimatePenaltyCup.SaveSystem;

namespace UltimatePenaltyCup.UI
{
    public class SettingsUI : MonoBehaviour
    {
        [Header("Audio Sliders")]
        [SerializeField] private Slider sliderMaster;
        [SerializeField] private Slider sliderMusic;
        [SerializeField] private Slider sliderSfx;
        [SerializeField] private Slider sliderCrowd;
        [SerializeField] private Slider sliderUi;
        [SerializeField] private TMP_Text txtMasterVal;
        [SerializeField] private TMP_Text txtMusicVal;
        [SerializeField] private TMP_Text txtSfxVal;
        [SerializeField] private TMP_Text txtCrowdVal;
        [SerializeField] private TMP_Text txtUiVal;

        [Header("Gameplay Sensitivity")]
        [SerializeField] private Slider sliderSensitivity;
        [SerializeField] private TMP_Text txtSensitivityVal;

        [Header("Display & Graphics")]
        [SerializeField] private Button btnResPrev;
        [SerializeField] private Button btnResNext;
        [SerializeField] private TMP_Text txtResolutionVal;
        [SerializeField] private Button btnToggleFullscreen;
        [SerializeField] private TMP_Text txtFullscreenVal;
        [SerializeField] private Button btnQualPrev;
        [SerializeField] private Button btnQualNext;
        [SerializeField] private TMP_Text txtQualityVal;

        [Header("Actions")]
        [SerializeField] private Button btnResetDefaults;
        [SerializeField] private Button btnBack;
        [SerializeField] private MainMenuUI mainMenuUI;

        [Header("Secret Code")]
        [SerializeField] private TMP_InputField inputSecretCode;
        [SerializeField] private Button btnRedeemCode;
        [SerializeField] private TMP_Text txtCodeMessage;

        private readonly List<Resolution> uniqueResolutions = new List<Resolution>();
        private int currentResIndex = 0;
        private bool isFullscreen = true;
        private int qualityIndex = 2;

        private void Awake()
        {
            BuildResolutionList();
        }

        private void OnEnable()
        {
            LoadFromSave();
        }

        private void Start()
        {
            LoadFromSave();

            if (sliderMaster != null) sliderMaster.onValueChanged.AddListener(OnMasterChanged);
            if (sliderMusic != null) sliderMusic.onValueChanged.AddListener(OnMusicChanged);
            if (sliderSfx != null) sliderSfx.onValueChanged.AddListener(OnSfxChanged);
            if (sliderCrowd != null) sliderCrowd.onValueChanged.AddListener(OnCrowdChanged);
            if (sliderUi != null) sliderUi.onValueChanged.AddListener(OnUiChanged);
            if (sliderSensitivity != null) sliderSensitivity.onValueChanged.AddListener(OnSensitivityChanged);

            if (btnResPrev != null) btnResPrev.onClick.AddListener(() => StepResolution(-1));
            if (btnResNext != null) btnResNext.onClick.AddListener(() => StepResolution(1));
            if (btnToggleFullscreen != null) btnToggleFullscreen.onClick.AddListener(ToggleFullscreen);
            if (btnQualPrev != null) btnQualPrev.onClick.AddListener(() => StepQuality(-1));
            if (btnQualNext != null) btnQualNext.onClick.AddListener(() => StepQuality(1));
            if (btnResetDefaults != null) btnResetDefaults.onClick.AddListener(RequestResetDefaultsConfirmation);

            if (btnBack != null)
            {
                btnBack.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
                    mainMenuUI?.ReturnToMainMenu();
                });
            }

            if (btnRedeemCode != null && inputSecretCode != null)
            {
                btnRedeemCode.onClick.AddListener(OnRedeemClicked);
            }

            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnCodeAttempt += HandleCodeResult;
            }
        }

        private void OnDestroy()
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnCodeAttempt -= HandleCodeResult;
            }
        }

        private void BuildResolutionList()
        {
            uniqueResolutions.Clear();
            var seen = new HashSet<string>();
            foreach (var res in Screen.resolutions)
            {
                if (res.width < 1024 || res.height < 600) continue;
                string key = $"{res.width}x{res.height}";
                if (seen.Add(key)) uniqueResolutions.Add(res);
            }
            if (uniqueResolutions.Count == 0)
            {
                uniqueResolutions.Add(new Resolution { width = 1280, height = 720 });
                uniqueResolutions.Add(new Resolution { width = 1920, height = 1080 });
            }
        }

        private void LoadFromSave()
        {
            var data = SaveManager.Instance != null ? SaveManager.Instance.CurrentData : null;

            float master = AudioManager.Instance != null ? AudioManager.Instance.MasterVolume : (data != null ? data.masterVolume : 1f);
            float music  = AudioManager.Instance != null ? AudioManager.Instance.MusicVolume  : (data != null ? data.musicVolume  : 0.8f);
            float sfx    = AudioManager.Instance != null ? AudioManager.Instance.SfxVolume    : (data != null ? data.sfxVolume    : 0.9f);
            float crowd  = AudioManager.Instance != null ? AudioManager.Instance.CrowdVolume  : (data != null ? data.crowdVolume  : 0.85f);
            float uiVol  = AudioManager.Instance != null ? AudioManager.Instance.UiVolume     : (data != null ? data.uiVolume     : 0.90f);
            float sens   = data != null ? Mathf.Clamp(data.aimSensitivity, 0.5f, 2.0f) : 1.0f;

            if (sliderMaster != null) sliderMaster.SetValueWithoutNotify(master);
            if (sliderMusic != null) sliderMusic.SetValueWithoutNotify(music);
            if (sliderSfx != null) sliderSfx.SetValueWithoutNotify(sfx);
            if (sliderCrowd != null) sliderCrowd.SetValueWithoutNotify(crowd);
            if (sliderUi != null) sliderUi.SetValueWithoutNotify(uiVol);
            if (sliderSensitivity != null) sliderSensitivity.SetValueWithoutNotify(sens);

            UpdatePercentLabel(txtMasterVal, master);
            UpdatePercentLabel(txtMusicVal, music);
            UpdatePercentLabel(txtSfxVal, sfx);
            UpdatePercentLabel(txtCrowdVal, crowd);
            UpdatePercentLabel(txtUiVal, uiVol);
            if (txtSensitivityVal != null) txtSensitivityVal.text = $"{sens:F1}x";

            isFullscreen = data != null ? data.isFullscreen : Screen.fullScreen;
            if (data != null && data.resolutionIndex >= 0 && data.resolutionIndex < uniqueResolutions.Count)
            {
                currentResIndex = data.resolutionIndex;
            }
            else
            {
                currentResIndex = uniqueResolutions.Count - 1;
                for (int i = 0; i < uniqueResolutions.Count; i++)
                {
                    if (uniqueResolutions[i].width == Screen.width && uniqueResolutions[i].height == Screen.height)
                    {
                        currentResIndex = i;
                        break;
                    }
                }
            }

            int maxQ = Mathf.Max(1, QualitySettings.names.Length) - 1;
            qualityIndex = data != null ? Mathf.Clamp(data.qualityLevel, 0, maxQ) : QualitySettings.GetQualityLevel();
            UpdateDisplayLabels();
        }

        private void OnMasterChanged(float val)
        {
            AudioManager.Instance?.SetMasterVolume(val);
            UpdatePercentLabel(txtMasterVal, val);
        }

        private void OnMusicChanged(float val)
        {
            AudioManager.Instance?.SetMusicVolume(val);
            UpdatePercentLabel(txtMusicVal, val);
        }

        private void OnSfxChanged(float val)
        {
            AudioManager.Instance?.SetSfxVolume(val);
            UpdatePercentLabel(txtSfxVal, val);
        }

        private void OnCrowdChanged(float val)
        {
            AudioManager.Instance?.SetCrowdVolume(val);
            UpdatePercentLabel(txtCrowdVal, val);
        }

        private void OnUiChanged(float val)
        {
            AudioManager.Instance?.SetUiVolume(val);
            UpdatePercentLabel(txtUiVal, val);
        }

        private void OnSensitivityChanged(float val)
        {
            float clamped = Mathf.Clamp(val, 0.5f, 2.0f);
            if (txtSensitivityVal != null) txtSensitivityVal.text = $"{clamped:F1}x";
            if (SaveManager.Instance != null && SaveManager.Instance.CurrentData != null)
            {
                SaveManager.Instance.CurrentData.aimSensitivity = clamped;
                SaveManager.Instance.Save();
            }
        }

        private void StepResolution(int delta)
        {
            AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
            if (uniqueResolutions.Count == 0) return;
            currentResIndex = (currentResIndex + delta + uniqueResolutions.Count) % uniqueResolutions.Count;
            ApplyAndSaveDisplay();
            var r = uniqueResolutions[currentResIndex];
            NotificationManager.EnsureInstance().ShowToast("RESOLUCIÓN ACTUALIZADA", $"{r.width} x {r.height}", NotificationKind.Info, 1.8f);
        }

        private void ToggleFullscreen()
        {
            AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
            isFullscreen = !isFullscreen;
            ApplyAndSaveDisplay();
            NotificationManager.EnsureInstance().ShowToast("MODO DE PANTALLA", isFullscreen ? "Pantalla completa activada" : "Modo ventana activado", NotificationKind.Info, 1.8f);
        }

        private void StepQuality(int delta)
        {
            AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
            int count = Mathf.Max(1, QualitySettings.names.Length);
            qualityIndex = (qualityIndex + delta + count) % count;
            QualitySettings.SetQualityLevel(qualityIndex, true);
            if (SaveManager.Instance != null && SaveManager.Instance.CurrentData != null)
            {
                SaveManager.Instance.CurrentData.qualityLevel = qualityIndex;
                SaveManager.Instance.Save();
            }
            UpdateDisplayLabels();
            string qName = (QualitySettings.names != null && qualityIndex < QualitySettings.names.Length) ? QualitySettings.names[qualityIndex] : qualityIndex.ToString();
            NotificationManager.EnsureInstance().ShowToast("CALIDAD GRÁFICA", $"Preset aplicado: {qName.ToUpper()}", NotificationKind.Success, 2.0f);
        }

        private void ApplyAndSaveDisplay()
        {
            if (currentResIndex >= 0 && currentResIndex < uniqueResolutions.Count)
            {
                var r = uniqueResolutions[currentResIndex];
                Screen.SetResolution(r.width, r.height, isFullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
            }
            if (SaveManager.Instance != null && SaveManager.Instance.CurrentData != null)
            {
                SaveManager.Instance.CurrentData.resolutionIndex = currentResIndex;
                SaveManager.Instance.CurrentData.isFullscreen = isFullscreen;
                SaveManager.Instance.Save();
            }
            UpdateDisplayLabels();
        }

        public void RequestResetDefaultsConfirmation()
        {
            NotificationManager.EnsureInstance().ShowConfirmModal(
                "RESTAURAR AJUSTES",
                "¿Deseas restablecer volumen, pantalla y calidad gráfica a los valores predeterminados?",
                "RESTAURAR",
                "CANCELAR",
                onConfirm: () =>
                {
                    ResetDefaults();
                    NotificationManager.EnsureInstance().ShowToast("CONFIGURACIÓN GUARDADA", "Valores predeterminados restaurados.", NotificationKind.Success);
                },
                isDestructive: false);
        }

        public void ResetDefaults()
        {
            AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
            AudioManager.Instance?.SetMasterVolume(1.0f);
            AudioManager.Instance?.SetMusicVolume(0.8f);
            AudioManager.Instance?.SetSfxVolume(0.9f);
            AudioManager.Instance?.SetCrowdVolume(0.85f);
            AudioManager.Instance?.SetUiVolume(0.90f);
            OnSensitivityChanged(1.0f);
            isFullscreen = true;
            qualityIndex = Mathf.Max(0, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(qualityIndex, true);
            currentResIndex = Mathf.Max(0, uniqueResolutions.Count - 1);
            ApplyAndSaveDisplay();
            LoadFromSave();
        }

        private void UpdateDisplayLabels()
        {
            if (txtResolutionVal != null && currentResIndex >= 0 && currentResIndex < uniqueResolutions.Count)
            {
                var r = uniqueResolutions[currentResIndex];
                txtResolutionVal.text = $"{r.width} x {r.height}";
            }
            if (txtFullscreenVal != null)
            {
                txtFullscreenVal.text = isFullscreen ? "PANTALLA COMPLETA" : "VENTANA";
            }
            if (txtQualityVal != null)
            {
                string[] labels = QualitySettings.names;
                txtQualityVal.text = (labels != null && qualityIndex < labels.Length) ? labels[qualityIndex].ToUpper() : $"CALIDAD {qualityIndex}";
            }
        }

        private void UpdatePercentLabel(TMP_Text txt, float val)
        {
            if (txt != null) txt.text = $"{Mathf.RoundToInt(val * 100)}%";
        }

        private void OnRedeemClicked()
        {
            if (inputSecretCode == null) return;
            string code = inputSecretCode.text;
            AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
            CurrencyManager.Instance?.TryRedeemCode(code);
            inputSecretCode.text = string.Empty;
        }

        private void HandleCodeResult(string msg, bool success)
        {
            NotificationManager.EnsureInstance().ShowToast(
                success ? "CÓDIGO CANJEADO" : "CÓDIGO INVÁLIDO",
                msg,
                success ? NotificationKind.Reward : NotificationKind.Warning);

            if (txtCodeMessage != null)
            {
                txtCodeMessage.gameObject.SetActive(true);
                txtCodeMessage.text = msg;
                txtCodeMessage.color = success ? new Color(0.15f, 0.80f, 0.45f) : new Color(0.90f, 0.28f, 0.30f);
                CancelInvoke(nameof(HideCodeMessage));
                Invoke(nameof(HideCodeMessage), 3f);
            }
        }

        private void HideCodeMessage()
        {
            if (txtCodeMessage != null) txtCodeMessage.gameObject.SetActive(false);
        }
    }
}
