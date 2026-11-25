#pragma warning disable 0649
using System;
using UnityEngine;
using UltimatePenaltyCup.Core;
using UltimatePenaltyCup.Audio;
using UltimatePenaltyCup.SaveSystem;
using UltimatePenaltyCup.Shop;

namespace UltimatePenaltyCup.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class BallController : MonoBehaviour
    {
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
        public Vector3 InitialPosition { get; private set; } = new Vector3(0f, 0.15f, -11f);

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
            rb.mass = 0.43f; // Official FIFA football mass ~430g
            rb.linearDamping = 0.15f;
            rb.angularDamping = 0.25f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            // PhysicMaterial for bounce
            PhysicsMaterial ballPhys = new PhysicsMaterial("BallPhysicMaterial")
            {
                bounciness = 0.65f,
                bounceCombine = PhysicsMaterialCombine.Maximum,
                dynamicFriction = 0.4f,
                staticFriction = 0.45f,
                frictionCombine = PhysicsMaterialCombine.Average
            };
            sphereCollider.material = ballPhys;
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
            rb.isKinematic = false;
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

        public void ResetBall(Vector3 position)
        {
            InitialPosition = position;
            HasBeenShot = false;

            if (rb != null)
            {
                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                rb.isKinematic = true;
            }

            transform.position = position;
            transform.rotation = Quaternion.identity;

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
                AudioManager.Instance?.PlaySfx(SfxType.PostHit);
            }
            else if (collision.gameObject.CompareTag("Goalkeeper"))
            {
                AudioManager.Instance?.PlaySfx(SfxType.S03_Save);
            }
        }
    }
}
