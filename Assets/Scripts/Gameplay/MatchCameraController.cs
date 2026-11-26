#pragma warning disable 0649
using UnityEngine;
using UltimatePenaltyCup.Player;

namespace UltimatePenaltyCup.Gameplay
{
    public class MatchCameraController : MonoBehaviour
    {
        public static MatchCameraController Instance { get; private set; }
        public static bool SkipIntroSweepForTests { get; set; } = false;

        [Header("Broadcast Penalty Framing")]
        [SerializeField] private Vector3 aimingPosition = new Vector3(0.58f, 1.92f, -17.35f);
        [SerializeField] private Vector3 aimingLookTarget = new Vector3(-0.08f, 1.05f, 0f);
        [SerializeField] private Vector3 trackingDollyOffset = new Vector3(-0.10f, 0.12f, 1.85f);

        [SerializeField] private float followSpeed = 5.5f;
        [SerializeField] private float aimFov = 49f;
        [SerializeField] private float actionFov = 42f;

        private Camera cam;
        private bool isTrackingBall = false;
        private bool isReturningToKicker = false;
        private bool isPlayingIntro = false;
        private float introTimer = 0f;
        private Vector3 positionVelocity;
        private Transform ballTransform;

        private void Awake()
        {
            Instance = this;
            cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
        }

        private void Start()
        {
            if (BallController.Instance != null)
            {
                ballTransform = BallController.Instance.transform;
            }
            if (PenaltyManager.Instance != null)
            {
                PenaltyManager.Instance.OnAttemptStarted += HandleAttemptStarted;
            }

            if (!SkipIntroSweepForTests && !Application.isBatchMode)
            {
                isPlayingIntro = true;
                introTimer = 0f;
                transform.position = new Vector3(4.5f, 5.2f, -20.5f);
                transform.LookAt(new Vector3(0f, 1.5f, 2f));
                if (cam != null) cam.fieldOfView = 56f;
            }
            else
            {
                ResetCameraToKicker(instant: true);
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
            ResetCameraToKicker(instant: false);
        }

        public void StartTrackingShot()
        {
            if (BallController.Instance != null)
            {
                ballTransform = BallController.Instance.transform;
                isTrackingBall = true;
                isReturningToKicker = false;
            }
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            if (isPlayingIntro)
            {
                introTimer += dt;
                bool skipPressed = (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
                                || (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame);
                if (skipPressed || introTimer >= 1.15f)
                {
                    isPlayingIntro = false;
                    ResetCameraToKicker(instant: false);
                }
                else
                {
                    float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(introTimer / 1.15f));
                    Vector3 startIntroPos = new Vector3(4.5f, 5.2f, -20.5f);
                    transform.position = Vector3.Lerp(startIntroPos, aimingPosition, u);
                    Vector3 lookP = Vector3.Lerp(new Vector3(0f, 1.5f, 2f), aimingLookTarget, u);
                    transform.rotation = Quaternion.LookRotation(lookP - transform.position);
                    if (cam != null) cam.fieldOfView = Mathf.Lerp(56f, aimFov, u);
                    return;
                }
            }

            if (isTrackingBall && ballTransform != null)
            {
                Vector3 targetPos = aimingPosition + trackingDollyOffset;
                transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref positionVelocity, 0.28f);

                // Clamped tracking between goal center and ball so framing never turns violently away from goal
                Vector3 lookPoint = Vector3.Lerp(aimingLookTarget, ballTransform.position, 0.55f);
                lookPoint.y = Mathf.Clamp(lookPoint.y, 0.5f, 2.8f);
                lookPoint.x = Mathf.Clamp(lookPoint.x, -4.5f, 4.5f);

                Quaternion targetRot = Quaternion.LookRotation(lookPoint - transform.position);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 1f - Mathf.Exp(-followSpeed * dt));

                if (cam != null)
                {
                    cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, actionFov, 1f - Mathf.Exp(-3.2f * dt));
                }
            }
            else
            {
                // Subtle broadcast aiming parallax (keeps reticle & player clean and natural)
                Vector3 parallaxLook = aimingLookTarget;
                if (PlayerShooter.Instance != null && PlayerShooter.Instance.CanShoot)
                {
                    Vector3 aim = PlayerShooter.Instance.CurrentAimTarget;
                    parallaxLook.x += Mathf.Clamp(aim.x * 0.035f, -0.15f, 0.15f);
                    parallaxLook.y += Mathf.Clamp((aim.y - 1.25f) * 0.035f, -0.06f, 0.06f);
                }

                Quaternion desiredAimRot = Quaternion.LookRotation(parallaxLook - aimingPosition);

                if (isReturningToKicker)
                {
                    transform.position = Vector3.SmoothDamp(transform.position, aimingPosition, ref positionVelocity, 0.24f);
                    transform.rotation = Quaternion.Slerp(transform.rotation, desiredAimRot, 1f - Mathf.Exp(-8f * dt));

                    if (cam != null)
                    {
                        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, aimFov, 1f - Mathf.Exp(-6f * dt));
                    }

                    if ((transform.position - aimingPosition).sqrMagnitude < 0.0004f &&
                        Quaternion.Angle(transform.rotation, desiredAimRot) < 0.25f)
                    {
                        transform.position = aimingPosition;
                        transform.rotation = desiredAimRot;
                        if (cam != null) cam.fieldOfView = aimFov;
                        isReturningToKicker = false;
                    }
                }
                else
                {
                    transform.position = aimingPosition;
                    transform.rotation = Quaternion.Slerp(transform.rotation, desiredAimRot, 1f - Mathf.Exp(-10f * dt));
                }
            }
        }

        public void ResetCameraToKicker(bool instant = false)
        {
            isPlayingIntro = false;
            isTrackingBall = false;
            positionVelocity = Vector3.zero;

            if (instant)
            {
                isReturningToKicker = false;
                transform.position = aimingPosition;
                transform.LookAt(aimingLookTarget);

                if (cam != null)
                {
                    cam.fieldOfView = aimFov;
                }
            }
            else
            {
                isReturningToKicker = true;
            }
        }
    }
}
