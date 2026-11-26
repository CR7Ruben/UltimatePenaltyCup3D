#pragma warning disable 0649
using System;
using UnityEngine;
using UltimatePenaltyCup.Core;
using UltimatePenaltyCup.Audio;
using UltimatePenaltyCup.SaveSystem;
using UltimatePenaltyCup.Shop;

namespace UltimatePenaltyCup.Gameplay
{
    [DefaultExecutionOrder(200)]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class BallController : MonoBehaviour
    {
        public const float BallMassKg = 0.43f;
        public const float LinearDampingValue = 0.12f;
        public const float AngularDampingValue = 0.22f;
        public const float MagnusCoefficient = 0.0020f;
        public const float MaxMagnusAccel = 1.1f;

        public static BallController Instance { get; private set; }

        [Header("Components")]
        [SerializeField] private Rigidbody rb;
        [SerializeField] private SphereCollider sphereCollider;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private ParticleSystem ballVfxParticles;

        [Header("Ball Customization Materials")]
        [SerializeField] private Material matClassic;
        [SerializeField] private Material matBlue;
        [SerializeField] private Material matFire;
        [SerializeField] private Material matRainbow;
        [SerializeField] private Material matGold;

        public bool HasBeenShot { get; private set; } = false;
        public bool WasSavedByGoalkeeper { get; private set; } = false;
        public bool HitPost { get; private set; } = false;
        public bool GoalScoredThisShot { get; private set; } = false;
        public Vector3 InitialPosition { get; private set; } = new Vector3(0f, 0.112f, -11f);
        public Vector3 PreviousPosition { get; private set; } = new Vector3(0f, 0.112f, -11f);
        public Vector3 Velocity => rb != null ? rb.linearVelocity : Vector3.zero;

        public event Action<Collision> OnBallCollision;

        private void Awake()
        {
            Instance = this;
            if (rb == null) rb = GetComponent<Rigidbody>();
            if (sphereCollider == null) sphereCollider = GetComponent<SphereCollider>();
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            if (trailRenderer == null) trailRenderer = GetComponent<TrailRenderer>();

            ConfigurePhysics();
        }

        private void Start()
        {
            ApplyEquippedBallSkin();
            ResetBall(InitialPosition);

            if (Customization.CustomizationManager.Instance != null)
            {
                Customization.CustomizationManager.Instance.OnBallEquipped += HandleBallEquipped;
            }
        }

        private void OnDestroy()
        {
            if (Customization.CustomizationManager.Instance != null)
            {
                Customization.CustomizationManager.Instance.OnBallEquipped -= HandleBallEquipped;
            }
        }

        private void HandleBallEquipped(BallType type)
        {
            ApplyEquippedBallSkin();
        }

        private void ConfigurePhysics()
        {
            rb.mass = BallMassKg; // Official FIFA football mass ~430g
            rb.linearDamping = LinearDampingValue;
            rb.angularDamping = AngularDampingValue;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            // PhysicMaterial for natural turf bounce & grip
            PhysicsMaterial ballPhys = new PhysicsMaterial("BallPhysicMaterial")
            {
                bounciness = 0.60f,
                bounceCombine = PhysicsMaterialCombine.Maximum,
                dynamicFriction = 0.42f,
                staticFriction = 0.46f,
                frictionCombine = PhysicsMaterialCombine.Average
            };
            sphereCollider.material = ballPhys;
        }

        private void FixedUpdate()
        {
            if (rb == null) return;

            PreviousPosition = transform.position;

            if (!HasBeenShot || rb.isKinematic || WasSavedByGoalkeeper || GoalScoredThisShot) return;

            // Gentle realistic Magnus curl proportional to spin x velocity
            Vector3 vel = rb.linearVelocity;
            if (vel.sqrMagnitude > 4f)
            {
                Vector3 magnus = ComputeMagnusAccel(vel, rb.angularVelocity);
                rb.AddForce(magnus, ForceMode.Acceleration);
            }
        }

        public static Vector3 ComputeMagnusAccel(Vector3 velocity, Vector3 angularVelocity)
        {
            if (velocity.sqrMagnitude <= 4f) return Vector3.zero;
            Vector3 magnus = Vector3.Cross(angularVelocity, velocity) * MagnusCoefficient;
            magnus.y = 0f;
            magnus.x = Mathf.Clamp(magnus.x, -MaxMagnusAccel, MaxMagnusAccel);
            magnus.z = 0f;
            return magnus;
        }

        public void ApplyEquippedBallSkin()
        {
            BallType equipped = SaveManager.Instance != null && SaveManager.Instance.CurrentData != null
                ? SaveManager.Instance.CurrentData.equippedBall
                : BallType.Classic;

            Material targetMat = equipped switch
            {
                BallType.Blue => matBlue,
                BallType.Fire => matFire,
                BallType.Rainbow => matRainbow,
                BallType.Gold => matGold,
                _ => matClassic
            };

            if (targetMat != null && meshRenderer != null)
            {
                meshRenderer.material = targetMat;
            }

            // Adjust trail color to match ball
            if (trailRenderer != null)
            {
                Color trailCol = equipped switch
                {
                    BallType.Blue => new Color(0f, 0.8f, 1f, 0.8f),
                    BallType.Fire => new Color(1f, 0.4f, 0f, 0.8f),
                    BallType.Rainbow => new Color(0.9f, 0.2f, 0.9f, 0.8f),
                    BallType.Gold => new Color(1f, 0.85f, 0.2f, 0.9f),
                    _ => new Color(1f, 1f, 1f, 0.6f)
                };
                trailRenderer.startColor = trailCol;
                trailRenderer.endColor = new Color(trailCol.r, trailCol.g, trailCol.b, 0f);

                if (ballVfxParticles != null)
                {
                    var main = ballVfxParticles.main;
                    main.startColor = trailCol;
                }
            }
        }

        public void SetCustomMaterials(Material classic, Material blue, Material fire, Material rainbow, Material gold)
        {
            matClassic = classic;
            matBlue = blue;
            matFire = fire;
            matRainbow = rainbow;
            matGold = gold;
            ApplyEquippedBallSkin();
        }

        public void Launch(Vector3 velocity, Vector3 angularVelocity)
        {
            if (HasBeenShot) return;

            HasBeenShot = true;
            GoalScoredThisShot = false;

            Vector3 launchPos = new Vector3(transform.position.x, Mathf.Max(transform.position.y, 0.115f), transform.position.z);
            PreviousPosition = launchPos;
            transform.position = launchPos;

            rb.isKinematic = false;
            rb.position = launchPos;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            Physics.SyncTransforms();
            rb.linearVelocity = velocity;
            rb.angularVelocity = angularVelocity;

            if (trailRenderer != null)
            {
                trailRenderer.Clear();
                trailRenderer.emitting = true;
            }

            if (ballVfxParticles != null)
            {
                ballVfxParticles.Play();
            }

            AudioManager.Instance?.PlaySfx(SfxType.S01_Kick);
        }

        public void MarkSavedByGoalkeeper()
        {
            WasSavedByGoalkeeper = true;
        }

        public void MarkGoalScored()
        {
            GoalScoredThisShot = true;
        }

        public void ResetBall(Vector3 position)
        {
            Vector3 safePos = new Vector3(position.x, Mathf.Max(position.y, 0.112f), position.z);
            InitialPosition = safePos;
            PreviousPosition = safePos;
            HasBeenShot = false;
            WasSavedByGoalkeeper = false;
            HitPost = false;
            GoalScoredThisShot = false;

            if (rb != null)
            {
                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                rb.interpolation = RigidbodyInterpolation.None;
                rb.isKinematic = true;
                rb.position = safePos;
                rb.rotation = Quaternion.identity;
            }

            transform.SetPositionAndRotation(safePos, Quaternion.identity);
            Physics.SyncTransforms();

            if (trailRenderer != null)
            {
                trailRenderer.emitting = false;
                trailRenderer.Clear();
            }

            if (ballVfxParticles != null)
            {
                ballVfxParticles.Stop();
                ballVfxParticles.Clear();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            OnBallCollision?.Invoke(collision);

            if (collision.gameObject.CompareTag("Post"))
            {
                HitPost = true;
                AudioManager.Instance?.PlaySfx(SfxType.PostHit);
                PenaltyManager.Instance?.NotifyPostHit();
            }
            else if (collision.gameObject.CompareTag("Goalkeeper"))
            {
                AudioManager.Instance?.PlaySfx(SfxType.S03_Save);
            }
        }
    }
}
