#pragma warning disable 0649
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UltimatePenaltyCup.Gameplay;
using UltimatePenaltyCup.Goalkeeper;

namespace UltimatePenaltyCup.Player
{
    public class PlayerShooter : MonoBehaviour
    {
        public static PlayerShooter Instance { get; private set; }

        [Header("Aiming Parameters")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Transform aimReticle; // 3D reticle on goal face
        [SerializeField] private LineRenderer trajectoryLine;
        [SerializeField] private float goalPlaneZ = 0.0f;
        [SerializeField] private Vector2 aimBoundsX = new Vector2(-3.5f, 3.5f);
        [SerializeField] private Vector2 aimBoundsY = new Vector2(0.2f, 2.35f);

        [Header("Power Parameters")]
        [SerializeField] private float powerChargeRate = 1.35f;
        [SerializeField] private float minShotSpeed = 15f;
        [SerializeField] private float maxShotSpeed = 31f;

        public float CurrentPower { get; private set; } = 0f;
        public bool IsCharging { get; private set; } = false;
        public bool CanShoot { get; private set; } = true;
        public Vector3 CurrentAimTarget { get; private set; }

        public event Action<float> OnPowerChanged; // 0..1
        public event Action<Vector3, float, bool> OnShotExecuted; // target, power, isHighPower

        private bool powerAscending = true;

        private void Awake()
        {
            Instance = this;
            if (mainCamera == null) mainCamera = Camera.main;
        }

        private void Start()
        {
            CurrentAimTarget = new Vector3(0f, 1.2f, goalPlaneZ);
            if (aimReticle != null) aimReticle.position = CurrentAimTarget;
        }

        private void Update()
        {
            if (!CanShoot) return;

            UpdateAiming();
            UpdatePowerCharge();
        }

        private void UpdateAiming()
        {
            Vector2 mousePos = Vector2.zero;
            if (Mouse.current != null)
            {
                mousePos = Mouse.current.position.ReadValue();
            }
            else
            {
                mousePos = Input.mousePosition;
            }

            Ray ray = mainCamera != null ? mainCamera.ScreenPointToRay(mousePos) : new Ray(transform.position, Vector3.forward);
            Plane goalPlane = new Plane(Vector3.forward, new Vector3(0f, 0f, goalPlaneZ));

            if (goalPlane.Raycast(ray, out float enterDist))
            {
                Vector3 hitPoint = ray.GetPoint(enterDist);
                float clampedX = Mathf.Clamp(hitPoint.x, aimBoundsX.x, aimBoundsX.y);
                float clampedY = Mathf.Clamp(hitPoint.y, aimBoundsY.x, aimBoundsY.y);
                CurrentAimTarget = new Vector3(clampedX, clampedY, goalPlaneZ);

                if (aimReticle != null)
                {
                    aimReticle.position = CurrentAimTarget;
                    aimReticle.gameObject.SetActive(true);
                }

                if (trajectoryLine != null && BallController.Instance != null)
                {
                    trajectoryLine.gameObject.SetActive(true);
                    trajectoryLine.SetPosition(0, BallController.Instance.transform.position);
                    trajectoryLine.SetPosition(1, CurrentAimTarget);
                }
            }
        }

        private void UpdatePowerCharge()
        {
            bool isMouseDown = false;
            bool isMouseUp = false;

            if (Mouse.current != null)
            {
                isMouseDown = Mouse.current.leftButton.isPressed;
                isMouseUp = Mouse.current.leftButton.wasReleasedThisFrame;
            }
            else
            {
                isMouseDown = Input.GetMouseButton(0);
                isMouseUp = Input.GetMouseButtonUp(0);
            }

            if (isMouseDown)
            {
                IsCharging = true;
                if (powerAscending)
                {
                    CurrentPower += Time.deltaTime * powerChargeRate;
                    if (CurrentPower >= 1f)
                    {
                        CurrentPower = 1f;
                        powerAscending = false;
                    }
                }
                else
                {
                    CurrentPower -= Time.deltaTime * powerChargeRate;
                    if (CurrentPower <= 0.1f)
                    {
                        CurrentPower = 0.1f;
                        powerAscending = true;
                    }
                }
                OnPowerChanged?.Invoke(CurrentPower);
            }
            else if (IsCharging && isMouseUp)
            {
                ExecuteShot();
            }
        }

        private void ExecuteShot()
        {
            CanShoot = false;
            IsCharging = false;
            OnPowerChanged?.Invoke(0f);

            if (aimReticle != null) aimReticle.gameObject.SetActive(false);
            if (trajectoryLine != null) trajectoryLine.gameObject.SetActive(false);

            Vector3 ballPos = BallController.Instance != null ? BallController.Instance.transform.position : transform.position;

            PlayerController.Instance?.PlayKickAnimation(ballPos, () =>
            {
                PerformPhysicsKick(ballPos);
            });
        }

        private void PerformPhysicsKick(Vector3 ballPos)
        {
            if (BallController.Instance == null) return;

            float speed = Mathf.Lerp(minShotSpeed, maxShotSpeed, CurrentPower);
            Vector3 toTarget = (CurrentAimTarget - ballPos).normalized;

            // Elevation curve adjustment based on distance and gravity
            float distanceZ = Mathf.Abs(goalPlaneZ - ballPos.z);
            float timeOfFlight = distanceZ / (speed * toTarget.z);
            float gravityDrop = 0.5f * 9.81f * timeOfFlight * timeOfFlight;

            Vector3 velocity = toTarget * speed;
            velocity.y += (gravityDrop / Mathf.Max(0.1f, timeOfFlight)) * 0.45f;

            // Over-power risk if power > 0.96f
            if (CurrentPower > 0.96f)
            {
                velocity.y += UnityEngine.Random.Range(0.6f, 1.4f);
            }

            // Spin torque for ball curve
            float spinX = CurrentAimTarget.x * 15f;
            Vector3 torque = new Vector3(UnityEngine.Random.Range(-5f, 5f), spinX, UnityEngine.Random.Range(-5f, 5f));

            bool isHighPower = CurrentPower >= 0.85f;

            BallController.Instance.Launch(velocity, torque);
            GoalkeeperAI.Instance?.OnShotFired(ballPos, velocity, CurrentPower);
            PenaltyManager.Instance?.OnShotFired(CurrentAimTarget, CurrentPower, isHighPower);
            MatchCameraController.Instance?.StartTrackingShot();

            OnShotExecuted?.Invoke(CurrentAimTarget, CurrentPower, isHighPower);
        }

        public void ResetShooter()
        {
            CanShoot = true;
            IsCharging = false;
            CurrentPower = 0f;
            powerAscending = true;
            OnPowerChanged?.Invoke(0f);

            if (aimReticle != null) aimReticle.gameObject.SetActive(true);
            if (trajectoryLine != null) trajectoryLine.gameObject.SetActive(true);
        }

        public void SetAimReticle(Transform reticle, LineRenderer line)
        {
            aimReticle = reticle;
            trajectoryLine = line;
        }
    }
}
