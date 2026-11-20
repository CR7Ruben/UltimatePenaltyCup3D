using System;
using UnityEngine;

namespace UltimatePenaltyCup.Core
{
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        public GameState CurrentState { get; private set; } = GameState.MainMenu;
        public GameState PreviousState { get; private set; } = GameState.MainMenu;

        public event Action<GameState, GameState> OnStateChanged;

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

        public void ChangeState(GameState newState)
        {
            if (CurrentState == newState) return;

            PreviousState = CurrentState;
            CurrentState = newState;

            Debug.Log($"[GameStateManager] State transition: {PreviousState} -> {CurrentState}");
            OnStateChanged?.Invoke(CurrentState, PreviousState);
        }

        public void RevertToPreviousState()
        {
            ChangeState(PreviousState);
        }
    }
}
