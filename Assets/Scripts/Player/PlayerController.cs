#pragma warning disable 0649
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

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

        [Header("Skinned / Humanoid Skeleton Bones")]
        [SerializeField] private Transform characterVisuals;
        [SerializeField] private Transform hips;
        [SerializeField] private Transform spine;
        [SerializeField] private Transform chest;
        [SerializeField] private Transform neck;
        [SerializeField] private Transform torso;
        [SerializeField] private Transform head;
        [SerializeField] private Transform leftArm;
        [SerializeField] private Transform leftForearm;
        [SerializeField] private Transform leftHand;
        [SerializeField] private Transform rightArm;
        [SerializeField] private Transform rightForearm;
        [SerializeField] private Transform rightHand;
        [SerializeField] private Transform leftLeg;
        [SerializeField] private Transform leftShin;
        [SerializeField] private Transform leftFoot;
        [SerializeField] private Transform rightLeg;
        [SerializeField] private Transform rightShin;
        [SerializeField] private Transform rightFoot;

        public PlayerAnimState CurrentAnimState { get; private set; } = PlayerAnimState.Idle;
        public bool ContactTriggeredThisKick { get; private set; } = false;

        private Vector3 startPosition;
        private Quaternion startRotation;
        private Vector3 hipsBaseLocalPos;
        private bool canMove = true;
        private Coroutine activeAnimCoroutine;
        private float walkCycleTimer = 0f;

        public bool CanMove => canMove;

        private void Awake()
        {
            Instance = this;
            startPosition = transform.position;
            startRotation = transform.rotation;
            AutoAssignRigReferences();
            if (hips != null)
            {
                hipsBaseLocalPos = hips.localPosition;
                if (hipsBaseLocalPos.y < 0.2f) hipsBaseLocalPos = new Vector3(0f, 0.94f, 0f);
            }
        }

        private void Start()
        {
            AutoAssignRigReferences();
            if (hips != null && hipsBaseLocalPos.y < 0.2f)
            {
                hipsBaseLocalPos = hips.localPosition.y >= 0.2f ? hips.localPosition : new Vector3(0f, 0.94f, 0f);
            }
        }

        private void AutoAssignRigReferences()
        {
            if (characterVisuals == null) characterVisuals = transform.Find("Visuals") ?? transform.Find("Skeleton") ?? transform;

            hips = hips ?? FindBone(characterVisuals, "Hips");
            spine = spine ?? FindBone(characterVisuals, "Spine");
            chest = chest ?? FindBone(characterVisuals, "Chest");
            neck = neck ?? FindBone(characterVisuals, "Neck");
            torso = torso ?? chest ?? spine ?? FindBone(characterVisuals, "Torso");
            head = head ?? FindBone(characterVisuals, "Head");
            leftArm = leftArm ?? FindBone(characterVisuals, "LeftUpperArm") ?? FindBone(characterVisuals, "Arm_Left");
            leftForearm = leftForearm ?? FindBone(characterVisuals, "LeftForearm");
            leftHand = leftHand ?? FindBone(characterVisuals, "LeftHand");
            rightArm = rightArm ?? FindBone(characterVisuals, "RightUpperArm") ?? FindBone(characterVisuals, "Arm_Right");
            rightForearm = rightForearm ?? FindBone(characterVisuals, "RightForearm");
            rightHand = rightHand ?? FindBone(characterVisuals, "RightHand");
            leftLeg = leftLeg ?? FindBone(characterVisuals, "LeftThigh") ?? FindBone(characterVisuals, "Leg_Left");
            leftShin = leftShin ?? FindBone(characterVisuals, "LeftShin");
            leftFoot = leftFoot ?? FindBone(characterVisuals, "LeftFoot");
            rightLeg = rightLeg ?? FindBone(characterVisuals, "RightThigh") ?? FindBone(characterVisuals, "Leg_Right");
            rightShin = rightShin ?? FindBone(characterVisuals, "RightShin");
            rightFoot = rightFoot ?? FindBone(characterVisuals, "RightFoot");
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
            if (!canMove || Time.timeScale <= 0f) return;

            HandleMovementAndAnimation();
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

                walkCycleTimer += dt * 11f;
                float swing = Mathf.Sin(walkCycleTimer);
                float legAngle = swing * 28f;
                float leftKnee = Mathf.Max(0f, -swing) * 38f + 10f;
                float rightKnee = Mathf.Max(0f, swing) * 38f + 10f;
                float armAngle = Mathf.Cos(walkCycleTimer) * 24f;
                float bob = Mathf.Abs(Mathf.Sin(walkCycleTimer * 2f)) * 0.03f;

                float blend = 1f - Mathf.Exp(-18f * dt);
                if (hips != null) hips.localPosition = Vector3.Lerp(hips.localPosition, new Vector3(hipsBaseLocalPos.x, hipsBaseLocalPos.y + bob, hipsBaseLocalPos.z), blend);
                if (spine != null) spine.localRotation = Quaternion.Slerp(spine.localRotation, Quaternion.Euler(6f, moveDir.x * 10f, 0f), blend);
                if (leftLeg != null) leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, Quaternion.Euler(-legAngle, 0f, 0f), blend);
                if (leftShin != null) leftShin.localRotation = Quaternion.Slerp(leftShin.localRotation, Quaternion.Euler(leftKnee, 0f, 0f), blend);
                if (leftFoot != null) leftFoot.localRotation = Quaternion.Slerp(leftFoot.localRotation, Quaternion.Euler(-leftKnee * 0.3f, 0f, 0f), blend);
                if (rightLeg != null) rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, Quaternion.Euler(legAngle, 0f, 0f), blend);
                if (rightShin != null) rightShin.localRotation = Quaternion.Slerp(rightShin.localRotation, Quaternion.Euler(rightKnee, 0f, 0f), blend);
                if (rightFoot != null) rightFoot.localRotation = Quaternion.Slerp(rightFoot.localRotation, Quaternion.Euler(-rightKnee * 0.3f, 0f, 0f), blend);
                if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.Euler(armAngle, 0f, 10f), blend);
                if (leftForearm != null) leftForearm.localRotation = Quaternion.Slerp(leftForearm.localRotation, Quaternion.Euler(-25f, 0f, 0f), blend);
                if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler(-armAngle, 0f, -10f), blend);
                if (rightForearm != null) rightForearm.localRotation = Quaternion.Slerp(rightForearm.localRotation, Quaternion.Euler(-25f, 0f, 0f), blend);
            }
            else
            {
                bool isCharging = PlayerShooter.Instance != null && PlayerShooter.Instance.IsCharging;
                CurrentAnimState = isCharging ? PlayerAnimState.PreparingCharge : PlayerAnimState.Idle;
                walkCycleTimer += dt * (isCharging ? 4.5f : 2.2f);
                float breathe = Mathf.Sin(walkCycleTimer) * (isCharging ? 0.008f : 0.012f);
                float idleBlend = 1f - Mathf.Exp(-12f * dt);

                float hipDrop = isCharging ? 0.065f : 0.02f;
                float lean = isCharging ? 10f : 4f;
                float knee = isCharging ? 22f : 10f;

                if (hips != null) hips.localPosition = Vector3.Lerp(hips.localPosition, new Vector3(hipsBaseLocalPos.x, hipsBaseLocalPos.y - hipDrop + breathe, hipsBaseLocalPos.z), idleBlend);
                if (spine != null) spine.localRotation = Quaternion.Slerp(spine.localRotation, Quaternion.Euler(lean, 0f, 0f), idleBlend);
                if (chest != null) chest.localRotation = Quaternion.Slerp(chest.localRotation, Quaternion.Euler(lean * 0.7f, 0f, 0f), idleBlend);
                if (neck != null) neck.localRotation = Quaternion.Slerp(neck.localRotation, Quaternion.Euler(-lean * 0.4f, 0f, 0f), idleBlend);
                if (head != null) head.localRotation = Quaternion.Slerp(head.localRotation, Quaternion.Euler(-lean * 1.1f, 0f, 0f), idleBlend);

                if (leftLeg != null) leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, Quaternion.Euler(-knee * 0.55f, -5f, -4f), idleBlend);
                if (leftShin != null) leftShin.localRotation = Quaternion.Slerp(leftShin.localRotation, Quaternion.Euler(knee, 0f, 0f), idleBlend);
                if (leftFoot != null) leftFoot.localRotation = Quaternion.Slerp(leftFoot.localRotation, Quaternion.Euler(-knee * 0.35f, 0f, 0f), idleBlend);

                if (rightLeg != null) rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, Quaternion.Euler(knee * 0.25f, 5f, 4f), idleBlend);
                if (rightShin != null) rightShin.localRotation = Quaternion.Slerp(rightShin.localRotation, Quaternion.Euler(knee * 0.85f, 0f, 0f), idleBlend);
                if (rightFoot != null) rightFoot.localRotation = Quaternion.Slerp(rightFoot.localRotation, Quaternion.Euler(-knee * 0.3f, 0f, 0f), idleBlend);

                float armSway = Mathf.Sin(walkCycleTimer) * 2.5f;
                float armSpread = isCharging ? 22f : 14f;
                if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.Euler(8f, 0f, -armSpread + armSway), idleBlend);
                if (leftForearm != null) leftForearm.localRotation = Quaternion.Slerp(leftForearm.localRotation, Quaternion.Euler(isCharging ? -36f : -22f, 0f, 0f), idleBlend);
                if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler(8f, 0f, armSpread - armSway), idleBlend);
                if (rightForearm != null) rightForearm.localRotation = Quaternion.Slerp(rightForearm.localRotation, Quaternion.Euler(isCharging ? -36f : -22f, 0f, 0f), idleBlend);
            }
        }

        public void PlayKickAnimation(Vector3 targetBallPos, Action onKickContact)
        {
            canMove = false;
            ContactTriggeredThisKick = false;
            if (activeAnimCoroutine != null) StopCoroutine(activeAnimCoroutine);
            activeAnimCoroutine = StartCoroutine(KickRoutine(targetBallPos, onKickContact));
        }

        private IEnumerator KickRoutine(Vector3 targetBallPos, Action onKickContact)
        {
            // 1. Athletic curved approach run-up (0.26s) arriving at plant spot beside ball
            CurrentAnimState = PlayerAnimState.RunUp;
            Vector3 approachPos = new Vector3(targetBallPos.x - 0.28f, 0f, targetBallPos.z - 0.36f);
            Vector3 startP = transform.position;
            float runDuration = 0.26f;
            float t = 0f;

            while (t < runDuration)
            {
                float dt = Time.deltaTime;
                t += dt;
                float u = Mathf.Clamp01(t / runDuration);
                float p = Mathf.SmoothStep(0f, 1f, u);
                // Slight natural lateral curve into left-of-ball angle
                Vector3 pos = Vector3.Lerp(startP, approachPos, p);
                pos.x -= Mathf.Sin(u * Mathf.PI) * 0.08f;
                transform.position = pos;

                Vector3 lookDir = (targetBallPos - transform.position);
                lookDir.y = 0f;
                if (lookDir.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), dt * 16f);
                }

                float runPhase = u * Mathf.PI * 3.0f;
                float legS = Mathf.Sin(runPhase);
                float strideBob = Mathf.Abs(Mathf.Cos(runPhase)) * 0.035f;
                float blend = 1f - Mathf.Exp(-24f * dt);

                if (hips != null) hips.localPosition = Vector3.Lerp(hips.localPosition, new Vector3(hipsBaseLocalPos.x, hipsBaseLocalPos.y - 0.03f + strideBob, hipsBaseLocalPos.z), blend);
                if (spine != null) spine.localRotation = Quaternion.Slerp(spine.localRotation, Quaternion.Euler(14f, legS * 8f, 0f), blend);
                if (chest != null) chest.localRotation = Quaternion.Slerp(chest.localRotation, Quaternion.Euler(8f, -legS * 10f, 0f), blend);

                float lHip = legS * 38f;
                float lKnee = Mathf.Max(0f, -legS) * 48f + 14f;
                float rHip = -legS * 38f;
                float rKnee = Mathf.Max(0f, legS) * 48f + 14f;

                if (leftLeg != null) leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, Quaternion.Euler(lHip, 0f, 0f), blend);
                if (leftShin != null) leftShin.localRotation = Quaternion.Slerp(leftShin.localRotation, Quaternion.Euler(lKnee, 0f, 0f), blend);
                if (leftFoot != null) leftFoot.localRotation = Quaternion.Slerp(leftFoot.localRotation, Quaternion.Euler(-lKnee * 0.35f, 0f, 0f), blend);
                if (rightLeg != null) rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, Quaternion.Euler(rHip, 0f, 0f), blend);
                if (rightShin != null) rightShin.localRotation = Quaternion.Slerp(rightShin.localRotation, Quaternion.Euler(rKnee, 0f, 0f), blend);
                if (rightFoot != null) rightFoot.localRotation = Quaternion.Slerp(rightFoot.localRotation, Quaternion.Euler(-rKnee * 0.35f, 0f, 0f), blend);

                if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.Euler(-legS * 32f, 0f, -18f), blend);
                if (leftForearm != null) leftForearm.localRotation = Quaternion.Slerp(leftForearm.localRotation, Quaternion.Euler(-50f, 0f, 0f), blend);
                if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler(legS * 32f, 0f, 18f), blend);
                if (rightForearm != null) rightForearm.localRotation = Quaternion.Slerp(rightForearm.localRotation, Quaternion.Euler(-50f, 0f, 0f), blend);

                yield return null;
            }

            // 2. Plant left foot & cock kicking right leg deep back (0.10s)
            CurrentAnimState = PlayerAnimState.PlantAndWindUp;
            float cockTime = 0.10f;
            t = 0f;
            Quaternion lLegCockStart = leftLeg != null ? leftLeg.localRotation : Quaternion.identity;
            Quaternion rLegCockStart = rightLeg != null ? rightLeg.localRotation : Quaternion.identity;
            Quaternion rShinCockStart = rightShin != null ? rightShin.localRotation : Quaternion.identity;
            Quaternion spineCockStart = spine != null ? spine.localRotation : Quaternion.identity;
            while (t < cockTime)
            {
                t += Time.deltaTime;
                float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / cockTime));
                if (hips != null) hips.localRotation = Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(4f, -12f, -10f), p);
                if (leftLeg != null) leftLeg.localRotation = Quaternion.Slerp(lLegCockStart, Quaternion.Euler(-18f, 0f, -8f), p);
                if (leftShin != null) leftShin.localRotation = Quaternion.Slerp(leftShin.localRotation, Quaternion.Euler(26f, 0f, 0f), p);
                if (leftFoot != null) leftFoot.localRotation = Quaternion.Slerp(leftFoot.localRotation, Quaternion.Euler(-8f, 0f, 0f), p);

                if (rightLeg != null) rightLeg.localRotation = Quaternion.Slerp(rLegCockStart, Quaternion.Euler(58f, 0f, 8f), p);
                if (rightShin != null) rightShin.localRotation = Quaternion.Slerp(rShinCockStart, Quaternion.Euler(76f, 0f, 0f), p);
                if (rightFoot != null) rightFoot.localRotation = Quaternion.Slerp(rightFoot.localRotation, Quaternion.Euler(24f, 0f, 0f), p);

                if (spine != null) spine.localRotation = Quaternion.Slerp(spineCockStart, Quaternion.Euler(10f, -24f, -8f), p);
                if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.Euler(-24f, 0f, -58f), p);
                if (leftForearm != null) leftForearm.localRotation = Quaternion.Slerp(leftForearm.localRotation, Quaternion.Euler(-18f, 0f, 0f), p);
                if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler(26f, 0f, 20f), p);
                yield return null;
            }

            // 3. Whip kicking leg forward into exact boot-ball contact (0.065s -> total pre-contact 0.425s)
            CurrentAnimState = PlayerAnimState.ContactStrike;
            float swingTime = 0.065f;
            t = 0f;
            while (t < swingTime)
            {
                t += Time.deltaTime;
                float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / swingTime));
                if (hips != null) hips.localRotation = Quaternion.Slerp(Quaternion.Euler(4f, -12f, -10f), Quaternion.Euler(6f, 6f, -6f), p);
                if (rightLeg != null) rightLeg.localRotation = Quaternion.Slerp(Quaternion.Euler(58f, 0f, 8f), Quaternion.Euler(-22f, 0f, 0f), p);
                if (rightShin != null) rightShin.localRotation = Quaternion.Slerp(Quaternion.Euler(76f, 0f, 0f), Quaternion.Euler(8f, 0f, 0f), p);
                if (rightFoot != null) rightFoot.localRotation = Quaternion.Slerp(Quaternion.Euler(24f, 0f, 0f), Quaternion.Euler(14f, 12f, 0f), p);
                if (spine != null) spine.localRotation = Quaternion.Slerp(Quaternion.Euler(10f, -24f, -8f), Quaternion.Euler(12f, 8f, 4f), p);
                yield return null;
            }

            // 4. Exact Boot-Ball Contact Strike! (Ball NEVER launches before this moment!)
            ContactTriggeredThisKick = true;
            onKickContact?.Invoke();

            // 5. Dynamic Follow-Through (0.22s)
            CurrentAnimState = PlayerAnimState.FollowThrough;
            float followTime = 0.22f;
            t = 0f;
            while (t < followTime)
            {
                t += Time.deltaTime;
                float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / followTime));
                if (hips != null) hips.localRotation = Quaternion.Slerp(Quaternion.Euler(6f, 6f, -6f), Quaternion.Euler(4f, 18f, 4f), p);
                if (rightLeg != null) rightLeg.localRotation = Quaternion.Slerp(Quaternion.Euler(-22f, 0f, 0f), Quaternion.Euler(-78f, -10f, -8f), p);
                if (rightShin != null) rightShin.localRotation = Quaternion.Slerp(rightShin.localRotation, Quaternion.Euler(12f, 0f, 0f), p);
                if (spine != null) spine.localRotation = Quaternion.Slerp(Quaternion.Euler(12f, 8f, 4f), Quaternion.Euler(14f, 22f, 8f), p);
                if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.Euler(22f, 0f, -26f), p);
                if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler(-56f, 0f, 34f), p);
                yield return null;
            }

            CurrentAnimState = PlayerAnimState.Recovering;
            yield return SmoothReturnToIdlePose(0.24f);
        }

        public void PlayGoalCelebration()
        {
            CurrentAnimState = PlayerAnimState.Celebrating;
            if (activeAnimCoroutine != null) StopCoroutine(activeAnimCoroutine);
            activeAnimCoroutine = StartCoroutine(GoalCelebrationRoutine());
        }

        private IEnumerator GoalCelebrationRoutine()
        {
            canMove = false;
            CurrentAnimState = PlayerAnimState.Celebrating;
            float timer = 0f;
            Vector3 basePos = new Vector3(transform.position.x, 0f, transform.position.z);

            while (timer < 1.35f)
            {
                timer += Time.deltaTime;
                float jump = Mathf.Abs(Mathf.Sin(timer * 7.5f)) * 0.34f;
                float blend = 1f - Mathf.Exp(-14f * Time.deltaTime);
                transform.position = new Vector3(basePos.x, jump, basePos.z);

                if (spine != null) spine.localRotation = Quaternion.Slerp(spine.localRotation, Quaternion.Euler(-10f, Mathf.Sin(timer * 5f) * 14f, 0f), blend);
                if (head != null) head.localRotation = Quaternion.Slerp(head.localRotation, Quaternion.Euler(-18f, 0f, 0f), blend);
                if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.Euler(-148f, 0f, -22f), blend);
                if (leftForearm != null) leftForearm.localRotation = Quaternion.Slerp(leftForearm.localRotation, Quaternion.Euler(-20f, 0f, 0f), blend);
                if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler(-148f, 0f, 22f), blend);
                if (rightForearm != null) rightForearm.localRotation = Quaternion.Slerp(rightForearm.localRotation, Quaternion.Euler(-20f, 0f, 0f), blend);
                if (leftLeg != null) leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, Quaternion.Euler(-jump * 40f, 0f, 0f), blend);
                if (leftShin != null) leftShin.localRotation = Quaternion.Slerp(leftShin.localRotation, Quaternion.Euler(jump * 80f, 0f, 0f), blend);
                if (rightLeg != null) rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, Quaternion.Euler(-jump * 40f, 0f, 0f), blend);
                if (rightShin != null) rightShin.localRotation = Quaternion.Slerp(rightShin.localRotation, Quaternion.Euler(jump * 80f, 0f, 0f), blend);

                yield return null;
            }
            transform.position = basePos;
            yield return SmoothReturnToIdlePose(0.22f);
        }

        public void PlayMissReaction()
        {
            CurrentAnimState = PlayerAnimState.Frustrated;
            if (activeAnimCoroutine != null) StopCoroutine(activeAnimCoroutine);
            activeAnimCoroutine = StartCoroutine(MissReactionRoutine());
        }

        private IEnumerator MissReactionRoutine()
        {
            canMove = false;
            CurrentAnimState = PlayerAnimState.Frustrated;
            float timer = 0f;

            while (timer < 1.35f)
            {
                timer += Time.deltaTime;
                float blend = 1f - Mathf.Exp(-12f * Time.deltaTime);
                if (spine != null) spine.localRotation = Quaternion.Slerp(spine.localRotation, Quaternion.Euler(18f, 0f, 0f), blend);
                if (head != null) head.localRotation = Quaternion.Slerp(head.localRotation, Quaternion.Euler(22f, Mathf.Sin(timer * 6f) * 10f, 0f), blend);
                if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.Euler(-115f, 0f, -38f), blend);
                if (leftForearm != null) leftForearm.localRotation = Quaternion.Slerp(leftForearm.localRotation, Quaternion.Euler(-95f, 0f, 0f), blend);
                if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler(-115f, 0f, 38f), blend);
                if (rightForearm != null) rightForearm.localRotation = Quaternion.Slerp(rightForearm.localRotation, Quaternion.Euler(-95f, 0f, 0f), blend);

                yield return null;
            }
            yield return SmoothReturnToIdlePose(0.22f);
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
                if (chest != null) chest.localRotation = Quaternion.Slerp(chest.localRotation, Quaternion.identity, blend);
                if (neck != null) neck.localRotation = Quaternion.Slerp(neck.localRotation, Quaternion.identity, blend);
                if (torso != null) torso.localRotation = Quaternion.Slerp(torso.localRotation, Quaternion.identity, blend);
                if (head != null) head.localRotation = Quaternion.Slerp(head.localRotation, Quaternion.identity, blend);
                if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.identity, blend);
                if (leftForearm != null) leftForearm.localRotation = Quaternion.Slerp(leftForearm.localRotation, Quaternion.identity, blend);
                if (leftHand != null) leftHand.localRotation = Quaternion.Slerp(leftHand.localRotation, Quaternion.identity, blend);
                if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.identity, blend);
                if (rightForearm != null) rightForearm.localRotation = Quaternion.Slerp(rightForearm.localRotation, Quaternion.identity, blend);
                if (rightHand != null) rightHand.localRotation = Quaternion.Slerp(rightHand.localRotation, Quaternion.identity, blend);
                if (leftLeg != null) leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, Quaternion.identity, blend);
                if (leftShin != null) leftShin.localRotation = Quaternion.Slerp(leftShin.localRotation, Quaternion.identity, blend);
                if (leftFoot != null) leftFoot.localRotation = Quaternion.Slerp(leftFoot.localRotation, Quaternion.identity, blend);
                if (rightLeg != null) rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, Quaternion.identity, blend);
                if (rightShin != null) rightShin.localRotation = Quaternion.Slerp(rightShin.localRotation, Quaternion.identity, blend);
                if (rightFoot != null) rightFoot.localRotation = Quaternion.Slerp(rightFoot.localRotation, Quaternion.identity, blend);

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
            if (chest != null) chest.localRotation = Quaternion.identity;
            if (neck != null) neck.localRotation = Quaternion.identity;
            if (torso != null) torso.localRotation = Quaternion.identity;
            if (head != null) head.localRotation = Quaternion.identity;
            if (leftArm != null) leftArm.localRotation = Quaternion.identity;
            if (leftForearm != null) leftForearm.localRotation = Quaternion.identity;
            if (leftHand != null) leftHand.localRotation = Quaternion.identity;
            if (rightArm != null) rightArm.localRotation = Quaternion.identity;
            if (rightForearm != null) rightForearm.localRotation = Quaternion.identity;
            if (rightHand != null) rightHand.localRotation = Quaternion.identity;
            if (leftLeg != null) leftLeg.localRotation = Quaternion.identity;
            if (leftShin != null) leftShin.localRotation = Quaternion.identity;
            if (leftFoot != null) leftFoot.localRotation = Quaternion.identity;
            if (rightLeg != null) rightLeg.localRotation = Quaternion.identity;
            if (rightShin != null) rightShin.localRotation = Quaternion.identity;
            if (rightFoot != null) rightFoot.localRotation = Quaternion.identity;
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
