using UnityEngine;

namespace UltimatePenaltyCup.Gameplay
{
    /// <summary>
    /// Smooth cinematic broadcast camera sway showcasing the 3D hero striker and stadium in MainMenu.
    /// </summary>
    public class MenuCameraOrbit : MonoBehaviour
    {
        [SerializeField] private Vector3 lookTarget = new Vector3(0.10f, 1.05f, -5.8f);
        [SerializeField] private Vector3 baseAnchor = new Vector3(1.15f, 1.65f, -14.15f);
        [SerializeField] private float swayAmplitudeX = 0.65f;
        [SerializeField] private float swayAmplitudeY = 0.16f;
        [SerializeField] private float orbitSpeed = 0.35f;

        private float timer = 0f;

        private void LateUpdate()
        {
            timer += Time.deltaTime * orbitSpeed;
            float x = baseAnchor.x + Mathf.Sin(timer) * swayAmplitudeX;
            float y = baseAnchor.y + Mathf.Cos(timer * 0.8f) * swayAmplitudeY;
            float z = baseAnchor.z + Mathf.Cos(timer * 0.5f) * 0.35f;

            transform.position = new Vector3(x, y, z);
            transform.LookAt(lookTarget);
        }
    }
}
