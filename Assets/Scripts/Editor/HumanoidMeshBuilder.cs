using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace UltimatePenaltyCup.Editor
{
    /// <summary>
    /// Procedural 22-bone Skinned Humanoid Mesh Generator.
    /// Produces watertight, anatomically sculpted 3D soccer character meshes with clavicles,
    /// multi-segment spine, fingers/gloves, ears, toe bones, smooth joint BoneWeights,
    /// facial BlendShapes (Blink, Focus, Effort, Happy, Frustrated), and dedicated UV submeshes.
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

            public int headStartVertex = -1;
            public int headEndVertex = -1;
            public Vector3 headCenter;
            public float headRx, headRy, headRz;

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
            Debug.Log("[HumanoidMeshBuilder] Built 22-bone humanized SkinnedMeshes with facial BlendShapes.");
        }

        public static BoneDef[] GetSkeletonDefinition(float hScale = 1.0f, float bScale = 1.0f)
        {
            float sx = bScale;
            return new BoneDef[]
            {
                // 0..5: Pelvis -> 3-segment Spine -> Neck -> Head
                new BoneDef { name = "Hips",          parent = -1, worldPos = new Vector3(0f, 0.94f * hScale, 0f) },            // 0
                new BoneDef { name = "Spine",         parent = 0,  worldPos = new Vector3(0f, 1.07f * hScale, 0.005f) },        // 1
                new BoneDef { name = "UpperSpine",    parent = 1,  worldPos = new Vector3(0f, 1.22f * hScale, 0.010f) },        // 2
                new BoneDef { name = "Chest",         parent = 2,  worldPos = new Vector3(0f, 1.36f * hScale, 0.012f) },        // 3
                new BoneDef { name = "Neck",          parent = 3,  worldPos = new Vector3(0f, 1.51f * hScale, 0.006f) },        // 4
                new BoneDef { name = "Head",          parent = 4,  worldPos = new Vector3(0f, 1.60f * hScale, 0.008f) },        // 5

                // 6..9: Left Clavicle -> Arm -> Forearm -> Hand
                new BoneDef { name = "LeftShoulder",  parent = 3,  worldPos = new Vector3(-0.095f * sx, 1.44f * hScale, 0f) },     // 6
                new BoneDef { name = "LeftUpperArm",  parent = 6,  worldPos = new Vector3(-0.215f * sx, 1.43f * hScale, 0f) },     // 7
                new BoneDef { name = "LeftForearm",   parent = 7,  worldPos = new Vector3(-0.248f * sx, 1.17f * hScale, 0.012f) }, // 8
                new BoneDef { name = "LeftHand",      parent = 8,  worldPos = new Vector3(-0.265f * sx, 0.94f * hScale, 0.048f) }, // 9

                // 10..13: Right Clavicle -> Arm -> Forearm -> Hand
                new BoneDef { name = "RightShoulder", parent = 3,  worldPos = new Vector3( 0.095f * sx, 1.44f * hScale, 0f) },     // 10
                new BoneDef { name = "RightUpperArm", parent = 10, worldPos = new Vector3( 0.215f * sx, 1.43f * hScale, 0f) },     // 11
                new BoneDef { name = "RightForearm",  parent = 11, worldPos = new Vector3( 0.248f * sx, 1.17f * hScale, 0.012f) }, // 12
                new BoneDef { name = "RightHand",     parent = 12, worldPos = new Vector3( 0.265f * sx, 0.94f * hScale, 0.048f) }, // 13

                // 14..17: Left Thigh -> Shin -> Foot -> Toe
                new BoneDef { name = "LeftThigh",     parent = 0,  worldPos = new Vector3(-0.105f * sx, 0.89f * hScale, 0f) },      // 14
                new BoneDef { name = "LeftShin",      parent = 14, worldPos = new Vector3(-0.105f * sx, 0.48f * hScale, 0.010f) },  // 15
                new BoneDef { name = "LeftFoot",      parent = 15, worldPos = new Vector3(-0.105f * sx, 0.08f * hScale, -0.010f) }, // 16
                new BoneDef { name = "LeftToe",       parent = 16, worldPos = new Vector3(-0.105f * sx, 0.025f * hScale, 0.115f) }, // 17

                // 18..21: Right Thigh -> Shin -> Foot -> Toe
                new BoneDef { name = "RightThigh",    parent = 0,  worldPos = new Vector3( 0.105f * sx, 0.89f * hScale, 0f) },      // 18
                new BoneDef { name = "RightShin",     parent = 18, worldPos = new Vector3( 0.105f * sx, 0.48f * hScale, 0.010f) },  // 19
                new BoneDef { name = "RightFoot",     parent = 19, worldPos = new Vector3( 0.105f * sx, 0.08f * hScale, -0.010f) }, // 20
                new BoneDef { name = "RightToe",      parent = 20, worldPos = new Vector3( 0.105f * sx, 0.025f * hScale, 0.115f) }  // 21
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

            // 1. ANATOMICAL TORSO JERSEY (Natural lumbar curve, lats V-taper, pectoral relief, trapezius into neck collar)
            BuildLoftedTube(ctx, 0, 24, new[]
            {
                Ring(new Vector3(0f, 0.94f * h, 0.004f),  0.142f * b, 0.102f * b, 0, 1, 0.72f, 0.28f),
                Ring(new Vector3(0f, 1.03f * h, -0.002f), 0.132f * b, 0.095f * b, 1, 0, 0.65f, 0.35f), // Athletic waist taper
                Ring(new Vector3(0f, 1.16f * h, 0.008f),  0.150f * b, 0.106f * b, 2, 1, 0.68f, 0.32f), // Lower ribs / core
                Ring(new Vector3(0f, 1.30f * h, 0.018f),  0.180f * b, 0.122f * b, 3, 2, 0.68f, 0.32f), // Lats & lower pecs
                Ring(new Vector3(0f, 1.40f * h, 0.014f),  0.194f * b, 0.124f * b, 3, 2, 0.85f, 0.15f), // Broad shoulders/chest
                Ring(new Vector3(0f, 1.47f * h, 0.004f),  0.168f * b, 0.104f * b, 3, 4, 0.72f, 0.28f), // Trapezius slope
                Ring(new Vector3(0f, 1.515f * h, 0.005f), 0.072f * b, 0.068f * b, 4, 3, 0.65f, 0.35f)  // Jersey collar rim
            }, jerseyUV: true, anatomicalTorso: true);

            // 2. CONTOURED ATHLETIC SHORTS & PELVIS
            BuildLoftedTube(ctx, 1, 20, new[]
            {
                Ring(new Vector3(0f, 0.795f * h, -0.002f), 0.126f * b, 0.098f * b, 0, 14, 0.85f, 0.15f),
                Ring(new Vector3(0f, 0.855f * h, -0.003f), 0.149f * b, 0.112f * b, 0, 1,  0.86f, 0.14f),
                Ring(new Vector3(0f, 0.915f * h, -0.002f), 0.150f * b, 0.114f * b, 0, 1,  0.84f, 0.16f),
                Ring(new Vector3(0f, 0.960f * h,  0.002f), 0.144f * b, 0.105f * b, 0, 1,  0.80f, 0.20f)
            }, anatomicalTorso: true);
            BuildLimbTube(ctx, 1, 16, new Vector3(-0.082f * b, 0.88f * h, 0f), new Vector3(-0.104f * b, 0.675f * h, 0.005f),
                0.078f * b, 0.084f * b, 14, 0, true, wEndA: 0.92f, wEndB: 0.08f);
            BuildLimbTube(ctx, 1, 16, new Vector3(0.082f * b, 0.88f * h, 0f), new Vector3(0.104f * b, 0.675f * h, 0.005f),
                0.078f * b, 0.084f * b, 18, 0, true, wEndA: 0.92f, wEndB: 0.08f);

            // 3. HUMAN NECK (Sternocleidomastoid contour tapering naturally into jaw/skull)
            BuildLoftedTube(ctx, 2, 16, new[]
            {
                Ring(new Vector3(0f, 1.485f * h, 0.004f), 0.064f * b, 0.062f * b, 4, 3, 0.68f, 0.32f),
                Ring(new Vector3(0f, 1.545f * h, 0.005f), 0.054f * b, 0.056f * b, 4, 5, 0.65f, 0.35f),
                Ring(new Vector3(0f, 1.605f * h, 0.008f), 0.056f * b, 0.064f * b, 5, 4, 0.72f, 0.28f)
            });

            // 4. SCULPTED HUMAN HEAD + EARS (Submesh 3 = Face, frontal UV strictly on +Z)
            Vector3 headCenter = new Vector3(0f, 1.695f * h, 0.012f);
            float headRx = 0.098f * b;
            float headRy = 0.124f * h;
            float headRz = 0.112f * b;
            ctx.headCenter = headCenter;
            ctx.headRx = headRx;
            ctx.headRy = headRy;
            ctx.headRz = headRz;
            ctx.headStartVertex = ctx.vertices.Count;
            BuildSculptedHead(ctx, 3, headCenter, headRx, headRy, headRz, boneHead: 5, boneNeck: 4);
            ctx.headEndVertex = ctx.vertices.Count;

            // Ears in skin body submesh 2 so facial eyes/mouth textures never bleed onto ears
            BuildEar(ctx, 2, headCenter + new Vector3(-headRx * 0.96f, -0.012f * h, -0.008f), isLeft: true, b, h, boneHead: 5);
            BuildEar(ctx, 2, headCenter + new Vector3( headRx * 0.96f, -0.012f * h, -0.008f), isLeft: false, b, h, boneHead: 5);

            // 5. LAYERED ATHLETIC HAIR (Submesh 4)
            BuildSculptedHair(ctx, 4, headCenter, headRx * 1.045f, headRy * 1.035f, headRz * 1.045f, boneHead: 5, isGoalkeeper);

            // 6. SEAMLESS MUSCULAR ARMS + HANDS WITH FINGERS / PRO GLOVES
            BuildArm(ctx, isLeft: true, h, b, isGoalkeeper, bones);
            BuildArm(ctx, isLeft: false, h, b, isGoalkeeper, bones);

            // 7. ANATOMICAL LEGS, KNEES, CALVES, ACHILLES & SCULPTED CLEATS
            BuildLeg(ctx, isLeft: true, h, b, bones);
            BuildLeg(ctx, isLeft: false, h, b, bones);

            // Write directly onto existing asset (never CopySerialized on Mesh!)
            string savePath = Path.Combine(MODELS_DIR, assetName).Replace('\\', '/');
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(savePath);
            Mesh mesh = existing != null ? existing : new Mesh();
            if (existing != null)
            {
                mesh.Clear(false);
            }
            mesh.name = assetName.Replace(".asset", "");
            mesh.indexFormat = ctx.vertices.Count > 65000
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(ctx.vertices);
            mesh.SetUVs(0, ctx.uvs);
            mesh.subMeshCount = submeshes;
            for (int s = 0; s < submeshes; s++)
            {
                mesh.SetTriangles(ctx.submeshTris[s], s);
            }
            mesh.boneWeights = ctx.weights.ToArray();

            Matrix4x4[] bindPoses = new Matrix4x4[bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                bindPoses[i] = Matrix4x4.Translate(-bones[i].worldPos);
            }
            mesh.bindposes = bindPoses;

            mesh.RecalculateNormals();
            BuildFacialBlendShapes(mesh, ctx);
            mesh.RecalculateBounds();
            mesh.UploadMeshData(false);

            if (existing != null)
            {
                EditorUtility.SetDirty(mesh);
            }
            else
            {
                AssetDatabase.CreateAsset(mesh, savePath);
            }
        }

        private static void BuildFacialBlendShapes(Mesh mesh, MeshBuilderContext ctx)
        {
            int vCount = ctx.vertices.Count;
            if (ctx.headStartVertex < 0 || ctx.headEndVertex <= ctx.headStartVertex) return;

            Vector3[] dBlink = new Vector3[vCount];
            Vector3[] dFocus = new Vector3[vCount];
            Vector3[] dEffort = new Vector3[vCount];
            Vector3[] dHappy = new Vector3[vCount];
            Vector3[] dFrustrated = new Vector3[vCount];

            for (int i = ctx.headStartVertex; i < ctx.headEndVertex; i++)
            {
                Vector3 v = ctx.vertices[i];
                float nx = (v.x - ctx.headCenter.x) / ctx.headRx;
                float ny = (v.y - ctx.headCenter.y) / ctx.headRy;
                float nz = (v.z - ctx.headCenter.z) / ctx.headRz;
                if (nz < 0.25f) continue;

                float eyeX = Mathf.Abs(Mathf.Abs(nx) - 0.34f);
                // Upper eyelid mask for blinking
                if (eyeX < 0.16f && ny > 0.02f && ny < 0.22f)
                {
                    float w = (1f - eyeX / 0.16f) * Mathf.Sin(Mathf.Clamp01((ny - 0.02f) / 0.20f) * Mathf.PI);
                    dBlink[i] = new Vector3(0f, -0.0085f * w, 0.003f * w);
                }

                // Eyebrow ridge mask for Focus, Effort, Frustrated
                if (eyeX < 0.24f && ny > 0.16f && ny < 0.34f)
                {
                    float w = 1f - eyeX / 0.24f;
                    float inner = 1f - Mathf.Clamp01(Mathf.Abs(nx) / 0.34f);
                    dFocus[i] = new Vector3(-Mathf.Sign(nx) * 0.0022f * w, -0.0035f * w, 0.001f * w);
                    dEffort[i] = new Vector3(-Mathf.Sign(nx) * 0.0035f * w, -0.0055f * w, 0.002f * w);
                    dFrustrated[i] = new Vector3(-Mathf.Sign(nx) * 0.002f * w, 0.0045f * w * inner, 0f);
                }

                // Cheek & mouth corners
                float mouthCorner = Mathf.Abs(Mathf.Abs(nx) - 0.26f);
                if (Mathf.Abs(nx) < 0.42f && ny > -0.58f && ny < -0.18f)
                {
                    float cornerW = Mathf.Clamp01(1f - mouthCorner / 0.22f);
                    float centerW = Mathf.Clamp01(1f - Mathf.Abs(nx) / 0.25f);
                    dHappy[i] = new Vector3(Mathf.Sign(nx) * 0.0032f * cornerW, 0.0065f * cornerW, 0.003f * cornerW);
                    dFrustrated[i] = new Vector3(0f, -0.0058f * cornerW, -0.0015f * cornerW);
                    if (ny < -0.34f)
                    {
                        dEffort[i] = new Vector3(0f, -0.0065f * centerW, 0.002f * centerW);
                    }
                }
            }

            mesh.AddBlendShapeFrame("Blink", 100f, dBlink, null, null);
            mesh.AddBlendShapeFrame("Focus", 100f, dFocus, null, null);
            mesh.AddBlendShapeFrame("Effort", 100f, dEffort, null, null);
            mesh.AddBlendShapeFrame("Happy", 100f, dHappy, null, null);
            mesh.AddBlendShapeFrame("Frustrated", 100f, dFrustrated, null, null);
        }

        private static void BuildArm(MeshBuilderContext ctx, bool isLeft, float h, float b, bool isGK, BoneDef[] bones)
        {
            float sign = isLeft ? -1f : 1f;
            int bShoulder = isLeft ? 6 : 10;
            int bUpper    = isLeft ? 7 : 11;
            int bFore     = isLeft ? 8 : 12;
            int bHand     = isLeft ? 9 : 13;

            Vector3 clavRoot = new Vector3(sign * 0.145f * b, 1.43f * h, 0.005f);
            Vector3 shoulder = bones[bUpper].worldPos;
            Vector3 deltoidCap = Vector3.Lerp(shoulder, bones[bFore].worldPos, 0.16f);
            Vector3 midUpper = Vector3.Lerp(shoulder, bones[bFore].worldPos, 0.56f) + new Vector3(0f, 0f, -0.006f);
            Vector3 elbow = bones[bFore].worldPos;
            Vector3 midFore = Vector3.Lerp(elbow, bones[bHand].worldPos, 0.40f) + new Vector3(0f, 0f, 0.006f);
            Vector3 wrist = bones[bHand].worldPos;

            // Seamless Jersey Sleeve (deltoid insertion -> sleeve hem, zero separate ball!)
            BuildLimbChain(ctx, 0, 14, new[]
            {
                ChainNode(clavRoot,   0.068f * b, 0.062f * b, 3,         bShoulder, 0.55f, 0.45f),
                ChainNode(shoulder,   0.066f * b, 0.060f * b, bShoulder, bUpper,    0.45f, 0.55f),
                ChainNode(deltoidCap, 0.060f * b, 0.056f * b, bUpper,    bShoulder, 0.80f, 0.20f),
                ChainNode(midUpper,   0.051f * b, 0.049f * b, bUpper,    bFore,     0.85f, 0.15f)
            }, sleeveUV: true);

            // Lower Bicep -> Elbow -> Forearm -> Wrist
            int forearmSub = isGK ? 0 : 2;
            BuildLimbChain(ctx, forearmSub, 14, new[]
            {
                ChainNode(midUpper, 0.047f * b, 0.045f * b, bUpper, bFore, 0.85f, 0.15f),
                ChainNode(elbow,    0.042f * b, 0.042f * b, bUpper, bFore, 0.48f, 0.52f),
                ChainNode(midFore,  0.045f * b, 0.043f * b, bFore,  bUpper, 0.90f, 0.10f),
                ChainNode(wrist,    0.026f * b, 0.032f * b, bHand,  bFore,  0.52f, 0.48f)
            }, sleeveUV: isGK);

            // Sculpted Hand with 5 Individual Fingers (Rubén) or Pro Ergonomic Goalkeeper Glove (Malágon)
            BuildHumanHandOrGlove(ctx, isGK ? 7 : 2, wrist, sign, h, b, isGK, bHand, bFore);
        }

        private static void BuildHumanHandOrGlove(MeshBuilderContext ctx, int submesh, Vector3 wrist, float sign, float h, float b, bool isGK, int bHand, int bFore)
        {
            float palmLen = (isGK ? 0.105f : 0.088f) * h;
            float palmW   = (isGK ? 0.052f : 0.041f) * b;
            float palmT   = (isGK ? 0.024f : 0.015f) * b;

            if (isGK)
            {
                // Padded wrist strap cuff
                BuildLimbChain(ctx, submesh, 12, new[]
                {
                    ChainNode(wrist + new Vector3(0f, 0.042f * h, -0.004f), 0.040f * b, 0.032f * b, bFore, bHand, 0.55f, 0.45f),
                    ChainNode(wrist,                                        0.044f * b, 0.034f * b, bHand, bFore, 0.72f, 0.28f)
                });
            }

            // Anatomical tapered palm (thin across thumb-to-pinky thickness, natural width in Y/Z)
            Vector3 palmMid = wrist + new Vector3(sign * 0.006f, -palmLen * 0.52f, 0.005f);
            Vector3 knuckles = wrist + new Vector3(sign * 0.010f, -palmLen, 0.010f);
            BuildLimbChain(ctx, submesh, 12, new[]
            {
                ChainNode(wrist,    0.026f * b, isGK ? palmW * 0.72f : 0.032f * b, bHand, bFore, 0.78f, 0.22f),
                ChainNode(palmMid,  palmT,      palmW,         bHand, bFore, 0.94f, 0.06f),
                ChainNode(knuckles, palmT * 0.85f, palmW * 0.96f, bHand, bHand, 1f, 0f)
            });

            // 4 Fingers (index, middle, ring, pinky) + opposable thumb — natural gentle curve
            float[] fingerZ = { 0.74f, 0.25f, -0.24f, -0.70f };
            float[] fingerLen = { 0.068f, 0.076f, 0.070f, 0.055f };
            float fScale = isGK ? 1.24f : 1.0f;

            for (int f = 0; f < 4; f++)
            {
                float rF = (isGK ? 0.0115f : 0.0078f) * b;
                Vector3 rootF = knuckles + new Vector3(-sign * 0.002f, 0.004f, fingerZ[f] * palmW * 0.88f);
                Vector3 midF  = rootF + new Vector3(-sign * 0.008f, -fingerLen[f] * fScale * 0.54f * h, 0.002f);
                Vector3 tipF  = midF  + new Vector3(-sign * 0.010f, -fingerLen[f] * fScale * 0.46f * h, 0.002f);
                BuildLimbChain(ctx, submesh, 8, new[]
                {
                    ChainNode(rootF, rF,        rF,        bHand, bHand, 1f, 0f),
                    ChainNode(midF,  rF * 0.88f, rF * 0.88f, bHand, bHand, 1f, 0f),
                    ChainNode(tipF,  rF * 0.52f, rF * 0.52f, bHand, bHand, 1f, 0f)
                });
            }

            // Opposable Thumb projecting slightly forward (+Z) & inward
            float rThumb = (isGK ? 0.013f : 0.0092f) * b;
            Vector3 thumbBase = wrist + new Vector3(-sign * 0.004f, -0.028f * h, palmW * 0.78f);
            Vector3 thumbMid  = thumbBase + new Vector3(-sign * 0.008f, -0.032f * h, 0.022f * b);
            Vector3 thumbTip  = thumbMid + new Vector3(-sign * 0.006f, -0.024f * h, 0.012f * b);
            BuildLimbChain(ctx, submesh, 8, new[]
            {
                ChainNode(thumbBase, rThumb,        rThumb,        bHand, bHand, 1f, 0f),
                ChainNode(thumbMid,  rThumb * 0.88f, rThumb * 0.88f, bHand, bHand, 1f, 0f),
                ChainNode(thumbTip,  rThumb * 0.56f, rThumb * 0.56f, bHand, bHand, 1f, 0f)
            });
        }

        private static void BuildLeg(MeshBuilderContext ctx, bool isLeft, float h, float b, BoneDef[] bones)
        {
            int bThigh = isLeft ? 14 : 18;
            int bShin  = isLeft ? 15 : 19;
            int bFoot  = isLeft ? 16 : 20;
            int bToe   = isLeft ? 17 : 21;

            Vector3 hip   = bones[bThigh].worldPos;
            Vector3 knee  = bones[bShin].worldPos;
            Vector3 sockTop = Vector3.Lerp(hip, knee, 0.86f);
            Vector3 ankle = bones[bFoot].worldPos;

            // Muscular Thigh tapering into Knee (Skin submesh 2)
            Vector3 shortsOverlap = new Vector3(hip.x, 0.73f * h, 0.004f);
            BuildLoftedTube(ctx, 2, 18, new[]
            {
                Ring(sockTop, 0.056f * b, 0.059f * b, bShin, bThigh, 0.42f, 0.58f),
                Ring(Vector3.Lerp(shortsOverlap, sockTop, 0.45f), 0.068f * b, 0.072f * b, bThigh, bShin, 0.86f, 0.14f),
                Ring(shortsOverlap, 0.072f * b, 0.074f * b, bThigh, 0, 0.90f, 0.10f)
            });

            // Knee Cap -> Sculpted Gastrocnemius Calf -> Tapered Achilles Ankle (Socks submesh 5)
            BuildLoftedTube(ctx, 5, 18, new[]
            {
                Ring(new Vector3(ankle.x, 0.074f * h, -0.008f), 0.039f * b, 0.043f * b, bFoot, bShin, 0.45f, 0.55f),
                Ring(Vector3.Lerp(ankle, knee, 0.25f) + new Vector3(0f, 0f, -0.004f), 0.043f * b, 0.047f * b, bShin, bFoot, 0.80f, 0.20f),
                Ring(Vector3.Lerp(ankle, knee, 0.58f) + new Vector3(0f, 0f, -0.010f), 0.066f * b, 0.071f * b, bShin, bThigh, 0.92f, 0.08f),
                Ring(Vector3.Lerp(ankle, knee, 0.85f), 0.059f * b, 0.061f * b, bShin, bThigh, 0.76f, 0.24f),
                Ring(knee + new Vector3(0f, 0f, 0.004f), 0.057f * b, 0.061f * b, bShin, bThigh, 0.55f, 0.45f),
                Ring(sockTop, 0.059f * b, 0.062f * b, bShin, bThigh, 0.42f, 0.58f)
            });

            // Sleek Sculpted Soccer Boot / Cleat weighted cleanly across Foot + Toe
            BuildSculptedBoot(ctx, 6, ankle.x, h, b, bFoot, bShin, bToe);
        }

        private static void BuildSculptedBoot(MeshBuilderContext ctx, int submesh, float xOffset, float h, float b, int bFoot, int bShin, int bToe)
        {
            int radialSegs = 16;
            int baseV = ctx.vertices.Count;
            var tris = ctx.submeshTris[submesh];

            // Profile along length Z: heel (-0.078) -> midfoot -> ball of foot -> toe box (+0.186)
            (float z, float cy, float rx, float ry, int primaryBone, float wPrimary, int secBone)[] profile =
            {
                (-0.078f, 0.046f * h, 0.035f * b, 0.036f * h, bFoot, 0.84f, bShin),
                (-0.045f, 0.052f * h, 0.043f * b, 0.044f * h, bFoot, 0.88f, bShin),
                ( 0.005f, 0.054f * h, 0.045f * b, 0.045f * h, bFoot, 0.94f, bShin),
                ( 0.062f, 0.043f * h, 0.048f * b, 0.034f * h, bFoot, 0.72f, bToe),
                ( 0.118f, 0.034f * h, 0.045f * b, 0.025f * h, bToe,  0.76f, bFoot),
                ( 0.164f, 0.025f * h, 0.032f * b, 0.016f * h, bToe,  0.94f, bFoot),
                ( 0.186f, 0.019f * h, 0.015f * b, 0.009f * h, bToe,  1.00f, bToe)
            };

            for (int r = 0; r < profile.Length; r++)
            {
                var p = profile[r];
                float v = r / (float)(profile.Length - 1);
                for (int i = 0; i <= radialSegs; i++)
                {
                    float ang = (i / (float)radialSegs) * Mathf.PI * 2f;
                    float cos = Mathf.Cos(ang);
                    float sin = Mathf.Sin(ang);
                    float dy = sin < 0f ? sin * p.ry * 0.72f : sin * p.ry;
                    float py = Mathf.Max(0.008f * h, p.cy + dy);
                    ctx.vertices.Add(new Vector3(xOffset + cos * p.rx, py, p.z));
                    ctx.uvs.Add(new Vector2(i / (float)radialSegs, v));
                    ctx.weights.Add(MakeWeight(p.primaryBone, p.wPrimary, p.secBone, 1f - p.wPrimary));
                }
            }

            int stride = radialSegs + 1;
            for (int r = 0; r < profile.Length - 1; r++)
            {
                for (int i = 0; i < radialSegs; i++)
                {
                    int a = baseV + r * stride + i;
                    int c = a + 1;
                    int d = a + stride;
                    int e = d + 1;
                    tris.Add(a); tris.Add(c); tris.Add(d);
                    tris.Add(c); tris.Add(e); tris.Add(d);
                }
            }

            int heelCap = ctx.vertices.Count;
            ctx.vertices.Add(new Vector3(xOffset, 0.044f * h, -0.088f));
            ctx.uvs.Add(new Vector2(0f, 0f));
            ctx.weights.Add(MakeWeight(bFoot, 0.85f, bShin, 0.15f));

            int toeCap = ctx.vertices.Count;
            ctx.vertices.Add(new Vector3(xOffset, 0.018f * h, 0.194f));
            ctx.uvs.Add(new Vector2(1f, 1f));
            ctx.weights.Add(MakeWeight(bToe, 1f, bFoot, 0f));

            int lastRingBase = baseV + (profile.Length - 1) * stride;
            for (int i = 0; i < radialSegs; i++)
            {
                tris.Add(heelCap); tris.Add(baseV + i + 1); tris.Add(baseV + i);
                tris.Add(toeCap); tris.Add(lastRingBase + i); tris.Add(lastRingBase + i + 1);
            }

            // Boot ankle collar
            BuildLoftedTube(ctx, submesh, 14, new[]
            {
                Ring(new Vector3(xOffset, 0.062f * h, -0.01f), 0.044f * b, 0.050f * b, bFoot, bShin, 0.78f, 0.22f),
                Ring(new Vector3(xOffset, 0.088f * h,  0.00f), 0.041f * b, 0.045f * b, bFoot, bShin, 0.58f, 0.42f)
            });

            // Molded studs touching y = 0.00
            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sz = 0; sz < 3; sz++)
                {
                    float zPos = -0.045f + sz * 0.08f;
                    int studBone = sz == 2 ? bToe : bFoot;
                    BuildChainCaps(ctx, submesh, new Vector3(xOffset + sx * 0.023f * b, 0.005f * h, zPos),
                        new Vector3(0.011f, 0.010f, 0.014f), studBone);
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

        private struct ChainSpec
        {
            public Vector3 center;
            public float rA, rB;
            public int boneA, boneB;
            public float wA, wB;
        }

        private static ChainSpec ChainNode(Vector3 c, float rA, float rB, int bA, int bB, float wA, float wB)
        {
            return new ChainSpec { center = c, rA = rA, rB = rB, boneA = bA, boneB = bB, wA = wA, wB = wB };
        }

        private static void BuildLoftedTube(MeshBuilderContext ctx, int submesh, int radialSegs, RingSpec[] rings, bool jerseyUV = false, bool anatomicalTorso = false)
        {
            if (jerseyUV)
            {
                BuildLoftedHemisphere(ctx, submesh, Mathf.Max(10, radialSegs / 2), rings, isFront: true, anatomicalTorso);
                BuildLoftedHemisphere(ctx, submesh, Mathf.Max(10, radialSegs / 2), rings, isFront: false, anatomicalTorso);
                return;
            }

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
                    float dz = sin * rs.rz;
                    if (anatomicalTorso && sin > 0f && Mathf.Abs(cos) < 0.7f) dz *= 1.06f; // Subtle chest/quads roundness
                    Vector3 pos = rs.center + new Vector3(cos * rs.rx, 0f, dz);

                    ctx.vertices.Add(pos);
                    ctx.uvs.Add(new Vector2(i / (float)radialSegs, v));
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

        private static void BuildLoftedHemisphere(MeshBuilderContext ctx, int submesh, int halfSegs, RingSpec[] rings, bool isFront, bool anatomicalTorso)
        {
            int ringCount = rings.Length;
            int baseVertex = ctx.vertices.Count;
            var tris = ctx.submeshTris[submesh];

            float thetaStart = isFront ? 0f : Mathf.PI;
            float thetaEnd   = isFront ? Mathf.PI : Mathf.PI * 2f;

            for (int r = 0; r < ringCount; r++)
            {
                float v = r / (float)(ringCount - 1);
                var rs = rings[r];
                for (int i = 0; i <= halfSegs; i++)
                {
                    float theta = Mathf.Lerp(thetaStart, thetaEnd, i / (float)halfSegs);
                    float cos = Mathf.Cos(theta);
                    float sin = Mathf.Sin(theta);
                    float dz = sin * rs.rz;
                    if (anatomicalTorso)
                    {
                        // Pectoral projection on upper chest (+Z) and mild spine recess along back center (-Z)
                        if (isFront && v > 0.45f && v < 0.85f && Mathf.Abs(cos) < 0.65f)
                        {
                            dz += 0.012f * Mathf.Sin(Mathf.Clamp01((v - 0.45f) / 0.40f) * Mathf.PI);
                        }
                        else if (!isFront && v > 0.2f && v < 0.88f && Mathf.Abs(cos) < 0.25f)
                        {
                            dz += 0.007f * (1f - Mathf.Abs(cos) / 0.25f);
                        }
                    }

                    Vector3 pos = rs.center + new Vector3(cos * rs.rx, 0f, dz);
                    // Front (+Z): U [0.04..0.46]; Back (-Z): U [0.54..0.96] strictly left->right when viewed from behind
                    float u = isFront
                        ? Mathf.Lerp(0.04f, 0.46f, (1f - cos) * 0.5f)
                        : Mathf.Lerp(0.54f, 0.96f, (cos + 1f) * 0.5f);

                    ctx.vertices.Add(pos);
                    ctx.uvs.Add(new Vector2(u, v));
                    ctx.weights.Add(MakeWeight(rs.boneA, rs.wA, rs.boneB, rs.wB));
                }
            }

            int stride = halfSegs + 1;
            for (int r = 0; r < ringCount - 1; r++)
            {
                for (int i = 0; i < halfSegs; i++)
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

        private static void BuildLimbChain(MeshBuilderContext ctx, int submesh, int segs, ChainSpec[] nodes, bool sleeveUV = false)
        {
            int count = nodes.Length;
            int baseV = ctx.vertices.Count;
            var tris = ctx.submeshTris[submesh];

            Vector3 prevRef = Vector3.forward;
            for (int n = 0; n < count; n++)
            {
                Vector3 axis;
                if (n == 0) axis = (nodes[1].center - nodes[0].center).normalized;
                else if (n == count - 1) axis = (nodes[n].center - nodes[n - 1].center).normalized;
                else axis = (nodes[n + 1].center - nodes[n - 1].center).normalized;

                if (axis.sqrMagnitude < 0.0001f) axis = Vector3.down;
                if (Mathf.Abs(Vector3.Dot(axis, prevRef)) > 0.92f)
                    prevRef = Mathf.Abs(Vector3.Dot(axis, Vector3.right)) > 0.9f ? Vector3.up : Vector3.right;
                Vector3 tangent = Vector3.Cross(axis, prevRef).normalized;
                Vector3 bitangent = Vector3.Cross(tangent, axis).normalized;

                float t = n / (float)(count - 1);
                var nd = nodes[n];

                for (int i = 0; i <= segs; i++)
                {
                    float ang = (i / (float)segs) * Mathf.PI * 2f;
                    Vector3 offset = tangent * (Mathf.Cos(ang) * nd.rA) + bitangent * (Mathf.Sin(ang) * nd.rB);
                    ctx.vertices.Add(nd.center + offset);
                    if (sleeveUV)
                    {
                        ctx.uvs.Add(new Vector2(Mathf.Lerp(0.04f, 0.14f, i / (float)segs), Mathf.Lerp(0.10f, 0.42f, t)));
                    }
                    else
                    {
                        ctx.uvs.Add(new Vector2(i / (float)segs, t));
                    }
                    ctx.weights.Add(MakeWeight(nd.boneA, nd.wA, nd.boneB, nd.wB));
                }
            }

            int stride = segs + 1;
            for (int n = 0; n < count - 1; n++)
            {
                for (int i = 0; i < segs; i++)
                {
                    int a = baseV + n * stride + i;
                    int b = a + 1;
                    int c = a + stride;
                    int d = c + 1;
                    tris.Add(a); tris.Add(c); tris.Add(b);
                    tris.Add(b); tris.Add(c); tris.Add(d);
                }
            }
        }

        private static void BuildLimbTube(MeshBuilderContext ctx, int submesh, int segs, Vector3 pA, Vector3 pB,
            float rA, float rB, int boneA, int boneB, bool capEnds, bool sleeveUV = false, float wEndA = 1f, float wEndB = 0f)
        {
            Vector3 mid = Vector3.Lerp(pA, pB, 0.5f);
            float rMid = Mathf.Lerp(rA, rB, 0.5f);
            BuildLimbChain(ctx, submesh, segs, new[]
            {
                ChainNode(pA,  rA,   rA,   boneA, boneB, 0.65f, 0.35f),
                ChainNode(mid, rMid, rMid, boneA, boneB, 0.85f, 0.15f),
                ChainNode(pB,  rB,   rB,   boneA, boneB, wEndA, wEndB)
            }, sleeveUV);
        }

        private static void BuildSculptedHead(MeshBuilderContext ctx, int submesh, Vector3 center, float rx, float ry, float rz, int boneHead, int boneNeck)
        {
            int latSegs = 20;
            int lonSegs = 28;
            int baseV = ctx.vertices.Count;
            var tris = ctx.submeshTris[submesh];

            for (int lat = 0; lat <= latSegs; lat++)
            {
                float phi = (lat / (float)latSegs) * Mathf.PI;
                float sinPhi = Mathf.Sin(phi);
                float cosPhi = -Mathf.Cos(phi);

                for (int lon = 0; lon <= lonSegs; lon++)
                {
                    float theta = (lon / (float)lonSegs) * Mathf.PI * 2f;
                    float x = Mathf.Sin(theta) * sinPhi;
                    float y = cosPhi;
                    float z = Mathf.Cos(theta) * sinPhi;

                    // Refined jawline, cheekbone fullness, and cranial roundness
                    float jawTaper = y < 0f ? Mathf.Lerp(1f, 0.74f, Mathf.Pow(-y, 0.85f)) : Mathf.Lerp(1f, 0.96f, y);
                    float craniumBack = z < -0.15f && y > -0.25f ? 1.04f : 1.0f;
                    Vector3 localPos = new Vector3(x * rx * jawTaper, y * ry, z * rz * craniumBack);

                    if (z > 0.35f)
                    {
                        // Subtle brow ridge projection
                        if (y > 0.14f && y < 0.32f && Mathf.Abs(x) < 0.55f)
                        {
                            localPos.z += 0.0085f * Mathf.Cos((x / 0.55f) * Mathf.PI * 0.5f) * Mathf.Sin(((y - 0.14f) / 0.18f) * Mathf.PI);
                        }
                        // Gentle eye socket hollow
                        float eyeDx = Mathf.Abs(Mathf.Abs(x) - 0.34f);
                        if (eyeDx < 0.18f && y > -0.02f && y < 0.16f)
                        {
                            localPos.z -= 0.0055f * (1f - eyeDx / 0.18f);
                        }
                        // Sculpted nose bridge & tip
                        if (Mathf.Abs(x) < 0.18f && y > -0.22f && y < 0.18f && z > 0.65f)
                        {
                            float noseProfile = Mathf.Sin(Mathf.Clamp01((0.18f - y) / 0.40f) * Mathf.PI * 0.8f);
                            localPos.z += 0.019f * (1f - Mathf.Abs(x) / 0.18f) * noseProfile;
                        }
                        // Soft chin protrusion
                        if (Mathf.Abs(x) < 0.25f && y < -0.58f && y > -0.88f)
                        {
                            localPos.z += 0.0065f * (1f - Mathf.Abs(x) / 0.25f);
                        }
                    }

                    ctx.vertices.Add(center + localPos);

                    float u, v;
                    if (z >= 0.04f)
                    {
                        u = 0.5f + x * 0.45f;
                        v = 0.5f + y * 0.45f;
                    }
                    else
                    {
                        u = 0.05f;
                        v = 0.05f;
                    }
                    ctx.uvs.Add(new Vector2(Mathf.Clamp01(u), Mathf.Clamp01(v)));

                    float wHead = Mathf.Clamp01(0.76f + y * 0.24f);
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

        private static void BuildEar(MeshBuilderContext ctx, int submesh, Vector3 center, bool isLeft, float b, float h, int boneHead)
        {
            float sign = isLeft ? -1f : 1f;
            BuildChainCaps(ctx, submesh, center + new Vector3(sign * 0.004f * b, 0f, 0f),
                new Vector3(0.014f * b, 0.042f * h, 0.024f * b), boneHead);
        }

        private static void BuildSculptedHair(MeshBuilderContext ctx, int submesh, Vector3 headCenter, float rx, float ry, float rz, int boneHead, bool isGoalkeeper)
        {
            int latSegs = 12;
            int lonSegs = 20;
            int baseV = ctx.vertices.Count;
            var tris = ctx.submeshTris[submesh];

            for (int lat = 0; lat <= latSegs; lat++)
            {
                float tLat = lat / (float)latSegs;
                float phi = Mathf.Lerp(Mathf.PI * 0.40f, Mathf.PI, tLat);
                float sinPhi = Mathf.Sin(phi);
                float cosPhi = -Mathf.Cos(phi);

                for (int lon = 0; lon <= lonSegs; lon++)
                {
                    float theta = (lon / (float)lonSegs) * Mathf.PI * 2f;
                    float x = Mathf.Sin(theta) * sinPhi;
                    float y = cosPhi;
                    float z = Mathf.Cos(theta) * sinPhi;

                    float crestBoost = !isGoalkeeper
                        ? (z > -0.25f && y > 0.42f && Mathf.Abs(x) < 0.62f
                            ? 0.014f * Mathf.Cos((x / 0.62f) * Mathf.PI * 0.5f) * Mathf.SmoothStep(0.42f, 0.92f, y)
                            : 0f)
                        : (y > 0.5f ? 0.005f : 0f);

                    Vector3 pos = headCenter + new Vector3(x * rx, y * ry + crestBoost + 0.005f, z * rz - 0.010f);
                    if (z > 0.44f && y < 0.58f)
                    {
                        pos.z = headCenter.z + 0.44f * rz;
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

        private static void BuildChainCaps(MeshBuilderContext ctx, int submesh, Vector3 center, Vector3 size, int boneA)
        {
            Vector3 half = size * 0.5f;
            int segs = 10;
            int rings = 3;
            int baseV = ctx.vertices.Count;
            var tris = ctx.submeshTris[submesh];

            for (int r = 0; r < rings; r++)
            {
                float ty = (r / (float)(rings - 1)) * 2f - 1f;
                float pinch = r == 1 ? 1.0f : 0.74f;
                for (int i = 0; i <= segs; i++)
                {
                    float ang = (i / (float)segs) * Mathf.PI * 2f;
                    Vector3 pos = center + new Vector3(Mathf.Cos(ang) * half.x * pinch, ty * half.y, Mathf.Sin(ang) * half.z * pinch);
                    ctx.vertices.Add(pos);
                    ctx.uvs.Add(new Vector2(i / (float)segs, r / (float)(rings - 1)));
                    ctx.weights.Add(MakeWeight(boneA, 1f, boneA, 0f));
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

            int botCenter = ctx.vertices.Count;
            ctx.vertices.Add(center + new Vector3(0f, -half.y, 0f));
            ctx.uvs.Add(new Vector2(0.5f, 0f));
            ctx.weights.Add(MakeWeight(boneA, 1f, boneA, 0f));

            int topCenter = ctx.vertices.Count;
            ctx.vertices.Add(center + new Vector3(0f, half.y, 0f));
            ctx.uvs.Add(new Vector2(0.5f, 1f));
            ctx.weights.Add(MakeWeight(boneA, 1f, boneA, 0f));

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
