#pragma warning disable 0649
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UltimatePenaltyCup.Audio;
using UltimatePenaltyCup.Economy;

namespace UltimatePenaltyCup.UI
{
    public class SettingsUI : MonoBehaviour
    {
        [Header("Audio Sliders")]
        [SerializeField] private Slider sliderMusic;
        [SerializeField] private Slider sliderSfx;
        [SerializeField] private TMP_Text txtMusicVal;
        [SerializeField] private TMP_Text txtSfxVal;

        [Header("Secret Code")]
        [SerializeField] private TMP_InputField inputSecretCode;
        [SerializeField] private Button btnRedeemCode;
        [SerializeField] private TMP_Text txtCodeMessage;

        [Header("Navigation")]
        [SerializeField] private Button btnBack;
        [SerializeField] private MainMenuUI mainMenuUI;

        private void Start()
        {
            if (sliderMusic != null)
            {
                sliderMusic.value = AudioManager.Instance != null ? AudioManager.Instance.MusicVolume : 0.8f;
                sliderMusic.onValueChanged.AddListener(OnMusicSliderChanged);
                UpdateMusicLabel(sliderMusic.value);
            }

            if (sliderSfx != null)
            {
                sliderSfx.value = AudioManager.Instance != null ? AudioManager.Instance.SfxVolume : 0.9f;
                sliderSfx.onValueChanged.AddListener(OnSfxSliderChanged);
                UpdateSfxLabel(sliderSfx.value);
            }

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

        private void OnMusicSliderChanged(float val)
        {
            AudioManager.Instance?.SetMusicVolume(val);
            UpdateMusicLabel(val);
        }

        private void OnSfxSliderChanged(float val)
        {
            AudioManager.Instance?.SetSfxVolume(val);
            UpdateSfxLabel(val);
        }

        private void UpdateMusicLabel(float val)
        {
            if (txtMusicVal != null) txtMusicVal.text = $"{Mathf.RoundToInt(val * 100)}%";
        }

        private void UpdateSfxLabel(float val)
        {
            if (txtSfxVal != null) txtSfxVal.text = $"{Mathf.RoundToInt(val * 100)}%";
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
            if (txtCodeMessage != null)
            {
                txtCodeMessage.gameObject.SetActive(true);
                txtCodeMessage.text = msg;
                txtCodeMessage.color = success ? Color.green : Color.red;
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
