using UnityEngine;

namespace UltimatePenaltyCup.Achievements
{
    [CreateAssetMenu(fileName = "Achievement_", menuName = "UltimatePenaltyCup/Achievement")]
    public class AchievementSO : ScriptableObject
    {
        public string id;
        public string title;
        [TextArea(2, 3)]
        public string description;
        public int rewardCoins;
        public int rewardPoints;
        public Sprite icon;
    }
}
