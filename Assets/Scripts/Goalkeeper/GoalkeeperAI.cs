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
        Anticipating,
        Reacting,
        DivingLeft,
        DivingRight,
        Saving,
        Recovering,
        Resetting
    }

    public enum GoalkeeperDiveStyle
    {
        None,
        LowLeft,
        MidLeft,
        HighLeft,
        LowRight,
        MidRight,
        HighRight,
        HighVerticalJump,
        FrontalCatch,
        LegBlock
    }

    public class GoalkeeperAI : MonoBehaviour
    {
        public static GoalkeeperAI Instance { get; private set; }

        [Header("Tuning (Set dynamically by RoundDataSO)")]
        [SerializeField] private float reactionDelay = 0.15f;
        [SerializeField] private float moveSpeed = 5.0f;
        [SerializeField] private float diveSpeed = 5.8f;
        [SerializeField] private float errorProbability = 0.42f;
        [SerializeField] private float reachRadius = 0.68f;

        [Header("Goal Bounds")]
        [SerializeField] private float minX = -3.35f;
        [SerializeField] private float maxX = 3.35f;
        [SerializeField] private float minY = 0.0f;
        [SerializeField] private float maxY = 2.25f;

        [Header("Skinned / 22-Bone Humanoid Rig")]
        [SerializeField] private Transform bodyRoot;
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

        [Header("Glove Intercept Anchors")]
        [SerializeField] private Transform leftGloveAnchor;
        [SerializeField] private Transform rightGloveAnchor;

        public GoalkeeperState CurrentState { get; private set; } = GoalkeeperState.Idle;
        public GoalkeeperDiveStyle ActiveDiveStyle { get; private set; } = GoalkeeperDiveStyle.None;
        public bool HasDived { get; private set; } = false;
        public bool IsRecoveringFromDive { get; private set; } = false;

        private Rigidbody rb;
        private Vector3 startPosition;
        private Quaternion startRotation;
        private Vector3 hipsBaseLocalPos;
        private bool isShotActive = false;
        private bool hasSavedCurrentShot = false;
        private float currentRoundNumber = 1;

        private Coroutine reactionCoroutine;
        private Coroutine idleShuffleCoroutine;
        private Coroutine anticipationCoroutine;
        private Coroutine animCoroutine;
        private Coroutine resetCoroutine;

        // Facial BlendShapes & expressions
        private int bsBlink = -1, bsFocus = -1, bsEffort = -1, bsHappy = -1, bsFrustrated = -1;
        private float nextBlinkTimer = 3.1f;
        private float blinkProgress = -1f;
        private float wFocus, wEffort, wHappy, wFrustrated;
        private bool isCelebratingSave = false;
        private bool isShowingConceded = false;
        private int saveCelebrationVariant = 0;
        private int concededVariant = 0;

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
            AutoAssignRigReferences();
            CacheBlendShapeIndices();
            if (hips != null)
            {
                hipsBaseLocalPos = hips.localPosition;
                if (hipsBaseLocalPos.y < 0.2f) hipsBaseLocalPos = new Vector3(0f, 0.99f, 0f);
            }
        }

        private void Start()
        {
            AutoAssignRigReferences();
            CacheBlendShapeIndices();
            if (hips != null && hipsBaseLocalPos.y < 0.2f)
            {
                hipsBaseLocalPos = hips.localPosition.y >= 0.2f ? hips.localPosition : new Vector3(0f, 0.99f, 0f);
            }
            StartIdleShuffle();
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            UpdateFaceExpressions(Time.deltaTime);
        }

        private void OnDisable()
        {
            StopAllGoalkeeperCoroutines();
        }

        private void StopAllGoalkeeperCoroutines()
        {
            if (reactionCoroutine != null) { StopCoroutine(reactionCoroutine); reactionCoroutine = null; }
            if (idleShuffleCoroutine != null) { StopCoroutine(idleShuffleCoroutine); idleShuffleCoroutine = null; }
            if (anticipationCoroutine != null) { StopCoroutine(anticipationCoroutine); anticipationCoroutine = null; }
            if (animCoroutine != null) { StopCoroutine(animCoroutine); animCoroutine = null; }
            if (resetCoroutine != null) { StopCoroutine(resetCoroutine); resetCoroutine = null; }
        }

        private void AutoAssignRigReferences()
        {
            if (bodyRoot == null) bodyRoot = FindFirstValid(transform.Find("Visuals"), transform.Find("Skeleton"), transform);
            if (skinnedMesh == null) skinnedMesh = GetComponentInChildren<SkinnedMeshRenderer>(true);

            if (hips == null)          hips          = FindBone(bodyRoot, "Hips");
            if (spine == null)         spine         = FindBone(bodyRoot, "Spine");
            if (upperSpine == null)    upperSpine    = FindBone(bodyRoot, "UpperSpine");
            if (chest == null)         chest         = FindBone(bodyRoot, "Chest");
            if (neck == null)          neck          = FindBone(bodyRoot, "Neck");
            if (torso == null)         torso         = FindFirstValid(chest, spine, FindBone(bodyRoot, "Torso"));
            if (head == null)          head          = FindBone(bodyRoot, "Head");
            if (leftShoulder == null)  leftShoulder  = FindBone(bodyRoot, "LeftShoulder");
            if (leftArm == null)       leftArm       = FindFirstValid(FindBone(bodyRoot, "LeftUpperArm"), FindBone(bodyRoot, "Arm_Left"));
            if (leftForearm == null)   leftForearm   = FindBone(bodyRoot, "LeftForearm");
            if (leftHand == null)      leftHand      = FindBone(bodyRoot, "LeftHand");
            if (rightShoulder == null) rightShoulder = FindBone(bodyRoot, "RightShoulder");
            if (rightArm == null)      rightArm      = FindFirstValid(FindBone(bodyRoot, "RightUpperArm"), FindBone(bodyRoot, "Arm_Right"));
            if (rightForearm == null)  rightForearm  = FindBone(bodyRoot, "RightForearm");
            if (rightHand == null)     rightHand     = FindBone(bodyRoot, "RightHand");
            if (leftLeg == null)       leftLeg       = FindFirstValid(FindBone(bodyRoot, "LeftThigh"), FindBone(bodyRoot, "Leg_Left"));
            if (leftShin == null)      leftShin      = FindBone(bodyRoot, "LeftShin");
            if (leftFoot == null)      leftFoot      = FindBone(bodyRoot, "LeftFoot");
            if (leftToe == null)       leftToe       = FindBone(bodyRoot, "LeftToe");
            if (rightLeg == null)      rightLeg      = FindFirstValid(FindBone(bodyRoot, "RightThigh"), FindBone(bodyRoot, "Leg_Right"));
            if (rightShin == null)     rightShin     = FindBone(bodyRoot, "RightShin");
            if (rightFoot == null)     rightFoot     = FindBone(bodyRoot, "RightFoot");
            if (rightToe == null)      rightToe      = FindBone(bodyRoot, "RightToe");

            if (leftGloveAnchor == null)  leftGloveAnchor  = FindFirstValid(leftHand, FindBone(bodyRoot, "GoalkeeperGlove_L"));
            if (rightGloveAnchor == null) rightGloveAnchor = FindFirstValid(rightHand, FindBone(bodyRoot, "GoalkeeperGlove_R"));
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

        private void UpdateFaceExpressions(float dt)
        {
            if (skinnedMesh == null) return;

            nextBlinkTimer -= dt;
            if (nextBlinkTimer <= 0f && blinkProgress < 0f)
            {
                blinkProgress = 0f;
                nextBlinkTimer = UnityEngine.Random.Range(2.4f, 4.8f);
            }

            float blinkWeight = 0f;
            if (blinkProgress >= 0f)
            {
                blinkProgress += dt / 0.15f;
                if (blinkProgress >= 1f) blinkProgress = -1f;
                else blinkWeight = Mathf.Sin(blinkProgress * Mathf.PI) * 100f;
            }

            float tgtFocus = (CurrentState == GoalkeeperState.Ready || CurrentState == GoalkeeperState.Anticipating) ? 80f : 0f;
            float tgtEffort = (CurrentState == GoalkeeperState.Reacting || CurrentState == GoalkeeperState.DivingLeft || CurrentState == GoalkeeperState.DivingRight || CurrentState == GoalkeeperState.Saving) ? 95f : 0f;
            float tgtHappy = isCelebratingSave ? 100f : 0f;
            float tgtFrust = isShowingConceded ? 90f : 0f;

            float blend = 1f - Mathf.Exp(-11f * dt);
            wFocus = Mathf.Lerp(wFocus, tgtFocus, blend);
            wEffort = Mathf.Lerp(wEffort, tgtEffort, blend);
            wHappy = Mathf.Lerp(wHappy, tgtHappy, blend);
            wFrustrated = Mathf.Lerp(wFrustrated, tgtFrust, blend);

            if (bsBlink >= 0) skinnedMesh.SetBlendShapeWeight(bsBlink, blinkWeight);
            if (bsFocus >= 0) skinnedMesh.SetBlendShapeWeight(bsFocus, wFocus);
            if (bsEffort >= 0) skinnedMesh.SetBlendShapeWeight(bsEffort, wEffort);
            if (bsHappy >= 0) skinnedMesh.SetBlendShapeWeight(bsHappy, wHappy);
            if (bsFrustrated >= 0) skinnedMesh.SetBlendShapeWeight(bsFrustrated, wFrustrated);
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
                reachRadius = 0.68f + (roundData.roundNumber - 1) * 0.08f;
            }
        }

        public void StartIdleShuffle()
        {
            if (!isActiveAndEnabled) return;
            if (idleShuffleCoroutine != null) StopCoroutine(idleShuffleCoroutine);
            if (anticipationCoroutine != null) { StopCoroutine(anticipationCoroutine); anticipationCoroutine = null; }
            isCelebratingSave = false;
            isShowingConceded = false;
            ActiveDiveStyle = GoalkeeperDiveStyle.None;
            IsRecoveringFromDive = false;
            CurrentState = GoalkeeperState.Ready;
            idleShuffleCoroutine = StartCoroutine(IdleShuffleRoutine());
        }

        public void OnKickerRunUpStarted(Vector3 ballPos)
        {
            if (!isActiveAndEnabled || isShotActive) return;
            if (idleShuffleCoroutine != null) { StopCoroutine(idleShuffleCoroutine); idleShuffleCoroutine = null; }
            if (anticipationCoroutine != null) { StopCoroutine(anticipationCoroutine); anticipationCoroutine = null; }
            CurrentState = GoalkeeperState.Anticipating;
            anticipationCoroutine = StartCoroutine(AnticipationSplitStepRoutine(ballPos));
        }

        private IEnumerator AnticipationSplitStepRoutine(Vector3 ballPos)
        {
            float elapsed = 0f;
            Vector3 fromP = transform.position;
            float targetX = Mathf.Lerp(fromP.x, startPosition.x + Mathf.Clamp(ballPos.x * 0.25f, -0.35f, 0.35f), 0.72f);

            while (!isShotActive && elapsed < 0.6f)
            {
                float dt = Time.deltaTime;
                elapsed += dt;
                float hop = elapsed < 0.14f ? Mathf.Sin((elapsed / 0.14f) * Mathf.PI) * 0.06f : 0f;
                float curX = Mathf.Lerp(fromP.x, targetX, Mathf.Clamp01(elapsed / 0.18f));
                MoveRoot(new Vector3(curX, startPosition.y + hop, startPosition.z), startRotation);

                float blend = 1f - Mathf.Exp(-18f * dt);
                ApplyAthleticCrouchPose(blend, crouchDepth: 0.145f, torsoTiltX: 19f, kneeBend: 48f,
                    headYaw: Mathf.Clamp(-ballPos.x * 4f, -15f, 15f), swayX: 0f, legStep: 0f, toeLift: 16f, handBreath: 0f);
                yield return null;
            }
        }

        private IEnumerator IdleShuffleRoutine()
        {
            float timer = 0f;
            while (!isShotActive)
            {
                float dt = Time.deltaTime;
                timer += dt * (2.5f + currentRoundNumber * 0.12f);
                float shuffleAmp = 0.36f + (currentRoundNumber - 1) * 0.04f;
                float offsetX = Mathf.Sin(timer) * shuffleAmp;
                float stepBounce = Mathf.Abs(Mathf.Cos(timer)) * 0.038f;
                Vector3 nextPos = new Vector3(startPosition.x + offsetX, startPosition.y + stepBounce, startPosition.z);
                MoveRoot(nextPos, startRotation);

                float blend = 1f - Mathf.Exp(-14f * dt);
                float weightShiftRoll = -Mathf.Cos(timer) * 5.0f;
                float legStepPhase = Mathf.Sin(timer * 2f) * 14f;
                float handReadiness = Mathf.Sin(timer * 1.4f) * 4f;
                float breath = Mathf.Sin(timer * 0.8f);
                ApplyAthleticCrouchPose(blend, crouchDepth: 0.095f + Mathf.Abs(Mathf.Sin(timer * 2f)) * 0.032f + breath * 0.008f,
                    torsoTiltX: 15.5f + breath * 1.5f, kneeBend: 38f, headYaw: Mathf.Cos(timer) * 7f,
                    swayX: weightShiftRoll, legStep: legStepPhase, toeLift: Mathf.Abs(Mathf.Cos(timer)) * 12f, handBreath: handReadiness);

                yield return null;
            }
        }

        private void ApplyAthleticCrouchPose(float blend, float crouchDepth, float torsoTiltX, float kneeBend, float headYaw, float swayX,
            float legStep = 0f, float toeLift = 0f, float handBreath = 0f)
        {
            if (hips != null)
            {
                hips.localPosition = Vector3.Lerp(hips.localPosition,
                    new Vector3(hipsBaseLocalPos.x, hipsBaseLocalPos.y - crouchDepth, hipsBaseLocalPos.z), blend);
                hips.localRotation = Quaternion.Slerp(hips.localRotation, Quaternion.Euler(2f, 0f, swayX), blend);
            }
            if (spine != null) spine.localRotation = Quaternion.Slerp(spine.localRotation, Quaternion.Euler(torsoTiltX * 0.55f, 0f, -swayX * 0.25f), blend);
            if (upperSpine != null) upperSpine.localRotation = Quaternion.Slerp(upperSpine.localRotation, Quaternion.Euler(torsoTiltX * 0.45f, 0f, 0f), blend);
            if (chest != null) chest.localRotation = Quaternion.Slerp(chest.localRotation, Quaternion.Euler(torsoTiltX * 0.35f, 0f, 0f), blend);
            if (neck != null) neck.localRotation = Quaternion.Slerp(neck.localRotation, Quaternion.Euler(-torsoTiltX * 0.45f, headYaw * 0.3f, 0f), blend);
            if (head != null) head.localRotation = Quaternion.Slerp(head.localRotation, Quaternion.Euler(-torsoTiltX * 0.65f, headYaw, -swayX * 0.35f), blend);

            // Wide athletic ready stance with stepping knees & toe flex
            if (leftLeg != null) leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, Quaternion.Euler(-kneeBend * 0.65f + legStep, -4f, -11f), blend);
            if (leftShin != null) leftShin.localRotation = Quaternion.Slerp(leftShin.localRotation, Quaternion.Euler(kneeBend, 0f, 0f), blend);
            if (leftFoot != null) leftFoot.localRotation = Quaternion.Slerp(leftFoot.localRotation, Quaternion.Euler(-kneeBend * 0.35f, 0f, 8f), blend);
            if (leftToe != null) leftToe.localRotation = Quaternion.Slerp(leftToe.localRotation, Quaternion.Euler(toeLift, 0f, 0f), blend);

            if (rightLeg != null) rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, Quaternion.Euler(-kneeBend * 0.65f - legStep, 4f, 11f), blend);
            if (rightShin != null) rightShin.localRotation = Quaternion.Slerp(rightShin.localRotation, Quaternion.Euler(kneeBend, 0f, 0f), blend);
            if (rightFoot != null) rightFoot.localRotation = Quaternion.Slerp(rightFoot.localRotation, Quaternion.Euler(-kneeBend * 0.35f, 0f, -8f), blend);
            if (rightToe != null) rightToe.localRotation = Quaternion.Slerp(rightToe.localRotation, Quaternion.Euler(toeLift, 0f, 0f), blend);

            // Clavicles rolled forward slightly, gloves poised in ready box
            if (leftShoulder != null) leftShoulder.localRotation = Quaternion.Slerp(leftShoulder.localRotation, Quaternion.Euler(4f, 12f, -4f), blend);
            if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.Euler(-38f + handBreath, 18f, -32f), blend);
            if (leftForearm != null) leftForearm.localRotation = Quaternion.Slerp(leftForearm.localRotation, Quaternion.Euler(-66f, 8f, 0f), blend);
            if (leftHand != null) leftHand.localRotation = Quaternion.Slerp(leftHand.localRotation, Quaternion.Euler(-16f, 20f, 0f), blend);

            if (rightShoulder != null) rightShoulder.localRotation = Quaternion.Slerp(rightShoulder.localRotation, Quaternion.Euler(4f, -12f, 4f), blend);
            if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler(-38f - handBreath, -18f, 32f), blend);
            if (rightForearm != null) rightForearm.localRotation = Quaternion.Slerp(rightForearm.localRotation, Quaternion.Euler(-66f, -8f, 0f), blend);
            if (rightHand != null) rightHand.localRotation = Quaternion.Slerp(rightHand.localRotation, Quaternion.Euler(-16f, -20f, 0f), blend);
        }

        public void OnShotFired(Vector3 ballPos, Vector3 shotVelocity, float power)
        {
            if (!isActiveAndEnabled) return;
            StopAllGoalkeeperCoroutines();
            isShotActive = true;
            hasSavedCurrentShot = false;
            HasDived = false;
            IsRecoveringFromDive = false;
            isCelebratingSave = false;
            isShowingConceded = false;
            ActiveDiveStyle = GoalkeeperDiveStyle.None;
            CurrentState = GoalkeeperState.Reacting;

            reactionCoroutine = StartCoroutine(ReactToShotRoutine(ballPos, shotVelocity, power));
        }

        private void PredictBallArrival(Vector3 ballPos, Vector3 shotVelocity, float power, out float predictedX, out float predictedY, out float timeToGoal)
        {
            const float dt = 0.02f;
            float dampMul = Mathf.Clamp01(1f - BallController.LinearDampingValue * dt);
            Vector3 angularVel = new Vector3(0f, -ballPos.x * 0.2f, 0f);
            Vector3 pos = ballPos;
            Vector3 vel = shotVelocity;
            timeToGoal = 0f;

            for (int step = 0; step < 120; step++)
            {
                Vector3 prevPos = pos;
                float prevT = timeToGoal;
                Vector3 magnus = BallController.ComputeMagnusAccel(vel, angularVel);
                vel += (Physics.gravity + magnus) * dt;
                vel *= dampMul;
                pos += vel * dt;
                timeToGoal += dt;

                if (pos.z >= startPosition.z)
                {
                    float denom = pos.z - prevPos.z;
                    float frac = Mathf.Abs(denom) > 0.0001f ? Mathf.Clamp01((startPosition.z - prevPos.z) / denom) : 1f;
                    Vector3 hit = Vector3.Lerp(prevPos, pos, frac);
                    timeToGoal = Mathf.Clamp(Mathf.Lerp(prevT, timeToGoal, frac), 0.14f, 1.4f);
                    predictedX = hit.x;
                    predictedY = hit.y;
                    return;
                }
            }

            predictedX = pos.x;
            predictedY = pos.y;
            timeToGoal = Mathf.Clamp(timeToGoal, 0.14f, 1.4f);
        }

        private IEnumerator ReactToShotRoutine(Vector3 ballPos, Vector3 shotVelocity, float power)
        {
            PredictBallArrival(ballPos, shotVelocity, power, out float predictedX, out float predictedY, out float timeToGoal);

            float reactTimer = 0f;
            while (reactTimer < reactionDelay)
            {
                float dt = Time.deltaTime;
                reactTimer += dt;
                float blend = 1f - Mathf.Exp(-20f * dt);
                float trackYaw = Mathf.Clamp(-predictedX * 10f, -26f, 26f);
                ApplyAthleticCrouchPose(blend, crouchDepth: 0.165f, torsoTiltX: 21f, kneeBend: 52f, headYaw: trackYaw,
                    swayX: -Mathf.Sign(predictedX) * 4.5f, legStep: 0f, toeLift: 14f, handBreath: 0f);
                yield return null;
            }

            bool isPlacedShot = Mathf.Abs(predictedX) > 0.65f;
            float effectiveError = isPlacedShot ? errorProbability : errorProbability * 0.35f;
            bool misread = UnityEngine.Random.value < effectiveError;

            if (misread)
            {
                float dirAway = Mathf.Abs(predictedX) > 0.35f ? -Mathf.Sign(predictedX) : (UnityEngine.Random.value < 0.5f ? -1f : 1f);
                predictedX = Mathf.Clamp(predictedX + dirAway * UnityEngine.Random.Range(1.85f, 3.10f), minX, maxX);
                predictedY = Mathf.Clamp(predictedY + UnityEngine.Random.Range(-0.45f, 0.55f), minY, maxY);
            }

            predictedX = Mathf.Clamp(predictedX, minX, maxX);
            predictedY = Mathf.Clamp(predictedY, minY, maxY);

            Vector3 fromPos = transform.position;
            float availableDiveTime = Mathf.Max(0.11f, timeToGoal - reactionDelay);
            float maxTravelDist = diveSpeed * availableDiveTime;

            float targetDeltaX = predictedX - fromPos.x;
            if (Mathf.Abs(targetDeltaX) > maxTravelDist)
            {
                targetDeltaX = Mathf.Sign(targetDeltaX) * maxTravelDist;
            }

            float finalX = Mathf.Clamp(fromPos.x + targetDeltaX, minX, maxX);
            float diveDistX = Mathf.Abs(finalX - fromPos.x);

            bool divingLeft = targetDeltaX > 0f;

            if (diveDistX <= 0.62f)
            {
                if (predictedY >= 1.62f)
                    ActiveDiveStyle = GoalkeeperDiveStyle.HighVerticalJump;
                else if (predictedY <= 0.62f && Mathf.Abs(predictedX - fromPos.x) <= 0.95f && power >= 0.65f)
                    ActiveDiveStyle = GoalkeeperDiveStyle.LegBlock;
                else
                    ActiveDiveStyle = GoalkeeperDiveStyle.FrontalCatch;

                CurrentState = GoalkeeperState.Saving;
            }
            else
            {
                if (predictedY < 0.72f)
                    ActiveDiveStyle = divingLeft ? GoalkeeperDiveStyle.LowLeft : GoalkeeperDiveStyle.LowRight;
                else if (predictedY < 1.48f)
                    ActiveDiveStyle = divingLeft ? GoalkeeperDiveStyle.MidLeft : GoalkeeperDiveStyle.MidRight;
                else
                    ActiveDiveStyle = divingLeft ? GoalkeeperDiveStyle.HighLeft : GoalkeeperDiveStyle.HighRight;

                CurrentState = divingLeft ? GoalkeeperState.DivingLeft : GoalkeeperState.DivingRight;
            }

            HasDived = true;
            OnSaveAttempted?.Invoke();

            float diveDuration = Mathf.Clamp(availableDiveTime * 0.94f, 0.11f, 0.34f);
            float diveTimer = 0f;

            float targetBodyY = ActiveDiveStyle switch
            {
                GoalkeeperDiveStyle.HighVerticalJump => Mathf.Clamp(predictedY - 0.85f, 0.25f, 0.88f),
                GoalkeeperDiveStyle.HighLeft or GoalkeeperDiveStyle.HighRight => Mathf.Clamp(predictedY * 0.55f, 0.45f, 0.92f),
                GoalkeeperDiveStyle.MidLeft or GoalkeeperDiveStyle.MidRight => Mathf.Clamp(predictedY * 0.38f, 0.22f, 0.52f),
                _ => 0.04f
            };

            while (diveTimer < diveDuration && !hasSavedCurrentShot)
            {
                float dt = Time.deltaTime;
                diveTimer += dt;
                float t = Mathf.Clamp01(diveTimer / diveDuration);
                float smoothT = Mathf.Sin(t * Mathf.PI * 0.5f);
                float boneBlend = 1f - Mathf.Exp(-26f * dt);

                float curX = Mathf.Lerp(fromPos.x, finalX, smoothT);
                float jumpBell = Mathf.Sin(t * Mathf.PI);
                float curY = ActiveDiveStyle == GoalkeeperDiveStyle.LegBlock || ActiveDiveStyle == GoalkeeperDiveStyle.LowLeft || ActiveDiveStyle == GoalkeeperDiveStyle.LowRight
                    ? Mathf.Max(0f, jumpBell * 0.10f)
                    : Mathf.Max(0f, Mathf.Lerp(0f, targetBodyY, smoothT) + jumpBell * (targetBodyY * 0.45f + 0.12f));

                MoveRoot(new Vector3(curX, curY, startPosition.z), startRotation);
                ApplyDivePose(ActiveDiveStyle, divingLeft, predictedY, smoothT, boneBlend);

                yield return null;
            }

            yield return TurfLandingAndProgressiveRecoveryRoutine();
        }

        private void ApplyDivePose(GoalkeeperDiveStyle style, bool divingLeft, float predictedY, float diveT, float boneBlend)
        {
            float rollDir = divingLeft ? 1f : -1f;

            switch (style)
            {
                case GoalkeeperDiveStyle.LowLeft:
                case GoalkeeperDiveStyle.LowRight:
                    SetHipsAndFlexibleSpine(0.34f, Quaternion.Euler(6f, 10f * rollDir, 76f * rollDir), 12f, 9f * rollDir, -28f * rollDir, diveT, boneBlend);
                    ApplyDiveLimbs(divingLeft, leadArmZ: -158f, trailArmZ: -124f, armForwardX: -24f, hipTrailAngle: 20f, kneeBend: 26f, blend: boneBlend);
                    break;

                case GoalkeeperDiveStyle.MidLeft:
                case GoalkeeperDiveStyle.MidRight:
                    SetHipsAndFlexibleSpine(0.56f, Quaternion.Euler(4f, 8f * rollDir, 68f * rollDir), 14f, 11f * rollDir, -24f * rollDir, diveT, boneBlend);
                    ApplyDiveLimbs(divingLeft, leadArmZ: -164f, trailArmZ: -136f, armForwardX: -14f, hipTrailAngle: 16f, kneeBend: 22f, blend: boneBlend);
                    break;

                case GoalkeeperDiveStyle.HighLeft:
                case GoalkeeperDiveStyle.HighRight:
                    SetHipsAndFlexibleSpine(0.76f, Quaternion.Euler(-4f, 6f * rollDir, 50f * rollDir), 10f, 12f * rollDir, -18f * rollDir, diveT, boneBlend);
                    ApplyDiveLimbs(divingLeft, leadArmZ: -172f, trailArmZ: -142f, armForwardX: -6f, hipTrailAngle: 12f, kneeBend: 16f, blend: boneBlend);
                    break;

                case GoalkeeperDiveStyle.HighVerticalJump:
                    if (hips != null)
                    {
                        hips.localPosition = Vector3.Lerp(hips.localPosition, new Vector3(hipsBaseLocalPos.x, hipsBaseLocalPos.y + 0.06f, hipsBaseLocalPos.z), boneBlend);
                        hips.localRotation = Quaternion.Slerp(hips.localRotation, Quaternion.Euler(-8f, 0f, 0f), boneBlend);
                    }
                    if (spine != null) spine.localRotation = Quaternion.Slerp(spine.localRotation, Quaternion.Euler(-8f, 0f, 0f), boneBlend);
                    if (upperSpine != null) upperSpine.localRotation = Quaternion.Slerp(upperSpine.localRotation, Quaternion.Euler(-8f, 0f, 0f), boneBlend);
                    if (chest != null) chest.localRotation = Quaternion.Slerp(chest.localRotation, Quaternion.Euler(-6f, 0f, 0f), boneBlend);
                    if (head != null) head.localRotation = Quaternion.Slerp(head.localRotation, Quaternion.Euler(-16f, 0f, 0f), boneBlend);
                    if (leftShoulder != null) leftShoulder.localRotation = Quaternion.Slerp(leftShoulder.localRotation, Quaternion.Euler(0f, 0f, 16f), boneBlend);
                    if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.Euler(-165f, 12f, -14f), boneBlend);
                    if (leftForearm != null) leftForearm.localRotation = Quaternion.Slerp(leftForearm.localRotation, Quaternion.Euler(-14f, 0f, 0f), boneBlend);
                    if (rightShoulder != null) rightShoulder.localRotation = Quaternion.Slerp(rightShoulder.localRotation, Quaternion.Euler(0f, 0f, -16f), boneBlend);
                    if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler(-165f, -12f, 14f), boneBlend);
                    if (rightForearm != null) rightForearm.localRotation = Quaternion.Slerp(rightForearm.localRotation, Quaternion.Euler(-14f, 0f, 0f), boneBlend);
                    if (leftLeg != null) leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, Quaternion.Euler(-14f, 0f, -6f), boneBlend);
                    if (leftShin != null) leftShin.localRotation = Quaternion.Slerp(leftShin.localRotation, Quaternion.Euler(30f, 0f, 0f), boneBlend);
                    if (leftFoot != null) leftFoot.localRotation = Quaternion.Slerp(leftFoot.localRotation, Quaternion.Euler(24f, 0f, 0f), boneBlend);
                    if (rightLeg != null) rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, Quaternion.Euler(-14f, 0f, 6f), boneBlend);
                    if (rightShin != null) rightShin.localRotation = Quaternion.Slerp(rightShin.localRotation, Quaternion.Euler(30f, 0f, 0f), boneBlend);
                    if (rightFoot != null) rightFoot.localRotation = Quaternion.Slerp(rightFoot.localRotation, Quaternion.Euler(24f, 0f, 0f), boneBlend);
                    break;

                case GoalkeeperDiveStyle.LegBlock:
                    if (hips != null)
                    {
                        hips.localPosition = Vector3.Lerp(hips.localPosition, new Vector3(hipsBaseLocalPos.x, 0.56f, hipsBaseLocalPos.z), boneBlend);
                        hips.localRotation = Quaternion.Slerp(hips.localRotation, Quaternion.Euler(10f, 0f, 14f * rollDir), boneBlend);
                    }
                    if (spine != null) spine.localRotation = Quaternion.Slerp(spine.localRotation, Quaternion.Euler(14f, 0f, 0f), boneBlend);
                    if (upperSpine != null) upperSpine.localRotation = Quaternion.Slerp(upperSpine.localRotation, Quaternion.Euler(10f, 0f, 0f), boneBlend);
                    if (chest != null) chest.localRotation = Quaternion.Slerp(chest.localRotation, Quaternion.Euler(10f, 0f, 0f), boneBlend);
                    if (leftLeg != null) leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, Quaternion.Euler(-48f, -15f, -38f), boneBlend);
                    if (leftShin != null) leftShin.localRotation = Quaternion.Slerp(leftShin.localRotation, Quaternion.Euler(48f, 0f, 0f), boneBlend);
                    if (rightLeg != null) rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, Quaternion.Euler(-22f, 20f, 44f), boneBlend);
                    if (rightShin != null) rightShin.localRotation = Quaternion.Slerp(rightShin.localRotation, Quaternion.Euler(18f, 0f, 0f), boneBlend);
                    if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.Euler(-48f, 12f, -45f), boneBlend);
                    if (leftForearm != null) leftForearm.localRotation = Quaternion.Slerp(leftForearm.localRotation, Quaternion.Euler(-35f, 0f, 0f), boneBlend);
                    if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler(-48f, -12f, 45f), boneBlend);
                    if (rightForearm != null) rightForearm.localRotation = Quaternion.Slerp(rightForearm.localRotation, Quaternion.Euler(-35f, 0f, 0f), boneBlend);
                    break;

                default: // FrontalCatch
                    float handPitch = Mathf.Lerp(-68f, -122f, Mathf.Clamp01(predictedY / 1.8f));
                    ApplyAthleticCrouchPose(boneBlend, crouchDepth: 0.10f, torsoTiltX: 16f, kneeBend: 38f, headYaw: 0f, swayX: 0f);
                    if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.Euler(handPitch, 18f, -16f), boneBlend);
                    if (leftForearm != null) leftForearm.localRotation = Quaternion.Slerp(leftForearm.localRotation, Quaternion.Euler(-34f, 0f, 0f), boneBlend);
                    if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler(handPitch, -18f, 16f), boneBlend);
                    if (rightForearm != null) rightForearm.localRotation = Quaternion.Slerp(rightForearm.localRotation, Quaternion.Euler(-34f, 0f, 0f), boneBlend);
                    break;
            }
        }

        private void SetHipsAndFlexibleSpine(float hipHeight, Quaternion hipRot, float spineArch, float lateralFlexZ, float headCounterRoll, float diveT, float blend)
        {
            if (hips != null)
            {
                hips.localRotation = Quaternion.Slerp(hips.localRotation, hipRot, diveT);
                hips.localPosition = Vector3.Lerp(hips.localPosition, new Vector3(hipsBaseLocalPos.x, hipHeight, hipsBaseLocalPos.z), diveT);
            }
            // Progressive spinal curve across lower/upper spine and chest for athletic banana stretch!
            if (spine != null) spine.localRotation = Quaternion.Slerp(spine.localRotation, Quaternion.Euler(spineArch * 0.55f, 0f, lateralFlexZ * 0.45f), blend);
            if (upperSpine != null) upperSpine.localRotation = Quaternion.Slerp(upperSpine.localRotation, Quaternion.Euler(spineArch * 0.45f, 0f, lateralFlexZ * 0.55f), blend);
            if (chest != null) chest.localRotation = Quaternion.Slerp(chest.localRotation, Quaternion.Euler(spineArch * 0.35f, 0f, lateralFlexZ * 0.40f), blend);
            if (neck != null) neck.localRotation = Quaternion.Slerp(neck.localRotation, Quaternion.Euler(-6f, 0f, headCounterRoll * 0.4f), blend);
            if (head != null) head.localRotation = Quaternion.Slerp(head.localRotation, Quaternion.Euler(-14f, 0f, headCounterRoll * 0.7f), blend);
        }

        private void ApplyDiveLimbs(bool divingLeft, float leadArmZ, float trailArmZ, float armForwardX, float hipTrailAngle, float kneeBend, float blend)
        {
            float rollDir = divingLeft ? -1f : 1f;
            if (divingLeft)
            {
                if (leftShoulder != null) leftShoulder.localRotation = Quaternion.Slerp(leftShoulder.localRotation, Quaternion.Euler(-8f, 0f, 18f), blend);
                if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.Euler(armForwardX, 0f, leadArmZ), blend);
                if (leftForearm != null) leftForearm.localRotation = Quaternion.Slerp(leftForearm.localRotation, Quaternion.Euler(-10f, 0f, 0f), blend);
                if (leftHand != null) leftHand.localRotation = Quaternion.Slerp(leftHand.localRotation, Quaternion.Euler(-12f, 22f, 0f), blend);

                if (rightShoulder != null) rightShoulder.localRotation = Quaternion.Slerp(rightShoulder.localRotation, Quaternion.Euler(-6f, 0f, -12f), blend);
                if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler(armForwardX - 12f, 0f, trailArmZ), blend);
                if (rightForearm != null) rightForearm.localRotation = Quaternion.Slerp(rightForearm.localRotation, Quaternion.Euler(-18f, 0f, 0f), blend);
                if (rightHand != null) rightHand.localRotation = Quaternion.Slerp(rightHand.localRotation, Quaternion.Euler(-12f, -22f, 0f), blend);
            }
            else
            {
                if (rightShoulder != null) rightShoulder.localRotation = Quaternion.Slerp(rightShoulder.localRotation, Quaternion.Euler(-8f, 0f, -18f), blend);
                if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler(armForwardX, 0f, -leadArmZ), blend);
                if (rightForearm != null) rightForearm.localRotation = Quaternion.Slerp(rightForearm.localRotation, Quaternion.Euler(-10f, 0f, 0f), blend);
                if (rightHand != null) rightHand.localRotation = Quaternion.Slerp(rightHand.localRotation, Quaternion.Euler(-12f, -22f, 0f), blend);

                if (leftShoulder != null) leftShoulder.localRotation = Quaternion.Slerp(leftShoulder.localRotation, Quaternion.Euler(-6f, 0f, 12f), blend);
                if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.Euler(armForwardX - 12f, 0f, -trailArmZ), blend);
                if (leftForearm != null) leftForearm.localRotation = Quaternion.Slerp(leftForearm.localRotation, Quaternion.Euler(-18f, 0f, 0f), blend);
                if (leftHand != null) leftHand.localRotation = Quaternion.Slerp(leftHand.localRotation, Quaternion.Euler(-12f, 22f, 0f), blend);
            }

            // Asymmetric athletic knee tuck on lead vs trailing drive leg + extended toes
            float leadHip = divingLeft ? -14f : hipTrailAngle;
            float trailHip = divingLeft ? hipTrailAngle : -14f;
            float leadKnee = divingLeft ? kneeBend * 1.15f : kneeBend * 0.75f;
            float trailKnee = divingLeft ? kneeBend * 0.75f : kneeBend * 1.15f;

            if (leftLeg != null) leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, Quaternion.Euler(leadHip, 0f, 10f * rollDir), blend);
            if (leftShin != null) leftShin.localRotation = Quaternion.Slerp(leftShin.localRotation, Quaternion.Euler(leadKnee, 0f, 0f), blend);
            if (leftFoot != null) leftFoot.localRotation = Quaternion.Slerp(leftFoot.localRotation, Quaternion.Euler(18f, 0f, 0f), blend);
            if (leftToe != null) leftToe.localRotation = Quaternion.Slerp(leftToe.localRotation, Quaternion.Euler(14f, 0f, 0f), blend);

            if (rightLeg != null) rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, Quaternion.Euler(trailHip, 0f, 10f * rollDir), blend);
            if (rightShin != null) rightShin.localRotation = Quaternion.Slerp(rightShin.localRotation, Quaternion.Euler(trailKnee, 0f, 0f), blend);
            if (rightFoot != null) rightFoot.localRotation = Quaternion.Slerp(rightFoot.localRotation, Quaternion.Euler(18f, 0f, 0f), blend);
            if (rightToe != null) rightToe.localRotation = Quaternion.Slerp(rightToe.localRotation, Quaternion.Euler(14f, 0f, 0f), blend);
        }

        private IEnumerator TurfLandingAndProgressiveRecoveryRoutine()
        {
            if (CurrentState == GoalkeeperState.DivingLeft || CurrentState == GoalkeeperState.DivingRight)
            {
                CurrentState = GoalkeeperState.Recovering;
                IsRecoveringFromDive = true;
                bool divingLeft = CurrentState == GoalkeeperState.DivingLeft || ActiveDiveStyle.ToString().Contains("Left");
                Vector3 landPos = transform.position;
                Quaternion startHipsRot = hips != null ? hips.localRotation : Quaternion.identity;
                Vector3 startHipsPos = hips != null ? hips.localPosition : hipsBaseLocalPos;

                AudioManager.Instance?.PlaySfx(SfxType.TurfLanding);

                // Stage 1 (0.20s): Cushioned landing onto turf - elbow/forearm absorb & hip settle
                float settleT = 0f;
                while (settleT < 0.20f)
                {
                    settleT += Time.deltaTime;
                    float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(settleT / 0.20f));
                    MoveRoot(new Vector3(landPos.x, Mathf.Lerp(landPos.y, 0f, p), startPosition.z), startRotation);
                    if (hips != null)
                    {
                        hips.localPosition = Vector3.Lerp(startHipsPos, new Vector3(hipsBaseLocalPos.x, 0.28f, hipsBaseLocalPos.z), p);
                    }
                    // Tuck elbows & knees smoothly on ground contact
                    if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.Euler(-54f, 0f, -45f), 0.2f);
                    if (leftForearm != null) leftForearm.localRotation = Quaternion.Slerp(leftForearm.localRotation, Quaternion.Euler(-68f, 0f, 0f), 0.2f);
                    if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler(-54f, 0f, 45f), 0.2f);
                    if (rightForearm != null) rightForearm.localRotation = Quaternion.Slerp(rightForearm.localRotation, Quaternion.Euler(-68f, 0f, 0f), 0.2f);
                    yield return null;
                }

                // Stage 2 (0.38s): Progressive push-up from grass -> kneeling support -> athletic crouch
                float pushT = 0f;
                float rollSide = divingLeft ? 1f : -1f;
                while (pushT < 0.38f)
                {
                    pushT += Time.deltaTime;
                    float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(pushT / 0.38f));
                    if (hips != null)
                    {
                        hips.localRotation = Quaternion.Slerp(startHipsRot, Quaternion.Euler(4f, 0f, 0f), p);
                        float midY = Mathf.Lerp(0.28f, hipsBaseLocalPos.y - 0.10f, p);
                        hips.localPosition = new Vector3(hipsBaseLocalPos.x, midY, hipsBaseLocalPos.z);
                    }
                    float crouchKnee = Mathf.Sin(p * Mathf.PI) * 58f + 18f;
                    if (spine != null) spine.localRotation = Quaternion.Slerp(spine.localRotation, Quaternion.Euler(Mathf.Sin(p * Mathf.PI) * 28f, 0f, (1f - p) * 14f * rollSide), 0.25f);
                    if (upperSpine != null) upperSpine.localRotation = Quaternion.Slerp(upperSpine.localRotation, Quaternion.Euler(Mathf.Sin(p * Mathf.PI) * 14f, 0f, 0f), 0.25f);
                    if (leftLeg != null) leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, Quaternion.Euler(-crouchKnee * 0.6f, 0f, -6f), 0.25f);
                    if (leftShin != null) leftShin.localRotation = Quaternion.Slerp(leftShin.localRotation, Quaternion.Euler(crouchKnee, 0f, 0f), 0.25f);
                    if (rightLeg != null) rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, Quaternion.Euler(-crouchKnee * 0.6f, 0f, 6f), 0.25f);
                    if (rightShin != null) rightShin.localRotation = Quaternion.Slerp(rightShin.localRotation, Quaternion.Euler(crouchKnee, 0f, 0f), 0.25f);
                    if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.Euler(-48f, 12f, -26f), 0.25f);
                    if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler(-48f, -12f, 26f), 0.25f);
                    yield return null;
                }
                IsRecoveringFromDive = false;
            }
        }

        private void FixedUpdate()
        {
            if (!isShotActive || hasSavedCurrentShot) return;

            BallController ball = BallController.Instance;
            if (ball == null || !ball.HasBeenShot || ball.WasSavedByGoalkeeper || ball.GoalScoredThisShot) return;

            Vector3 prevPos = ball.PreviousPosition;
            Vector3 currPos = ball.transform.position;
            float minZ = Mathf.Min(prevPos.z, currPos.z);
            float maxZ = Mathf.Max(prevPos.z, currPos.z);

            if (maxZ < startPosition.z - 0.42f || minZ > 0.12f) return;

            Vector3 pelvisPos = hips != null ? hips.position : transform.position + Vector3.up * 0.85f;
            Vector3 torsoPos = chest != null ? chest.position : transform.position + Vector3.up * 1.25f;
            Vector3 leftHandPos = leftGloveAnchor != null ? leftGloveAnchor.position : pelvisPos + Vector3.left * 0.45f;
            Vector3 rightHandPos = rightGloveAnchor != null ? rightGloveAnchor.position : pelvisPos + Vector3.right * 0.45f;

            float dPelvis = DistanceToSegment(pelvisPos, prevPos, currPos);
            float dTorso = DistanceToSegment(torsoPos, prevPos, currPos);
            float dL = DistanceToSegment(leftHandPos, prevPos, currPos);
            float dR = DistanceToSegment(rightHandPos, prevPos, currPos);

            float bodyRadius = 0.28f;
            float gloveRadius = 0.26f + (currentRoundNumber - 1) * 0.025f;

            bool footIntercept = false;
            if (leftFoot != null && rightFoot != null)
            {
                float dFootL = DistanceToSegment(leftFoot.position, prevPos, currPos);
                float dFootR = DistanceToSegment(rightFoot.position, prevPos, currPos);
                footIntercept = dFootL <= 0.22f || dFootR <= 0.22f;
            }

            if (dPelvis <= bodyRadius || dTorso <= bodyRadius || dL <= gloveRadius || dR <= gloveRadius || footIntercept)
            {
                PerformSaveDeflection(ball);
            }
        }

        private static float DistanceToSegment(Vector3 point, Vector3 segA, Vector3 segB)
        {
            Vector3 ab = segB - segA;
            float lenSq = ab.sqrMagnitude;
            if (lenSq < 0.00001f) return Vector3.Distance(point, segA);
            float t = Mathf.Clamp01(Vector3.Dot(point - segA, ab) / lenSq);
            return Vector3.Distance(point, segA + ab * t);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!isShotActive || hasSavedCurrentShot) return;

            BallController ball = collision.gameObject.GetComponent<BallController>();
            if (ball != null && ball.HasBeenShot && !ball.WasSavedByGoalkeeper && !ball.GoalScoredThisShot)
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
                float sideDir = ball.transform.position.x - transform.position.x;
                if (Mathf.Abs(sideDir) < 0.15f) sideDir = UnityEngine.Random.value < 0.5f ? -0.6f : 0.6f;

                Vector3 deflectDir = new Vector3(Mathf.Sign(sideDir) * 0.7f, 0.42f, -1.0f).normalized;
                ballRb.linearVelocity = deflectDir * UnityEngine.Random.Range(6.5f, 9.5f);
                ballRb.angularVelocity = new Vector3(4f, sideDir * 8f, 0f);
            }

            AudioManager.Instance?.PlaySfx(SfxType.S03_Save);
            if (PenaltyManager.Instance != null)
            {
                PenaltyManager.Instance.OnGoalkeeperSavedBall();
            }
            else
            {
                PlaySaveCelebration();
            }
        }

        public void PlaySaveCelebration()
        {
            if (!isActiveAndEnabled) return;
            if (animCoroutine != null) StopCoroutine(animCoroutine);
            saveCelebrationVariant = (saveCelebrationVariant + 1) % 2;
            animCoroutine = StartCoroutine(SaveCelebrationRoutine(saveCelebrationVariant));
        }

        private IEnumerator SaveCelebrationRoutine(int variant)
        {
            CurrentState = GoalkeeperState.Recovering;
            isCelebratingSave = true;
            Vector3 startP = transform.position;
            Quaternion startR = transform.rotation;
            Quaternion startHipsRot = hips != null ? hips.localRotation : Quaternion.identity;
            Vector3 startHipsPos = hips != null ? hips.localPosition : hipsBaseLocalPos;

            float recoverT = 0f;
            while (recoverT < 0.25f)
            {
                recoverT += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(recoverT / 0.25f));
                MoveRoot(new Vector3(startP.x, Mathf.Lerp(startP.y, 0f, u), startPosition.z), Quaternion.Slerp(startR, startRotation, u));
                if (hips != null)
                {
                    hips.localPosition = Vector3.Lerp(startHipsPos, hipsBaseLocalPos, u);
                    hips.localRotation = Quaternion.Slerp(startHipsRot, Quaternion.identity, u);
                }
                if (leftLeg != null) leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, Quaternion.identity, u);
                if (leftShin != null) leftShin.localRotation = Quaternion.Slerp(leftShin.localRotation, Quaternion.identity, u);
                if (rightLeg != null) rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, Quaternion.identity, u);
                if (rightShin != null) rightShin.localRotation = Quaternion.Slerp(rightShin.localRotation, Quaternion.identity, u);
                yield return null;
            }

            float timer = 0f;
            while (timer < 1.35f)
            {
                timer += Time.deltaTime;
                float blend = 1f - Mathf.Exp(-14f * Time.deltaTime);
                if (variant == 0)
                {
                    float pump = Mathf.Sin(timer * 12f) * 14f;
                    if (spine != null) spine.localRotation = Quaternion.Slerp(spine.localRotation, Quaternion.Euler(-8f, 0f, 0f), blend);
                    if (chest != null) chest.localRotation = Quaternion.Slerp(chest.localRotation, Quaternion.Euler(-6f, 0f, 0f), blend);
                    if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.Euler(-135f + pump, 0f, -20f), blend);
                    if (leftForearm != null) leftForearm.localRotation = Quaternion.Slerp(leftForearm.localRotation, Quaternion.Euler(-35f, 0f, 0f), blend);
                    if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler(-135f - pump, 0f, 20f), blend);
                    if (rightForearm != null) rightForearm.localRotation = Quaternion.Slerp(rightForearm.localRotation, Quaternion.Euler(-35f, 0f, 0f), blend);
                }
                else
                {
                    // Glove clap & chest thump hype
                    float clap = Mathf.Abs(Mathf.Sin(timer * 8f));
                    if (spine != null) spine.localRotation = Quaternion.Slerp(spine.localRotation, Quaternion.Euler(4f, 0f, 0f), blend);
                    if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.Euler(-62f, 28f - clap * 18f, -14f), blend);
                    if (leftForearm != null) leftForearm.localRotation = Quaternion.Slerp(leftForearm.localRotation, Quaternion.Euler(-65f, 0f, 0f), blend);
                    if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler(-62f, -28f + clap * 18f, 14f), blend);
                    if (rightForearm != null) rightForearm.localRotation = Quaternion.Slerp(rightForearm.localRotation, Quaternion.Euler(-65f, 0f, 0f), blend);
                }
                yield return null;
            }
        }

        public void PlayConcededReaction()
        {
            if (!isActiveAndEnabled) return;
            if (animCoroutine != null) StopCoroutine(animCoroutine);
            concededVariant = (concededVariant + 1) % 2;
            animCoroutine = StartCoroutine(ConcededReactionRoutine(concededVariant));
        }

        private IEnumerator ConcededReactionRoutine(int variant)
        {
            CurrentState = GoalkeeperState.Recovering;
            isShowingConceded = true;
            Vector3 startP = transform.position;
            Quaternion startHipsRot = hips != null ? hips.localRotation : Quaternion.identity;
            Vector3 startHipsPos = hips != null ? hips.localPosition : hipsBaseLocalPos;

            float recoverT = 0f;
            while (recoverT < 0.25f)
            {
                recoverT += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(recoverT / 0.25f));
                MoveRoot(new Vector3(startP.x, Mathf.Lerp(startP.y, 0f, u), startPosition.z), startRotation);
                if (hips != null)
                {
                    hips.localPosition = Vector3.Lerp(startHipsPos, hipsBaseLocalPos, u);
                    hips.localRotation = Quaternion.Slerp(startHipsRot, Quaternion.identity, u);
                }
                yield return null;
            }

            float timer = 0f;
            while (timer < 1.35f)
            {
                timer += Time.deltaTime;
                float blend = 1f - Mathf.Exp(-12f * Time.deltaTime);
                if (variant == 0)
                {
                    if (spine != null) spine.localRotation = Quaternion.Slerp(spine.localRotation, Quaternion.Euler(20f, 0f, 0f), blend);
                    if (head != null) head.localRotation = Quaternion.Slerp(head.localRotation, Quaternion.Euler(22f, Mathf.Sin(timer * 5f) * 12f, 0f), blend);
                    if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.Euler(-20f, 0f, -45f), blend);
                    if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler(-20f, 0f, 45f), blend);
                }
                else
                {
                    // Adjust gloves + disappointed shake
                    if (spine != null) spine.localRotation = Quaternion.Slerp(spine.localRotation, Quaternion.Euler(10f, 0f, 0f), blend);
                    if (head != null) head.localRotation = Quaternion.Slerp(head.localRotation, Quaternion.Euler(14f, Mathf.Sin(timer * 7f) * 16f, 0f), blend);
                    if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.Euler(-44f, 16f, -12f), blend);
                    if (leftForearm != null) leftForearm.localRotation = Quaternion.Slerp(leftForearm.localRotation, Quaternion.Euler(-58f, 0f, 0f), blend);
                    if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler(-44f, -16f, 12f), blend);
                    if (rightForearm != null) rightForearm.localRotation = Quaternion.Slerp(rightForearm.localRotation, Quaternion.Euler(-58f, 0f, 0f), blend);
                }
                yield return null;
            }
        }

        public void ResetPosition()
        {
            if (!isActiveAndEnabled) return;
            StopAllGoalkeeperCoroutines();

            isShotActive = false;
            hasSavedCurrentShot = false;
            HasDived = false;
            isCelebratingSave = false;
            isShowingConceded = false;
            CurrentState = GoalkeeperState.Resetting;

            resetCoroutine = StartCoroutine(SmoothResetRoutine());
        }

        private IEnumerator SmoothResetRoutine()
        {
            Vector3 currentPos = transform.position;
            Quaternion currentRot = transform.rotation;
            Quaternion startHipsRot = hips != null ? hips.localRotation : Quaternion.identity;
            Vector3 startHipsPos = hips != null ? hips.localPosition : hipsBaseLocalPos;

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 4.5f;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                MoveRoot(Vector3.Lerp(currentPos, startPosition, u), Quaternion.Slerp(currentRot, startRotation, u));
                if (hips != null)
                {
                    hips.localPosition = Vector3.Lerp(startHipsPos, hipsBaseLocalPos, u);
                    hips.localRotation = Quaternion.Slerp(startHipsRot, Quaternion.identity, u);
                }
                yield return null;
            }

            MoveRoot(startPosition, startRotation);
            ResetPose();
            StartIdleShuffle();
        }

        private void MoveRoot(Vector3 worldPos, Quaternion worldRot)
        {
            transform.SetPositionAndRotation(worldPos, worldRot);
        }

        private void ResetPose()
        {
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
    }
}
