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
        [SerializeField] private Transform directionArrowRoot;
        [SerializeField] private Renderer[] directionArrowRenderers;
        [SerializeField] private float goalPlaneZ = 0.0f;
        [SerializeField] private Vector2 aimBoundsX = new Vector2(-4.25f, 4.25f);
        [SerializeField] private Vector2 aimBoundsY = new Vector2(0.15f, 2.95f);

        [Header("Power Parameters")]
        [SerializeField] private float powerChargeRate = 1.15f;
        [SerializeField] private float minShotSpeed = 17.5f;
        [SerializeField] private float maxShotSpeed = 30.5f;
        [SerializeField] private float perfectZoneMin = 0.68f;
        [SerializeField] private float perfectZoneMax = 0.88f;

        public float CurrentPower { get; private set; } = 0f;
        public bool IsCharging { get; private set; } = false;
        public bool CanShoot { get; private set; } = true;
        public Vector3 CurrentAimTarget { get; private set; }
        public Vector3 LastPredictedLaunchVelocity { get; private set; }

        public event Action<float> OnPowerChanged; // 0..1
        public event Action<float, bool, bool> OnPowerUpdated; // power, isPerfect, isOverpower
        public event Action<Vector3, float, bool> OnShotExecuted; // target, power, isHighPower

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private bool powerAscending = true;
        private float reticlePulseTimer = 0f;
        private float inputCooldownTimer = 0f;
        private Vector3 rawAimTarget;
        private Vector2 chargeStartMousePos;
        private bool aimUnlockedDuringCharge = false;
        private MaterialPropertyBlock reticlePropBlock;
        private MaterialPropertyBlock arrowPropBlock;
        private readonly Vector3[] trajectoryPoints = new Vector3[20];
        private readonly Vector3[] simBuffer = new Vector3[160];

        private void Awake()
        {
            Instance = this;
            if (mainCamera == null) mainCamera = Camera.main;
            if (aimReticle != null && aimReticleRenderer == null)
                aimReticleRenderer = aimReticle.GetComponent<Renderer>();
            if (directionArrowRoot != null && (directionArrowRenderers == null || directionArrowRenderers.Length == 0))
                directionArrowRenderers = directionArrowRoot.GetComponentsInChildren<Renderer>(true);
            reticlePropBlock = new MaterialPropertyBlock();
            arrowPropBlock = new MaterialPropertyBlock();
        }

        private void Start()
        {
            rawAimTarget = new Vector3(0f, 1.15f, goalPlaneZ);
            CurrentAimTarget = rawAimTarget;
            if (aimReticle != null) aimReticle.position = CurrentAimTarget;
            UpdateVisualFeedback();
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

            // Keep aim steady while charging power unless player deliberately drags mouse (>36px)
            if (IsCharging && !aimUnlockedDuringCharge)
            {
                if (Vector2.Distance(mousePos, chargeStartMousePos) < 36f)
                {
                    return;
                }
                aimUnlockedDuringCharge = true;
            }

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
                    aimUnlockedDuringCharge = false;
                    chargeStartMousePos = Mouse.current != null
                        ? Mouse.current.position.ReadValue()
                        : (Vector2)Input.mousePosition;
                    // Snap aim target cleanly to current cursor aim at start of charge
                    CurrentAimTarget = rawAimTarget;
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

        private Color GetFeedbackColor()
        {
            if (!IsCharging) return new Color(0.08f, 0.72f, 1.0f, 0.95f); // Crisp cyan default
            if (CurrentPower > 0.92f) return new Color(0.95f, 0.25f, 0.25f, 0.95f); // Red overpower risk
            if (CurrentPower >= perfectZoneMin && CurrentPower <= perfectZoneMax) return new Color(1.0f, 0.80f, 0.15f, 1.0f); // Gold sweet spot
            return new Color(0.15f, 0.85f, 0.45f, 0.95f); // Emerald normal
        }

        private void UpdateVisualFeedback()
        {
            reticlePulseTimer += Time.deltaTime * 4f;
            Color col = GetFeedbackColor();

            if (aimReticle != null)
            {
                aimReticle.gameObject.SetActive(true);
                aimReticle.position = CurrentAimTarget + new Vector3(0f, 0f, -0.04f);

                float baseScale = IsCharging
                    ? Mathf.Lerp(0.72f, 0.42f, CurrentPower)
                    : 0.62f + Mathf.Sin(reticlePulseTimer) * 0.04f;

                if (IsCharging && CurrentPower > 0.92f)
                {
                    baseScale += 0.14f;
                }
                aimReticle.localScale = Vector3.one * baseScale;

                if (aimReticleRenderer != null)
                {
                    if (reticlePropBlock == null) reticlePropBlock = new MaterialPropertyBlock();
                    aimReticleRenderer.GetPropertyBlock(reticlePropBlock);
                    reticlePropBlock.SetColor(ColorId, col);
                    reticlePropBlock.SetColor(BaseColorId, col);
                    aimReticleRenderer.SetPropertyBlock(reticlePropBlock);
                }
            }

            if (BallController.Instance != null)
            {
                Vector3 startPos = BallController.Instance.transform.position;
                float previewPower = IsCharging ? Mathf.Clamp(CurrentPower, 0.22f, 1f) : 0.65f;

                ComputeShotSolution(startPos, CurrentAimTarget, previewPower, out Vector3 launchVel, out Vector3 spinVel, out _);
                LastPredictedLaunchVelocity = launchVel;

                // True simulated physics curve landing precisely on CurrentAimTarget (ZERO fake lateral curl!)
                if (trajectoryLine != null)
                {
                    trajectoryLine.gameObject.SetActive(true);
                    int count = trajectoryPoints.Length;
                    if (trajectoryLine.positionCount != count)
                    {
                        trajectoryLine.positionCount = count;
                    }
                    SampleSimulatedTrajectory(startPos, launchVel, spinVel, trajectoryPoints);
                    trajectoryLine.startColor = new Color(col.r, col.g, col.b, 0.88f);
                    trajectoryLine.endColor = new Color(col.r, col.g, col.b, 0.28f);
                    trajectoryLine.SetPositions(trajectoryPoints);
                }

                // 3D Ground-to-Goal Directional Arrow strictly aligned to actual initial launch vector
                if (directionArrowRoot != null)
                {
                    directionArrowRoot.gameObject.SetActive(true);
                    Vector3 aimDir = (CurrentAimTarget - startPos).normalized;
                    Vector3 arrowAnchor = startPos + new Vector3(0f, 0.02f, 0f);
                    directionArrowRoot.position = arrowAnchor;
                    directionArrowRoot.rotation = Quaternion.LookRotation(aimDir, Vector3.up);
                    float lengthScale = Mathf.Lerp(0.85f, 1.30f, previewPower);
                    directionArrowRoot.localScale = new Vector3(1f, 1f, lengthScale);

                    if (directionArrowRenderers != null)
                    {
                        if (arrowPropBlock == null) arrowPropBlock = new MaterialPropertyBlock();
                        foreach (var r in directionArrowRenderers)
                        {
                            if (r == null) continue;
                            r.GetPropertyBlock(arrowPropBlock);
                            arrowPropBlock.SetColor(ColorId, col);
                            arrowPropBlock.SetColor(BaseColorId, col);
                            r.SetPropertyBlock(arrowPropBlock);
                        }
                    }
                }
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
            if (directionArrowRoot != null) directionArrowRoot.gameObject.SetActive(false);

            PenaltyManager.Instance?.OnShotKickingStarted();

            Vector3 ballPos = BallController.Instance != null ? BallController.Instance.transform.position : new Vector3(0f, 0.115f, -11f);
            GoalkeeperAI.Instance?.OnKickerRunUpStarted(ballPos);

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
            if (directionArrowRoot != null) directionArrowRoot.gameObject.SetActive(false);

            PenaltyManager.Instance?.OnShotKickingStarted();

            Vector3 ballPos = BallController.Instance != null ? BallController.Instance.transform.position : new Vector3(0f, 0.115f, -11f);
            GoalkeeperAI.Instance?.OnKickerRunUpStarted(ballPos);

            if (!skipRunUp && PlayerController.Instance != null)
            {
                PlayerController.Instance.PlayKickAnimation(ballPos, () => PerformPhysicsKick(ballPos));
            }
            else
            {
                PerformPhysicsKick(ballPos);
            }
        }

        /// <summary>
        /// Computes exact launch velocity & spin compensated for PhysX damping + gravity + Magnus
        /// so both visual trajectory and actual physics kick land on the exact intended target.
        /// </summary>
        public void ComputeShotSolution(Vector3 ballPos, Vector3 target, float normalizedPower,
            out Vector3 launchVelocity, out Vector3 angularVelocity, out float flightTime)
        {
            float p = Mathf.Clamp01(normalizedPower);
            float speed = Mathf.Lerp(minShotSpeed, maxShotSpeed, p);
            Vector3 delta = target - ballPos;
            Vector3 dir = delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector3.forward;

            float distanceZ = Mathf.Max(0.5f, Mathf.Abs(goalPlaneZ - ballPos.z));
            float forwardSpeed = Mathf.Max(8f, speed * Mathf.Max(0.25f, dir.z));
            float tEst = distanceZ / forwardSpeed;

            angularVelocity = new Vector3(0f, -target.x * 2.2f, 0f);
            float vx = delta.x / tEst;
            float vz = delta.z / tEst;
            float vy = (delta.y + 0.5f * 9.81f * tEst * tEst) / tEst;
            launchVelocity = new Vector3(vx, vy, vz);

            // 2 fixed-point feedback steps matching PhysX discrete FixedUpdate(0.02s) exactly
            for (int iter = 0; iter < 2; iter++)
            {
                SimulateToGoalPlane(ballPos, launchVelocity, angularVelocity, goalPlaneZ, out Vector3 hitPos, out float simT);
                float tCorr = Mathf.Max(0.15f, simT);
                Vector3 err = target - hitPos;
                launchVelocity.x += err.x / tCorr;
                launchVelocity.y += err.y / tCorr;
            }

            SimulateToGoalPlane(ballPos, launchVelocity, angularVelocity, goalPlaneZ, out _, out flightTime);
        }

        public void SimulateToGoalPlane(Vector3 startPos, Vector3 initialVelocity, Vector3 angularVelocity, float targetZ, out Vector3 hitPos, out float hitTime)
        {
            const float dt = 0.02f;
            float dampMul = Mathf.Clamp01(1f - BallController.LinearDampingValue * dt);
            Vector3 pos = startPos;
            Vector3 vel = initialVelocity;
            hitTime = 0f;

            for (int step = 0; step < 150; step++)
            {
                Vector3 prevPos = pos;
                float prevT = hitTime;

                Vector3 magnus = BallController.ComputeMagnusAccel(vel, angularVelocity);
                vel += (Physics.gravity + magnus) * dt;
                vel *= dampMul;
                pos += vel * dt;
                hitTime += dt;

                if (pos.z >= targetZ)
                {
                    float denom = pos.z - prevPos.z;
                    float frac = Mathf.Abs(denom) > 0.0001f ? Mathf.Clamp01((targetZ - prevPos.z) / denom) : 1f;
                    hitPos = Vector3.Lerp(prevPos, pos, frac);
                    hitTime = Mathf.Lerp(prevT, hitTime, frac);
                    return;
                }
            }

            hitPos = pos;
        }

        public void SampleSimulatedTrajectory(Vector3 startPos, Vector3 initialVelocity, Vector3 angularVelocity, Vector3[] outPoints)
        {
            if (outPoints == null || outPoints.Length < 2) return;

            const float dt = 0.015f;
            float dampMul = Mathf.Clamp01(1f - BallController.LinearDampingValue * dt);
            Vector3 pos = startPos;
            Vector3 vel = initialVelocity;

            int rawCount = 1;
            simBuffer[0] = pos;

            for (int step = 1; step < simBuffer.Length; step++)
            {
                Vector3 prevPos = pos;
                Vector3 magnus = BallController.ComputeMagnusAccel(vel, angularVelocity);
                vel += (Physics.gravity + magnus) * dt;
                vel *= dampMul;
                pos += vel * dt;

                if (pos.z >= goalPlaneZ)
                {
                    float denom = pos.z - prevPos.z;
                    float frac = Mathf.Abs(denom) > 0.0001f ? Mathf.Clamp01((goalPlaneZ - prevPos.z) / denom) : 1f;
                    simBuffer[rawCount++] = Vector3.Lerp(prevPos, pos, frac);
                    break;
                }
                simBuffer[rawCount++] = pos;
            }

            for (int i = 0; i < outPoints.Length; i++)
            {
                float u = (i / (float)(outPoints.Length - 1)) * (rawCount - 1);
                int idx0 = Mathf.Clamp(Mathf.FloorToInt(u), 0, rawCount - 1);
                int idx1 = Mathf.Clamp(idx0 + 1, 0, rawCount - 1);
                outPoints[i] = Vector3.Lerp(simBuffer[idx0], simBuffer[idx1], u - idx0);
            }
        }

        public Vector3[] GetPreviewTrajectoryPoints(Vector3 ballPos, Vector3 targetPos, float normalizedPower, int count = 20)
        {
            ComputeShotSolution(ballPos, targetPos, normalizedPower, out Vector3 launchVel, out Vector3 spinVel, out _);
            Vector3[] pts = new Vector3[Mathf.Max(2, count)];
            SampleSimulatedTrajectory(ballPos, launchVel, spinVel, pts);
            return pts;
        }

        private void PerformPhysicsKick(Vector3 ballPos)
        {
            if (BallController.Instance == null) return;

            bool isOver = CurrentPower > 0.92f;

            // Keep shot strictly aligned to the aiming indicator; only strong red overpower adds natural lift/risk
            Vector3 target = CurrentAimTarget;
            if (isOver)
            {
                target.x += UnityEngine.Random.Range(-0.12f, 0.12f);
                target.y += UnityEngine.Random.Range(0.14f, 0.42f);
            }

            ComputeShotSolution(ballPos, target, CurrentPower, out Vector3 velocity, out Vector3 torque, out _);
            LastPredictedLaunchVelocity = velocity;

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
                if (directionArrowRoot != null) directionArrowRoot.gameObject.SetActive(false);
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
            aimUnlockedDuringCharge = false;
            inputCooldownTimer = 0.20f;
            OnPowerChanged?.Invoke(0f);
            OnPowerUpdated?.Invoke(0f, false, false);

            if (aimReticle != null) aimReticle.gameObject.SetActive(true);
            if (trajectoryLine != null) trajectoryLine.gameObject.SetActive(true);
            if (directionArrowRoot != null) directionArrowRoot.gameObject.SetActive(true);
        }

        public void SetAimReticle(Transform reticle, LineRenderer line, Transform arrowRoot = null)
        {
            aimReticle = reticle;
            trajectoryLine = line;
            if (arrowRoot != null)
            {
                directionArrowRoot = arrowRoot;
                directionArrowRenderers = arrowRoot.GetComponentsInChildren<Renderer>(true);
            }
            if (aimReticle != null) aimReticleRenderer = aimReticle.GetComponent<Renderer>();
        }
    }
}
