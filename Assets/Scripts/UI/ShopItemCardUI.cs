#pragma warning disable 0649
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UltimatePenaltyCup.Core;
using UltimatePenaltyCup.Shop;
using UltimatePenaltyCup.SaveSystem;
using UltimatePenaltyCup.Economy;
using UltimatePenaltyCup.Customization;
using UltimatePenaltyCup.Audio;

namespace UltimatePenaltyCup.UI
{
    public class ShopItemCardUI : MonoBehaviour
    {
        public enum CardType { Ball, Stadium }

        [Header("Type & Asset")]
        public CardType cardType = CardType.Ball;
        public BallItemSO ballItem;
        public StadiumItemSO stadiumItem;

        [Header("UI Elements")]
        [SerializeField] private TMP_Text txtName;
        [SerializeField] private TMP_Text txtPrice;
        [SerializeField] private TMP_Text txtDescription;
        [SerializeField] private Image imgPreview;
        [SerializeField] private Button btnAction;
        [SerializeField] private TMP_Text txtActionButton;
        [SerializeField] private Image imgActionButton;

        private void Start()
        {
            if (btnAction != null)
            {
                btnAction.onClick.AddListener(OnActionClicked);
            }
            Refresh();
        }

        public void SetupBall(BallItemSO item)
        {
            cardType = CardType.Ball;
            ballItem = item;
            Refresh();
        }

        public void SetupStadium(StadiumItemSO item)
        {
            cardType = CardType.Stadium;
            stadiumItem = item;
            Refresh();
        }

        public void Refresh()
        {
            if (cardType == CardType.Ball && ballItem != null)
            {
                if (txtName != null) txtName.text = ballItem.displayName;
                if (txtDescription != null) txtDescription.text = ballItem.description;
                if (imgPreview != null) imgPreview.color = ballItem.primaryColor;

                bool isUnlocked = SaveManager.Instance != null && SaveManager.Instance.IsBallUnlocked(ballItem.ballType);
                bool isEquipped = SaveManager.Instance != null && SaveManager.Instance.CurrentData != null && SaveManager.Instance.CurrentData.equippedBall == ballItem.ballType;
                bool canAfford = CurrencyManager.Instance != null && CurrencyManager.Instance.HasEnoughCoins(ballItem.price);

                if (txtPrice != null)
                {
                    txtPrice.text = ballItem.price == 0 ? "GRATIS" : $"{ballItem.price:N0} Monedas";
                }

                UpdateActionButton(isUnlocked, isEquipped, canAfford);
            }
            else if (cardType == CardType.Stadium && stadiumItem != null)
            {
                if (txtName != null) txtName.text = stadiumItem.displayName;
                if (txtDescription != null) txtDescription.text = stadiumItem.description;
                if (imgPreview != null) imgPreview.color = stadiumItem.skyColor;

                bool isUnlocked = SaveManager.Instance != null && SaveManager.Instance.IsStadiumUnlocked(stadiumItem.stadiumType);
                bool isEquipped = SaveManager.Instance != null && SaveManager.Instance.CurrentData != null && SaveManager.Instance.CurrentData.selectedStadium == stadiumItem.stadiumType;
                bool canAfford = CurrencyManager.Instance != null && CurrencyManager.Instance.HasEnoughCoins(stadiumItem.price);

                if (txtPrice != null)
                {
                    txtPrice.text = stadiumItem.price == 0 ? "GRATIS" : $"{stadiumItem.price:N0} Monedas";
                }

                UpdateActionButton(isUnlocked, isEquipped, canAfford);
            }
        }

        private void UpdateActionButton(bool isUnlocked, bool isEquipped, bool canAfford)
        {
            if (btnAction == null || txtActionButton == null) return;

            if (isEquipped)
            {
                txtActionButton.text = "EQUIPADO";
                btnAction.interactable = false;
                if (imgActionButton != null) imgActionButton.color = new Color(0.2f, 0.75f, 0.35f);
            }
            else if (isUnlocked)
            {
                txtActionButton.text = "EQUIPAR";
                btnAction.interactable = true;
                if (imgActionButton != null) imgActionButton.color = new Color(0.2f, 0.5f, 0.95f);
            }
            else
            {
                txtActionButton.text = "COMPRAR";
                btnAction.interactable = canAfford;
                if (imgActionButton != null)
                {
                    imgActionButton.color = canAfford ? new Color(0.95f, 0.75f, 0.15f) : new Color(0.5f, 0.5f, 0.5f);
                }
            }
        }

        private void OnActionClicked()
        {
            AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);

            if (cardType == CardType.Ball && ballItem != null)
            {
                bool isUnlocked = SaveManager.Instance.IsBallUnlocked(ballItem.ballType);
                if (isUnlocked)
                {
                    CustomizationManager.Instance?.EquipBall(ballItem.ballType);
                    Refresh();
                }
                else
                {
                    ShopManager.Instance?.BuyBall(ballItem.ballType);
                }
            }
            else if (cardType == CardType.Stadium && stadiumItem != null)
            {
                bool isUnlocked = SaveManager.Instance.IsStadiumUnlocked(stadiumItem.stadiumType);
                if (isUnlocked)
                {
                    CustomizationManager.Instance?.SelectStadium(stadiumItem.stadiumType);
                    Refresh();
                }
                else
                {
                    ShopManager.Instance?.BuyStadium(stadiumItem.stadiumType);
                }
            }
        }
    }
}
