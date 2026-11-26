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
            CheckGoalCross(other);
        }

        private void OnTriggerStay(Collider other)
        {
            CheckGoalCross(other);
        }

        private void FixedUpdate()
        {
            BallController ball = BallController.Instance;
            if (ball == null || !ball.HasBeenShot || ball.WasSavedByGoalkeeper || ball.GoalScoredThisShot) return;

            Vector3 prevPos = ball.PreviousPosition;
            Vector3 currPos = ball.transform.position;

            // Swept goal-line crossing backup check to guarantee zero tunneling on high-velocity shots
            if (prevPos.z <= 0.08f && currPos.z > 0.08f)
            {
                float denom = currPos.z - prevPos.z;
                float t = denom > 0.0001f ? Mathf.Clamp01((0.08f - prevPos.z) / denom) : 1f;
                Vector3 crossPoint = Vector3.Lerp(prevPos, currPos, t);

                if (Mathf.Abs(crossPoint.x) <= goalHalfWidth - 0.05f &&
                    crossPoint.y <= goalHeight - 0.05f &&
                    crossPoint.y >= -0.15f)
                {
                    ScoreGoal(ball, currPos);
                }
            }
        }

        private void CheckGoalCross(Collider other)
        {
            BallController ball = other.GetComponent<BallController>();
            if (ball == null || !ball.HasBeenShot || ball.WasSavedByGoalkeeper || ball.GoalScoredThisShot) return;

            Vector3 entryPoint = ball.transform.position;
            // Ball must have crossed the goal line cleanly inside the frame
            if (entryPoint.z < 0.06f) return;
            if (Mathf.Abs(entryPoint.x) > goalHalfWidth - 0.05f) return;
            if (entryPoint.y > goalHeight - 0.05f || entryPoint.y < -0.15f) return;

            ScoreGoal(ball, entryPoint);
        }

        private void ScoreGoal(BallController ball, Vector3 entryPoint)
        {
            if (ball.GoalScoredThisShot) return;
            ball.MarkGoalScored();

            Vector3 localPoint = transform.InverseTransformPoint(entryPoint);
            bool isCorner = CheckIfCornerOrPrecision(localPoint);

            GoalNetRipple.Instance?.TriggerImpact(entryPoint, ball.Velocity);
            AudioManager.Instance?.PlaySfx(SfxType.S02_Goal);
            OnGoalScored?.Invoke(isCorner, entryPoint);
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
