#pragma warning disable 0649
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UltimatePenaltyCup.Achievements;
using UltimatePenaltyCup.Audio;

namespace UltimatePenaltyCup.UI
{
    public class AchievementsUI : MonoBehaviour
    {
        [System.Serializable]
        public struct AchievementCard
        {
            public string achievementId;
            public GameObject cardObject;
            public Image imgCardBg;
            public Image imgMedalIcon;
            public TMP_Text txtTitle;
            public TMP_Text txtDescription;
            public Image imgProgressFill;
            public TMP_Text txtProgress;
            public TMP_Text txtReward;
            public GameObject unlockedBadge;
            public GameObject lockedBadge;
        }

        [SerializeField] private AchievementCard[] achievementCards;
        [SerializeField] private TMP_Text txtSummaryHeader;
        [SerializeField] private Button btnBack;
        [SerializeField] private MainMenuUI mainMenuUI;

        private void OnEnable()
        {
            RefreshAchievements();
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
            RefreshAchievements();
        }

        public void RefreshAchievements()
        {
            if (achievementCards == null) return;

            int unlockedCount = 0;
            foreach (var card in achievementCards)
            {
                bool isUnlocked = AchievementManager.Instance != null && AchievementManager.Instance.IsUnlocked(card.achievementId);
                if (isUnlocked) unlockedCount++;

                AchievementSO so = AchievementManager.Instance != null ? AchievementManager.Instance.GetAchievement(card.achievementId) : null;

                if (card.unlockedBadge != null) card.unlockedBadge.SetActive(isUnlocked);
                if (card.lockedBadge != null) card.lockedBadge.SetActive(!isUnlocked);

                if (card.imgCardBg != null)
                {
                    card.imgCardBg.color = isUnlocked
                        ? new Color(0.08f, 0.22f, 0.36f, 0.96f)
                        : new Color(0.06f, 0.12f, 0.24f, 0.92f);
                }

                if (card.imgMedalIcon != null)
                {
                    card.imgMedalIcon.color = isUnlocked
                        ? new Color(1.0f, 0.80f, 0.22f, 1f)
                        : new Color(0.35f, 0.40f, 0.50f, 0.65f);
                }

                if (AchievementManager.Instance != null)
                {
                    float prog = AchievementManager.Instance.GetAchievementProgress(card.achievementId, out string label);
                    if (card.imgProgressFill != null)
                    {
                        card.imgProgressFill.fillAmount = prog;
                        card.imgProgressFill.color = isUnlocked
                            ? new Color(0.15f, 0.78f, 0.44f)
                            : new Color(0.08f, 0.49f, 1.0f);
                    }
                    if (card.txtProgress != null) card.txtProgress.text = label;
                }

                if (so != null)
                {
                    if (card.txtTitle != null) card.txtTitle.text = so.title;
                    if (card.txtDescription != null) card.txtDescription.text = so.description;
                    if (card.txtReward != null)
                    {
                        card.txtReward.text = so.rewardCoins > 0 ? $"+{so.rewardCoins:N0} Monedas" : (so.rewardPoints > 0 ? $"+{so.rewardPoints:N0} Pts" : "Medalla");
                    }
                }
            }

            if (txtSummaryHeader != null)
            {
                txtSummaryHeader.text = $"Desbloqueados: {unlockedCount} / {achievementCards.Length}";
            }
        }
    }
}
