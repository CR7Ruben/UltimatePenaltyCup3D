#pragma warning disable 0649
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UltimatePenaltyCup.Core;
using UltimatePenaltyCup.Customization;
using UltimatePenaltyCup.SaveSystem;
using UltimatePenaltyCup.Shop;
using UltimatePenaltyCup.Audio;

namespace UltimatePenaltyCup.UI
{
    public class CustomizationUI : MonoBehaviour
    {
        [Header("Independent Tabs")]
        [SerializeField] private Button btnTabBalls;
        [SerializeField] private Button btnTabStadiums;
        [SerializeField] private GameObject panelBallsTab;
        [SerializeField] private GameObject panelStadiumsTab;
        [SerializeField] private Transform ballsContainer;
        [SerializeField] private Transform stadiumsContainer;

        [Header("Active Selection Preview")]
        [SerializeField] private Image imgActivePreview;
        [SerializeField] private TMP_Text txtPreviewTitle;
        [SerializeField] private TMP_Text txtPreviewCategory;
        [SerializeField] private TMP_Text txtPreviewDesc;
        [SerializeField] private TMP_Text txtStadiumModeStatus;
        [SerializeField] private Button btnAutoRoundStadium;

        [Header("Navigation")]
        [SerializeField] private Button btnBack;
        [SerializeField] private MainMenuUI mainMenuUI;

        private bool showingBalls = true;

        private void OnEnable()
        {
            SwitchTab(true);
            RefreshAll();
        }

        private void Start()
        {
            if (btnTabBalls != null) btnTabBalls.onClick.AddListener(() => { AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick); SwitchTab(true); });
            if (btnTabStadiums != null) btnTabStadiums.onClick.AddListener(() => { AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick); SwitchTab(false); });

            if (btnAutoRoundStadium != null)
            {
                btnAutoRoundStadium.onClick.AddListener(() =>
                {
                    CustomizationManager.Instance?.SetUseRoundStadiumAutomatic();
                    RefreshAll();
                });
            }

            if (btnBack != null)
            {
                btnBack.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
                    mainMenuUI?.ReturnToMainMenu();
                });
            }

            if (CustomizationManager.Instance != null)
            {
                CustomizationManager.Instance.OnBallEquipped += HandleItemChanged;
                CustomizationManager.Instance.OnStadiumSelected += HandleStadiumChanged;
            }

            RefreshAll();
        }

        private void OnDestroy()
        {
            if (CustomizationManager.Instance != null)
            {
                CustomizationManager.Instance.OnBallEquipped -= HandleItemChanged;
                CustomizationManager.Instance.OnStadiumSelected -= HandleStadiumChanged;
            }
        }

        private void HandleItemChanged(BallType b) => RefreshAll();
        private void HandleStadiumChanged(StadiumType s) => RefreshAll();

        public void SwitchTab(bool showBalls)
        {
            showingBalls = showBalls;
            if (panelBallsTab != null) panelBallsTab.SetActive(showBalls);
            if (panelStadiumsTab != null) panelStadiumsTab.SetActive(!showBalls);

            if (btnTabBalls != null)
                btnTabBalls.GetComponent<Image>().color = showBalls ? new Color(0.08f, 0.49f, 1.0f) : new Color(0.11f, 0.19f, 0.34f);
            if (btnTabStadiums != null)
                btnTabStadiums.GetComponent<Image>().color = !showBalls ? new Color(0.08f, 0.49f, 1.0f) : new Color(0.11f, 0.19f, 0.34f);

            UpdatePreviewPanel();
        }

        public void RefreshAll()
        {
            if (ballsContainer != null)
            {
                foreach (var card in ballsContainer.GetComponentsInChildren<ShopItemCardUI>(true))
                    card.Refresh();
            }

            if (stadiumsContainer != null)
            {
                foreach (var card in stadiumsContainer.GetComponentsInChildren<ShopItemCardUI>(true))
                    card.Refresh();
            }

            UpdatePreviewPanel();
        }

        private void UpdatePreviewPanel()
        {
            var data = SaveManager.Instance != null ? SaveManager.Instance.CurrentData : null;
            if (txtStadiumModeStatus != null)
            {
                bool autoMode = data == null || !data.useCustomStadiumOverride;
                txtStadiumModeStatus.text = autoMode
                    ? "Modo Estadio: Automático por Ronda"
                    : $"Estadio Fijo: {data.selectedStadium}";
            }

            if (btnAutoRoundStadium != null)
            {
                btnAutoRoundStadium.gameObject.SetActive(!showingBalls);
            }

            if (showingBalls)
            {
                BallType equipped = CustomizationManager.Instance != null ? CustomizationManager.Instance.EquippedBall : BallType.Classic;
                BallItemSO item = ShopManager.Instance != null ? ShopManager.Instance.GetBallItem(equipped) : null;
                if (txtPreviewCategory != null) txtPreviewCategory.text = "BALÓN EQUIPADO";
                if (item != null)
                {
                    if (txtPreviewTitle != null) txtPreviewTitle.text = item.displayName;
                    if (txtPreviewDesc != null) txtPreviewDesc.text = item.description;
                    if (imgActivePreview != null && item.icon != null)
                    {
                        imgActivePreview.sprite = item.icon;
                        imgActivePreview.color = Color.white;
                        imgActivePreview.preserveAspect = true;
                    }
                }
            }
            else
            {
                StadiumType selected = CustomizationManager.Instance != null ? CustomizationManager.Instance.SelectedStadium : StadiumType.Cantera;
                StadiumItemSO item = ShopManager.Instance != null ? ShopManager.Instance.GetStadiumItem(selected) : null;
                if (txtPreviewCategory != null) txtPreviewCategory.text = "ESTADIO SELECCIONADO";
                if (item != null)
                {
                    if (txtPreviewTitle != null) txtPreviewTitle.text = item.displayName;
                    if (txtPreviewDesc != null) txtPreviewDesc.text = item.description;
                    if (imgActivePreview != null && item.icon != null)
                    {
                        imgActivePreview.sprite = item.icon;
                        imgActivePreview.color = Color.white;
                        imgActivePreview.preserveAspect = true;
                    }
                }
            }
        }
    }
}
