#pragma warning disable 0649
using UnityEngine;

namespace UltimatePenaltyCup.Gameplay
{
    public class MatchCameraController : MonoBehaviour
    {
        public static MatchCameraController Instance { get; private set; }

        [Header("Broadcast Penalty Framing")]
        [SerializeField] private Vector3 aimingPosition = new Vector3(0.55f, 2.05f, -15.4f);
        [SerializeField] private Vector3 aimingLookTarget = new Vector3(0f, 1.25f, 0f);
        [SerializeField] private Vector3 trackingDollyOffset = new Vector3(0f, 0.15f, 1.4f);

        [SerializeField] private float followSpeed = 4.5f;
        [SerializeField] private float aimFov = 46f;
        [SerializeField] private float actionFov = 41f;

        private Camera cam;
        private bool isTrackingBall = false;
        private Transform ballTransform;

        private void Awake()
        {
            Instance = this;
            cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
        }

        private void Start()
        {
            ResetCameraToKicker();

            if (PenaltyManager.Instance != null)
            {
                PenaltyManager.Instance.OnAttemptStarted += HandleAttemptStarted;
            }

            if (BallController.Instance != null)
            {
                ballTransform = BallController.Instance.transform;
            }
        }

        private void OnDestroy()
        {
            if (PenaltyManager.Instance != null)
            {
                PenaltyManager.Instance.OnAttemptStarted -= HandleAttemptStarted;
            }
        }

        private void HandleAttemptStarted(int attempt, int max)
        {
            ResetCameraToKicker();
        }

        public void StartTrackingShot()
        {
            if (BallController.Instance != null)
            {
                ballTransform = BallController.Instance.transform;
                isTrackingBall = true;
            }
        }

        private void LateUpdate()
        {
            if (!isTrackingBall || ballTransform == null) return;

            Vector3 targetPos = aimingPosition + trackingDollyOffset;
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 2.2f);

            // Clamped tracking between goal center and ball so framing never turns violently away from goal
            Vector3 lookPoint = Vector3.Lerp(aimingLookTarget, ballTransform.position, 0.55f);
            lookPoint.y = Mathf.Clamp(lookPoint.y, 0.5f, 2.8f);
            lookPoint.x = Mathf.Clamp(lookPoint.x, -4.5f, 4.5f);

            Quaternion targetRot = Quaternion.LookRotation(lookPoint - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * followSpeed);

            if (cam != null)
            {
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, actionFov, Time.deltaTime * 2.8f);
            }
        }

        public void ResetCameraToKicker()
        {
            isTrackingBall = false;
            transform.position = aimingPosition;
            transform.LookAt(aimingLookTarget);

            if (cam != null)
            {
                cam.fieldOfView = aimFov;
            }
        }
    }
}
