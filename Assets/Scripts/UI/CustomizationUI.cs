#pragma warning disable 0649
using UnityEngine;
using UnityEngine.UI;
using UltimatePenaltyCup.Core;
using UltimatePenaltyCup.Customization;
using UltimatePenaltyCup.Audio;

namespace UltimatePenaltyCup.UI
{
    public class CustomizationUI : MonoBehaviour
    {
        [Header("Containers")]
        [SerializeField] private Transform ballsContainer;
        [SerializeField] private Transform stadiumsContainer;

        [Header("Navigation")]
        [SerializeField] private Button btnBack;
        [SerializeField] private MainMenuUI mainMenuUI;

        private void OnEnable()
        {
            RefreshAll();
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

            if (CustomizationManager.Instance != null)
            {
                CustomizationManager.Instance.OnBallEquipped += (b) => RefreshAll();
                CustomizationManager.Instance.OnStadiumSelected += (s) => RefreshAll();
            }

            RefreshAll();
        }

        public void RefreshAll()
        {
            if (ballsContainer != null)
            {
                foreach (var card in ballsContainer.GetComponentsInChildren<ShopItemCardUI>(true))
                {
                    card.Refresh();
                }
            }

            if (stadiumsContainer != null)
            {
                foreach (var card in stadiumsContainer.GetComponentsInChildren<ShopItemCardUI>(true))
                {
                    card.Refresh();
                }
            }
        }
    }
}
