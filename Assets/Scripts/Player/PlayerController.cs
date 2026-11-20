#pragma warning disable 0649
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UltimatePenaltyCup.Player
{
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 4.0f;
        [SerializeField] private Vector2 movementBoundsX = new Vector2(-2.5f, 2.5f);
        [SerializeField] private Vector2 movementBoundsZ = new Vector2(-14.5f, -11.8f);

        [Header("Humanoid Rig Components")]
        [SerializeField] private Transform characterVisuals;
        [SerializeField] private Transform head;
        [SerializeField] private Transform torso;
        [SerializeField] private Transform leftArm;
        [SerializeField] private Transform rightArm;
        [SerializeField] private Transform leftLeg;
        [SerializeField] private Transform rightLeg; // Kicking leg

        private Vector3 startPosition;
        private Quaternion startRotation;
        private bool canMove = true;
        private Coroutine activeAnimCoroutine;
        private float walkCycleTimer = 0f;
        private bool isIdle = true;

        public bool CanMove => canMove;

        private void Awake()
        {
            Instance = this;
            startPosition = transform.position;
            startRotation = transform.rotation;
        }

        private void Start()
        {
            AutoAssignRigReferences();
        }

        private void AutoAssignRigReferences()
        {
            if (characterVisuals == null) characterVisuals = transform.Find("Visuals");
            if (characterVisuals != null)
            {
                if (torso == null) torso = characterVisuals.Find("Torso");
                if (head == null) head = characterVisuals.Find("Head");
                if (leftArm == null) leftArm = characterVisuals.Find("Arm_Left");
                if (rightArm == null) rightArm = characterVisuals.Find("Arm_Right");
                if (leftLeg == null) leftLeg = characterVisuals.Find("Leg_Left");
                if (rightLeg == null) rightLeg = characterVisuals.Find("Leg_Right");
            }
        }

        private void Update()
        {
            if (!canMove) return;

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

            if (moveDir.sqrMagnitude > 0.01f)
            {
                isIdle = false;
                Vector3 newPos = transform.position + moveDir * moveSpeed * Time.deltaTime;
                newPos.x = Mathf.Clamp(newPos.x, movementBoundsX.x, movementBoundsX.y);
                newPos.z = Mathf.Clamp(newPos.z, movementBoundsZ.x, movementBoundsZ.y);
                transform.position = newPos;

                // Humanoid walk/jog animation cycle
                walkCycleTimer += Time.deltaTime * 10f;
                float legAngle = Mathf.Sin(walkCycleTimer) * 28f;
                float armAngle = Mathf.Cos(walkCycleTimer) * 25f;
                float bodyBob = Mathf.Abs(Mathf.Sin(walkCycleTimer * 2f)) * 0.05f;

                if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(-legAngle, 0f, 0f);
                if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(legAngle, 0f, 0f);
                if (leftArm != null) leftArm.localRotation = Quaternion.Euler(armAngle, 0f, 15f);
                if (rightArm != null) rightArm.localRotation = Quaternion.Euler(-armAngle, 0f, -15f);

                if (characterVisuals != null)
                {
                    characterVisuals.localPosition = new Vector3(0f, bodyBob, 0f);
                    characterVisuals.localRotation = Quaternion.Euler(5f, moveDir.x * 12f, -moveDir.x * 6f);
                }
            }
            else
            {
                // Idle breathing anticipation
                isIdle = true;
                walkCycleTimer += Time.deltaTime * 2.5f;
                float breathe = Mathf.Sin(walkCycleTimer) * 0.02f;

                if (characterVisuals != null)
                {
                    characterVisuals.localPosition = new Vector3(0f, breathe, 0f);
                    characterVisuals.localRotation = Quaternion.identity;
                }

                if (leftLeg != null) leftLeg.localRotation = Quaternion.identity;
                if (rightLeg != null) rightLeg.localRotation = Quaternion.identity;
                if (leftArm != null) leftArm.localRotation = Quaternion.Euler(0f, 0f, 12f + Mathf.Sin(walkCycleTimer) * 3f);
                if (rightArm != null) rightArm.localRotation = Quaternion.Euler(0f, 0f, -12f - Mathf.Sin(walkCycleTimer) * 3f);
            }
        }

        public void PlayKickAnimation(Vector3 targetBallPos, Action onKickContact)
        {
            canMove = false;
            if (activeAnimCoroutine != null) StopCoroutine(activeAnimCoroutine);
            activeAnimCoroutine = StartCoroutine(KickRoutine(targetBallPos, onKickContact));
        }

        private IEnumerator KickRoutine(Vector3 targetBallPos, Action onKickContact)
        {
            // 1. Explosive run-up towards ball
            Vector3 approachPos = targetBallPos + new Vector3(-0.35f, 0f, -0.4f);
            Vector3 startP = transform.position;
            float runDuration = 0.24f;
            float t = 0f;

            while (t < runDuration)
            {
                t += Time.deltaTime;
                float progress = Mathf.Clamp01(t / runDuration);
                transform.position = Vector3.Lerp(startP, approachPos, progress);

                // Run-up leg pump
                float runPhase = progress * Mathf.PI * 4f;
                if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(Mathf.Sin(runPhase) * 40f, 0f, 0f);
                if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(-Mathf.Sin(runPhase) * 40f, 0f, 0f);
                if (leftArm != null) leftArm.localRotation = Quaternion.Euler(-Mathf.Sin(runPhase) * 35f, 0f, 15f);
                if (rightArm != null) rightArm.localRotation = Quaternion.Euler(Mathf.Sin(runPhase) * 35f, 0f, -15f);

                yield return null;
            }

            // 2. Cock kicking leg back with torso twist
            float cockTime = 0.09f;
            t = 0f;
            while (t < cockTime)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / cockTime);
                if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(Mathf.Lerp(0f, -65f, p), 0f, 5f);
                if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(Mathf.Lerp(0f, 15f, p), 0f, -5f); // Plant foot
                if (torso != null) torso.localRotation = Quaternion.Euler(0f, Mathf.Lerp(0f, -20f, p), -5f);
                if (leftArm != null) leftArm.localRotation = Quaternion.Euler(-30f, 0f, 45f); // Counterbalance
                yield return null;
            }

            // 3. Kick strike contact
            onKickContact?.Invoke();

            // 4. Explosive follow-through swing
            float followTime = 0.16f;
            t = 0f;
            while (t < followTime)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / followTime);
                if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(Mathf.Lerp(-65f, 75f, p), 0f, 0f);
                if (torso != null) torso.localRotation = Quaternion.Euler(10f, Mathf.Lerp(-20f, 15f, p), 0f);
                if (rightArm != null) rightArm.localRotation = Quaternion.Euler(-45f, 0f, -30f);
                yield return null;
            }

            // Return to standing
            yield return new WaitForSeconds(0.1f);
            ResetPose();
        }

        public void PlayGoalCelebration()
        {
            if (activeAnimCoroutine != null) StopCoroutine(activeAnimCoroutine);
            activeAnimCoroutine = StartCoroutine(GoalCelebrationRoutine());
        }

        private IEnumerator GoalCelebrationRoutine()
        {
            canMove = false;
            float timer = 0f;
            Vector3 basePos = transform.position;

            while (timer < 2.0f)
            {
                timer += Time.deltaTime;
                float jump = Mathf.Abs(Mathf.Sin(timer * 8f)) * 0.45f;
                transform.position = new Vector3(basePos.x, basePos.y + jump, basePos.z);

                // Both arms raised high in victory!
                if (leftArm != null) leftArm.localRotation = Quaternion.Euler(-150f, 0f, 25f);
                if (rightArm != null) rightArm.localRotation = Quaternion.Euler(-150f, 0f, -25f);
                if (torso != null) torso.localRotation = Quaternion.Euler(-10f, Mathf.Sin(timer * 6f) * 15f, 0f);
                if (head != null) head.localRotation = Quaternion.Euler(-20f, 0f, 0f); // Looking up

                yield return null;
            }
            transform.position = basePos;
            ResetPose();
        }

        public void PlayMissReaction()
        {
            if (activeAnimCoroutine != null) StopCoroutine(activeAnimCoroutine);
            activeAnimCoroutine = StartCoroutine(MissReactionRoutine());
        }

        private IEnumerator MissReactionRoutine()
        {
            canMove = false;
            float timer = 0f;

            while (timer < 1.8f)
            {
                timer += Time.deltaTime;
                // Hands on head in disbelief
                if (leftArm != null) leftArm.localRotation = Quaternion.Euler(-110f, 0f, 65f);
                if (rightArm != null) rightArm.localRotation = Quaternion.Euler(-110f, 0f, -65f);
                if (torso != null) torso.localRotation = Quaternion.Euler(20f, 0f, 0f); // Slumped
                if (head != null) head.localRotation = Quaternion.Euler(25f, 0f, 0f); // Looking down

                yield return null;
            }
            ResetPose();
        }

        private void ResetPose()
        {
            if (characterVisuals != null)
            {
                characterVisuals.localPosition = Vector3.zero;
                characterVisuals.localRotation = Quaternion.identity;
            }
            if (torso != null) torso.localRotation = Quaternion.identity;
            if (head != null) head.localRotation = Quaternion.identity;
            if (leftArm != null) leftArm.localRotation = Quaternion.identity;
            if (rightArm != null) rightArm.localRotation = Quaternion.identity;
            if (leftLeg != null) leftLeg.localRotation = Quaternion.identity;
            if (rightLeg != null) rightLeg.localRotation = Quaternion.identity;
        }

        public void ResetPosition()
        {
            if (activeAnimCoroutine != null) StopCoroutine(activeAnimCoroutine);

            transform.position = startPosition;
            transform.rotation = startRotation;
            ResetPose();
            canMove = true;
        }

        public void SetControlEnabled(bool enabled)
        {
            canMove = enabled;
        }
    }
}
