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
                    NotificationManager.EnsureInstance().ShowToast("ESTADIO AUTOMÁTICO", "Cada ronda usará su sede oficial.", NotificationKind.Info);
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

        private void HandleItemChanged(BallType b)
        {
            NotificationManager.EnsureInstance().ShowToast("BALÓN EQUIPADO", $"Se equipó el balón {b}.", NotificationKind.Success);
            RefreshAll();
        }

        private void HandleStadiumChanged(StadiumType s)
        {
            var data = SaveManager.Instance != null ? SaveManager.Instance.CurrentData : null;
            if (data != null && data.useCustomStadiumOverride)
            {
                NotificationManager.EnsureInstance().ShowToast("ESTADIO PERSONALIZADO", $"Se jugarán todas las rondas en {s}.", NotificationKind.Success);
            }
            RefreshAll();
        }

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
            var saveMgr = SaveManager.Instance;
            if (saveMgr == null) saveMgr = FindAnyObjectByType<SaveManager>();
            var shopMgr = ShopManager.Instance;
            if (shopMgr == null) shopMgr = FindAnyObjectByType<ShopManager>();
            var custMgr = CustomizationManager.Instance;
            if (custMgr == null) custMgr = FindAnyObjectByType<CustomizationManager>();

            var data = saveMgr != null ? saveMgr.CurrentData : null;
            bool autoMode = data == null || !data.useCustomStadiumOverride;
            StadiumType selectedStad = data != null ? data.selectedStadium : StadiumType.Cantera;
            StadiumItemSO selectedStadItem = shopMgr != null ? shopMgr.GetStadiumItem(selectedStad) : null;
            string selectedStadName = selectedStadItem != null ? selectedStadItem.displayName : selectedStad.ToString();

            if (txtStadiumModeStatus != null)
            {
                txtStadiumModeStatus.text = autoMode
                    ? "MODO ESTADIO: AUTOMÁTICO POR RONDA\n(Cada ronda usa su estadio oficial)"
                    : $"MODO ESTADIO FIJO: {selectedStadName.ToUpper()}\n(Se aplicará en todas las rondas)";
                txtStadiumModeStatus.color = autoMode ? new Color(0.15f, 0.78f, 0.44f) : new Color(1.0f, 0.78f, 0.24f);
            }

            if (btnAutoRoundStadium != null)
            {
                btnAutoRoundStadium.gameObject.SetActive(!showingBalls);
                btnAutoRoundStadium.interactable = !autoMode;
                TMP_Text autoTxt = btnAutoRoundStadium.GetComponentInChildren<TMP_Text>();
                if (autoTxt != null)
                {
                    autoTxt.text = autoMode ? "MODO AUTOMÁTICO ACTIVO" : "USAR AUTOMÁTICO POR RONDA";
                }
                Image autoImg = btnAutoRoundStadium.GetComponent<Image>();
                if (autoImg != null)
                {
                    autoImg.color = autoMode ? new Color(0.15f, 0.78f, 0.44f) : new Color(0.08f, 0.49f, 1.0f);
                }
            }

            if (showingBalls)
            {
                BallType equipped = custMgr != null ? custMgr.EquippedBall : BallType.Classic;
                BallItemSO item = shopMgr != null ? shopMgr.GetBallItem(equipped) : null;
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
                if (txtPreviewCategory != null)
                    txtPreviewCategory.text = autoMode ? "ESTADIO PERSONALIZADO (DESACTIVADO)" : "ESTADIO ACTIVO EN TODAS LAS RONDAS";
                if (selectedStadItem != null)
                {
                    if (txtPreviewTitle != null) txtPreviewTitle.text = autoMode ? "Automático por Ronda" : selectedStadItem.displayName;
                    if (txtPreviewDesc != null)
                        txtPreviewDesc.text = autoMode
                            ? "Selecciona cualquier estadio desbloqueado a la izquierda para fijarlo en las 5 rondas."
                            : selectedStadItem.description;
                    if (imgActivePreview != null && selectedStadItem.icon != null)
                    {
                        imgActivePreview.sprite = selectedStadItem.icon;
                        imgActivePreview.color = Color.white;
                        imgActivePreview.preserveAspect = true;
                    }
                }
            }
        }
    }
}
