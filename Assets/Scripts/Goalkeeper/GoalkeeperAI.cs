#pragma warning disable 0649
using System;
using System.Collections;
using UnityEngine;
using UltimatePenaltyCup.Levels;
using UltimatePenaltyCup.Gameplay;
using UltimatePenaltyCup.Audio;

namespace UltimatePenaltyCup.Goalkeeper
{
    public enum GoalkeeperState
    {
        Idle,
        Ready,
        Reacting,
        DivingLeft,
        DivingRight,
        Saving,
        Recovering,
        Resetting
    }

    public class GoalkeeperAI : MonoBehaviour
    {
        public static GoalkeeperAI Instance { get; private set; }

        [Header("Tuning (Set dynamically by RoundDataSO)")]
        [SerializeField] private float reactionDelay = 0.24f;
        [SerializeField] private float moveSpeed = 4.2f;
        [SerializeField] private float diveSpeed = 5.4f;
        [SerializeField] private float errorProbability = 0.40f;
        [SerializeField] private float reachRadius = 0.78f;

        [Header("Goal Bounds")]
        [SerializeField] private float minX = -3.35f;
        [SerializeField] private float maxX = 3.35f;
        [SerializeField] private float minY = 0.0f;
        [SerializeField] private float maxY = 2.25f;

        [Header("Skinned / Humanoid Skeleton Bones")]
        [SerializeField] private Transform bodyRoot;
        [SerializeField] private Transform hips;
        [SerializeField] private Transform spine;
        [SerializeField] private Transform chest;
        [SerializeField] private Transform torso;
        [SerializeField] private Transform head;
        [SerializeField] private Transform leftArm;
        [SerializeField] private Transform leftForearm;
        [SerializeField] private Transform rightArm;
        [SerializeField] private Transform rightForearm;
        [SerializeField] private Transform leftLeg;
        [SerializeField] private Transform leftShin;
        [SerializeField] private Transform rightLeg;
        [SerializeField] private Transform rightShin;

        [Header("Glove Intercept Anchors")]
        [SerializeField] private Transform leftGloveAnchor;
        [SerializeField] private Transform rightGloveAnchor;

        public GoalkeeperState CurrentState { get; private set; } = GoalkeeperState.Idle;
        public bool HasDived { get; private set; } = false;

        private Rigidbody rb;
        private Vector3 startPosition;
        private Quaternion startRotation;
        private Vector3 hipsBaseLocalPos;
        private bool isShotActive = false;
        private bool hasSavedCurrentShot = false;
        private float currentRoundNumber = 1;

        private Coroutine reactionCoroutine;
        private Coroutine idleShuffleCoroutine;
        private Coroutine animCoroutine;

        public event Action OnSaveAttempted;

        private void Awake()
        {
            Instance = this;
            rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            }
            startPosition = transform.position;
            startRotation = transform.rotation;
        }

        private void Start()
        {
            AutoAssignRigReferences();
            if (hips != null) hipsBaseLocalPos = hips.localPosition;
            StartIdleShuffle();
        }

        private void AutoAssignRigReferences()
        {
            if (bodyRoot == null) bodyRoot = transform.Find("Visuals") ?? transform.Find("Skeleton") ?? transform;

            hips = hips ?? FindBone(bodyRoot, "Hips");
            spine = spine ?? FindBone(bodyRoot, "Spine");
            chest = chest ?? FindBone(bodyRoot, "Chest");
            torso = torso ?? chest ?? spine ?? FindBone(bodyRoot, "Torso");
            head = head ?? FindBone(bodyRoot, "Head");
            leftArm = leftArm ?? FindBone(bodyRoot, "LeftUpperArm") ?? FindBone(bodyRoot, "Arm_Left");
            leftForearm = leftForearm ?? FindBone(bodyRoot, "LeftForearm");
            rightArm = rightArm ?? FindBone(bodyRoot, "RightUpperArm") ?? FindBone(bodyRoot, "Arm_Right");
            rightForearm = rightForearm ?? FindBone(bodyRoot, "RightForearm");
            leftLeg = leftLeg ?? FindBone(bodyRoot, "LeftThigh") ?? FindBone(bodyRoot, "Leg_Left");
            leftShin = leftShin ?? FindBone(bodyRoot, "LeftShin");
            rightLeg = rightLeg ?? FindBone(bodyRoot, "RightThigh") ?? FindBone(bodyRoot, "Leg_Right");
            rightShin = rightShin ?? FindBone(bodyRoot, "RightShin");

            leftGloveAnchor = leftGloveAnchor ?? FindBone(bodyRoot, "LeftHand") ?? FindBone(bodyRoot, "GoalkeeperGlove_L");
            rightGloveAnchor = rightGloveAnchor ?? FindBone(bodyRoot, "RightHand") ?? FindBone(bodyRoot, "GoalkeeperGlove_R");
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

        public void Configure(RoundDataSO roundData)
        {
            if (roundData != null)
            {
                currentRoundNumber = roundData.roundNumber;
                reactionDelay = roundData.reactionDelay;
                moveSpeed = roundData.moveSpeed;
                diveSpeed = roundData.diveSpeed;
                errorProbability = roundData.errorProbability;
                reachRadius = 0.68f + (roundData.roundNumber - 1) * 0.09f; // 0.68m in R1 -> 1.04m in R5
            }
        }

        public void StartIdleShuffle()
        {
            if (idleShuffleCoroutine != null) StopCoroutine(idleShuffleCoroutine);
            CurrentState = GoalkeeperState.Ready;
            idleShuffleCoroutine = StartCoroutine(IdleShuffleRoutine());
        }

        private IEnumerator IdleShuffleRoutine()
        {
            float timer = 0f;
            while (!isShotActive)
            {
                timer += Time.deltaTime * 2.8f;
                float shuffleAmp = 0.45f + (currentRoundNumber - 1) * 0.05f;
                float offsetX = Mathf.Sin(timer) * shuffleAmp;
                Vector3 nextPos = new Vector3(startPosition.x + offsetX, startPosition.y, startPosition.z);
                MoveRoot(nextPos, startRotation);

                // Athletic goalie crouch stance (knees bent, chest forward, gloves ready)
                float crouch = Mathf.Abs(Mathf.Sin(timer * 2f)) * 0.05f;
                if (hips != null)
                {
                    hips.localPosition = new Vector3(hipsBaseLocalPos.x, hipsBaseLocalPos.y - 0.08f - crouch, hipsBaseLocalPos.z);
                    hips.localRotation = Quaternion.identity;
                }
                if (spine != null) spine.localRotation = Quaternion.Euler(14f, 0f, 0f);
                if (chest != null) chest.localRotation = Quaternion.Euler(10f, 0f, 0f);
                if (head != null) head.localRotation = Quaternion.Euler(-18f, 0f, 0f);

                if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(-22f, 0f, -8f);
                if (leftShin != null) leftShin.localRotation = Quaternion.Euler(34f, 0f, 0f);
                if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(-22f, 0f, 8f);
                if (rightShin != null) rightShin.localRotation = Quaternion.Euler(34f, 0f, 0f);

                float armBob = Mathf.Sin(timer * 2f) * 6f;
                if (leftArm != null) leftArm.localRotation = Quaternion.Euler(-38f, 15f, -32f + armBob);
                if (leftForearm != null) leftForearm.localRotation = Quaternion.Euler(-55f, 0f, 0f);
                if (rightArm != null) rightArm.localRotation = Quaternion.Euler(-38f, -15f, 32f - armBob);
                if (rightForearm != null) rightForearm.localRotation = Quaternion.Euler(-55f, 0f, 0f);

                yield return null;
            }
        }

        public void OnShotFired(Vector3 ballPos, Vector3 shotVelocity, float power)
        {
            isShotActive = true;
            hasSavedCurrentShot = false;
            HasDived = false;
            CurrentState = GoalkeeperState.Reacting;

            if (idleShuffleCoroutine != null)
            {
                StopCoroutine(idleShuffleCoroutine);
                idleShuffleCoroutine = null;
            }

            if (reactionCoroutine != null) StopCoroutine(reactionCoroutine);
            reactionCoroutine = StartCoroutine(ReactToShotRoutine(ballPos, shotVelocity, power));
        }

        private IEnumerator ReactToShotRoutine(Vector3 ballPos, Vector3 shotVelocity, float power)
        {
            // Keep eyes locked and tense crouch during human reaction time
            yield return new WaitForSeconds(reactionDelay);

            float dz = startPosition.z - ballPos.z;
            float vz = Mathf.Max(1.0f, shotVelocity.z);
            float timeToGoal = Mathf.Clamp(dz / vz, 0.12f, 1.5f);

            // Predict exact ball position when it crosses goalkeeper's Z plane
            float predictedX = ballPos.x + shotVelocity.x * timeToGoal;
            float predictedY = ballPos.y + shotVelocity.y * timeToGoal - 0.5f * 9.81f * timeToGoal * timeToGoal;

            // Apply round-based goalkeeper error margin (central shots rarely misjudged)
            bool isWideShot = Mathf.Abs(predictedX) > 1.0f;
            float effectiveError = isWideShot ? errorProbability : errorProbability * 0.35f;

            if (UnityEngine.Random.value < effectiveError)
            {
                // Misjudge direction or timing on lower rounds
                float offset = (UnityEngine.Random.value < 0.5f ? -1f : 1f) * UnityEngine.Random.Range(1.0f, 2.2f);
                predictedX += offset;
                predictedY += UnityEngine.Random.Range(-0.5f, 0.6f);
            }

            predictedX = Mathf.Clamp(predictedX, minX, maxX);
            predictedY = Mathf.Clamp(predictedY, minY, maxY);

            // Physical reach constraint based on diveSpeed & remaining flight time
            Vector3 fromPos = transform.position;
            float availableDiveTime = Mathf.Max(0.14f, timeToGoal - reactionDelay);
            float maxTravelDist = diveSpeed * availableDiveTime * 1.15f;

            float targetDeltaX = predictedX - fromPos.x;
            if (Mathf.Abs(targetDeltaX) > maxTravelDist)
            {
                targetDeltaX = Mathf.Sign(targetDeltaX) * maxTravelDist;
            }

            float finalX = Mathf.Clamp(fromPos.x + targetDeltaX, minX, maxX);
            float finalY = Mathf.Clamp(predictedY, 0.0f, 1.45f); // Center of mass max height
            bool reachHighHands = predictedY > 1.45f;

            HasDived = true;
            OnSaveAttempted?.Invoke();

            float diveDistX = Mathf.Abs(finalX - fromPos.x);
            // Remember keeper faces -Z (180° Y), so moving toward world +X is keeper's LEFT side!
            bool divingTowardWorldPosX = targetDeltaX > 0f;

            if (diveDistX > 0.55f)
            {
                CurrentState = divingTowardWorldPosX ? GoalkeeperState.DivingLeft : GoalkeeperState.DivingRight;
            }
            else
            {
                CurrentState = GoalkeeperState.Saving;
            }

            float diveDuration = Mathf.Clamp(availableDiveTime * 0.92f, 0.22f, 0.45f);
            float diveTimer = 0f;

            while (diveTimer < diveDuration && !hasSavedCurrentShot)
            {
                diveTimer += Time.deltaTime;
                float t = Mathf.Clamp01(diveTimer / diveDuration);
                float smoothT = Mathf.Sin(t * Mathf.PI * 0.5f); // Fast explosive start

                float curX = Mathf.Lerp(fromPos.x, finalX, smoothT);
                // Arc trajectory that lands cleanly on turf (never < 0)
                float jumpArc = Mathf.Sin(t * Mathf.PI) * Mathf.Clamp(finalY * 0.65f, 0.08f, 0.85f);
                float curY = Mathf.Max(0f, Mathf.Lerp(0f, finalY * 0.35f, smoothT) + jumpArc * (diveDistX > 0.55f ? 0.8f : 1.0f));

                MoveRoot(new Vector3(curX, curY, startPosition.z), startRotation);

                if (diveDistX > 0.55f)
                {
                    // Athletic horizontal dive pose on Hips (keeps feet clear & natural)
                    float localRollDir = divingTowardWorldPosX ? -1f : 1f;
                    if (hips != null)
                    {
                        hips.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, 68f * localRollDir, smoothT));
                        hips.localPosition = new Vector3(hipsBaseLocalPos.x, Mathf.Lerp(hipsBaseLocalPos.y, 0.48f, smoothT), hipsBaseLocalPos.z);
                    }
                    if (spine != null) spine.localRotation = Quaternion.Euler(8f, 0f, 0f);
                    if (chest != null) chest.localRotation = Quaternion.Euler(5f, 0f, 0f);
                    if (head != null) head.localRotation = Quaternion.Euler(0f, 0f, -30f * localRollDir);

                    // Extend arms along dive direction to catch/parry
                    if (divingTowardWorldPosX)
                    {
                        if (leftArm != null) leftArm.localRotation = Quaternion.Euler(-15f, 0f, -155f);
                        if (leftForearm != null) leftForearm.localRotation = Quaternion.Euler(-10f, 0f, 0f);
                        if (rightArm != null) rightArm.localRotation = Quaternion.Euler(-25f, 0f, -110f);
                        if (rightForearm != null) rightForearm.localRotation = Quaternion.Euler(-20f, 0f, 0f);
                    }
                    else
                    {
                        if (rightArm != null) rightArm.localRotation = Quaternion.Euler(-15f, 0f, 155f);
                        if (rightForearm != null) rightForearm.localRotation = Quaternion.Euler(-10f, 0f, 0f);
                        if (leftArm != null) leftArm.localRotation = Quaternion.Euler(-25f, 0f, 110f);
                        if (leftForearm != null) leftForearm.localRotation = Quaternion.Euler(-20f, 0f, 0f);
                    }

                    if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(15f, 0f, -12f * localRollDir);
                    if (leftShin != null) leftShin.localRotation = Quaternion.Euler(22f, 0f, 0f);
                    if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(-10f, 0f, -12f * localRollDir);
                    if (rightShin != null) rightShin.localRotation = Quaternion.Euler(18f, 0f, 0f);
                }
                else
                {
                    // Central save / high leap
                    if (hips != null) hips.localRotation = Quaternion.identity;
                    float upAngle = reachHighHands ? -155f : -85f;
                    if (leftArm != null) leftArm.localRotation = Quaternion.Euler(upAngle, 10f, -18f);
                    if (leftForearm != null) leftForearm.localRotation = Quaternion.Euler(-15f, 0f, 0f);
                    if (rightArm != null) rightArm.localRotation = Quaternion.Euler(upAngle, -10f, 18f);
                    if (rightForearm != null) rightForearm.localRotation = Quaternion.Euler(-15f, 0f, 0f);
                }

                yield return null;
            }

            // Settle onto turf if diving
            if (CurrentState == GoalkeeperState.DivingLeft || CurrentState == GoalkeeperState.DivingRight)
            {
                CurrentState = GoalkeeperState.Recovering;
                Vector3 landPos = transform.position;
                float settleT = 0f;
                while (settleT < 0.25f)
                {
                    settleT += Time.deltaTime;
                    float p = Mathf.Clamp01(settleT / 0.25f);
                    MoveRoot(new Vector3(landPos.x, Mathf.Lerp(landPos.y, 0f, p), startPosition.z), startRotation);
                    yield return null;
                }
            }
        }

        private void FixedUpdate()
        {
            if (!isShotActive || hasSavedCurrentShot) return;

            BallController ball = BallController.Instance;
            if (ball == null || !ball.HasBeenShot || ball.WasSavedByGoalkeeper) return;

            Vector3 ballPos = ball.transform.position;
            // Swept proximity intercept check near the goal line to eliminate high-speed physics tunneling
            if (ballPos.z < startPosition.z - 0.9f || ballPos.z > 0.14f) return;

            Vector3 keeperCenter = transform.position + Vector3.up * 0.95f;
            Vector3 leftHandPos = leftGloveAnchor != null ? leftGloveAnchor.position : keeperCenter + Vector3.left * 0.45f;
            Vector3 rightHandPos = rightGloveAnchor != null ? rightGloveAnchor.position : keeperCenter + Vector3.right * 0.45f;

            float dBody = Vector3.Distance(ballPos, keeperCenter);
            float dL = Vector3.Distance(ballPos, leftHandPos);
            float dR = Vector3.Distance(ballPos, rightHandPos);
            float minDist = Mathf.Min(dBody, Mathf.Min(dL, dR));

            float effectiveCatchRadius = (dBody < Mathf.Min(dL, dR)) ? reachRadius * 0.82f : reachRadius * 0.92f;
            if (minDist <= effectiveCatchRadius)
            {
                PerformSaveDeflection(ball);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!isShotActive || hasSavedCurrentShot) return;

            BallController ball = collision.gameObject.GetComponent<BallController>();
            if (ball != null && ball.HasBeenShot && !ball.WasSavedByGoalkeeper)
            {
                PerformSaveDeflection(ball);
            }
        }

        private void PerformSaveDeflection(BallController ball)
        {
            if (hasSavedCurrentShot) return;
            hasSavedCurrentShot = true;
            CurrentState = GoalkeeperState.Saving;

            ball.MarkSavedByGoalkeeper();

            Rigidbody ballRb = ball.GetComponent<Rigidbody>();
            if (ballRb != null && !ballRb.isKinematic)
            {
                // Deflect firmly forward/sideways AWAY from the goal line (-Z)
                float sideDir = ball.transform.position.x - transform.position.x;
                if (Mathf.Abs(sideDir) < 0.15f) sideDir = UnityEngine.Random.value < 0.5f ? -0.6f : 0.6f;

                Vector3 deflectDir = new Vector3(Mathf.Sign(sideDir) * 0.7f, 0.42f, -1.0f).normalized;
                ballRb.linearVelocity = deflectDir * UnityEngine.Random.Range(6.5f, 9.5f);
                ballRb.angularVelocity = new Vector3(4f, sideDir * 8f, 0f);
            }

            AudioManager.Instance?.PlaySfx(SfxType.S03_Save);
            PenaltyManager.Instance?.OnGoalkeeperSavedBall();
            PlaySaveCelebration();
        }

        public void PlaySaveCelebration()
        {
            if (animCoroutine != null) StopCoroutine(animCoroutine);
            animCoroutine = StartCoroutine(SaveCelebrationRoutine());
        }

        private IEnumerator SaveCelebrationRoutine()
        {
            CurrentState = GoalkeeperState.Recovering;
            // Stand up smoothly to celebrate save
            Vector3 p = transform.position;
            MoveRoot(new Vector3(p.x, 0f, startPosition.z), startRotation);
            if (hips != null)
            {
                hips.localPosition = hipsBaseLocalPos;
                hips.localRotation = Quaternion.identity;
            }
            if (leftLeg != null) leftLeg.localRotation = Quaternion.identity;
            if (leftShin != null) leftShin.localRotation = Quaternion.identity;
            if (rightLeg != null) rightLeg.localRotation = Quaternion.identity;
            if (rightShin != null) rightShin.localRotation = Quaternion.identity;

            float timer = 0f;
            while (timer < 1.5f)
            {
                timer += Time.deltaTime;
                float pump = Mathf.Sin(timer * 12f) * 14f;
                if (spine != null) spine.localRotation = Quaternion.Euler(-8f, 0f, 0f);
                if (chest != null) chest.localRotation = Quaternion.Euler(-6f, 0f, 0f);
                if (leftArm != null) leftArm.localRotation = Quaternion.Euler(-135f + pump, 0f, -20f);
                if (leftForearm != null) leftForearm.localRotation = Quaternion.Euler(-35f, 0f, 0f);
                if (rightArm != null) rightArm.localRotation = Quaternion.Euler(-135f - pump, 0f, 20f);
                if (rightForearm != null) rightForearm.localRotation = Quaternion.Euler(-35f, 0f, 0f);
                yield return null;
            }
        }

        public void PlayConcededReaction()
        {
            if (animCoroutine != null) StopCoroutine(animCoroutine);
            animCoroutine = StartCoroutine(ConcededReactionRoutine());
        }

        private IEnumerator ConcededReactionRoutine()
        {
            CurrentState = GoalkeeperState.Recovering;
            Vector3 p = transform.position;
            MoveRoot(new Vector3(p.x, 0f, startPosition.z), startRotation);
            if (hips != null)
            {
                hips.localPosition = hipsBaseLocalPos;
                hips.localRotation = Quaternion.identity;
            }

            float timer = 0f;
            while (timer < 1.5f)
            {
                timer += Time.deltaTime;
                if (spine != null) spine.localRotation = Quaternion.Euler(20f, 0f, 0f);
                if (head != null) head.localRotation = Quaternion.Euler(22f, Mathf.Sin(timer * 5f) * 12f, 0f);
                if (leftArm != null) leftArm.localRotation = Quaternion.Euler(-20f, 0f, -45f);
                if (rightArm != null) rightArm.localRotation = Quaternion.Euler(-20f, 0f, 45f);
                yield return null;
            }
        }

        public void ResetPosition()
        {
            if (reactionCoroutine != null) StopCoroutine(reactionCoroutine);
            if (idleShuffleCoroutine != null) StopCoroutine(idleShuffleCoroutine);
            if (animCoroutine != null) StopCoroutine(animCoroutine);

            isShotActive = false;
            hasSavedCurrentShot = false;
            HasDived = false;
            CurrentState = GoalkeeperState.Resetting;

            StartCoroutine(SmoothResetRoutine());
        }

        private IEnumerator SmoothResetRoutine()
        {
            Vector3 currentPos = transform.position;
            Quaternion currentRot = transform.rotation;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 4.5f;
                MoveRoot(Vector3.Lerp(currentPos, startPosition, t), Quaternion.Slerp(currentRot, startRotation, t));
                yield return null;
            }

            MoveRoot(startPosition, startRotation);
            ResetPose();
            StartIdleShuffle();
        }

        private void MoveRoot(Vector3 worldPos, Quaternion worldRot)
        {
            if (rb != null && rb.isKinematic)
            {
                transform.SetPositionAndRotation(worldPos, worldRot);
            }
            else
            {
                transform.SetPositionAndRotation(worldPos, worldRot);
            }
        }

        private void ResetPose()
        {
            if (hips != null)
            {
                hips.localPosition = hipsBaseLocalPos;
                hips.localRotation = Quaternion.identity;
            }
            if (spine != null) spine.localRotation = Quaternion.identity;
            if (chest != null) chest.localRotation = Quaternion.identity;
            if (head != null) head.localRotation = Quaternion.identity;
            if (leftArm != null) leftArm.localRotation = Quaternion.identity;
            if (leftForearm != null) leftForearm.localRotation = Quaternion.identity;
            if (rightArm != null) rightArm.localRotation = Quaternion.identity;
            if (rightForearm != null) rightForearm.localRotation = Quaternion.identity;
            if (leftLeg != null) leftLeg.localRotation = Quaternion.identity;
            if (leftShin != null) leftShin.localRotation = Quaternion.identity;
            if (rightLeg != null) rightLeg.localRotation = Quaternion.identity;
            if (rightShin != null) rightShin.localRotation = Quaternion.identity;
        }
    }
}
