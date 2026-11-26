using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace UltimatePenaltyCup.Editor
{
    /// <summary>
    /// Procedural 17-bone Skinned Humanoid Mesh Generator.
    /// Produces watertight, anatomically proportioned 3D soccer character meshes
    /// with blended joint BoneWeights and dedicated UV submeshes (no disconnected primitives
    /// and no face texture bleeding onto limbs).
    /// </summary>
    public static class HumanoidMeshBuilder
    {
        private const string MODELS_DIR = "Assets/Art/Models";

        public struct BoneDef
        {
            public string name;
            public int parent;
            public Vector3 worldPos;
        }

        private class MeshBuilderContext
        {
            public readonly List<Vector3> vertices = new List<Vector3>();
            public readonly List<Vector2> uvs = new List<Vector2>();
            public readonly List<BoneWeight> weights = new List<BoneWeight>();
            public readonly List<int>[] submeshTris;

            public MeshBuilderContext(int submeshCount)
            {
                submeshTris = new List<int>[submeshCount];
                for (int i = 0; i < submeshCount; i++) submeshTris[i] = new List<int>();
            }
        }

        [MenuItem("Ultimate Penalty Cup/Visual Upgrade/Build Skinned Humanoid Meshes", false, 98)]
        public static void BuildAllCharacterMeshes()
        {
            if (!Directory.Exists(MODELS_DIR)) Directory.CreateDirectory(MODELS_DIR);

            CreateCharacterMesh("Mesh_RubenGonzalez.asset", heightScale: 1.0f, bulkScale: 1.0f, isGoalkeeper: false);
            CreateCharacterMesh("Mesh_GoalkeeperMalagon.asset", heightScale: 1.055f, bulkScale: 1.12f, isGoalkeeper: true);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[HumanoidMeshBuilder] Built unified skinned humanoid meshes.");
        }

        public static BoneDef[] GetSkeletonDefinition(float hScale = 1.0f, float bScale = 1.0f)
        {
            float sx = bScale;
            return new BoneDef[]
            {
                new BoneDef { name = "Hips",          parent = -1, worldPos = new Vector3(0f, 0.94f * hScale, 0f) },          // 0
                new BoneDef { name = "Spine",         parent = 0,  worldPos = new Vector3(0f, 1.08f * hScale, 0f) },          // 1
                new BoneDef { name = "Chest",         parent = 1,  worldPos = new Vector3(0f, 1.32f * hScale, 0f) },          // 2
                new BoneDef { name = "Neck",          parent = 2,  worldPos = new Vector3(0f, 1.50f * hScale, 0f) },          // 3
                new BoneDef { name = "Head",          parent = 3,  worldPos = new Vector3(0f, 1.60f * hScale, 0f) },          // 4

                new BoneDef { name = "LeftUpperArm",  parent = 2,  worldPos = new Vector3(-0.22f * sx, 1.44f * hScale, 0f) }, // 5
                new BoneDef { name = "LeftForearm",   parent = 5,  worldPos = new Vector3(-0.26f * sx, 1.17f * hScale, 0f) }, // 6
                new BoneDef { name = "LeftHand",      parent = 6,  worldPos = new Vector3(-0.28f * sx, 0.94f * hScale, 0f) }, // 7

                new BoneDef { name = "RightUpperArm", parent = 2,  worldPos = new Vector3( 0.22f * sx, 1.44f * hScale, 0f) }, // 8
                new BoneDef { name = "RightForearm",  parent = 8,  worldPos = new Vector3( 0.26f * sx, 1.17f * hScale, 0f) }, // 9
                new BoneDef { name = "RightHand",     parent = 9,  worldPos = new Vector3( 0.28f * sx, 0.94f * hScale, 0f) }, // 10

                new BoneDef { name = "LeftThigh",     parent = 0,  worldPos = new Vector3(-0.105f * sx, 0.89f * hScale, 0f) }, // 11
                new BoneDef { name = "LeftShin",      parent = 11, worldPos = new Vector3(-0.105f * sx, 0.48f * hScale, 0f) }, // 12
                new BoneDef { name = "LeftFoot",      parent = 12, worldPos = new Vector3(-0.105f * sx, 0.08f * hScale, 0f) }, // 13

                new BoneDef { name = "RightThigh",    parent = 0,  worldPos = new Vector3( 0.105f * sx, 0.89f * hScale, 0f) }, // 14
                new BoneDef { name = "RightShin",     parent = 14, worldPos = new Vector3( 0.105f * sx, 0.48f * hScale, 0f) }, // 15
                new BoneDef { name = "RightFoot",     parent = 15, worldPos = new Vector3( 0.105f * sx, 0.08f * hScale, 0f) }  // 16
            };
        }

        private static void CreateCharacterMesh(string assetName, float heightScale, float bulkScale, bool isGoalkeeper)
        {
            // Submeshes: 0=Jersey, 1=Shorts, 2=SkinBody, 3=Face, 4=Hair, 5=Socks, 6=Boots, (7=GKGloves)
            int submeshes = isGoalkeeper ? 8 : 7;
            var ctx = new MeshBuilderContext(submeshes);
            var bones = GetSkeletonDefinition(heightScale, bulkScale);

            float h = heightScale;
            float b = bulkScale;

            // 1. TORSO JERSEY (Connected chest -> shoulders -> neck base; continuous pelvis)
            BuildLoftedTube(ctx, 0, 16, new[]
            {
                Ring(new Vector3(0f, 0.96f * h, 0f), 0.145f * b, 0.105f * b, 0, 1, 0.7f, 0.3f),
                Ring(new Vector3(0f, 1.07f * h, 0f), 0.140f * b, 0.100f * b, 1, 2, 0.75f, 0.25f),
                Ring(new Vector3(0f, 1.22f * h, 0.01f), 0.165f * b, 0.115f * b, 1, 2, 0.35f, 0.65f),
                Ring(new Vector3(0f, 1.37f * h, 0.01f), 0.195f * b, 0.125f * b, 2, 1, 0.90f, 0.10f),
                Ring(new Vector3(0f, 1.48f * h, 0f), 0.185f * b, 0.110f * b, 2, 3, 0.85f, 0.15f),
                Ring(new Vector3(0f, 1.52f * h, 0f), 0.075f * b, 0.070f * b, 3, 2, 0.6f, 0.4f)
            }, jerseyUV: true);

            // 2. SHORTS (Pelvis + thigh cuffs connected seamlessly)
            BuildLoftedTube(ctx, 1, 14, new[]
            {
                Ring(new Vector3(0f, 0.85f * h, 0f), 0.160f * b, 0.115f * b, 0, 11, 0.85f, 0.15f),
                Ring(new Vector3(0f, 0.98f * h, 0f), 0.155f * b, 0.112f * b, 0, 1, 0.85f, 0.15f)
            });
            BuildLimbTube(ctx, 1, 12, new Vector3(-0.105f * b, 0.89f * h, 0f), new Vector3(-0.105f * b, 0.70f * h, 0f),
                0.092f * b, 0.088f * b, 0, 11, true);
            BuildLimbTube(ctx, 1, 12, new Vector3(0.105f * b, 0.89f * h, 0f), new Vector3(0.105f * b, 0.70f * h, 0f),
                0.092f * b, 0.088f * b, 0, 14, true);

            // 3. NECK (Pure Skin submesh 2)
            BuildLoftedTube(ctx, 2, 12, new[]
            {
                Ring(new Vector3(0f, 1.49f * h, 0f), 0.062f * b, 0.062f * b, 3, 2, 0.7f, 0.3f),
                Ring(new Vector3(0f, 1.56f * h, 0f), 0.056f * b, 0.056f * b, 3, 4, 0.6f, 0.4f),
                Ring(new Vector3(0f, 1.62f * h, 0.01f), 0.058f * b, 0.062f * b, 4, 3, 0.7f, 0.3f)
            });

            // 4. HEAD (Submesh 3 = Face, frontal UV mapped ONLY on +Z!)
            Vector3 headCenter = new Vector3(0f, 1.70f * h, 0.01f);
            BuildSculptedHead(ctx, 3, headCenter, 0.102f * b, 0.125f * h, 0.112f * b, 4, 3);

            // 5. STYLIZED HAIR (Submesh 4 = Hair)
            BuildSculptedHair(ctx, 4, headCenter, 0.108f * b, 0.130f * h, 0.118f * b, 4, isGoalkeeper);

            // 6. ARMS (Left & Right: Upper sleeve = Jersey submesh 0, Forearm = Skin 2 (or Jersey for GK), Hand = Skin 2 or Gloves 7)
            BuildArm(ctx, isLeft: true, h, b, isGoalkeeper, bones);
            BuildArm(ctx, isLeft: false, h, b, isGoalkeeper, bones);

            // 7. LEGS (Thigh = Skin 2, Knee/Shin/Calf = Socks 5, Boot = Boots 6)
            BuildLeg(ctx, isLeft: true, h, b, bones);
            BuildLeg(ctx, isLeft: false, h, b, bones);

            // Construct final Unity Mesh
            Mesh mesh = new Mesh();
            mesh.name = assetName.Replace(".asset", "");
            mesh.SetVertices(ctx.vertices);
            mesh.SetUVs(0, ctx.uvs);
            mesh.subMeshCount = submeshes;
            for (int s = 0; s < submeshes; s++)
            {
                mesh.SetTriangles(ctx.submeshTris[s], s);
            }
            mesh.boneWeights = ctx.weights.ToArray();

            // Compute Bindposes relative to root origin (model space == world space at build time)
            Matrix4x4[] bindPoses = new Matrix4x4[bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                bindPoses[i] = Matrix4x4.Translate(-bones[i].worldPos);
            }
            mesh.bindposes = bindPoses;

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            string savePath = Path.Combine(MODELS_DIR, assetName).Replace('\\', '/');
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(savePath);
            if (existing != null)
            {
                EditorUtility.CopySerialized(mesh, existing);
                EditorUtility.SetDirty(existing);
            }
            else
            {
                AssetDatabase.CreateAsset(mesh, savePath);
            }
        }

        private static void BuildArm(MeshBuilderContext ctx, bool isLeft, float h, float b, bool isGK, BoneDef[] bones)
        {
            float sign = isLeft ? -1f : 1f;
            int bUpper = isLeft ? 5 : 8;
            int bFore = isLeft ? 6 : 9;
            int bHand = isLeft ? 7 : 10;

            Vector3 shoulder = bones[bUpper].worldPos;
            Vector3 midUpper = Vector3.Lerp(bones[bUpper].worldPos, bones[bFore].worldPos, 0.55f);
            Vector3 elbow = bones[bFore].worldPos;
            Vector3 wrist = bones[bHand].worldPos;
            Vector3 handEnd = wrist + new Vector3(sign * 0.015f, -0.145f * h, 0.01f);

            // Shoulder cap into chest (submesh 0 jersey solid sleeve UV)
            BuildSphere(ctx, 0, shoulder, 0.064f * b, 10, 8, bUpper, 2, 0.7f, sleeveUV: true);

            // Sleeve (Jersey submesh 0 solid fabric)
            BuildLimbTube(ctx, 0, 12, shoulder, midUpper, 0.058f * b, 0.052f * b, bUpper, bFore, true, sleeveUV: true);

            // Forearm (GK long sleeve = submesh 0, striker bare skin = submesh 2)
            int forearmSubmesh = isGK ? 0 : 2;
            if (!isGK)
            {
                BuildLimbTube(ctx, 2, 12, midUpper, elbow, 0.048f * b, 0.043f * b, bUpper, bFore, true);
            }
            else
            {
                BuildLimbTube(ctx, 0, 12, midUpper, elbow, 0.052f * b, 0.046f * b, bUpper, bFore, true, sleeveUV: true);
            }
            BuildLimbTube(ctx, forearmSubmesh, 12, elbow, wrist, 0.044f * b, 0.035f * b, bFore, bHand, true, sleeveUV: isGK);

            // Hand / Goalkeeper Glove
            int handSub = isGK ? 7 : 2;
            float handW = isGK ? 0.068f * b : 0.042f * b;
            float handT = isGK ? 0.036f * b : 0.022f * b;
            if (isGK)
            {
                // Wrist strap cuff
                BuildLimbTube(ctx, 7, 12, wrist + new Vector3(0, 0.04f, 0), wrist, 0.050f * b, 0.048f * b, bFore, bHand, true);
            }
            BuildBoxyCapsule(ctx, handSub, Vector3.Lerp(wrist, handEnd, 0.45f), new Vector3(handT * 2f, 0.145f * h, handW * 2f), bHand, bFore, 0.88f);
            // Thumb
            BuildBoxyCapsule(ctx, handSub, wrist + new Vector3(0f, -0.05f * h, handW * 0.9f), new Vector3(handT * 1.1f, 0.06f * h, handT * 1.4f), bHand, bFore, 0.92f);
        }

        private static void BuildLeg(MeshBuilderContext ctx, bool isLeft, float h, float b, BoneDef[] bones)
        {
            int bThigh = isLeft ? 11 : 14;
            int bShin = isLeft ? 12 : 15;
            int bFoot = isLeft ? 13 : 16;

            Vector3 hip = bones[bThigh].worldPos;
            Vector3 knee = bones[bShin].worldPos;
            Vector3 sockTop = Vector3.Lerp(hip, knee, 0.88f);
            Vector3 ankle = bones[bFoot].worldPos;

            // Muscular thigh (Skin submesh 2) emerging cleanly out of shorts
            Vector3 shortsExit = new Vector3(hip.x, 0.72f * h, 0f);
            BuildLimbTube(ctx, 2, 14, shortsExit, sockTop, 0.078f * b, 0.062f * b, bThigh, bShin, true);

            // Knee + Calf Soccer Sock (Socks submesh 5)
            BuildLoftedTube(ctx, 5, 14, new[]
            {
                Ring(ankle, 0.044f * b, 0.046f * b, bFoot, bShin, 0.45f, 0.55f),
                Ring(Vector3.Lerp(ankle, knee, 0.45f), 0.054f * b, 0.056f * b, bShin, bFoot, 0.85f, 0.15f),
                Ring(Vector3.Lerp(ankle, knee, 0.72f), 0.064f * b, 0.065f * b, bShin, bThigh, 0.85f, 0.15f),
                Ring(sockTop, 0.060f * b, 0.062f * b, bShin, bThigh, 0.55f, 0.45f)
            });

            // Sculpted Soccer Cleat / Boot (Boots submesh 6) sleek & grounded cleanly at y = 0.0f
            Vector3 heel = new Vector3(ankle.x, 0.042f * h, -0.040f);
            Vector3 midFoot = new Vector3(ankle.x, 0.040f * h, 0.042f);
            Vector3 toeBox = new Vector3(ankle.x, 0.030f * h, 0.130f);

            BuildBoxyCapsule(ctx, 6, heel, new Vector3(0.086f * b, 0.076f * h, 0.082f), bFoot, bShin, 0.85f);
            BuildBoxyCapsule(ctx, 6, midFoot, new Vector3(0.090f * b, 0.070f * h, 0.105f), bFoot, bShin, 0.95f);
            BuildBoxyCapsule(ctx, 6, toeBox, new Vector3(0.082f * b, 0.050f * h, 0.085f), bFoot, bShin, 1.0f);

            // 6 subtle low cleats touching y = 0.00m
            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sz = 0; sz < 3; sz++)
                {
                    float zPos = -0.04f + sz * 0.08f;
                    BuildBoxyCapsule(ctx, 6, new Vector3(ankle.x + sx * 0.026f * b, 0.006f * h, zPos),
                        new Vector3(0.012f, 0.012f, 0.012f), bFoot, bFoot, 1.0f);
                }
            }
        }

        private struct RingSpec
        {
            public Vector3 center;
            public float rx, rz;
            public int boneA, boneB;
            public float wA, wB;
        }

        private static RingSpec Ring(Vector3 c, float rx, float rz, int bA, int bB, float wA, float wB)
        {
            return new RingSpec { center = c, rx = rx, rz = rz, boneA = bA, boneB = bB, wA = wA, wB = wB };
        }

        private static void BuildLoftedTube(MeshBuilderContext ctx, int submesh, int radialSegs, RingSpec[] rings, bool jerseyUV = false)
        {
            int ringCount = rings.Length;
            int baseVertex = ctx.vertices.Count;
            var tris = ctx.submeshTris[submesh];

            for (int r = 0; r < ringCount; r++)
            {
                float v = r / (float)(ringCount - 1);
                var rs = rings[r];
                for (int i = 0; i <= radialSegs; i++)
                {
                    float theta = (i / (float)radialSegs) * Mathf.PI * 2f;
                    float cos = Mathf.Cos(theta);
                    float sin = Mathf.Sin(theta);
                    Vector3 pos = rs.center + new Vector3(cos * rs.rx, 0f, sin * rs.rz);

                    ctx.vertices.Add(pos);
                    if (jerseyUV)
                    {
                        // Front hemisphere (+Z, sin >= 0) maps left-to-right [0.04..0.46];
                        // Back hemisphere (-Z, sin < 0) maps left-to-right from viewer behind (+X on right!) [0.54..0.96]
                        float u = sin >= 0f
                            ? Mathf.Lerp(0.04f, 0.46f, (cos + 1f) * 0.5f)
                            : Mathf.Lerp(0.54f, 0.96f, (cos + 1f) * 0.5f);
                        ctx.uvs.Add(new Vector2(u, v));
                    }
                    else
                    {
                        ctx.uvs.Add(new Vector2(i / (float)radialSegs, v));
                    }
                    ctx.weights.Add(MakeWeight(rs.boneA, rs.wA, rs.boneB, rs.wB));
                }
            }

            int stride = radialSegs + 1;
            for (int r = 0; r < ringCount - 1; r++)
            {
                for (int i = 0; i < radialSegs; i++)
                {
                    int a = baseVertex + r * stride + i;
                    int b = a + 1;
                    int c = a + stride;
                    int d = c + 1;

                    tris.Add(a); tris.Add(c); tris.Add(b);
                    tris.Add(b); tris.Add(c); tris.Add(d);
                }
            }
        }

        private static void BuildLimbTube(MeshBuilderContext ctx, int submesh, int segs, Vector3 pA, Vector3 pB,
            float rA, float rB, int boneA, int boneB, bool capEnds, bool sleeveUV = false)
        {
            Vector3 axis = (pB - pA).normalized;
            Vector3 refVec = Mathf.Abs(Vector3.Dot(axis, Vector3.forward)) > 0.9f ? Vector3.right : Vector3.forward;
            Vector3 tangent = Vector3.Cross(axis, refVec).normalized;
            Vector3 bitangent = Vector3.Cross(tangent, axis).normalized;

            int rings = 4;
            int baseV = ctx.vertices.Count;
            var tris = ctx.submeshTris[submesh];

            for (int r = 0; r < rings; r++)
            {
                float t = r / (float)(rings - 1);
                Vector3 c = Vector3.Lerp(pA, pB, t);
                float rad = Mathf.Lerp(rA, rB, t);
                float wA = 1f - t;
                float wB = t;

                for (int i = 0; i <= segs; i++)
                {
                    float ang = (i / (float)segs) * Mathf.PI * 2f;
                    Vector3 offset = (tangent * Mathf.Cos(ang) + bitangent * Mathf.Sin(ang)) * rad;
                    ctx.vertices.Add(c + offset);
                    if (sleeveUV)
                    {
                        ctx.uvs.Add(new Vector2(Mathf.Lerp(0.04f, 0.14f, i / (float)segs), Mathf.Lerp(0.10f, 0.42f, t)));
                    }
                    else
                    {
                        ctx.uvs.Add(new Vector2(i / (float)segs, t));
                    }
                    ctx.weights.Add(MakeWeight(boneA, wA, boneB, wB));
                }
            }

            int stride = segs + 1;
            for (int r = 0; r < rings - 1; r++)
            {
                for (int i = 0; i < segs; i++)
                {
                    int a = baseV + r * stride + i;
                    int b = a + 1;
                    int c = a + stride;
                    int d = c + 1;

                    tris.Add(a); tris.Add(c); tris.Add(b);
                    tris.Add(b); tris.Add(c); tris.Add(d);
                }
            }
        }

        private static void BuildSculptedHead(MeshBuilderContext ctx, int submesh, Vector3 center, float rx, float ry, float rz, int boneHead, int boneNeck)
        {
            int latSegs = 14;
            int lonSegs = 20;
            int baseV = ctx.vertices.Count;
            var tris = ctx.submeshTris[submesh];

            for (int lat = 0; lat <= latSegs; lat++)
            {
                float phi = (lat / (float)latSegs) * Mathf.PI; // 0 (bottom chin) to PI (crown)
                float sinPhi = Mathf.Sin(phi);
                float cosPhi = -Mathf.Cos(phi);

                for (int lon = 0; lon <= lonSegs; lon++)
                {
                    float theta = (lon / (float)lonSegs) * Mathf.PI * 2f;
                    float x = Mathf.Sin(theta) * sinPhi;
                    float y = cosPhi;
                    float z = Mathf.Cos(theta) * sinPhi;

                    // Subtle athletic jaw taper
                    float jawTaper = y < 0f ? Mathf.Lerp(1f, 0.80f, -y) : 1f;
                    Vector3 localPos = new Vector3(x * rx * jawTaper, y * ry, z * rz);

                    // Subtle nose bridge bump on front (+Z)
                    if (z > 0.7f && Mathf.Abs(x) < 0.22f && y > -0.15f && y < 0.15f)
                    {
                        localPos.z += 0.016f * (1f - Mathf.Abs(x) / 0.22f);
                    }

                    ctx.vertices.Add(center + localPos);

                    // Clean frontal UV projection: face texture only on front hemisphere (+Z)
                    float u, v;
                    if (z >= 0.05f)
                    {
                        u = 0.5f + x * 0.44f;
                        v = 0.5f + y * 0.44f;
                    }
                    else
                    {
                        // Back/sides sample pure skin corner pixel so eyes/mouth NEVER appear on back of head
                        u = 0.05f;
                        v = 0.05f;
                    }
                    ctx.uvs.Add(new Vector2(Mathf.Clamp01(u), Mathf.Clamp01(v)));

                    float wHead = Mathf.Clamp01(0.75f + y * 0.25f);
                    ctx.weights.Add(MakeWeight(boneHead, wHead, boneNeck, 1f - wHead));
                }
            }

            int stride = lonSegs + 1;
            for (int lat = 0; lat < latSegs; lat++)
            {
                for (int lon = 0; lon < lonSegs; lon++)
                {
                    int a = baseV + lat * stride + lon;
                    int b = a + 1;
                    int c = a + stride;
                    int d = c + 1;

                    tris.Add(a); tris.Add(b); tris.Add(c);
                    tris.Add(b); tris.Add(d); tris.Add(c);
                }
            }
        }

        private static void BuildSculptedHair(MeshBuilderContext ctx, int submesh, Vector3 headCenter, float rx, float ry, float rz, int boneHead, bool isGoalkeeper)
        {
            // Sculpted hair cap covering top, sides, and back of head (leaving front face clean!)
            int latSegs = 10;
            int lonSegs = 16;
            int baseV = ctx.vertices.Count;
            var tris = ctx.submeshTris[submesh];

            for (int lat = 0; lat <= latSegs; lat++)
            {
                float tLat = lat / (float)latSegs;
                float phi = Mathf.Lerp(Mathf.PI * 0.42f, Mathf.PI, tLat);
                float sinPhi = Mathf.Sin(phi);
                float cosPhi = -Mathf.Cos(phi);

                for (int lon = 0; lon <= lonSegs; lon++)
                {
                    float theta = (lon / (float)lonSegs) * Mathf.PI * 2f;
                    float x = Mathf.Sin(theta) * sinPhi;
                    float y = cosPhi;
                    float z = Mathf.Cos(theta) * sinPhi;

                    float crestBoost = (!isGoalkeeper && z > -0.2f && y > 0.35f && Mathf.Abs(x) < 0.45f)
                        ? 0.025f * (1f - Mathf.Abs(x) / 0.45f)
                        : 0f;

                    Vector3 pos = headCenter + new Vector3(x * rx, y * ry + crestBoost + 0.008f, z * rz - 0.012f);
                    // Pull front hairline back behind forehead
                    if (z > 0.45f && y < 0.55f)
                    {
                        pos.z = headCenter.z + 0.45f * rz;
                    }

                    ctx.vertices.Add(pos);
                    ctx.uvs.Add(new Vector2(lon / (float)lonSegs, tLat));
                    ctx.weights.Add(MakeWeight(boneHead, 1f, boneHead, 0f));
                }
            }

            int stride = lonSegs + 1;
            for (int lat = 0; lat < latSegs; lat++)
            {
                for (int lon = 0; lon < lonSegs; lon++)
                {
                    int a = baseV + lat * stride + lon;
                    int b = a + 1;
                    int c = a + stride;
                    int d = c + 1;

                    tris.Add(a); tris.Add(b); tris.Add(c);
                    tris.Add(b); tris.Add(d); tris.Add(c);
                }
            }
        }

        private static void BuildSphere(MeshBuilderContext ctx, int submesh, Vector3 center, float radius, int lonSegs, int latSegs, int boneA, int boneB, float wA, bool sleeveUV = false)
        {
            int baseV = ctx.vertices.Count;
            var tris = ctx.submeshTris[submesh];
            for (int lat = 0; lat <= latSegs; lat++)
            {
                float phi = (lat / (float)latSegs) * Mathf.PI;
                for (int lon = 0; lon <= lonSegs; lon++)
                {
                    float theta = (lon / (float)lonSegs) * Mathf.PI * 2f;
                    Vector3 n = new Vector3(Mathf.Sin(theta) * Mathf.Sin(phi), -Mathf.Cos(phi), Mathf.Cos(theta) * Mathf.Sin(phi));
                    ctx.vertices.Add(center + n * radius);
                    if (sleeveUV)
                    {
                        ctx.uvs.Add(new Vector2(Mathf.Lerp(0.04f, 0.14f, lon / (float)lonSegs), Mathf.Lerp(0.10f, 0.42f, lat / (float)latSegs)));
                    }
                    else
                    {
                        ctx.uvs.Add(new Vector2(lon / (float)lonSegs, lat / (float)latSegs));
                    }
                    ctx.weights.Add(MakeWeight(boneA, wA, boneB, 1f - wA));
                }
            }
            int stride = lonSegs + 1;
            for (int lat = 0; lat < latSegs; lat++)
            {
                for (int lon = 0; lon < lonSegs; lon++)
                {
                    int a = baseV + lat * stride + lon;
                    int b = a + 1;
                    int c = a + stride;
                    int d = c + 1;
                    tris.Add(a); tris.Add(b); tris.Add(c);
                    tris.Add(b); tris.Add(d); tris.Add(c);
                }
            }
        }

        private static void BuildBoxyCapsule(MeshBuilderContext ctx, int submesh, Vector3 center, Vector3 size, int boneA, int boneB, float wA)
        {
            // Rounded chamfered box (8-seg bevel) for hands, gloves, and boots
            Vector3 half = size * 0.5f;
            int segs = 10;
            int rings = 3;
            int baseV = ctx.vertices.Count;
            var tris = ctx.submeshTris[submesh];

            for (int r = 0; r < rings; r++)
            {
                float ty = (r / (float)(rings - 1)) * 2f - 1f;
                float pinch = r == 1 ? 1.0f : 0.78f;
                for (int i = 0; i <= segs; i++)
                {
                    float ang = (i / (float)segs) * Mathf.PI * 2f;
                    float cx = Mathf.Sign(Mathf.Cos(ang)) * Mathf.Pow(Mathf.Abs(Mathf.Cos(ang)), 0.65f);
                    float cz = Mathf.Sign(Mathf.Sin(ang)) * Mathf.Pow(Mathf.Abs(Mathf.Sin(ang)), 0.65f);
                    Vector3 pos = center + new Vector3(cx * half.x * pinch, ty * half.y, cz * half.z * pinch);
                    ctx.vertices.Add(pos);
                    ctx.uvs.Add(new Vector2(i / (float)segs, r / (float)(rings - 1)));
                    ctx.weights.Add(MakeWeight(boneA, wA, boneB, 1f - wA));
                }
            }

            int stride = segs + 1;
            for (int r = 0; r < rings - 1; r++)
            {
                for (int i = 0; i < segs; i++)
                {
                    int a = baseV + r * stride + i;
                    int b = a + 1;
                    int c = a + stride;
                    int d = c + 1;
                    tris.Add(a); tris.Add(c); tris.Add(b);
                    tris.Add(b); tris.Add(c); tris.Add(d);
                }
            }

            // Cap top and bottom
            int botCenter = ctx.vertices.Count;
            ctx.vertices.Add(center + new Vector3(0f, -half.y, 0f));
            ctx.uvs.Add(new Vector2(0.5f, 0f));
            ctx.weights.Add(MakeWeight(boneA, wA, boneB, 1f - wA));

            int topCenter = ctx.vertices.Count;
            ctx.vertices.Add(center + new Vector3(0f, half.y, 0f));
            ctx.uvs.Add(new Vector2(0.5f, 1f));
            ctx.weights.Add(MakeWeight(boneA, wA, boneB, 1f - wA));

            for (int i = 0; i < segs; i++)
            {
                tris.Add(botCenter); tris.Add(baseV + i); tris.Add(baseV + i + 1);
                int topRow = baseV + (rings - 1) * stride;
                tris.Add(topCenter); tris.Add(topRow + i + 1); tris.Add(topRow + i);
            }
        }

        private static BoneWeight MakeWeight(int b0, float w0, int b1, float w1)
        {
            float sum = w0 + w1;
            if (sum <= 0.0001f) return new BoneWeight { boneIndex0 = b0, weight0 = 1f };
            return new BoneWeight
            {
                boneIndex0 = b0,
                weight0 = w0 / sum,
                boneIndex1 = b1,
                weight1 = w1 / sum
            };
        }
    }
}
