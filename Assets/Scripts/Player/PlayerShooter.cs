#pragma warning disable 0649
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UltimatePenaltyCup.Gameplay;
using UltimatePenaltyCup.Goalkeeper;
using UltimatePenaltyCup.SaveSystem;

namespace UltimatePenaltyCup.Player
{
    public class PlayerShooter : MonoBehaviour
    {
        public static PlayerShooter Instance { get; private set; }

        [Header("Aiming Parameters")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Transform aimReticle; // Circular 3D reticle on goal plane
        [SerializeField] private Renderer aimReticleRenderer;
        [SerializeField] private LineRenderer trajectoryLine;
        [SerializeField] private float goalPlaneZ = 0.0f;
        [SerializeField] private Vector2 aimBoundsX = new Vector2(-4.25f, 4.25f);
        [SerializeField] private Vector2 aimBoundsY = new Vector2(0.15f, 2.95f);

        [Header("Power Parameters")]
        [SerializeField] private float powerChargeRate = 1.15f;
        [SerializeField] private float minShotSpeed = 17.0f;
        [SerializeField] private float maxShotSpeed = 30.5f;
        [SerializeField] private float perfectZoneMin = 0.68f;
        [SerializeField] private float perfectZoneMax = 0.88f;

        public float CurrentPower { get; private set; } = 0f;
        public bool IsCharging { get; private set; } = false;
        public bool CanShoot { get; private set; } = true;
        public Vector3 CurrentAimTarget { get; private set; }

        public event Action<float> OnPowerChanged; // 0..1
        public event Action<float, bool, bool> OnPowerUpdated; // power, isPerfect, isOverpower
        public event Action<Vector3, float, bool> OnShotExecuted; // target, power, isHighPower

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private bool powerAscending = true;
        private float reticlePulseTimer = 0f;
        private float inputCooldownTimer = 0f;
        private Vector3 rawAimTarget;
        private MaterialPropertyBlock reticlePropBlock;
        private readonly Vector3[] trajectoryPoints = new Vector3[14];

        private void Awake()
        {
            Instance = this;
            if (mainCamera == null) mainCamera = Camera.main;
            if (aimReticle != null && aimReticleRenderer == null)
                aimReticleRenderer = aimReticle.GetComponent<Renderer>();
            reticlePropBlock = new MaterialPropertyBlock();
        }

        private void Start()
        {
            rawAimTarget = new Vector3(0f, 1.15f, goalPlaneZ);
            CurrentAimTarget = rawAimTarget;
            if (aimReticle != null) aimReticle.position = CurrentAimTarget;
        }

        private void Update()
        {
            if (inputCooldownTimer > 0f)
            {
                inputCooldownTimer = Mathf.Max(0f, inputCooldownTimer - Time.unscaledDeltaTime);
            }

            if (Time.timeScale <= 0f) return;
            if (PenaltyManager.Instance != null && !PenaltyManager.Instance.CanAcceptShotInput)
            {
                if (!IsCharging) return;
            }
            if (!CanShoot) return;

            UpdateAiming();
            UpdatePowerCharge();
            UpdateVisualFeedback();
        }

        private void UpdateAiming()
        {
            if (mainCamera == null) mainCamera = Camera.main;

            Vector2 mousePos = Mouse.current != null
                ? Mouse.current.position.ReadValue()
                : (Vector2)Input.mousePosition;

            float sensitivity = SaveManager.Instance != null && SaveManager.Instance.CurrentData != null
                ? Mathf.Clamp(SaveManager.Instance.CurrentData.aimSensitivity, 0.4f, 2.0f)
                : 1.0f;

            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 adjustedMouse = screenCenter + (mousePos - screenCenter) * sensitivity;

            Ray ray = mainCamera != null
                ? mainCamera.ScreenPointToRay(adjustedMouse)
                : new Ray(transform.position, Vector3.forward);

            Plane goalPlane = new Plane(Vector3.forward, new Vector3(0f, 0f, goalPlaneZ));

            if (goalPlane.Raycast(ray, out float enterDist))
            {
                Vector3 hitPoint = ray.GetPoint(enterDist);
                float clampedX = Mathf.Clamp(hitPoint.x, aimBoundsX.x, aimBoundsX.y);
                float clampedY = Mathf.Clamp(hitPoint.y, aimBoundsY.x, aimBoundsY.y);
                rawAimTarget = new Vector3(clampedX, clampedY, goalPlaneZ);
            }

            float aimBlend = 1f - Mathf.Exp(-24f * Time.deltaTime);
            CurrentAimTarget = Vector3.Lerp(CurrentAimTarget, rawAimTarget, aimBlend);
        }

        private void UpdatePowerCharge()
        {
            if (inputCooldownTimer > 0f && !IsCharging) return;

            // Ignore clicks over UI (like Pause button)
            if (!IsCharging && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            bool isMouseDown = Mouse.current != null
                ? Mouse.current.leftButton.isPressed
                : Input.GetMouseButton(0);

            bool isMouseUp = Mouse.current != null
                ? Mouse.current.leftButton.wasReleasedThisFrame
                : Input.GetMouseButtonUp(0);

            if (isMouseDown)
            {
                if (!IsCharging)
                {
                    IsCharging = true;
                    CurrentPower = 0.22f;
                    powerAscending = true;
                    PenaltyManager.Instance?.SetChargingState();
                }

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
                    if (CurrentPower <= 0.18f)
                    {
                        CurrentPower = 0.18f;
                        powerAscending = true;
                    }
                }

                bool isPerfect = CurrentPower >= perfectZoneMin && CurrentPower <= perfectZoneMax;
                bool isOver = CurrentPower > 0.92f;
                OnPowerChanged?.Invoke(CurrentPower);
                OnPowerUpdated?.Invoke(CurrentPower, isPerfect, isOver);
            }
            else if (IsCharging && isMouseUp)
            {
                ExecuteShot();
            }
        }

        private void UpdateVisualFeedback()
        {
            reticlePulseTimer += Time.deltaTime * 4f;

            if (aimReticle != null)
            {
                aimReticle.gameObject.SetActive(true);
                aimReticle.position = CurrentAimTarget + new Vector3(0f, 0f, -0.04f);

                float baseScale = IsCharging
                    ? Mathf.Lerp(0.72f, 0.42f, CurrentPower)
                    : 0.62f + Mathf.Sin(reticlePulseTimer) * 0.04f;

                if (IsCharging && CurrentPower > 0.92f)
                {
                    // Slight accuracy expansion when overcharging
                    baseScale += 0.18f;
                }
                aimReticle.localScale = Vector3.one * baseScale;

                if (aimReticleRenderer != null)
                {
                    if (reticlePropBlock == null) reticlePropBlock = new MaterialPropertyBlock();

                    Color col = new Color(0.08f, 0.55f, 1.0f, 0.95f); // Electric blue default
                    if (IsCharging)
                    {
                        if (CurrentPower > 0.92f) col = new Color(0.95f, 0.25f, 0.25f, 0.95f); // Red risk
                        else if (CurrentPower >= perfectZoneMin && CurrentPower <= perfectZoneMax) col = new Color(1.0f, 0.80f, 0.15f, 1.0f); // Gold sweet spot
                        else col = new Color(0.15f, 0.85f, 0.45f, 0.95f); // Green good
                    }
                    aimReticleRenderer.GetPropertyBlock(reticlePropBlock);
                    reticlePropBlock.SetColor(ColorId, col);
                    reticlePropBlock.SetColor(BaseColorId, col);
                    aimReticleRenderer.SetPropertyBlock(reticlePropBlock);
                }
            }

            if (trajectoryLine != null && BallController.Instance != null)
            {
                trajectoryLine.gameObject.SetActive(true);
                Vector3 startPos = BallController.Instance.transform.position;
                int count = trajectoryPoints.Length;
                if (trajectoryLine.positionCount != count)
                {
                    trajectoryLine.positionCount = count;
                }

                float previewPower = IsCharging ? Mathf.Clamp(CurrentPower, 0.22f, 1f) : 0.65f;
                Vector3 initialVel = ComputeCompensatedLaunchVelocity(startPos, CurrentAimTarget, previewPower, out float flightTime);
                float curlSign = -Mathf.Sign(CurrentAimTarget.x) * Mathf.Abs(CurrentAimTarget.x) * 0.03f * previewPower;

                for (int i = 0; i < count; i++)
                {
                    float u = i / (float)(count - 1);
                    float elapsed = u * flightTime;
                    Vector3 p = startPos + initialVel * elapsed + 0.5f * Physics.gravity * (elapsed * elapsed);
                    p.x += curlSign * Mathf.Sin(u * Mathf.PI);
                    trajectoryPoints[i] = p;
                }
                trajectoryLine.SetPositions(trajectoryPoints);
            }
        }

        private void ExecuteShot()
        {
            CanShoot = false;
            IsCharging = false;
            OnPowerChanged?.Invoke(0f);
            OnPowerUpdated?.Invoke(0f, false, false);

            if (aimReticle != null) aimReticle.gameObject.SetActive(false);
            if (trajectoryLine != null) trajectoryLine.gameObject.SetActive(false);

            PenaltyManager.Instance?.OnShotKickingStarted();

            Vector3 ballPos = BallController.Instance != null ? BallController.Instance.transform.position : new Vector3(0f, 0.11f, -11f);

            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.PlayKickAnimation(ballPos, () =>
                {
                    PerformPhysicsKick(ballPos);
                });
            }
            else
            {
                PerformPhysicsKick(ballPos);
            }
        }

        /// <summary>
        /// Fires a real physics shot through the full PlayerShooter -> BallController -> GoalkeeperAI -> GoalDetector pipeline.
        /// Used by automated Play Mode simulation tests.
        /// </summary>
        public void ExecuteAutomatedShot(Vector3 targetOnGoalPlane, float normalizedPower, bool skipRunUp = false)
        {
            CanShoot = false;
            IsCharging = false;
            CurrentAimTarget = targetOnGoalPlane;
            rawAimTarget = targetOnGoalPlane;
            CurrentPower = Mathf.Clamp(normalizedPower, 0.22f, 1.0f);

            OnPowerChanged?.Invoke(0f);
            OnPowerUpdated?.Invoke(0f, false, false);

            if (aimReticle != null) aimReticle.gameObject.SetActive(false);
            if (trajectoryLine != null) trajectoryLine.gameObject.SetActive(false);

            PenaltyManager.Instance?.OnShotKickingStarted();

            Vector3 ballPos = BallController.Instance != null ? BallController.Instance.transform.position : new Vector3(0f, 0.11f, -11f);

            if (!skipRunUp && PlayerController.Instance != null)
            {
                PlayerController.Instance.PlayKickAnimation(ballPos, () => PerformPhysicsKick(ballPos));
            }
            else
            {
                PerformPhysicsKick(ballPos);
            }
        }

        private Vector3 ComputeCompensatedLaunchVelocity(Vector3 ballPos, Vector3 target, float normalizedPower, out float timeOfFlight)
        {
            float speed = Mathf.Lerp(minShotSpeed, maxShotSpeed, Mathf.Clamp01(normalizedPower));
            Vector3 delta = target - ballPos;
            Vector3 dir = delta.normalized;

            float distanceZ = Mathf.Abs(goalPlaneZ - ballPos.z);
            float forwardSpeed = Mathf.Max(8f, speed * dir.z);
            timeOfFlight = distanceZ / forwardSpeed;

            // Slight linear damping compensation (~4.5%) so the ball lands accurately on reticle
            const float dragComp = 1.045f;
            float vx = (delta.x / timeOfFlight) * dragComp;
            float vz = (delta.z / timeOfFlight) * dragComp;
            float vy = (delta.y + 0.5f * 9.81f * timeOfFlight * timeOfFlight) / timeOfFlight;

            return new Vector3(vx, vy, vz);
        }

        private void PerformPhysicsKick(Vector3 ballPos)
        {
            if (BallController.Instance == null) return;

            bool isPerfect = CurrentPower >= perfectZoneMin && CurrentPower <= perfectZoneMax;
            bool isOver = CurrentPower > 0.92f;

            // Distinct accuracy & spread by power zone
            Vector3 target = CurrentAimTarget;
            if (isOver)
            {
                target.x += UnityEngine.Random.Range(-0.48f, 0.48f);
                target.y += UnityEngine.Random.Range(0.18f, 0.68f);
            }
            else if (!isPerfect)
            {
                target.x += UnityEngine.Random.Range(-0.14f, 0.14f);
                target.y += UnityEngine.Random.Range(-0.08f, 0.10f);
            }

            Vector3 velocity = ComputeCompensatedLaunchVelocity(ballPos, target, CurrentPower, out _);

            float spinY = -target.x * 5.0f;
            Vector3 torque = new Vector3(0f, spinY, 0f);

            bool isHighPower = CurrentPower >= 0.82f;

            BallController.Instance.Launch(velocity, torque);
            GoalkeeperAI.Instance?.OnShotFired(ballPos, velocity, CurrentPower);
            PenaltyManager.Instance?.OnShotFired(target, CurrentPower, isHighPower);
            MatchCameraController.Instance?.StartTrackingShot();

            OnShotExecuted?.Invoke(target, CurrentPower, isHighPower);
        }

        public void SetInputLocked(bool locked)
        {
            CanShoot = !locked;
            if (locked)
            {
                IsCharging = false;
                if (aimReticle != null) aimReticle.gameObject.SetActive(false);
                if (trajectoryLine != null) trajectoryLine.gameObject.SetActive(false);
                OnPowerChanged?.Invoke(0f);
                OnPowerUpdated?.Invoke(0f, false, false);
            }
        }

        public void ResetShooter()
        {
            CanShoot = true;
            IsCharging = false;
            CurrentPower = 0f;
            powerAscending = true;
            inputCooldownTimer = 0.20f;
            OnPowerChanged?.Invoke(0f);
            OnPowerUpdated?.Invoke(0f, false, false);

            if (aimReticle != null) aimReticle.gameObject.SetActive(true);
            if (trajectoryLine != null) trajectoryLine.gameObject.SetActive(true);
        }

        public void SetAimReticle(Transform reticle, LineRenderer line)
        {
            aimReticle = reticle;
            trajectoryLine = line;
            if (aimReticle != null) aimReticleRenderer = aimReticle.GetComponent<Renderer>();
        }
    }
}
