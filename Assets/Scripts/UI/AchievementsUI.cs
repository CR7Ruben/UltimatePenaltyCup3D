#pragma warning disable 0649
using System;
using System.Collections.Generic;
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
            public TMP_Text txtTitle;
            public TMP_Text txtDescription;
            public TMP_Text txtReward;
            public GameObject unlockedBadge;
            public GameObject lockedBadge;
        }

        [SerializeField] private AchievementCard[] achievementCards;
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

            foreach (var card in achievementCards)
            {
                bool isUnlocked = AchievementManager.Instance != null && AchievementManager.Instance.IsUnlocked(card.achievementId);
                AchievementSO so = AchievementManager.Instance != null ? AchievementManager.Instance.GetAchievement(card.achievementId) : null;

                if (card.unlockedBadge != null) card.unlockedBadge.SetActive(isUnlocked);
                if (card.lockedBadge != null) card.lockedBadge.SetActive(!isUnlocked);

                if (so != null)
                {
                    if (card.txtTitle != null) card.txtTitle.text = so.title;
                    if (card.txtDescription != null) card.txtDescription.text = so.description;
                    if (card.txtReward != null)
                    {
                        card.txtReward.text = so.rewardCoins > 0 ? $"+{so.rewardCoins} Monedas" : (so.rewardPoints > 0 ? $"+{so.rewardPoints} Pts" : "Medalla Especial");
                    }
                }
            }
        }
    }
}
