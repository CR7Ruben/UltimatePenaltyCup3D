#pragma warning disable 0649
using System.Collections;
using UnityEngine;

namespace UltimatePenaltyCup.Gameplay
{
    public class MatchCameraController : MonoBehaviour
    {
        public static MatchCameraController Instance { get; private set; }

        [Header("Camera Positions")]
        [SerializeField] private Vector3 aimingPosition = new Vector3(0f, 2.7f, -16.4f);
        [SerializeField] private Vector3 aimingRotation = new Vector3(8.5f, 0f, 0f);

        [SerializeField] private float followSpeed = 6.0f;
        [SerializeField] private float aimFov = 55f;
        [SerializeField] private float actionFov = 50f;

        private Camera cam;
        private bool isTrackingBall = false;
        private Transform ballTransform;
        private Vector3 startLocalPos;

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
                PenaltyManager.Instance.OnAttemptStarted += (att, max) => ResetCameraToKicker();
            }

            if (BallController.Instance != null)
            {
                ballTransform = BallController.Instance.transform;
            }
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
            if (isTrackingBall && ballTransform != null)
            {
                // Smoothly look towards the ball in flight with dynamic framing
                Vector3 toBall = ballTransform.position - transform.position;
                Quaternion targetRot = Quaternion.LookRotation(toBall);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * followSpeed);

                // Subtle FOV zoom
                if (cam != null)
                {
                    cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, actionFov, Time.deltaTime * 3f);
                }
            }
        }

        public void ResetCameraToKicker()
        {
            isTrackingBall = false;
            transform.position = aimingPosition;
            transform.rotation = Quaternion.Euler(aimingRotation);

            if (cam != null)
            {
                cam.fieldOfView = aimFov;
            }
        }
    }
}
