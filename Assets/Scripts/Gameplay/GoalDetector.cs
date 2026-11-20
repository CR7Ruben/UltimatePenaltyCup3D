#pragma warning disable 0649
using System;
using UnityEngine;
using UltimatePenaltyCup.Audio;

namespace UltimatePenaltyCup.Gameplay
{
    [RequireComponent(typeof(BoxCollider))]
    public class GoalDetector : MonoBehaviour
    {
        public static GoalDetector Instance { get; private set; }

        [Header("Goal Dimensions")]
        [SerializeField] private float goalHalfWidth = 3.66f;
        [SerializeField] private float goalHeight = 2.44f;
        [SerializeField] private float cornerThreshold = 0.95f;

        public event Action<bool, Vector3> OnGoalScored;

        private void Awake()
        {
            Instance = this;
            var col = GetComponent<BoxCollider>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            BallController ball = other.GetComponent<BallController>();
            if (ball != null && ball.HasBeenShot)
            {
                Vector3 entryPoint = ball.transform.position;
                Vector3 localPoint = transform.InverseTransformPoint(entryPoint);

                bool isCorner = CheckIfCornerOrPrecision(localPoint);

                AudioManager.Instance?.PlaySfx(SfxType.S02_Goal);
                OnGoalScored?.Invoke(isCorner, entryPoint);
            }
        }

        private bool CheckIfCornerOrPrecision(Vector3 localPoint)
        {
            // Upper corners: high Y and near left/right posts
            bool isHigh = localPoint.y > (goalHeight - cornerThreshold);
            bool isWide = Mathf.Abs(localPoint.x) > (goalHalfWidth - cornerThreshold);
            bool isLowCorner = localPoint.y < cornerThreshold && Mathf.Abs(localPoint.x) > (goalHalfWidth - cornerThreshold * 0.8f);

            return (isHigh && isWide) || isLowCorner;
        }
    }
}
