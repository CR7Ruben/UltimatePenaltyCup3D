using UnityEngine;
using UltimatePenaltyCup.Core;

namespace UltimatePenaltyCup.Shop
{
    [CreateAssetMenu(fileName = "BallItem_", menuName = "UltimatePenaltyCup/BallItem")]
    public class BallItemSO : ScriptableObject
    {
        public BallType ballType;
        public string displayName;
        [TextArea(2, 3)]
        public string description;
        public int price;
        public Color primaryColor = Color.white;
        public Color secondaryColor = Color.black;
        public Color trailColor = Color.cyan;
        public Material material;
        public Sprite icon;
    }
}
