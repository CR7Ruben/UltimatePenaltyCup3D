using System;
using UnityEngine;
using UltimatePenaltyCup.Core;
using UltimatePenaltyCup.SaveSystem;
using UltimatePenaltyCup.Shop;
using UltimatePenaltyCup.Audio;

namespace UltimatePenaltyCup.Customization
{
    public class CustomizationManager : MonoBehaviour
    {
        public static CustomizationManager Instance { get; private set; }

        public event Action<BallType> OnBallEquipped;
        public event Action<StadiumType> OnStadiumSelected;

        public BallType EquippedBall => SaveManager.Instance != null && SaveManager.Instance.CurrentData != null
            ? SaveManager.Instance.CurrentData.equippedBall
            : BallType.Classic;

        public StadiumType SelectedStadium => SaveManager.Instance != null && SaveManager.Instance.CurrentData != null
            ? SaveManager.Instance.CurrentData.selectedStadium
            : StadiumType.Cantera;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool EquipBall(BallType ball)
        {
            if (SaveManager.Instance == null) return false;

            if (!SaveManager.Instance.IsBallUnlocked(ball))
            {
                Debug.LogWarning($"[CustomizationManager] No puedes equipar un balón no desbloqueado: {ball}");
                return false;
            }

            SaveManager.Instance.EquipBall(ball);
            AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
            OnBallEquipped?.Invoke(ball);
            Debug.Log($"[CustomizationManager] Balón equipado: {ball}");
            return true;
        }

        public bool SelectStadium(StadiumType stadium)
        {
            if (SaveManager.Instance == null) return false;

            if (!SaveManager.Instance.IsStadiumUnlocked(stadium))
            {
                Debug.LogWarning($"[CustomizationManager] No puedes seleccionar un estadio no desbloqueado: {stadium}");
                return false;
            }

            SaveManager.Instance.SelectStadium(stadium);
            AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
            OnStadiumSelected?.Invoke(stadium);
            Debug.Log($"[CustomizationManager] Estadio seleccionado: {stadium}");
            return true;
        }
    }
}
