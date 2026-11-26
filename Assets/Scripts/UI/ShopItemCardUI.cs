#pragma warning disable 0649
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
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
        [Header("Mode")]
        [SerializeField] private bool isCustomizationOnly = false;
        [SerializeField] private GameObject badgeEquipped;

        public event Action<ShopItemCardUI> OnCardSelected;

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
                if (imgPreview != null)
                {
                    if (ballItem.icon != null)
                    {
                        imgPreview.sprite = ballItem.icon;
                        imgPreview.color = Color.white;
                        imgPreview.preserveAspect = true;
                    }
                    else
                    {
                        imgPreview.color = ballItem.primaryColor;
                    }
                }

                bool isUnlocked = SaveManager.Instance != null && SaveManager.Instance.IsBallUnlocked(ballItem.ballType);
                bool isEquipped = SaveManager.Instance != null && SaveManager.Instance.CurrentData != null && SaveManager.Instance.CurrentData.equippedBall == ballItem.ballType;
                bool canAfford = CurrencyManager.Instance != null && CurrencyManager.Instance.HasEnoughCoins(ballItem.price);

                if (txtPrice != null)
                {
                    txtPrice.text = isUnlocked ? "EN PROPIEDAD" : (ballItem.price == 0 ? "GRATIS" : $"{ballItem.price:N0}");
                    txtPrice.color = isUnlocked ? new Color(0.15f, 0.78f, 0.44f) : new Color(1.0f, 0.78f, 0.24f);
                }

                UpdateActionButton(isUnlocked, isEquipped, canAfford);
            }
            else if (cardType == CardType.Stadium && stadiumItem != null)
            {
                if (txtName != null) txtName.text = stadiumItem.displayName;
                if (txtDescription != null) txtDescription.text = stadiumItem.description;
                if (imgPreview != null)
                {
                    if (stadiumItem.icon != null)
                    {
                        imgPreview.sprite = stadiumItem.icon;
                        imgPreview.color = Color.white;
                        imgPreview.preserveAspect = true;
                    }
                    else
                    {
                        imgPreview.color = stadiumItem.skyColor;
                    }
                }

                bool isUnlocked = SaveManager.Instance != null && SaveManager.Instance.IsStadiumUnlocked(stadiumItem.stadiumType);
                bool isCustomSelected = SaveManager.Instance != null && SaveManager.Instance.CurrentData != null &&
                                        SaveManager.Instance.CurrentData.useCustomStadiumOverride &&
                                        SaveManager.Instance.CurrentData.selectedStadium == stadiumItem.stadiumType;
                bool canAfford = CurrencyManager.Instance != null && CurrencyManager.Instance.HasEnoughCoins(stadiumItem.price);

                if (txtPrice != null)
                {
                    txtPrice.text = isUnlocked ? "EN PROPIEDAD" : (stadiumItem.price == 0 ? "GRATIS" : $"{stadiumItem.price:N0}");
                    txtPrice.color = isUnlocked ? new Color(0.15f, 0.78f, 0.44f) : new Color(1.0f, 0.78f, 0.24f);
                }

                UpdateActionButton(isUnlocked, isCustomSelected, canAfford);
            }
        }

        private void UpdateActionButton(bool isUnlocked, bool isEquipped, bool canAfford)
        {
            if (badgeEquipped != null) badgeEquipped.SetActive(isEquipped);
            if (btnAction == null || txtActionButton == null) return;

            if (isEquipped)
            {
                txtActionButton.text = "EQUIPADO";
                txtActionButton.color = Color.white;
                btnAction.interactable = false;
                if (imgActionButton != null) imgActionButton.color = new Color(0.15f, 0.78f, 0.44f);
            }
            else if (isUnlocked)
            {
                txtActionButton.text = "EQUIPAR";
                txtActionButton.color = Color.white;
                btnAction.interactable = true;
                if (imgActionButton != null) imgActionButton.color = new Color(0.08f, 0.49f, 1.0f);
            }
            else
            {
                if (isCustomizationOnly)
                {
                    txtActionButton.text = "BLOQUEADO";
                    txtActionButton.color = new Color(0.75f, 0.78f, 0.85f);
                    btnAction.interactable = false;
                    if (imgActionButton != null) imgActionButton.color = new Color(0.24f, 0.28f, 0.36f);
                }
                else
                {
                    txtActionButton.text = canAfford ? "COMPRAR" : "SIN FONDOS";
                    txtActionButton.color = canAfford ? new Color(0.03f, 0.07f, 0.14f) : new Color(0.75f, 0.78f, 0.85f);
                    btnAction.interactable = canAfford;
                    if (imgActionButton != null)
                    {
                        imgActionButton.color = canAfford ? new Color(1.0f, 0.78f, 0.24f) : new Color(0.24f, 0.28f, 0.36f);
                    }
                }
            }
        }

        private void OnActionClicked()
        {
            AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
            OnCardSelected?.Invoke(this);

            if (cardType == CardType.Ball && ballItem != null)
            {
                bool isUnlocked = SaveManager.Instance != null && SaveManager.Instance.IsBallUnlocked(ballItem.ballType);
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
                bool isUnlocked = SaveManager.Instance != null && SaveManager.Instance.IsStadiumUnlocked(stadiumItem.stadiumType);
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
