#pragma warning disable 0649
using System;
using System.Collections;
using UnityEngine;
using UltimatePenaltyCup.Levels;
using UltimatePenaltyCup.Gameplay;

namespace UltimatePenaltyCup.Goalkeeper
{
    public class GoalkeeperAI : MonoBehaviour
    {
        public static GoalkeeperAI Instance { get; private set; }

        [Header("Tuning (Set dynamically by RoundDataSO)")]
        [SerializeField] private float reactionDelay = 0.35f;
        [SerializeField] private float moveSpeed = 4.5f;
        [SerializeField] private float diveSpeed = 5.0f;
        [SerializeField] private float errorProbability = 0.3f;

        [Header("Goal Bounds")]
        [SerializeField] private float minX = -3.2f;
        [SerializeField] private float maxX = 3.2f;
        [SerializeField] private float minY = 0.0f;
        [SerializeField] private float maxY = 2.3f;

        [Header("Humanoid Visual Parts")]
        [SerializeField] private Transform bodyRoot;
        [SerializeField] private Transform torso;
        [SerializeField] private Transform head;
        [SerializeField] private Transform leftArm;  // With glove
        [SerializeField] private Transform rightArm; // With glove
        [SerializeField] private Transform leftLeg;
        [SerializeField] private Transform rightLeg;

        private Vector3 startPosition;
        private Quaternion startRotation;
        private bool isShotActive = false;
        private bool hasDived = false;
        public bool HasDived => hasDived;

        private Coroutine reactionCoroutine;
        private Coroutine idleShuffleCoroutine;
        private Coroutine animCoroutine;

        public event Action OnSaveAttempted;

        private void Awake()
        {
            Instance = this;
            startPosition = transform.position;
            startRotation = transform.rotation;
        }

        private void Start()
        {
            AutoAssignRigReferences();
            StartIdleShuffle();
        }

        private void AutoAssignRigReferences()
        {
            if (bodyRoot == null) bodyRoot = transform.Find("Visuals") ?? transform;
            if (torso == null) torso = bodyRoot.Find("Torso");
            if (head == null) head = bodyRoot.Find("Head");
            if (leftArm == null) leftArm = bodyRoot.Find("Arm_Left");
            if (rightArm == null) rightArm = bodyRoot.Find("Arm_Right");
            if (leftLeg == null) leftLeg = bodyRoot.Find("Leg_Left");
            if (rightLeg == null) rightLeg = bodyRoot.Find("Leg_Right");
        }

        public void Configure(RoundDataSO roundData)
        {
            if (roundData != null)
            {
                reactionDelay = roundData.reactionDelay;
                moveSpeed = roundData.moveSpeed;
                diveSpeed = roundData.diveSpeed;
                errorProbability = roundData.errorProbability;
            }
        }

        public void StartIdleShuffle()
        {
            if (idleShuffleCoroutine != null) StopCoroutine(idleShuffleCoroutine);
            idleShuffleCoroutine = StartCoroutine(IdleShuffleRoutine());
        }

        private IEnumerator IdleShuffleRoutine()
        {
            float timer = 0f;
            while (!isShotActive)
            {
                timer += Time.deltaTime * 3.0f;
                float offsetX = Mathf.Sin(timer) * 0.55f;
                transform.position = new Vector3(startPosition.x + offsetX, startPosition.y, startPosition.z);

                // Ready athletic goalie stance
                float bounce = Mathf.Abs(Mathf.Sin(timer * 2f)) * 0.06f;
                if (torso != null) torso.localPosition = new Vector3(0f, 1.25f - bounce, 0f);

                // Hands held wide, ready to block
                if (leftArm != null) leftArm.localRotation = Quaternion.Euler(-15f, 0f, 35f + Mathf.Sin(timer) * 5f);
                if (rightArm != null) rightArm.localRotation = Quaternion.Euler(-15f, 0f, -35f - Mathf.Sin(timer) * 5f);

                yield return null;
            }
        }

        public void OnShotFired(Vector3 ballPos, Vector3 shotVelocity, float power)
        {
            isShotActive = true;
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
            // Human reaction delay
            yield return new WaitForSeconds(reactionDelay);

            float dz = startPosition.z - ballPos.z;
            float vz = shotVelocity.z;

            Vector3 targetIntercept = startPosition;

            if (vz > 1f)
            {
                float timeToGoal = dz / vz;
                float predictedX = ballPos.x + shotVelocity.x * timeToGoal;
                float predictedY = ballPos.y + shotVelocity.y * timeToGoal - 0.5f * 9.81f * timeToGoal * timeToGoal;

                // Check error probability (keeper misjudges)
                bool makeMistake = UnityEngine.Random.value < errorProbability;
                if (makeMistake)
                {
                    float mistakeOffset = UnityEngine.Random.Range(-1.6f, 1.6f);
                    predictedX += mistakeOffset;
                }

                predictedX = Mathf.Clamp(predictedX, minX, maxX);
                predictedY = Mathf.Clamp(predictedY, minY, maxY);

                targetIntercept = new Vector3(predictedX, predictedY, startPosition.z);
            }

            hasDived = true;
            OnSaveAttempted?.Invoke();

            // Smooth full-body dive animation
            float diveTimer = 0f;
            Vector3 fromPos = transform.position;
            float diveDuration = 0.50f;

            float diveDirX = Mathf.Sign(targetIntercept.x - fromPos.x);
            float diveDistX = Mathf.Abs(targetIntercept.x - fromPos.x);
            bool isHighDive = targetIntercept.y > 1.2f;

            while (diveTimer < diveDuration)
            {
                diveTimer += Time.deltaTime;
                float t = Mathf.Clamp01(diveTimer / diveDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                transform.position = Vector3.Lerp(fromPos, targetIntercept, smoothT);

                // Body dive tilt & limb stretch
                if (diveDistX > 0.4f)
                {
                    float tiltAngle = Mathf.Lerp(0f, -65f * diveDirX, smoothT);
                    transform.rotation = startRotation * Quaternion.Euler(0f, 0f, tiltAngle);

                    // Reach leading arm toward ball
                    if (diveDirX > 0)
                    {
                        if (rightArm != null) rightArm.localRotation = Quaternion.Euler(0f, 0f, -90f);
                        if (leftArm != null) leftArm.localRotation = Quaternion.Euler(0f, 0f, 20f);
                    }
                    else
                    {
                        if (leftArm != null) leftArm.localRotation = Quaternion.Euler(0f, 0f, 90f);
                        if (rightArm != null) rightArm.localRotation = Quaternion.Euler(0f, 0f, -20f);
                    }

                    // Legs kick out in flight
                    if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(20f * diveDirX, 0f, -25f * diveDirX);
                    if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(-20f * diveDirX, 0f, -25f * diveDirX);
                }
                else if (isHighDive)
                {
                    // Vertical leap with both hands reaching up
                    if (leftArm != null) leftArm.localRotation = Quaternion.Euler(-140f, 0f, 15f);
                    if (rightArm != null) rightArm.localRotation = Quaternion.Euler(-140f, 0f, -15f);
                }

                yield return null;
            }
        }

        public void PlaySaveCelebration()
        {
            if (animCoroutine != null) StopCoroutine(animCoroutine);
            animCoroutine = StartCoroutine(SaveCelebrationRoutine());
        }

        private IEnumerator SaveCelebrationRoutine()
        {
            float timer = 0f;
            while (timer < 1.8f)
            {
                timer += Time.deltaTime;
                // Double fist pump!
                if (leftArm != null) leftArm.localRotation = Quaternion.Euler(-100f, 0f, 25f + Mathf.Sin(timer * 10f) * 15f);
                if (rightArm != null) rightArm.localRotation = Quaternion.Euler(-100f, 0f, -25f - Mathf.Sin(timer * 10f) * 15f);
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
            float timer = 0f;
            while (timer < 1.8f)
            {
                timer += Time.deltaTime;
                // Turn around looking back at net
                if (torso != null) torso.localRotation = Quaternion.Euler(15f, 160f, 0f);
                if (head != null) head.localRotation = Quaternion.Euler(20f, 0f, 0f);
                yield return null;
            }
        }

        public void ResetPosition()
        {
            if (reactionCoroutine != null) StopCoroutine(reactionCoroutine);
            if (idleShuffleCoroutine != null) StopCoroutine(idleShuffleCoroutine);
            if (animCoroutine != null) StopCoroutine(animCoroutine);

            isShotActive = false;
            hasDived = false;

            StartCoroutine(SmoothResetRoutine());
        }

        private IEnumerator SmoothResetRoutine()
        {
            Vector3 currentPos = transform.position;
            Quaternion currentRot = transform.rotation;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 3.5f;
                transform.position = Vector3.Lerp(currentPos, startPosition, t);
                transform.rotation = Quaternion.Slerp(currentRot, startRotation, t);
                yield return null;
            }

            transform.position = startPosition;
            transform.rotation = startRotation;
            ResetPose();
            StartIdleShuffle();
        }

        private void ResetPose()
        {
            if (torso != null)
            {
                torso.localPosition = new Vector3(0f, 1.25f, 0f);
                torso.localRotation = Quaternion.identity;
            }
            if (head != null) head.localRotation = Quaternion.identity;
            if (leftArm != null) leftArm.localRotation = Quaternion.identity;
            if (rightArm != null) rightArm.localRotation = Quaternion.identity;
            if (leftLeg != null) leftLeg.localRotation = Quaternion.identity;
            if (rightLeg != null) rightLeg.localRotation = Quaternion.identity;
        }

        private void OnCollisionEnter(Collision collision)
        {
            BallController ball = collision.gameObject.GetComponent<BallController>();
            if (ball != null && ball.HasBeenShot)
            {
                Rigidbody ballRb = ball.GetComponent<Rigidbody>();
                if (ballRb != null)
                {
                    Vector3 deflectDir = (ball.transform.position - transform.position).normalized + Vector3.up * 0.35f;
                    ballRb.linearVelocity = deflectDir * 7.5f;
                }

                PenaltyManager.Instance?.OnGoalkeeperSavedBall();
                PlaySaveCelebration();
            }
        }
    }
}
