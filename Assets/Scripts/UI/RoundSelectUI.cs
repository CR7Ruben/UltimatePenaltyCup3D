#pragma warning disable 0649
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UltimatePenaltyCup.Core;
using UltimatePenaltyCup.Levels;
using UltimatePenaltyCup.SaveSystem;
using UltimatePenaltyCup.Shop;
using UltimatePenaltyCup.Audio;

namespace UltimatePenaltyCup.UI
{
    public class RoundSelectUI : MonoBehaviour
    {
        [System.Serializable]
        public struct RoundCard
        {
            public int roundNumber;
            public GameObject cardObject;
            public Image imgStadiumPreview;
            public TMP_Text txtTitle;
            public TMP_Text txtDifficulty;
            public TMP_Text txtDescription;
            public TMP_Text txtTarget;
            public TMP_Text txtRewards;
            public GameObject lockOverlay;
            public TMP_Text txtLockReason;
            public Button btnPlay;
            public TMP_Text txtBtnPlay;
        }

        [Header("Round Cards (1 to 5)")]
        [SerializeField] private RoundCard[] roundCards;

        [Header("Navigation")]
        [SerializeField] private Button btnBack;
        [SerializeField] private MainMenuUI mainMenuUI;
        [SerializeField] private TMP_Text txtActiveStadiumMode;

        private void OnEnable()
        {
            RefreshCards();
        }

        private void Start()
        {
            if (btnBack != null)
            {
                btnBack.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
                    mainMenuUI?.ReturnToMainMenu();
                });
            }

            SetupCardButtons();
            RefreshCards();
        }

        private void SetupCardButtons()
        {
            if (roundCards == null) return;

            foreach (var card in roundCards)
            {
                int rNum = card.roundNumber;
                if (card.btnPlay != null)
                {
                    card.btnPlay.onClick.RemoveAllListeners();
                    card.btnPlay.onClick.AddListener(() => OnPlayRoundClicked(rNum));
                }
            }
        }

        public void RefreshCards()
        {
            if (roundCards == null) return;

            var saveData = SaveManager.Instance != null ? SaveManager.Instance.CurrentData : null;
            bool customStadiumActive = saveData != null && saveData.useCustomStadiumOverride &&
                                       SaveManager.Instance.IsStadiumUnlocked(saveData.selectedStadium);
            StadiumItemSO customStadiumItem = customStadiumActive && ShopManager.Instance != null
                ? ShopManager.Instance.GetStadiumItem(saveData.selectedStadium)
                : null;

            if (txtActiveStadiumMode != null)
            {
                string customName = customStadiumItem != null ? customStadiumItem.displayName : (saveData != null ? saveData.selectedStadium.ToString() : "Personalizado");
                txtActiveStadiumMode.text = customStadiumActive
                    ? $"ESTADIO FIJO ACTIVO: {customName.ToUpper()} (TODAS LAS RONDAS)"
                    : "ESTADIO: AUTOMÁTICO POR RONDA";
                txtActiveStadiumMode.color = customStadiumActive ? new Color(1.0f, 0.78f, 0.24f) : new Color(0.15f, 0.78f, 0.44f);
            }

            foreach (var card in roundCards)
            {
                bool isUnlocked = SaveManager.Instance != null && SaveManager.Instance.IsRoundUnlocked(card.roundNumber);
                RoundDataSO data = GameManager.Instance != null
                    ? GameManager.Instance.GetRoundData(card.roundNumber)
                    : (RoundManager.Instance != null ? RoundManager.Instance.GetRound(card.roundNumber) : null);

                if (card.lockOverlay != null)
                {
                    card.lockOverlay.SetActive(!isUnlocked);
                }

                if (card.txtLockReason != null)
                {
                    card.txtLockReason.text = card.roundNumber > 1 ? $"Supera Ronda {card.roundNumber - 1}" : "Bloqueado";
                }

                if (card.btnPlay != null)
                {
                    card.btnPlay.interactable = isUnlocked;
                    if (card.txtBtnPlay != null)
                    {
                        card.txtBtnPlay.text = isUnlocked ? "JUGAR RONDA" : "BLOQUEADA";
                    }
                }

                if (data != null)
                {
                    if (card.txtTitle != null) card.txtTitle.text = $"RONDA {data.roundNumber}\n{data.roundName.ToUpper()}";
                    if (card.txtDifficulty != null) card.txtDifficulty.text = $"IA: {data.goalkeeperTypeTitle}";
                    if (card.txtDescription != null) card.txtDescription.text = data.description;
                    if (card.txtTarget != null) card.txtTarget.text = $"Meta: {data.requiredGoals} / {data.maxAttempts} goles";
                    if (card.txtRewards != null) card.txtRewards.text = $"+{data.winCoins:N0} Monedas";

                    if (card.imgStadiumPreview != null && ShopManager.Instance != null)
                    {
                        StadiumType displayStadium = customStadiumActive ? saveData.selectedStadium : data.stadiumTheme;
                        StadiumItemSO stadium = ShopManager.Instance.GetStadiumItem(displayStadium);
                        if (stadium != null && stadium.icon != null)
                        {
                            card.imgStadiumPreview.sprite = stadium.icon;
                            card.imgStadiumPreview.color = isUnlocked ? Color.white : new Color(0.45f, 0.48f, 0.55f, 1f);
                        }
                    }
                }
            }
        }

        private void OnPlayRoundClicked(int roundNumber)
        {
            AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
            GameManager.Instance?.SelectRoundAndStartMatch(roundNumber);
        }
    }
}
