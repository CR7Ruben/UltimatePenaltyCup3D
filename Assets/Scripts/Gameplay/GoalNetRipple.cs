using System.Collections.Generic;
using UnityEngine;

namespace UltimatePenaltyCup.Gameplay
{
    public class GoalNetRipple : MonoBehaviour
    {
        public static GoalNetRipple Instance { get; private set; }

        private class NetMeshData
        {
            public Transform transform;
            public Mesh mesh;
            public Vector3[] baseVertices;
            public Vector3[] currentVertices;
        }

        private readonly List<NetMeshData> netMeshes = new List<NetMeshData>();
        private Vector3 impactWorldPos;
        private Vector3 impactDir;
        private float rippleIntensity = 0f;
        private float rippleTimer = 0f;

        private void Awake()
        {
            Instance = this;
            CacheNetMeshes();
        }

        private void CacheNetMeshes()
        {
            netMeshes.Clear();
            MeshFilter[] filters = GetComponentsInChildren<MeshFilter>(true);
            foreach (var mf in filters)
            {
                if (mf == null || mf.sharedMesh == null) continue;
                if (!mf.gameObject.name.StartsWith("Net")) continue;

                Mesh runtimeMesh = Instantiate(mf.sharedMesh);
                mf.sharedMesh = runtimeMesh;
                Vector3[] baseVerts = runtimeMesh.vertices;
                Vector3[] currVerts = new Vector3[baseVerts.Length];
                System.Array.Copy(baseVerts, currVerts, baseVerts.Length);

                netMeshes.Add(new NetMeshData
                {
                    transform = mf.transform,
                    mesh = runtimeMesh,
                    baseVertices = baseVerts,
                    currentVertices = currVerts
                });
            }
        }

        public void TriggerImpact(Vector3 worldPos, Vector3 ballVelocity)
        {
            impactWorldPos = worldPos;
            impactDir = ballVelocity.sqrMagnitude > 0.1f ? ballVelocity.normalized : Vector3.forward;
            rippleIntensity = Mathf.Clamp(ballVelocity.magnitude * 0.022f, 0.18f, 0.48f);
            rippleTimer = 0f;
        }

        private void Update()
        {
            if (rippleIntensity <= 0.002f) return;

            rippleTimer += Time.deltaTime;
            rippleIntensity = Mathf.Lerp(rippleIntensity, 0f, Time.deltaTime * 2.8f);

            float waveOsc = Mathf.Sin(rippleTimer * 18f);

            for (int m = 0; m < netMeshes.Count; m++)
            {
                var data = netMeshes[m];
                if (data.mesh == null || data.transform == null) continue;

                Vector3 localImpact = data.transform.InverseTransformPoint(impactWorldPos);
                Vector3 localDir = data.transform.InverseTransformDirection(impactDir);

                for (int i = 0; i < data.baseVertices.Length; i++)
                {
                    Vector3 v = data.baseVertices[i];
                    float dist = Vector3.Distance(v, localImpact);
                    float falloff = Mathf.Exp(-dist * 0.65f);
                    float disp = rippleIntensity * falloff * (0.65f + 0.35f * waveOsc);
                    data.currentVertices[i] = v + localDir * disp;
                }

                data.mesh.vertices = data.currentVertices;
                data.mesh.RecalculateNormals();
            }

            if (rippleIntensity <= 0.003f)
            {
                ResetNets();
            }
        }

        public void ResetNets()
        {
            rippleIntensity = 0f;
            for (int m = 0; m < netMeshes.Count; m++)
            {
                var data = netMeshes[m];
                if (data.mesh == null) continue;
                System.Array.Copy(data.baseVertices, data.currentVertices, data.baseVertices.Length);
                data.mesh.vertices = data.currentVertices;
                data.mesh.RecalculateNormals();
            }
        }
    }
}
