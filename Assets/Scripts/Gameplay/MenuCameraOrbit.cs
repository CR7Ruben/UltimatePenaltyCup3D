using UnityEngine;

namespace UltimatePenaltyCup.Gameplay
{
    /// <summary>
    /// Gentle broadcast camera orbit over the 3D stadium in MainMenu.
    /// </summary>
    public class MenuCameraOrbit : MonoBehaviour
    {
        [SerializeField] private Vector3 lookTarget = new Vector3(-2.0f, 1.3f, -4.5f);
        [SerializeField] private float baseRadius = 11.5f;
        [SerializeField] private float baseHeight = 4.2f;
        [SerializeField] private float orbitSpeed = 0.08f;

        private float angle = 0.35f;

        private void LateUpdate()
        {
            angle += Time.deltaTime * orbitSpeed;
            float x = lookTarget.x + Mathf.Sin(angle) * baseRadius;
            float z = lookTarget.z - Mathf.Cos(angle) * baseRadius;
            float y = baseHeight + Mathf.Sin(angle * 0.7f) * 0.45f;

            transform.position = new Vector3(x, y, z);
            transform.LookAt(lookTarget);
        }
    }
}
