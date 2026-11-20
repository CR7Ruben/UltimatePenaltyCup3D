using UnityEngine;
using UltimatePenaltyCup.Core;

namespace UltimatePenaltyCup.Shop
{
    [CreateAssetMenu(fileName = "StadiumItem_", menuName = "UltimatePenaltyCup/StadiumItem")]
    public class StadiumItemSO : ScriptableObject
    {
        public StadiumType stadiumType;
        public string displayName;
        [TextArea(2, 3)]
        public string description;
        public int price;

        [Header("Visual Environment")]
        public Color skyColor = new Color(0.2f, 0.45f, 0.8f);
        public Color equatorColor = new Color(0.3f, 0.4f, 0.5f);
        public Color groundLightColor = new Color(0.15f, 0.25f, 0.15f);
        public Color directionalLightColor = Color.white;
        public float directionalLightIntensity = 1.2f;
        public Vector3 lightRotation = new Vector3(50f, -30f, 0f);

        [Header("Materials & Themes")]
        public Material grassMaterial;
        public Material bannerMaterial;
        public Sprite icon;
    }
}
