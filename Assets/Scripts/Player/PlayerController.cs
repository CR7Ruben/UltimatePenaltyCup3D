#pragma warning disable 0649
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UltimatePenaltyCup.Audio;

namespace UltimatePenaltyCup.Player
{
    public enum PlayerAnimState
    {
        Idle,
        PreparingCharge,
        Moving,
        RunUp,
        PlantAndWindUp,
        ContactStrike,
        FollowThrough,
        Recovering,
        Celebrating,
        Frustrated
    }

    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 3.8f;
        [SerializeField] private Vector2 movementBoundsX = new Vector2(-2.2f, 2.2f);
        [SerializeField] private Vector2 movementBoundsZ = new Vector2(-14.2f, -12.1f);

        [Header("Skinned / 22-Bone Humanoid Rig")]
        [SerializeField] private Transform characterVisuals;
        [SerializeField] private SkinnedMeshRenderer skinnedMesh;
        [SerializeField] private Transform hips;
        [SerializeField] private Transform spine;
        [SerializeField] private Transform upperSpine;
        [SerializeField] private Transform chest;
        [SerializeField] private Transform neck;
        [SerializeField] private Transform torso;
        [SerializeField] private Transform head;
        [SerializeField] private Transform leftShoulder;
        [SerializeField] private Transform leftArm;
        [SerializeField] private Transform leftForearm;
        [SerializeField] private Transform leftHand;
        [SerializeField] private Transform rightShoulder;
        [SerializeField] private Transform rightArm;
        [SerializeField] private Transform rightForearm;
        [SerializeField] private Transform rightHand;
        [SerializeField] private Transform leftLeg;
        [SerializeField] private Transform leftShin;
        [SerializeField] private Transform leftFoot;
        [SerializeField] private Transform leftToe;
        [SerializeField] private Transform rightLeg;
        [SerializeField] private Transform rightShin;
        [SerializeField] private Transform rightFoot;
        [SerializeField] private Transform rightToe;

        public PlayerAnimState CurrentAnimState { get; private set; } = PlayerAnimState.Idle;
        public bool ContactTriggeredThisKick { get; private set; } = false;

        private Vector3 startPosition;
        private Quaternion startRotation;
        private Vector3 hipsBaseLocalPos;
        private bool canMove = true;
        private Coroutine activeAnimCoroutine;
        private float walkCycleTimer = 0f;
        private float lastStepPhase = 0f;

        // Facial BlendShapes & blinking
        private int bsBlink = -1, bsFocus = -1, bsEffort = -1, bsHappy = -1, bsFrustrated = -1;
        private float nextBlinkTimer = 2.6f;
        private float blinkProgress = -1f;
        private float wFocus, wEffort, wHappy, wFrustrated;
        private int celebrationVariant = 0;
        private int missVariant = 0;

        public bool CanMove => canMove;

        private void Awake()
        {
            Instance = this;
            startPosition = transform.position;
            startRotation = transform.rotation;
            AutoAssignRigReferences();
            CacheBlendShapeIndices();
            if (hips != null)
            {
                hipsBaseLocalPos = hips.localPosition;
                if (hipsBaseLocalPos.y < 0.2f) hipsBaseLocalPos = new Vector3(0f, 0.94f, 0f);
            }
        }

        private void Start()
        {
            AutoAssignRigReferences();
            CacheBlendShapeIndices();
            if (hips != null && hipsBaseLocalPos.y < 0.2f)
            {
                hipsBaseLocalPos = hips.localPosition.y >= 0.2f ? hips.localPosition : new Vector3(0f, 0.94f, 0f);
            }
        }

        private void AutoAssignRigReferences()
        {
            if (characterVisuals == null) characterVisuals = FindFirstValid(transform.Find("Visuals"), transform.Find("Skeleton"), transform);
            if (skinnedMesh == null) skinnedMesh = GetComponentInChildren<SkinnedMeshRenderer>(true);

            if (hips == null)          hips          = FindBone(characterVisuals, "Hips");
            if (spine == null)         spine         = FindBone(characterVisuals, "Spine");
            if (upperSpine == null)    upperSpine    = FindBone(characterVisuals, "UpperSpine");
            if (chest == null)         chest         = FindBone(characterVisuals, "Chest");
            if (neck == null)          neck          = FindBone(characterVisuals, "Neck");
            if (torso == null)         torso         = FindFirstValid(chest, spine, FindBone(characterVisuals, "Torso"));
            if (head == null)          head          = FindBone(characterVisuals, "Head");
            if (leftShoulder == null)  leftShoulder  = FindBone(characterVisuals, "LeftShoulder");
            if (leftArm == null)       leftArm       = FindFirstValid(FindBone(characterVisuals, "LeftUpperArm"), FindBone(characterVisuals, "Arm_Left"));
            if (leftForearm == null)   leftForearm   = FindBone(characterVisuals, "LeftForearm");
            if (leftHand == null)      leftHand      = FindBone(characterVisuals, "LeftHand");
            if (rightShoulder == null) rightShoulder = FindBone(characterVisuals, "RightShoulder");
            if (rightArm == null)      rightArm      = FindFirstValid(FindBone(characterVisuals, "RightUpperArm"), FindBone(characterVisuals, "Arm_Right"));
            if (rightForearm == null)  rightForearm  = FindBone(characterVisuals, "RightForearm");
            if (rightHand == null)     rightHand     = FindBone(characterVisuals, "RightHand");
            if (leftLeg == null)       leftLeg       = FindFirstValid(FindBone(characterVisuals, "LeftThigh"), FindBone(characterVisuals, "Leg_Left"));
            if (leftShin == null)      leftShin      = FindBone(characterVisuals, "LeftShin");
            if (leftFoot == null)      leftFoot      = FindBone(characterVisuals, "LeftFoot");
            if (leftToe == null)       leftToe       = FindBone(characterVisuals, "LeftToe");
            if (rightLeg == null)      rightLeg      = FindFirstValid(FindBone(characterVisuals, "RightThigh"), FindBone(characterVisuals, "Leg_Right"));
            if (rightShin == null)     rightShin     = FindBone(characterVisuals, "RightShin");
            if (rightFoot == null)     rightFoot     = FindBone(characterVisuals, "RightFoot");
            if (rightToe == null)      rightToe      = FindBone(characterVisuals, "RightToe");
        }

        private static Transform FindFirstValid(params Transform[] candidates)
        {
            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i] != null) return candidates[i];
            }
            return null;
        }

        private void CacheBlendShapeIndices()
        {
            if (skinnedMesh == null || skinnedMesh.sharedMesh == null) return;
            Mesh m = skinnedMesh.sharedMesh;
            bsBlink = m.GetBlendShapeIndex("Blink");
            bsFocus = m.GetBlendShapeIndex("Focus");
            bsEffort = m.GetBlendShapeIndex("Effort");
            bsHappy = m.GetBlendShapeIndex("Happy");
            bsFrustrated = m.GetBlendShapeIndex("Frustrated");
        }

        private Transform FindBone(Transform root, string boneName)
        {
            if (root == null) return null;
            if (root.name == boneName) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindBone(root.GetChild(i), boneName);
                if (found != null) return found;
            }
            return null;
        }

        private void OnDisable()
        {
            if (activeAnimCoroutine != null)
            {
                StopCoroutine(activeAnimCoroutine);
                activeAnimCoroutine = null;
            }
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            UpdateFaceExpressions(Time.deltaTime);
            if (!canMove) return;

            HandleMovementAndAnimation();
        }

        private void UpdateFaceExpressions(float dt)
        {
            if (skinnedMesh == null) return;

            // Natural blinking cycle
            nextBlinkTimer -= dt;
            if (nextBlinkTimer <= 0f && blinkProgress < 0f)
            {
                blinkProgress = 0f;
                nextBlinkTimer = UnityEngine.Random.Range(2.6f, 4.9f);
            }

            float blinkWeight = 0f;
            if (blinkProgress >= 0f)
            {
                blinkProgress += dt / 0.16f;
                if (blinkProgress >= 1f)
                {
                    blinkProgress = -1f;
                }
                else
                {
                    blinkWeight = Mathf.Sin(blinkProgress * Mathf.PI) * 100f;
                }
            }

            float tgtFocus = (CurrentAnimState == PlayerAnimState.PreparingCharge || CurrentAnimState == PlayerAnimState.RunUp) ? 85f : (CurrentAnimState == PlayerAnimState.Idle ? 35f : 0f);
            float tgtEffort = (CurrentAnimState == PlayerAnimState.PlantAndWindUp || CurrentAnimState == PlayerAnimState.ContactStrike || CurrentAnimState == PlayerAnimState.FollowThrough) ? 95f : 0f;
            float tgtHappy = CurrentAnimState == PlayerAnimState.Celebrating ? 100f : 0f;
            float tgtFrust = CurrentAnimState == PlayerAnimState.Frustrated ? 95f : 0f;

            float speed = 1f - Mathf.Exp(-10f * dt);
            wFocus = Mathf.Lerp(wFocus, tgtFocus, speed);
            wEffort = Mathf.Lerp(wEffort, tgtEffort, speed);
            wHappy = Mathf.Lerp(wHappy, tgtHappy, speed);
            wFrustrated = Mathf.Lerp(wFrustrated, tgtFrust, speed);

            if (bsBlink >= 0) skinnedMesh.SetBlendShapeWeight(bsBlink, blinkWeight);
            if (bsFocus >= 0) skinnedMesh.SetBlendShapeWeight(bsFocus, wFocus);
            if (bsEffort >= 0) skinnedMesh.SetBlendShapeWeight(bsEffort, wEffort);
            if (bsHappy >= 0) skinnedMesh.SetBlendShapeWeight(bsHappy, wHappy);
            if (bsFrustrated >= 0) skinnedMesh.SetBlendShapeWeight(bsFrustrated, wFrustrated);
        }

        private void HandleMovementAndAnimation()
        {
            float horizontal = 0f;
            float vertical = 0f;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) vertical += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) vertical -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontal += 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontal -= 1f;
            }
            else
            {
                horizontal = Input.GetAxisRaw("Horizontal");
                vertical = Input.GetAxisRaw("Vertical");
            }

            Vector3 moveDir = new Vector3(horizontal, 0f, vertical).normalized;
            float dt = Time.deltaTime;

            if (moveDir.sqrMagnitude > 0.01f)
            {
                CurrentAnimState = PlayerAnimState.Moving;
                Vector3 newPos = transform.position + moveDir * moveSpeed * dt;
                newPos.x = Mathf.Clamp(newPos.x, movementBoundsX.x, movementBoundsX.y);
                newPos.z = Mathf.Clamp(newPos.z, movementBoundsZ.x, movementBoundsZ.y);
                newPos.y = 0f;
                transform.position = newPos;

                walkCycleTimer += dt * 10.5f;
                float swing = Mathf.Sin(walkCycleTimer);
                if (Mathf.Sign(swing) != Mathf.Sign(lastStepPhase) && Mathf.Abs(swing) > 0.1f)
                {
                    AudioManager.Instance?.PlaySfx(SfxType.Footstep);
                    lastStepPhase = swing;
                }

                float legAngle = swing * 26f;
                float leftKnee = Mathf.Max(0f, -swing) * 36f + 12f;
                float rightKnee = Mathf.Max(0f, swing) * 36f + 12f;
                float armAngle = Mathf.Cos(walkCycleTimer) * 20f;
                float bob = Mathf.Abs(Mathf.Sin(walkCycleTimer * 2f)) * 0.026f;

                float blend = 1f - Mathf.Exp(-16f * dt);
                if (hips != null)
                {
                    hips.localPosition = Vector3.Lerp(hips.localPosition, new Vector3(hipsBaseLocalPos.x, hipsBaseLocalPos.y + bob, hipsBaseLocalPos.z), blend);
                    hips.localRotation = Quaternion.Slerp(hips.localRotation, Quaternion.Euler(2f, swing * 4f, -swing * 2.5f), blend);
                }
                if (spine != null) spine.localRotation = Quaternion.Slerp(spine.localRotation, Quaternion.Euler(4f, -swing * 3f, 0f), blend);
                if (upperSpine != null) upperSpine.localRotation = Quaternion.Slerp(upperSpine.localRotation, Quaternion.Euler(3f, -swing * 4f, 0f), blend);
                if (chest != null) chest.localRotation = Quaternion.Slerp(chest.localRotation, Quaternion.Euler(3f, -swing * 5f + moveDir.x * 8f, 0f), blend);
                if (neck != null) neck.localRotation = Quaternion.Slerp(neck.localRotation, Quaternion.Euler(-4f, 0f, 0f), blend);
                if (head != null) head.localRotation = Quaternion.Slerp(head.localRotation, Quaternion.Euler(-3f, 0f, 0f), blend);

                if (leftShoulder != null) leftShoulder.localRotation = Quaternion.Slerp(leftShoulder.localRotation, Quaternion.Euler(0f, armAngle * 0.25f, 3f), blend);
                if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.Euler(armAngle, 4f, -11f), blend);
                if (leftForearm != null) leftForearm.localRotation = Quaternion.Slerp(leftForearm.localRotation, Quaternion.Euler(-28f, 0f, 0f), blend);
                if (leftHand != null) leftHand.localRotation = Quaternion.Slerp(leftHand.localRotation, Quaternion.Euler(-8f, 6f, 0f), blend);

                if (rightShoulder != null) rightShoulder.localRotation = Quaternion.Slerp(rightShoulder.localRotation, Quaternion.Euler(0f, -armAngle * 0.25f, -3f), blend);
                if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler(-armAngle, -4f, 11f), blend);
                if (rightForearm != null) rightForearm.localRotation = Quaternion.Slerp(rightForearm.localRotation, Quaternion.Euler(-28f, 0f, 0f), blend);
                if (rightHand != null) rightHand.localRotation = Quaternion.Slerp(rightHand.localRotation, Quaternion.Euler(-8f, -6f, 0f), blend);

                if (leftLeg != null) leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, Quaternion.Euler(-legAngle, 0f, -2f), blend);
                if (leftShin != null) leftShin.localRotation = Quaternion.Slerp(leftShin.localRotation, Quaternion.Euler(leftKnee, 0f, 0f), blend);
                if (leftFoot != null) leftFoot.localRotation = Quaternion.Slerp(leftFoot.localRotation, Quaternion.Euler(-leftKnee * 0.32f, 0f, 0f), blend);
                if (leftToe != null) leftToe.localRotation = Quaternion.Slerp(leftToe.localRotation, Quaternion.Euler(Mathf.Max(0f, -swing) * 16f, 0f, 0f), blend);

                if (rightLeg != null) rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, Quaternion.Euler(legAngle, 0f, 2f), blend);
                if (rightShin != null) rightShin.localRotation = Quaternion.Slerp(rightShin.localRotation, Quaternion.Euler(rightKnee, 0f, 0f), blend);
                if (rightFoot != null) rightFoot.localRotation = Quaternion.Slerp(rightFoot.localRotation, Quaternion.Euler(-rightKnee * 0.32f, 0f, 0f), blend);
                if (rightToe != null) rightToe.localRotation = Quaternion.Slerp(rightToe.localRotation, Quaternion.Euler(Mathf.Max(0f, swing) * 16f, 0f, 0f), blend);
            }
            else
            {
                bool isCharging = PlayerShooter.Instance != null && PlayerShooter.Instance.IsCharging;
                CurrentAnimState = isCharging ? PlayerAnimState.PreparingCharge : PlayerAnimState.Idle;
                walkCycleTimer += dt * (isCharging ? 4.0f : 1.8f);

                float breath = Mathf.Sin(walkCycleTimer);
                float weightShift = Mathf.Sin(walkCycleTimer * 0.45f);
                float gazeWave = Mathf.Sin(walkCycleTimer * 0.65f);
                float idleBlend = 1f - Mathf.Exp(-10f * dt);

                float hipDrop = isCharging ? 0.068f : 0.028f;
                float shiftX = isCharging ? 0.01f : weightShift * 0.022f;
                float lean = isCharging ? 10.5f : 4.2f;
                float knee = isCharging ? 24f : 12f;

                if (hips != null)
                {
                    hips.localPosition = Vector3.Lerp(hips.localPosition, new Vector3(hipsBaseLocalPos.x + shiftX, hipsBaseLocalPos.y - hipDrop + breath * 0.008f, hipsBaseLocalPos.z), idleBlend);
                    hips.localRotation = Quaternion.Slerp(hips.localRotation, Quaternion.Euler(1.5f, isCharging ? -6f : weightShift * 3.5f, isCharging ? 0f : -weightShift * 2.5f), idleBlend);
                }
                if (spine != null) spine.localRotation = Quaternion.Slerp(spine.localRotation, Quaternion.Euler(lean * 0.55f, isCharging ? 3f : -weightShift * 2f, 0f), idleBlend);
                if (upperSpine != null) upperSpine.localRotation = Quaternion.Slerp(upperSpine.localRotation, Quaternion.Euler(lean * 0.45f + breath * 1.6f, 0f, 0f), idleBlend);
                if (chest != null) chest.localRotation = Quaternion.Slerp(chest.localRotation, Quaternion.Euler(lean * 0.40f + breath * 2.0f, 0f, 0f), idleBlend);
                if (neck != null) neck.localRotation = Quaternion.Slerp(neck.localRotation, Quaternion.Euler(-lean * 0.45f, gazeWave * 3f, 0f), idleBlend);
                if (head != null)
                {
                    // Natural look between goal and ball before shot
                    float aimLookYaw = PlayerShooter.Instance != null && PlayerShooter.Instance.CanShoot
                        ? Mathf.Clamp(PlayerShooter.Instance.CurrentAimTarget.x * 3.5f, -12f, 12f)
                        : gazeWave * 6f;
                    float pitchLook = isCharging ? 8f : Mathf.Lerp(-3f, 9f, (gazeWave + 1f) * 0.5f);
                    head.localRotation = Quaternion.Slerp(head.localRotation, Quaternion.Euler(pitchLook - lean * 0.8f, aimLookYaw, 0f), idleBlend);
                }

                // Relaxed asymmetric athletic stance (left leg slightly planted forward, right ready)
                if (leftLeg != null) leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, Quaternion.Euler(-knee * 0.58f, -6f, -4.5f), idleBlend);
                if (leftShin != null) leftShin.localRotation = Quaternion.Slerp(leftShin.localRotation, Quaternion.Euler(knee + weightShift * 3f, 0f, 0f), idleBlend);
                if (leftFoot != null) leftFoot.localRotation = Quaternion.Slerp(leftFoot.localRotation, Quaternion.Euler(-knee * 0.36f, 0f, 2f), idleBlend);
                if (leftToe != null) leftToe.localRotation = Quaternion.Slerp(leftToe.localRotation, Quaternion.identity, idleBlend);

                if (rightLeg != null) rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, Quaternion.Euler(knee * 0.22f, 7f, 5.5f), idleBlend);
                if (rightShin != null) rightShin.localRotation = Quaternion.Slerp(rightShin.localRotation, Quaternion.Euler(knee * 0.90f - weightShift * 3f, 0f, 0f), idleBlend);
                if (rightFoot != null) rightFoot.localRotation = Quaternion.Slerp(rightFoot.localRotation, Quaternion.Euler(-knee * 0.32f, 0f, -2f), idleBlend);
                if (rightToe != null) rightToe.localRotation = Quaternion.Slerp(rightToe.localRotation, Quaternion.Euler(4f, 0f, 0f), idleBlend);

                float armSpread = isCharging ? 18f : 11f;
                float shoulderDrop = isCharging ? -3f : breath * 2.2f;
                if (leftShoulder != null) leftShoulder.localRotation = Quaternion.Slerp(leftShoulder.localRotation, Quaternion.Euler(0f, 0f, shoulderDrop + 3f), idleBlend);
                if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.Euler(6f + breath * 1.5f, 8f, -armSpread), idleBlend);
                if (leftForearm != null) leftForearm.localRotation = Quaternion.Slerp(leftForearm.localRotation, Quaternion.Euler(isCharging ? -42f : -26f, 6f, 0f), idleBlend);
                if (leftHand != null) leftHand.localRotation = Quaternion.Slerp(leftHand.localRotation, Quaternion.Euler(0f, 10f, -8f), idleBlend);

                if (rightShoulder != null) rightShoulder.localRotation = Quaternion.Slerp(rightShoulder.localRotation, Quaternion.Euler(0f, 0f, -shoulderDrop - 3f), idleBlend);
                if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler(6f - breath * 1.5f, -8f, armSpread), idleBlend);
                if (rightForearm != null) rightForearm.localRotation = Quaternion.Slerp(rightForearm.localRotation, Quaternion.Euler(isCharging ? -42f : -26f, -6f, 0f), idleBlend);
                if (rightHand != null) rightHand.localRotation = Quaternion.Slerp(rightHand.localRotation, Quaternion.Euler(0f, -10f, 8f), idleBlend);
            }
        }

        public void PlayKickAnimation(Vector3 targetBallPos, Action onKickContact)
        {
            PlayKickAnimation(targetBallPos, new Vector3(0f, 1.2f, 0f), 0.72f, onKickContact);
        }

        public void PlayKickAnimation(Vector3 targetBallPos, Vector3 aimTarget, float normalizedPower, Action onKickContact)
        {
            canMove = false;
            ContactTriggeredThisKick = false;
            if (activeAnimCoroutine != null) StopCoroutine(activeAnimCoroutine);
            activeAnimCoroutine = StartCoroutine(KickRoutine(targetBallPos, aimTarget, Mathf.Clamp01(normalizedPower), onKickContact));
        }

        private IEnumerator KickRoutine(Vector3 targetBallPos, Vector3 aimTarget, float power, Action onKickContact)
        {
            // Shot-specific athletic styling (purely visual - zero alteration to ballistic vector)
            float powerFactor = Mathf.Clamp01(power);
            float aimSideBias = Mathf.Clamp(aimTarget.x / 3.5f, -1f, 1f);
            float aimLoftBias = Mathf.Clamp((aimTarget.y - 1.2f) / 1.4f, -0.5f, 1.0f);

            // 1. Curved Approach Run-Up (0.26s) with coordinated 22-bone torso counter-rotation
            CurrentAnimState = PlayerAnimState.RunUp;
            Vector3 approachPos = new Vector3(targetBallPos.x - 0.29f, 0f, targetBallPos.z - 0.35f);
            Vector3 startP = transform.position;
            float runDuration = 0.26f;
            float t = 0f;
            float prevStrideSign = 0f;

            while (t < runDuration)
            {
                float dt = Time.deltaTime;
                t += dt;
                float u = Mathf.Clamp01(t / runDuration);
                float p = Mathf.SmoothStep(0f, 1f, u);
                Vector3 pos = Vector3.Lerp(startP, approachPos, p);
                pos.x -= Mathf.Sin(u * Mathf.PI) * 0.085f;
                transform.position = pos;

                Vector3 lookDir = targetBallPos - transform.position;
                lookDir.y = 0f;
                if (lookDir.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), dt * 16f);
                }

                float runPhase = u * Mathf.PI * 3.0f;
                float legS = Mathf.Sin(runPhase);
                if (Mathf.Sign(legS) != Mathf.Sign(prevStrideSign) && u > 0.15f)
                {
                    AudioManager.Instance?.PlaySfx(SfxType.Footstep);
                    prevStrideSign = legS;
                }

                float strideBob = Mathf.Abs(Mathf.Cos(runPhase)) * 0.032f;
                float blend = 1f - Mathf.Exp(-22f * dt);

                if (hips != null)
                {
                    hips.localPosition = Vector3.Lerp(hips.localPosition, new Vector3(hipsBaseLocalPos.x, hipsBaseLocalPos.y - 0.035f + strideBob, hipsBaseLocalPos.z), blend);
                    hips.localRotation = Quaternion.Slerp(hips.localRotation, Quaternion.Euler(4f, legS * 8f, -legS * 3f), blend);
                }
                if (spine != null) spine.localRotation = Quaternion.Slerp(spine.localRotation, Quaternion.Euler(8f, -legS * 6f, 0f), blend);
                if (upperSpine != null) upperSpine.localRotation = Quaternion.Slerp(upperSpine.localRotation, Quaternion.Euler(6f, -legS * 6f, 0f), blend);
                if (chest != null) chest.localRotation = Quaternion.Slerp(chest.localRotation, Quaternion.Euler(6f, -legS * 8f, 0f), blend);
                if (head != null) head.localRotation = Quaternion.Slerp(head.localRotation, Quaternion.Euler(4f, 0f, 0f), blend);

                float lHip = legS * 38f;
                float lKnee = Mathf.Max(0f, -legS) * 48f + 16f;
                float rHip = -legS * 38f;
                float rKnee = Mathf.Max(0f, legS) * 48f + 16f;

                if (leftLeg != null) leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, Quaternion.Euler(lHip, 0f, -3f), blend);
                if (leftShin != null) leftShin.localRotation = Quaternion.Slerp(leftShin.localRotation, Quaternion.Euler(lKnee, 0f, 0f), blend);
                if (leftFoot != null) leftFoot.localRotation = Quaternion.Slerp(leftFoot.localRotation, Quaternion.Euler(-lKnee * 0.35f, 0f, 0f), blend);
                if (leftToe != null) leftToe.localRotation = Quaternion.Slerp(leftToe.localRotation, Quaternion.Euler(Mathf.Max(0f, -legS) * 18f, 0f, 0f), blend);

                if (rightLeg != null) rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, Quaternion.Euler(rHip, 0f, 3f), blend);
                if (rightShin != null) rightShin.localRotation = Quaternion.Slerp(rightShin.localRotation, Quaternion.Euler(rKnee, 0f, 0f), blend);
                if (rightFoot != null) rightFoot.localRotation = Quaternion.Slerp(rightFoot.localRotation, Quaternion.Euler(-rKnee * 0.35f, 0f, 0f), blend);
                if (rightToe != null) rightToe.localRotation = Quaternion.Slerp(rightToe.localRotation, Quaternion.Euler(Mathf.Max(0f, legS) * 18f, 0f, 0f), blend);

                if (leftShoulder != null) leftShoulder.localRotation = Quaternion.Slerp(leftShoulder.localRotation, Quaternion.Euler(0f, -legS * 8f, 4f), blend);
                if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.Euler(-legS * 30f, 0f, -18f), blend);
                if (leftForearm != null) leftForearm.localRotation = Quaternion.Slerp(leftForearm.localRotation, Quaternion.Euler(-52f, 0f, 0f), blend);

                if (rightShoulder != null) rightShoulder.localRotation = Quaternion.Slerp(rightShoulder.localRotation, Quaternion.Euler(0f, legS * 8f, -4f), blend);
                if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler(legS * 30f, 0f, 18f), blend);
                if (rightForearm != null) rightForearm.localRotation = Quaternion.Slerp(rightForearm.localRotation, Quaternion.Euler(-52f, 0f, 0f), blend);

                yield return null;
            }

            // 2. Plant Left Support Foot & Cock Kicking Leg Back with Clavicle Open (0.105s)
            CurrentAnimState = PlayerAnimState.PlantAndWindUp;
            float cockTime = 0.105f;
            t = 0f;
            float backCockAngle = Mathf.Lerp(42f, 68f, powerFactor);
            float torsoLeanRoll = Mathf.Lerp(-8f, -17f, powerFactor);
            float leftArmBalanceZ = Mathf.Lerp(-44f, -70f, powerFactor);

            Quaternion lLegCockStart = leftLeg != null ? leftLeg.localRotation : Quaternion.identity;
            Quaternion rLegCockStart = rightLeg != null ? rightLeg.localRotation : Quaternion.identity;
            Quaternion rShinCockStart = rightShin != null ? rightShin.localRotation : Quaternion.identity;
            Quaternion spineCockStart = spine != null ? spine.localRotation : Quaternion.identity;

            while (t < cockTime)
            {
                t += Time.deltaTime;
                float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / cockTime));
                if (hips != null)
                {
                    hips.localPosition = Vector3.Lerp(hips.localPosition, new Vector3(hipsBaseLocalPos.x, hipsBaseLocalPos.y - 0.085f, hipsBaseLocalPos.z), p);
                    hips.localRotation = Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(6f, -15f, torsoLeanRoll), p);
                }
                if (leftLeg != null) leftLeg.localRotation = Quaternion.Slerp(lLegCockStart, Quaternion.Euler(-24f, -8f, -9f), p);
                if (leftShin != null) leftShin.localRotation = Quaternion.Slerp(leftShin.localRotation, Quaternion.Euler(34f, 0f, 0f), p);
                if (leftFoot != null) leftFoot.localRotation = Quaternion.Slerp(leftFoot.localRotation, Quaternion.Euler(-10f, 0f, 0f), p);
                if (leftToe != null) leftToe.localRotation = Quaternion.Slerp(leftToe.localRotation, Quaternion.Euler(8f, 0f, 0f), p);

                if (rightLeg != null) rightLeg.localRotation = Quaternion.Slerp(rLegCockStart, Quaternion.Euler(backCockAngle, 0f, 10f), p);
                if (rightShin != null) rightShin.localRotation = Quaternion.Slerp(rShinCockStart, Quaternion.Euler(Mathf.Lerp(56f, 84f, powerFactor), 0f, 0f), p);
                if (rightFoot != null) rightFoot.localRotation = Quaternion.Slerp(rightFoot.localRotation, Quaternion.Euler(26f, 0f, 0f), p);
                if (rightToe != null) rightToe.localRotation = Quaternion.Slerp(rightToe.localRotation, Quaternion.Euler(14f, 0f, 0f), p);

                if (spine != null) spine.localRotation = Quaternion.Slerp(spineCockStart, Quaternion.Euler(6f, -14f, -4f), p);
                if (upperSpine != null) upperSpine.localRotation = Quaternion.Slerp(upperSpine.localRotation, Quaternion.Euler(5f, -14f, -4f), p);
                if (chest != null) chest.localRotation = Quaternion.Slerp(chest.localRotation, Quaternion.Euler(4f, -12f, 0f), p);
                if (leftShoulder != null) leftShoulder.localRotation = Quaternion.Slerp(leftShoulder.localRotation, Quaternion.Euler(0f, -12f, 10f), p);
                if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.Euler(-28f, -12f, leftArmBalanceZ), p);
                if (leftForearm != null) leftForearm.localRotation = Quaternion.Slerp(leftForearm.localRotation, Quaternion.Euler(-22f, 0f, 0f), p);
                if (rightShoulder != null) rightShoulder.localRotation = Quaternion.Slerp(rightShoulder.localRotation, Quaternion.Euler(0f, 10f, -8f), p);
                if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler(28f, 0f, 22f), p);
                yield return null;
            }

            // 3. Whip Hip + Kicking Leg Forward Into Exact Boot-Ball Contact (0.060s -> total 0.425s pre-contact)
            CurrentAnimState = PlayerAnimState.ContactStrike;
            float swingTime = 0.060f;
            t = 0f;
            float sideFootYaw = powerFactor < 0.55f ? -26f : 14f;
            float contactPitch = Mathf.Lerp(-18f, -26f, powerFactor);

            while (t < swingTime)
            {
                t += Time.deltaTime;
                float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / swingTime));
                if (hips != null) hips.localRotation = Quaternion.Slerp(Quaternion.Euler(6f, -15f, torsoLeanRoll), Quaternion.Euler(5f, 8f + aimSideBias * 6f, -7f), p);
                if (rightLeg != null) rightLeg.localRotation = Quaternion.Slerp(Quaternion.Euler(backCockAngle, 0f, 10f), Quaternion.Euler(contactPitch, 0f, 0f), p);
                if (rightShin != null) rightShin.localRotation = Quaternion.Slerp(Quaternion.Euler(Mathf.Lerp(56f, 84f, powerFactor), 0f, 0f), Quaternion.Euler(10f, 0f, 0f), p);
                if (rightFoot != null) rightFoot.localRotation = Quaternion.Slerp(Quaternion.Euler(26f, 0f, 0f), Quaternion.Euler(16f, sideFootYaw, 0f), p);
                if (spine != null) spine.localRotation = Quaternion.Slerp(Quaternion.Euler(6f, -14f, -4f), Quaternion.Euler(10f - aimLoftBias * 6f, 8f, 4f), p);
                if (chest != null) chest.localRotation = Quaternion.Slerp(Quaternion.Euler(4f, -12f, 0f), Quaternion.Euler(6f, 10f, 4f), p);
                yield return null;
            }

            // 4. Exact Boot-Ball Contact Strike! (Ball NEVER launches before this moment!)
            ContactTriggeredThisKick = true;
            onKickContact?.Invoke();

            // 5. Dynamic Follow-Through & Weight Recovery Hop (0.24s)
            CurrentAnimState = PlayerAnimState.FollowThrough;
            float followTime = 0.24f;
            t = 0f;
            float followPitch = Mathf.Lerp(-56f, -90f, powerFactor);
            float followLeanBack = aimLoftBias * 8f;

            while (t < followTime)
            {
                t += Time.deltaTime;
                float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / followTime));
                float hopY = Mathf.Sin(p * Mathf.PI) * Mathf.Lerp(0.03f, 0.08f, powerFactor);
                if (hips != null)
                {
                    hips.localPosition = Vector3.Lerp(hips.localPosition, new Vector3(hipsBaseLocalPos.x, hipsBaseLocalPos.y - 0.03f + hopY, hipsBaseLocalPos.z), p);
                    hips.localRotation = Quaternion.Slerp(Quaternion.Euler(5f, 8f, -7f), Quaternion.Euler(3f - followLeanBack * 0.5f, 22f + aimSideBias * 10f, 5f), p);
                }
                if (rightLeg != null) rightLeg.localRotation = Quaternion.Slerp(Quaternion.Euler(contactPitch, 0f, 0f), Quaternion.Euler(followPitch, -12f, -10f), p);
                if (rightShin != null) rightShin.localRotation = Quaternion.Slerp(rightShin.localRotation, Quaternion.Euler(16f, 0f, 0f), p);
                if (rightFoot != null) rightFoot.localRotation = Quaternion.Slerp(rightFoot.localRotation, Quaternion.Euler(20f, 0f, 0f), p);
                if (leftLeg != null) leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, Quaternion.Euler(-8f, 0f, -6f), p);
                if (leftShin != null) leftShin.localRotation = Quaternion.Slerp(leftShin.localRotation, Quaternion.Euler(18f, 0f, 0f), p);

                if (spine != null) spine.localRotation = Quaternion.Slerp(Quaternion.Euler(10f, 8f, 4f), Quaternion.Euler(12f - followLeanBack, 16f, 6f), p);
                if (upperSpine != null) upperSpine.localRotation = Quaternion.Slerp(upperSpine.localRotation, Quaternion.Euler(4f, 14f, 4f), p);
                if (chest != null) chest.localRotation = Quaternion.Slerp(chest.localRotation, Quaternion.Euler(4f, 16f, 4f), p);
                if (leftShoulder != null) leftShoulder.localRotation = Quaternion.Slerp(leftShoulder.localRotation, Quaternion.Euler(0f, 10f, -6f), p);
                if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.Euler(26f, 0f, -24f), p);
                if (rightShoulder != null) rightShoulder.localRotation = Quaternion.Slerp(rightShoulder.localRotation, Quaternion.Euler(0f, -14f, 8f), p);
                if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler(-62f * Mathf.Lerp(0.75f, 1.15f, powerFactor), 0f, 36f), p);
                yield return null;
            }

            CurrentAnimState = PlayerAnimState.Recovering;
            yield return SmoothReturnToIdlePose(0.24f);
        }

        public void PlayGoalCelebration()
        {
            CurrentAnimState = PlayerAnimState.Celebrating;
            if (activeAnimCoroutine != null) StopCoroutine(activeAnimCoroutine);
            celebrationVariant = (celebrationVariant + 1) % 3;
            activeAnimCoroutine = StartCoroutine(GoalCelebrationRoutine(celebrationVariant));
        }

        private IEnumerator GoalCelebrationRoutine(int variant)
        {
            canMove = false;
            CurrentAnimState = PlayerAnimState.Celebrating;
            float timer = 0f;
            Vector3 basePos = new Vector3(transform.position.x, 0f, transform.position.z);

            while (timer < 1.35f)
            {
                timer += Time.deltaTime;
                float blend = 1f - Mathf.Exp(-12f * Time.deltaTime);

                if (variant == 0)
                {
                    // Joyful fist-pump leap
                    float jump = Mathf.Abs(Mathf.Sin(timer * 6.8f)) * 0.30f;
                    transform.position = new Vector3(basePos.x, jump, basePos.z);
                    if (spine != null) spine.localRotation = Quaternion.Slerp(spine.localRotation, Quaternion.Euler(-8f, Mathf.Sin(timer * 5f) * 12f, 0f), blend);
                    if (head != null) head.localRotation = Quaternion.Slerp(head.localRotation, Quaternion.Euler(-16f, 0f, 0f), blend);
                    if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.Euler(-142f, 0f, -24f), blend);
                    if (leftForearm != null) leftForearm.localRotation = Quaternion.Slerp(leftForearm.localRotation, Quaternion.Euler(-22f, 0f, 0f), blend);
                    if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler(-148f, 0f, 24f), blend);
                    if (rightForearm != null) rightForearm.localRotation = Quaternion.Slerp(rightForearm.localRotation, Quaternion.Euler(-22f, 0f, 0f), blend);
                    if (leftLeg != null) leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, Quaternion.Euler(-jump * 42f, 0f, 0f), blend);
                    if (leftShin != null) leftShin.localRotation = Quaternion.Slerp(leftShin.localRotation, Quaternion.Euler(jump * 85f, 0f, 0f), blend);
                    if (rightLeg != null) rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, Quaternion.Euler(-jump * 42f, 0f, 0f), blend);
                    if (rightShin != null) rightShin.localRotation = Quaternion.Slerp(rightShin.localRotation, Quaternion.Euler(jump * 85f, 0f, 0f), blend);
                }
                else if (variant == 1)
                {
                    // Airplane wing glide sway
                    float bank = Mathf.Sin(timer * 4.5f) * 14f;
                    if (hips != null) hips.localRotation = Quaternion.Slerp(hips.localRotation, Quaternion.Euler(6f, Mathf.Sin(timer * 2.5f) * 18f, bank * 0.5f), blend);
                    if (spine != null) spine.localRotation = Quaternion.Slerp(spine.localRotation, Quaternion.Euler(14f, 0f, bank), blend);
                    if (head != null) head.localRotation = Quaternion.Slerp(head.localRotation, Quaternion.Euler(-12f, 0f, -bank * 0.4f), blend);
                    if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.Euler(-12f, 0f, -84f), blend);
                    if (leftForearm != null) leftForearm.localRotation = Quaternion.Slerp(leftForearm.localRotation, Quaternion.Euler(-8f, 0f, 0f), blend);
                    if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler(-12f, 0f, 84f), blend);
                    if (rightForearm != null) rightForearm.localRotation = Quaternion.Slerp(rightForearm.localRotation, Quaternion.Euler(-8f, 0f, 0f), blend);
                }
                else
                {
                    // Confident double fist-pump & crowd salute
                    float pump = Mathf.Sin(timer * 9f) * 16f;
                    if (spine != null) spine.localRotation = Quaternion.Slerp(spine.localRotation, Quaternion.Euler(-6f, 0f, 0f), blend);
                    if (chest != null) chest.localRotation = Quaternion.Slerp(chest.localRotation, Quaternion.Euler(-8f, 0f, 0f), blend);
                    if (head != null) head.localRotation = Quaternion.Slerp(head.localRotation, Quaternion.Euler(-14f, Mathf.Sin(timer * 3f) * 16f, 0f), blend);
                    if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler(-122f + pump, 0f, 22f), blend);
                    if (rightForearm != null) rightForearm.localRotation = Quaternion.Slerp(rightForearm.localRotation, Quaternion.Euler(-48f, 0f, 0f), blend);
                    if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.Euler(-42f, 26f, -18f), blend);
                    if (leftForearm != null) leftForearm.localRotation = Quaternion.Slerp(leftForearm.localRotation, Quaternion.Euler(-78f, 0f, 0f), blend);
                }

                yield return null;
            }
            transform.position = basePos;
            yield return SmoothReturnToIdlePose(0.24f);
        }

        public void PlayMissReaction()
        {
            CurrentAnimState = PlayerAnimState.Frustrated;
            if (activeAnimCoroutine != null) StopCoroutine(activeAnimCoroutine);
            missVariant = (missVariant + 1) % 2;
            activeAnimCoroutine = StartCoroutine(MissReactionRoutine(missVariant));
        }

        private IEnumerator MissReactionRoutine(int variant)
        {
            canMove = false;
            CurrentAnimState = PlayerAnimState.Frustrated;
            float timer = 0f;

            while (timer < 1.35f)
            {
                timer += Time.deltaTime;
                float blend = 1f - Mathf.Exp(-10f * Time.deltaTime);

                if (variant == 0)
                {
                    // Hands behind head / disbelief sigh
                    if (spine != null) spine.localRotation = Quaternion.Slerp(spine.localRotation, Quaternion.Euler(14f, 0f, 0f), blend);
                    if (upperSpine != null) upperSpine.localRotation = Quaternion.Slerp(upperSpine.localRotation, Quaternion.Euler(8f, 0f, 0f), blend);
                    if (head != null) head.localRotation = Quaternion.Slerp(head.localRotation, Quaternion.Euler(18f, Mathf.Sin(timer * 5.5f) * 10f, 0f), blend);
                    if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.Euler(-118f, 0f, -38f), blend);
                    if (leftForearm != null) leftForearm.localRotation = Quaternion.Slerp(leftForearm.localRotation, Quaternion.Euler(-96f, 0f, 0f), blend);
                    if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler(-118f, 0f, 38f), blend);
                    if (rightForearm != null) rightForearm.localRotation = Quaternion.Slerp(rightForearm.localRotation, Quaternion.Euler(-96f, 0f, 0f), blend);
                }
                else
                {
                    // Hands on hips + head shake looking at turf
                    if (hips != null) hips.localPosition = Vector3.Lerp(hips.localPosition, new Vector3(hipsBaseLocalPos.x, hipsBaseLocalPos.y - 0.04f, hipsBaseLocalPos.z), blend);
                    if (spine != null) spine.localRotation = Quaternion.Slerp(spine.localRotation, Quaternion.Euler(16f, 0f, 0f), blend);
                    if (head != null) head.localRotation = Quaternion.Slerp(head.localRotation, Quaternion.Euler(26f, Mathf.Sin(timer * 7.5f) * 15f, 0f), blend);
                    if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.Euler(-12f, 0f, -36f), blend);
                    if (leftForearm != null) leftForearm.localRotation = Quaternion.Slerp(leftForearm.localRotation, Quaternion.Euler(-76f, -32f, 0f), blend);
                    if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler(-12f, 0f, 36f), blend);
                    if (rightForearm != null) rightForearm.localRotation = Quaternion.Slerp(rightForearm.localRotation, Quaternion.Euler(-76f, 32f, 0f), blend);
                }

                yield return null;
            }
            yield return SmoothReturnToIdlePose(0.24f);
        }

        private IEnumerator SmoothReturnToIdlePose(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float blend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                transform.rotation = Quaternion.Slerp(transform.rotation, startRotation, blend);

                if (hips != null)
                {
                    hips.localPosition = Vector3.Lerp(hips.localPosition, hipsBaseLocalPos, blend);
                    hips.localRotation = Quaternion.Slerp(hips.localRotation, Quaternion.identity, blend);
                }
                if (spine != null) spine.localRotation = Quaternion.Slerp(spine.localRotation, Quaternion.identity, blend);
                if (upperSpine != null) upperSpine.localRotation = Quaternion.Slerp(upperSpine.localRotation, Quaternion.identity, blend);
                if (chest != null) chest.localRotation = Quaternion.Slerp(chest.localRotation, Quaternion.identity, blend);
                if (neck != null) neck.localRotation = Quaternion.Slerp(neck.localRotation, Quaternion.identity, blend);
                if (torso != null) torso.localRotation = Quaternion.Slerp(torso.localRotation, Quaternion.identity, blend);
                if (head != null) head.localRotation = Quaternion.Slerp(head.localRotation, Quaternion.identity, blend);
                if (leftShoulder != null) leftShoulder.localRotation = Quaternion.Slerp(leftShoulder.localRotation, Quaternion.identity, blend);
                if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.identity, blend);
                if (leftForearm != null) leftForearm.localRotation = Quaternion.Slerp(leftForearm.localRotation, Quaternion.identity, blend);
                if (leftHand != null) leftHand.localRotation = Quaternion.Slerp(leftHand.localRotation, Quaternion.identity, blend);
                if (rightShoulder != null) rightShoulder.localRotation = Quaternion.Slerp(rightShoulder.localRotation, Quaternion.identity, blend);
                if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.identity, blend);
                if (rightForearm != null) rightForearm.localRotation = Quaternion.Slerp(rightForearm.localRotation, Quaternion.identity, blend);
                if (rightHand != null) rightHand.localRotation = Quaternion.Slerp(rightHand.localRotation, Quaternion.identity, blend);
                if (leftLeg != null) leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, Quaternion.identity, blend);
                if (leftShin != null) leftShin.localRotation = Quaternion.Slerp(leftShin.localRotation, Quaternion.identity, blend);
                if (leftFoot != null) leftFoot.localRotation = Quaternion.Slerp(leftFoot.localRotation, Quaternion.identity, blend);
                if (leftToe != null) leftToe.localRotation = Quaternion.Slerp(leftToe.localRotation, Quaternion.identity, blend);
                if (rightLeg != null) rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, Quaternion.identity, blend);
                if (rightShin != null) rightShin.localRotation = Quaternion.Slerp(rightShin.localRotation, Quaternion.identity, blend);
                if (rightFoot != null) rightFoot.localRotation = Quaternion.Slerp(rightFoot.localRotation, Quaternion.identity, blend);
                if (rightToe != null) rightToe.localRotation = Quaternion.Slerp(rightToe.localRotation, Quaternion.identity, blend);

                yield return null;
            }

            transform.rotation = startRotation;
            ResetPose();
            CurrentAnimState = PlayerAnimState.Idle;
        }

        private void ResetPose()
        {
            if (characterVisuals != null)
            {
                characterVisuals.localPosition = Vector3.zero;
                characterVisuals.localRotation = Quaternion.identity;
            }
            if (hips != null)
            {
                hips.localPosition = hipsBaseLocalPos;
                hips.localRotation = Quaternion.identity;
            }
            if (spine != null) spine.localRotation = Quaternion.identity;
            if (upperSpine != null) upperSpine.localRotation = Quaternion.identity;
            if (chest != null) chest.localRotation = Quaternion.identity;
            if (neck != null) neck.localRotation = Quaternion.identity;
            if (torso != null) torso.localRotation = Quaternion.identity;
            if (head != null) head.localRotation = Quaternion.identity;
            if (leftShoulder != null) leftShoulder.localRotation = Quaternion.identity;
            if (leftArm != null) leftArm.localRotation = Quaternion.identity;
            if (leftForearm != null) leftForearm.localRotation = Quaternion.identity;
            if (leftHand != null) leftHand.localRotation = Quaternion.identity;
            if (rightShoulder != null) rightShoulder.localRotation = Quaternion.identity;
            if (rightArm != null) rightArm.localRotation = Quaternion.identity;
            if (rightForearm != null) rightForearm.localRotation = Quaternion.identity;
            if (rightHand != null) rightHand.localRotation = Quaternion.identity;
            if (leftLeg != null) leftLeg.localRotation = Quaternion.identity;
            if (leftShin != null) leftShin.localRotation = Quaternion.identity;
            if (leftFoot != null) leftFoot.localRotation = Quaternion.identity;
            if (leftToe != null) leftToe.localRotation = Quaternion.identity;
            if (rightLeg != null) rightLeg.localRotation = Quaternion.identity;
            if (rightShin != null) rightShin.localRotation = Quaternion.identity;
            if (rightFoot != null) rightFoot.localRotation = Quaternion.identity;
            if (rightToe != null) rightToe.localRotation = Quaternion.identity;
        }

        public void ResetPosition()
        {
            if (activeAnimCoroutine != null) StopCoroutine(activeAnimCoroutine);

            transform.position = startPosition;
            transform.rotation = startRotation;
            ResetPose();
            ContactTriggeredThisKick = false;
            CurrentAnimState = PlayerAnimState.Idle;
            canMove = true;
        }

        public void SetControlEnabled(bool enabled)
        {
            canMove = enabled;
        }
    }
}
