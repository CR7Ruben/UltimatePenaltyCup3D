using UnityEngine;

namespace UltimatePenaltyCup.Gameplay
{
    /// <summary>
    /// Gentle wind flutter for stadium corner flags without allocating any per-frame memory.
    /// </summary>
    public class StadiumFlagWave : MonoBehaviour
    {
        [SerializeField] private float waveSpeed = 4.2f;
        [SerializeField] private float waveAngle = 9f;
        private float phase;
        private Quaternion baseRot;

        private void Awake()
        {
            baseRot = transform.localRotation;
            phase = transform.position.x * 0.7f + transform.position.z * 0.3f;
        }

        private void Update()
        {
            float angle = Mathf.Sin(Time.time * waveSpeed + phase) * waveAngle
                        + Mathf.Sin(Time.time * waveSpeed * 1.7f + phase) * (waveAngle * 0.35f);
            transform.localRotation = baseRot * Quaternion.Euler(0f, angle, angle * 0.25f);
        }
    }
}
