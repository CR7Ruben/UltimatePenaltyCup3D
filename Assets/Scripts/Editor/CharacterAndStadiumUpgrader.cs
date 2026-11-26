using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UltimatePenaltyCup.Player;
using UltimatePenaltyCup.Goalkeeper;
using UltimatePenaltyCup.Gameplay;
using UltimatePenaltyCup.Audio;

namespace UltimatePenaltyCup.Editor
{
    public static class CharacterAndStadiumUpgrader
    {
        [MenuItem("Ultimate Penalty Cup/Visual Upgrade/01 - Rebuild Player (Rubén González)", false, 100)]
        public static void RebuildPlayerPrefab()
        {
            const string path = "Assets/Prefabs/Player_RubenGonzalez.prefab";
            Mesh mesh = Load<Mesh>("Assets/Art/Models/Mesh_RubenGonzalez.asset");
            if (mesh == null)
            {
                HumanoidMeshBuilder.BuildAllCharacterMeshes();
                mesh = Load<Mesh>("Assets/Art/Models/Mesh_RubenGonzalez.asset");
            }

            GameObject root = new GameObject("Player_RubenGonzalez");
            var boneMap = BuildSkeletonHierarchy(root.transform, 1.0f, 1.0f, out Transform[] bones, out Transform skeletonRoot);

            GameObject meshObj = new GameObject("Ruben_SkinnedMesh");
            meshObj.transform.SetParent(root.transform, false);
            SkinnedMeshRenderer smr = meshObj.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = mesh;
            smr.bones = bones;
            smr.rootBone = boneMap["Hips"];
            smr.localBounds = new Bounds(Vector3.zero, new Vector3(1.5f, 2.2f, 1.5f));
            smr.updateWhenOffscreen = true;
            smr.sharedMaterials = new Material[]
            {
                Load<Material>("Assets/Materials/Mat_Player_Kit.mat"),
                Load<Material>("Assets/Materials/Mat_Player_Shorts.mat"),
                Load<Material>("Assets/Materials/Mat_Skin_Body.mat"),
                Load<Material>("Assets/Materials/Mat_Face_Ruben.mat"),
                Load<Material>("Assets/Materials/Mat_Hair_Dark.mat"),
                Load<Material>("Assets/Materials/Mat_Player_Socks.mat"),
                Load<Material>("Assets/Materials/Mat_Boots_Player.mat")
            };

            PlayerController pc = root.AddComponent<PlayerController>();
            SetField(pc, "characterVisuals", skeletonRoot);
            SetField(pc, "skinnedMesh", smr);
            SetField(pc, "hips", boneMap["Hips"]);
            SetField(pc, "spine", boneMap["Spine"]);
            SetField(pc, "upperSpine", boneMap["UpperSpine"]);
            SetField(pc, "chest", boneMap["Chest"]);
            SetField(pc, "neck", boneMap["Neck"]);
            SetField(pc, "torso", boneMap["Chest"]);
            SetField(pc, "head", boneMap["Head"]);
            SetField(pc, "leftShoulder", boneMap["LeftShoulder"]);
            SetField(pc, "leftArm", boneMap["LeftUpperArm"]);
            SetField(pc, "leftForearm", boneMap["LeftForearm"]);
            SetField(pc, "leftHand", boneMap["LeftHand"]);
            SetField(pc, "rightShoulder", boneMap["RightShoulder"]);
            SetField(pc, "rightArm", boneMap["RightUpperArm"]);
            SetField(pc, "rightForearm", boneMap["RightForearm"]);
            SetField(pc, "rightHand", boneMap["RightHand"]);
            SetField(pc, "leftLeg", boneMap["LeftThigh"]);
            SetField(pc, "leftShin", boneMap["LeftShin"]);
            SetField(pc, "leftFoot", boneMap["LeftFoot"]);
            SetField(pc, "leftToe", boneMap["LeftToe"]);
            SetField(pc, "rightLeg", boneMap["RightThigh"]);
            SetField(pc, "rightShin", boneMap["RightShin"]);
            SetField(pc, "rightFoot", boneMap["RightFoot"]);
            SetField(pc, "rightToe", boneMap["RightToe"]);

            // Circular Reticle (transparent UI ring sprite)
            PlayerShooter ps = root.AddComponent<PlayerShooter>();
            GameObject reticle = GameObject.CreatePrimitive(PrimitiveType.Quad);
            reticle.name = "AimReticle_Indicator";
            reticle.transform.SetParent(root.transform, false);
            reticle.transform.localPosition = new Vector3(0.68f, 1.15f, 13.06f);
            reticle.transform.localScale = Vector3.one * 0.62f;
            Object.DestroyImmediate(reticle.GetComponent<Collider>());

            Material reticleMat = Load<Material>("Assets/Materials/Mat_AimReticle.mat");
            if (reticleMat != null)
            {
                reticle.GetComponent<Renderer>().sharedMaterial = reticleMat;
            }

            // 20-point Simulated Physics Trajectory Arc
            GameObject lineObj = new GameObject("TrajectoryGuideline");
            lineObj.transform.SetParent(root.transform, false);
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.positionCount = 20;
            lr.startWidth = 0.048f;
            lr.endWidth = 0.018f;
            lr.useWorldSpace = true;
            Material lineMat = Load<Material>("Assets/Materials/Mat_TrajectoryLine.mat") ?? Load<Material>("Assets/Materials/Mat_Lines_White.mat");
            if (lineMat != null) lr.sharedMaterial = lineMat;
            lr.startColor = new Color(0.15f, 0.78f, 1.0f, 0.85f);
            lr.endColor = new Color(1.0f, 0.85f, 0.2f, 0.28f);

            // Crisp 3D Directional Arrow anchored at ball pointing straight toward launch vector
            GameObject arrowRoot = new GameObject("DirectionArrowRoot");
            arrowRoot.transform.SetParent(root.transform, false);
            arrowRoot.transform.localPosition = new Vector3(0.68f, 0.135f, 2.10f);
            Material arrowMat = Load<Material>("Assets/Materials/Mat_Lines_White.mat") ?? lineMat;
            GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shaft.name = "Arrow_Shaft";
            shaft.transform.SetParent(arrowRoot.transform, false);
            shaft.transform.localPosition = new Vector3(0f, 0f, 1.15f);
            shaft.transform.localScale = new Vector3(0.06f, 0.012f, 1.95f);
            shaft.GetComponent<Renderer>().sharedMaterial = arrowMat;
            shaft.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            Object.DestroyImmediate(shaft.GetComponent<Collider>());

            GameObject tipL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tipL.name = "Arrow_Head_L";
            tipL.transform.SetParent(arrowRoot.transform, false);
            tipL.transform.localPosition = new Vector3(-0.085f, 0f, 2.04f);
            tipL.transform.localRotation = Quaternion.Euler(0f, 38f, 0f);
            tipL.transform.localScale = new Vector3(0.055f, 0.014f, 0.30f);
            tipL.GetComponent<Renderer>().sharedMaterial = arrowMat;
            tipL.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            Object.DestroyImmediate(tipL.GetComponent<Collider>());

            GameObject tipR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tipR.name = "Arrow_Head_R";
            tipR.transform.SetParent(arrowRoot.transform, false);
            tipR.transform.localPosition = new Vector3(0.085f, 0f, 2.04f);
            tipR.transform.localRotation = Quaternion.Euler(0f, -38f, 0f);
            tipR.transform.localScale = new Vector3(0.055f, 0.014f, 0.30f);
            tipR.GetComponent<Renderer>().sharedMaterial = arrowMat;
            tipR.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            Object.DestroyImmediate(tipR.GetComponent<Collider>());

            ps.SetAimReticle(reticle.transform, lr, arrowRoot.transform);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log("[Upgrader] Player_RubenGonzalez.prefab upgraded with unified 17-bone SkinnedMesh.");
        }

        [MenuItem("Ultimate Penalty Cup/Visual Upgrade/02 - Rebuild Goalkeeper (Malágon)", false, 101)]
        public static void RebuildGoalkeeperPrefab()
        {
            const string path = "Assets/Prefabs/Goalkeeper_Malagon.prefab";
            Mesh mesh = Load<Mesh>("Assets/Art/Models/Mesh_GoalkeeperMalagon.asset");
            if (mesh == null)
            {
                HumanoidMeshBuilder.BuildAllCharacterMeshes();
                mesh = Load<Mesh>("Assets/Art/Models/Mesh_GoalkeeperMalagon.asset");
            }

            GameObject root = new GameObject("Goalkeeper_Malagon");
            root.tag = "Goalkeeper";

            // Kinematic Rigidbody + Torso CapsuleCollider so OnCollisionEnter ALWAYS triggers cleanly!
            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            CapsuleCollider bodyCol = root.AddComponent<CapsuleCollider>();
            bodyCol.center = new Vector3(0f, 0.82f, 0f);
            bodyCol.radius = 0.22f;
            bodyCol.height = 1.25f;

            var boneMap = BuildSkeletonHierarchy(root.transform, 1.055f, 1.12f, out Transform[] bones, out Transform skeletonRoot);

            // Attach Glove Colliders directly on Hand bones for real physical hand deflection
            AddGloveCollider(boneMap["LeftHand"].gameObject, "LeftGloveCollider");
            AddGloveCollider(boneMap["RightHand"].gameObject, "RightGloveCollider");

            GameObject meshObj = new GameObject("Malagon_SkinnedMesh");
            meshObj.transform.SetParent(root.transform, false);
            SkinnedMeshRenderer smr = meshObj.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = mesh;
            smr.bones = bones;
            smr.rootBone = boneMap["Hips"];
            smr.localBounds = new Bounds(Vector3.zero, new Vector3(1.6f, 2.3f, 1.6f));
            smr.updateWhenOffscreen = true;
            smr.sharedMaterials = new Material[]
            {
                Load<Material>("Assets/Materials/Mat_Goalkeeper_Kit.mat"),
                Load<Material>("Assets/Materials/Mat_GK_Shorts.mat"),
                Load<Material>("Assets/Materials/Mat_Skin_Body.mat"),
                Load<Material>("Assets/Materials/Mat_Face_Malagon.mat"),
                Load<Material>("Assets/Materials/Mat_Hair_Dark.mat"),
                Load<Material>("Assets/Materials/Mat_GK_Socks.mat"),
                Load<Material>("Assets/Materials/Mat_Boots_GK.mat"),
                Load<Material>("Assets/Materials/Mat_GK_Gloves.mat")
            };

            GoalkeeperAI gkAI = root.AddComponent<GoalkeeperAI>();
            SetField(gkAI, "bodyRoot", skeletonRoot);
            SetField(gkAI, "skinnedMesh", smr);
            SetField(gkAI, "hips", boneMap["Hips"]);
            SetField(gkAI, "spine", boneMap["Spine"]);
            SetField(gkAI, "upperSpine", boneMap["UpperSpine"]);
            SetField(gkAI, "chest", boneMap["Chest"]);
            SetField(gkAI, "neck", boneMap["Neck"]);
            SetField(gkAI, "torso", boneMap["Chest"]);
            SetField(gkAI, "head", boneMap["Head"]);
            SetField(gkAI, "leftShoulder", boneMap["LeftShoulder"]);
            SetField(gkAI, "leftArm", boneMap["LeftUpperArm"]);
            SetField(gkAI, "leftForearm", boneMap["LeftForearm"]);
            SetField(gkAI, "leftHand", boneMap["LeftHand"]);
            SetField(gkAI, "rightShoulder", boneMap["RightShoulder"]);
            SetField(gkAI, "rightArm", boneMap["RightUpperArm"]);
            SetField(gkAI, "rightForearm", boneMap["RightForearm"]);
            SetField(gkAI, "rightHand", boneMap["RightHand"]);
            SetField(gkAI, "leftLeg", boneMap["LeftThigh"]);
            SetField(gkAI, "leftShin", boneMap["LeftShin"]);
            SetField(gkAI, "leftFoot", boneMap["LeftFoot"]);
            SetField(gkAI, "leftToe", boneMap["LeftToe"]);
            SetField(gkAI, "rightLeg", boneMap["RightThigh"]);
            SetField(gkAI, "rightShin", boneMap["RightShin"]);
            SetField(gkAI, "rightFoot", boneMap["RightFoot"]);
            SetField(gkAI, "rightToe", boneMap["RightToe"]);
            SetField(gkAI, "leftGloveAnchor", boneMap["LeftHand"]);
            SetField(gkAI, "rightGloveAnchor", boneMap["RightHand"]);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log("[Upgrader] Goalkeeper_Malagon.prefab upgraded with unified SkinnedMesh + Colliders.");
        }

        private static void AddGloveCollider(GameObject boneObj, string colName)
        {
            GameObject holder = new GameObject(colName);
            holder.tag = "Goalkeeper";
            holder.transform.SetParent(boneObj.transform, false);
            holder.transform.localPosition = new Vector3(0f, -0.07f, 0f);
            BoxCollider bc = holder.AddComponent<BoxCollider>();
            bc.size = new Vector3(0.24f, 0.28f, 0.24f);
        }

        private static Dictionary<string, Transform> BuildSkeletonHierarchy(Transform root, float hScale, float bScale, out Transform[] boneArray, out Transform skeletonRoot)
        {
            var defs = HumanoidMeshBuilder.GetSkeletonDefinition(hScale, bScale);
            boneArray = new Transform[defs.Length];
            var map = new Dictionary<string, Transform>();

            GameObject skel = new GameObject("Skeleton");
            skel.transform.SetParent(root, false);
            skeletonRoot = skel.transform;

            for (int i = 0; i < defs.Length; i++)
            {
                GameObject boneGo = new GameObject(defs[i].name);
                Transform t = boneGo.transform;
                Transform parentT = defs[i].parent >= 0 ? boneArray[defs[i].parent] : skel.transform;
                t.SetParent(parentT, false);

                Vector3 parentWorld = defs[i].parent >= 0 ? defs[defs[i].parent].worldPos : Vector3.zero;
                t.localPosition = defs[i].worldPos - parentWorld;
                t.localRotation = Quaternion.identity;

                boneArray[i] = t;
                map[defs[i].name] = t;
            }
            return map;
        }

        [MenuItem("Ultimate Penalty Cup/Visual Upgrade/03 - Rebuild Regulation Goal (Net Ripple)", false, 102)]
        public static void RebuildGoalPrefab()
        {
            const string path = "Assets/Prefabs/GoalPrefab.prefab";
            Material matPost = Load<Material>("Assets/Materials/Mat_Goal_Posts.mat");
            Material matNet = Load<Material>("Assets/Materials/Mat_Goal_Net.mat");

            GameObject goalRoot = new GameObject("Goal_Regulation");
            goalRoot.AddComponent<GoalNetRipple>();

            const float halfW = 3.66f;
            const float height = 2.44f;
            const float depth = 2.0f;
            const float r = 0.06f;

            // White cylindrical posts & crossbar (tagged "Post")
            CreateCylinderPart("Post_Left", goalRoot.transform, new Vector3(-halfW, height * 0.5f, 0f), Quaternion.identity, new Vector3(r * 2f, height * 0.5f, r * 2f), matPost, true, "Post");
            CreateCylinderPart("Post_Right", goalRoot.transform, new Vector3(halfW, height * 0.5f, 0f), Quaternion.identity, new Vector3(r * 2f, height * 0.5f, r * 2f), matPost, true, "Post");
            CreateCylinderPart("Crossbar", goalRoot.transform, new Vector3(0f, height, 0f), Quaternion.Euler(0f, 0f, 90f), new Vector3(r * 2f, halfW, r * 2f), matPost, true, "Post");

            // Rear stanchions & ground base frame
            CreateCylinderPart("BackBar_Top", goalRoot.transform, new Vector3(0f, height, depth), Quaternion.Euler(0f, 0f, 90f), new Vector3(r * 1.2f, halfW, r * 1.2f), matPost, false);
            CreateCylinderPart("BackBar_Bottom", goalRoot.transform, new Vector3(0f, 0.04f, depth), Quaternion.Euler(0f, 0f, 90f), new Vector3(r * 1.2f, halfW, r * 1.2f), matPost, false);
            CreateCylinderPart("Stanchion_BackL", goalRoot.transform, new Vector3(-halfW, height * 0.5f, depth), Quaternion.identity, new Vector3(r * 1.2f, height * 0.5f, r * 1.2f), matPost, false);
            CreateCylinderPart("Stanchion_BackR", goalRoot.transform, new Vector3(halfW, height * 0.5f, depth), Quaternion.identity, new Vector3(r * 1.2f, height * 0.5f, r * 1.2f), matPost, false);
            CreateCylinderPart("SideBar_TopL", goalRoot.transform, new Vector3(-halfW, height, depth * 0.5f), Quaternion.Euler(90f, 0f, 0f), new Vector3(r * 1.2f, depth * 0.5f, r * 1.2f), matPost, false);
            CreateCylinderPart("SideBar_TopR", goalRoot.transform, new Vector3(halfW, height, depth * 0.5f), Quaternion.Euler(90f, 0f, 0f), new Vector3(r * 1.2f, depth * 0.5f, r * 1.2f), matPost, false);
            CreateCylinderPart("SideBar_BotL", goalRoot.transform, new Vector3(-halfW, 0.04f, depth * 0.5f), Quaternion.Euler(90f, 0f, 0f), new Vector3(r * 1.2f, depth * 0.5f, r * 1.2f), matPost, false);
            CreateCylinderPart("SideBar_BotR", goalRoot.transform, new Vector3(halfW, 0.04f, depth * 0.5f), Quaternion.Euler(90f, 0f, 0f), new Vector3(r * 1.2f, depth * 0.5f, r * 1.2f), matPost, false);

            // Subdivided Net Panels (deformable by GoalNetRipple + physical BoxColliders to keep ball in net)
            CreateSubdividedNet("Net_Back", goalRoot.transform, new Vector3(0f, height * 0.5f, depth), Quaternion.identity, halfW * 2f, height, 14, 7, matNet, true);
            CreateSubdividedNet("Net_Top", goalRoot.transform, new Vector3(0f, height, depth * 0.5f), Quaternion.Euler(90f, 0f, 0f), halfW * 2f, depth, 14, 5, matNet, true);
            CreateSubdividedNet("Net_Left", goalRoot.transform, new Vector3(-halfW, height * 0.5f, depth * 0.5f), Quaternion.Euler(0f, 90f, 0f), depth, height, 6, 7, matNet, true);
            CreateSubdividedNet("Net_Right", goalRoot.transform, new Vector3(halfW, height * 0.5f, depth * 0.5f), Quaternion.Euler(0f, -90f, 0f), depth, height, 6, 7, matNet, true);

            // Strictly internal Goal Trigger Detector
            GameObject detector = new GameObject("GoalTriggerDetector");
            detector.transform.SetParent(goalRoot.transform, false);
            detector.transform.localPosition = new Vector3(0f, height * 0.5f, depth * 0.52f);
            BoxCollider detCol = detector.AddComponent<BoxCollider>();
            detCol.isTrigger = true;
            detCol.size = new Vector3(halfW * 2f - 0.16f, height - 0.12f, depth * 0.85f);
            GoalDetector gd = detector.AddComponent<GoalDetector>();
            SetField(gd, "goalHalfWidth", halfW);
            SetField(gd, "goalHeight", height);
            SetField(gd, "cornerThreshold", 0.90f);

            PrefabUtility.SaveAsPrefabAsset(goalRoot, path);
            Object.DestroyImmediate(goalRoot);
            Debug.Log("[Upgrader] GoalPrefab.prefab upgraded with subdivided rippling net.");
        }

        [MenuItem("Ultimate Penalty Cup/Visual Upgrade/04 - Rebuild Stadium Prefab (4 Distinct Venues)", false, 103)]
        public static void RebuildStadiumPrefab()
        {
            const string path = "Assets/Prefabs/StadiumEnvironment.prefab";

            Material matGrassCantera = Load<Material>("Assets/Materials/Mat_Grass_Cantera.mat");
            Material matLines = Load<Material>("Assets/Materials/Mat_Lines_White.mat");
            Material matStands = Load<Material>("Assets/Materials/Mat_Stadium_Stands.mat");
            Material matConcrete = Load<Material>("Assets/Materials/Mat_Stadium_Concrete.mat") ?? matStands;
            Material matBanner = Load<Material>("Assets/Materials/Mat_Stadium_Banners.mat");
            Material matMetal = Load<Material>("Assets/Materials/Mat_Stadium_Metal.mat") ?? Load<Material>("Assets/Materials/Mat_Goal_Posts.mat");
            Material matSeats = Load<Material>("Assets/Materials/Mat_Stadium_Seats.mat") ?? Load<Material>("Assets/Materials/Mat_Player_Shorts.mat");
            Material matFoliage = Load<Material>("Assets/Materials/Mat_Foliage.mat") ?? matGrassCantera;
            Material matFence = Load<Material>("Assets/Materials/Mat_Pitch_Fence.mat") ?? matMetal;
            Material matGold = Load<Material>("Assets/Materials/Mat_GoldTrophy.mat");

            GameObject root = new GameObject("StadiumEnvironment");
            StadiumController sc = root.AddComponent<StadiumController>();
            root.AddComponent<CrowdManager>();
            root.AddComponent<StadiumAmbienceController>();

            // ========== REGULATION PITCH (75m x 105m, top surface cleanly at y = 0.0) ==========
            GameObject pitch = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pitch.name = "Pitch_Grass";
            pitch.transform.SetParent(root.transform);
            pitch.transform.localPosition = new Vector3(0f, -0.05f, -5f);
            pitch.transform.localScale = new Vector3(75f, 0.10f, 105f);
            pitch.GetComponent<Renderer>().sharedMaterial = matGrassCantera;

            // Pitch Surround Apron (track/runoff concrete border)
            GameObject apron = CreateBox("Runoff_Apron", root.transform, new Vector3(0f, -0.08f, -5f), new Vector3(110f, 0.08f, 140f), matConcrete, false);
            apron.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

            // ========== REGULATION WHITE LINES ==========
            GameObject linesContainer = new GameObject("Pitch_Lines");
            linesContainer.transform.SetParent(root.transform, false);

            const float lineY = 0.004f;
            const float lw = 0.12f;
            // Penalty Box: 40.32m wide x 16.5m deep
            CreateBox("Line_PenaltyBox_Back", linesContainer.transform, new Vector3(0f, lineY, -16.5f), new Vector3(40.32f, 0.008f, lw), matLines, false);
            CreateBox("Line_PenaltyBox_L", linesContainer.transform, new Vector3(-20.16f, lineY, -8.25f), new Vector3(lw, 0.008f, 16.5f), matLines, false);
            CreateBox("Line_PenaltyBox_R", linesContainer.transform, new Vector3(20.16f, lineY, -8.25f), new Vector3(lw, 0.008f, 16.5f), matLines, false);

            // Goal Box: 18.32m wide x 5.5m deep
            CreateBox("Line_GoalBox_Back", linesContainer.transform, new Vector3(0f, lineY, -5.5f), new Vector3(18.32f, 0.008f, lw), matLines, false);
            CreateBox("Line_GoalBox_L", linesContainer.transform, new Vector3(-9.16f, lineY, -2.75f), new Vector3(lw, 0.008f, 5.5f), matLines, false);
            CreateBox("Line_GoalBox_R", linesContainer.transform, new Vector3(9.16f, lineY, -2.75f), new Vector3(lw, 0.008f, 5.5f), matLines, false);

            // Goal Line & Touchlines
            CreateBox("Line_GoalLine", linesContainer.transform, new Vector3(0f, lineY, 0f), new Vector3(68f, 0.008f, lw), matLines, false);
            CreateBox("Line_TouchL", linesContainer.transform, new Vector3(-34f, lineY, -25f), new Vector3(lw, 0.008f, 50f), matLines, false);
            CreateBox("Line_TouchR", linesContainer.transform, new Vector3(34f, lineY, -25f), new Vector3(lw, 0.008f, 50f), matLines, false);

            // Smooth Penalty Arc (18 connected chord segments around z=-11, r=9.15m outside penalty box z < -16.5)
            float arcRadius = 9.15f;
            for (int i = -9; i < 9; i++)
            {
                float a0 = i * 6.5f * Mathf.Deg2Rad;
                float a1 = (i + 1) * 6.5f * Mathf.Deg2Rad;
                Vector3 p0 = new Vector3(Mathf.Sin(a0) * arcRadius, lineY, -11f - Mathf.Cos(a0) * arcRadius);
                Vector3 p1 = new Vector3(Mathf.Sin(a1) * arcRadius, lineY, -11f - Mathf.Cos(a1) * arcRadius);
                if (p0.z > -16.45f && p1.z > -16.45f) continue;

                Vector3 mid = (p0 + p1) * 0.5f;
                float segLen = Vector3.Distance(p0, p1) + 0.03f;
                GameObject seg = CreateBox($"PenArc_{i + 9}", linesContainer.transform, mid, new Vector3(segLen, 0.008f, lw), matLines, false);
                seg.transform.rotation = Quaternion.LookRotation(Vector3.Cross(Vector3.up, (p1 - p0).normalized));
            }

            // Crisp regulation Penalty Spot at z = -11m (18cm diameter covered naturally by 22cm ball)
            GameObject spot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spot.name = "PenaltySpot";
            spot.transform.SetParent(root.transform);
            spot.transform.localPosition = new Vector3(0f, 0.004f, -11f);
            spot.transform.localScale = new Vector3(0.18f, 0.004f, 0.18f);
            Renderer spotRend = spot.GetComponent<Renderer>();
            spotRend.sharedMaterial = matLines;
            spotRend.shadowCastingMode = ShadowCastingMode.Off;
            Object.DestroyImmediate(spot.GetComponent<Collider>());

            // Combine all pitch lines into a single draw call (shadow casting off)
            CombineStaticMeshesByMaterial(linesContainer, "PitchLines", castShadows: false);

            // Corner Flags
            CreateCornerFlag("CornerFlag_L", root.transform, new Vector3(-34f, 0f, 0f), matLines, matGold);
            CreateCornerFlag("CornerFlag_R", root.transform, new Vector3(34f, 0f, 0f), matLines, matGold);

            // ========== 4 ARCHITECTURALLY DISTINCT VENUES ==========
            GameObject vCantera = BuildCanteraVenue(root.transform, matConcrete, matFence, matFoliage, matSeats, matMetal);
            GameObject vNight = BuildNightVenue(root.transform, matConcrete, matStands, matBanner, matMetal, matSeats, out Light[] nightLights);
            GameObject vPro = BuildProfessionalVenue(root.transform, matConcrete, matStands, matBanner, matMetal, matSeats, out Light[] proLights);
            GameObject vWorldCup = BuildWorldCupVenue(root.transform, matConcrete, matStands, matBanner, matMetal, matSeats, matGold, out Light[] wcLights, out ParticleSystem confetti);

            // Static mesh batching by material per venue (reduces ~160 draw calls to ~6-8 per active venue)
            CombineStaticMeshesByMaterial(vCantera, "Venue_Cantera", castShadows: true);
            CombineStaticMeshesByMaterial(vNight, "Venue_Night", castShadows: true);
            CombineStaticMeshesByMaterial(vPro, "Venue_Professional", castShadows: true);
            CombineStaticMeshesByMaterial(vWorldCup, "Venue_WorldCup", castShadows: true);

            // Merge all floodlights for StadiumController
            var allLights = new List<Light>();
            allLights.AddRange(nightLights);
            allLights.AddRange(proLights);
            allLights.AddRange(wcLights);

            // Wire StadiumController
            SetField(sc, "pitchRenderer", pitch.GetComponent<Renderer>());
            SetField(sc, "bannerMaterial", matBanner);
            SetField(sc, "venueCantera", vCantera);
            SetField(sc, "venueNight", vNight);
            SetField(sc, "venueProfessional", vPro);
            SetField(sc, "venueWorldCup", vWorldCup);
            SetField(sc, "matGrassCantera", matGrassCantera);
            SetField(sc, "matGrassNight", Load<Material>("Assets/Materials/Mat_Grass_Night.mat"));
            SetField(sc, "matGrassPro", Load<Material>("Assets/Materials/Mat_Grass_Pro.mat"));
            SetField(sc, "matGrassWorldCup", Load<Material>("Assets/Materials/Mat_Grass_WorldCup.mat"));
            SetField(sc, "floodlights", allLights.ToArray());
            SetField(sc, "confettiSystem", confetti);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log("[Upgrader] StadiumEnvironment.prefab rebuilt with 4 architecturally distinct venues.");
        }

        [MenuItem("Ultimate Penalty Cup/Visual Upgrade/05 - Rebuild Ball Prefab (FIFA 0.22m + VFX)", false, 104)]
        public static void RebuildBallPrefab()
        {
            const string path = "Assets/Prefabs/BallPrefab.prefab";

            Material matClassic = Load<Material>("Assets/Materials/Mat_Ball_Classic.mat");
            Material matBlue = Load<Material>("Assets/Materials/Mat_Ball_Blue.mat");
            Material matFire = Load<Material>("Assets/Materials/Mat_Ball_Fire.mat");
            Material matRainbow = Load<Material>("Assets/Materials/Mat_Ball_Rainbow.mat");
            Material matGold = Load<Material>("Assets/Materials/Mat_Ball_Gold.mat");

            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "Ball_3D";
            ball.transform.localScale = Vector3.one * 0.22f; // Official FIFA Size 5 diameter = 22cm
            ball.GetComponent<Renderer>().sharedMaterial = matClassic;

            TrailRenderer trail = ball.AddComponent<TrailRenderer>();
            trail.time = 0.28f;
            trail.startWidth = 0.16f;
            trail.endWidth = 0.01f;
            trail.emitting = false;
            trail.sharedMaterial = Load<Material>("Assets/Materials/Mat_Lines_White.mat") ?? matClassic;

            GameObject vfxObj = new GameObject("BallVFX_Sparks");
            vfxObj.transform.SetParent(ball.transform, false);
            ParticleSystem vfx = vfxObj.AddComponent<ParticleSystem>();
            var vm = vfx.main;
            vm.startLifetime = 0.35f;
            vm.startSpeed = 1.6f;
            vm.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.09f);
            vm.maxParticles = 45;
            vm.startColor = new Color(1f, 0.9f, 0.3f, 0.8f);
            vm.loop = true;
            var vem = vfx.emission;
            vem.rateOverTime = 28f;
            vfx.Stop();

            BallController bc = ball.AddComponent<BallController>();
            bc.SetCustomMaterials(matClassic, matBlue, matFire, matRainbow, matGold);
            SetField(bc, "ballVfxParticles", vfx);

            PrefabUtility.SaveAsPrefabAsset(ball, path);
            Object.DestroyImmediate(ball);
            Debug.Log("[Upgrader] BallPrefab.prefab rebuilt at 0.22m FIFA scale.");
        }

        [MenuItem("Ultimate Penalty Cup/Visual Upgrade/06 - Run ALL Visual & Prefab Upgrades", false, 110)]
        public static void RunAllUpgrades()
        {
            HumanoidMeshBuilder.BuildAllCharacterMeshes();
            RebuildPlayerPrefab();
            RebuildGoalkeeperPrefab();
            RebuildGoalPrefab();
            RebuildBallPrefab();
            RebuildStadiumPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Upgrader] ALL VISUAL UPGRADES COMPLETE.");
        }

        // ==================== VENUE BUILDERS ====================
        private static GameObject BuildCanteraVenue(Transform parent, Material matConcrete, Material matFence, Material matFoliage, Material matSeats, Material matMetal)
        {
            GameObject root = new GameObject("Venue_Cantera");
            root.transform.SetParent(parent, false);

            // Low perimeter training fence & rails
            CreateBox("Fence_Back", root.transform, new Vector3(0f, 0.7f, 7.5f), new Vector3(68f, 1.4f, 0.14f), matFence, false);
            CreateBox("Fence_L", root.transform, new Vector3(-34f, 0.7f, -15f), new Vector3(0.14f, 1.4f, 45f), matFence, false);
            CreateBox("Fence_R", root.transform, new Vector3(34f, 0.7f, -15f), new Vector3(0.14f, 1.4f, 45f), matFence, false);

            // Small cozy 2-tier local bleachers behind goal & sides
            BuildTerracedStand("Cantera_Stand_Back", root.transform, new Vector3(0f, 0f, 12.5f), Quaternion.identity, 36f, 2, 1.0f, 2.2f, matConcrete, matSeats, matMetal, withRoof: false);
            BuildTerracedStand("Cantera_Stand_L", root.transform, new Vector3(-37f, 0f, -8f), Quaternion.Euler(0f, -90f, 0f), 32f, 2, 1.0f, 2.2f, matConcrete, matSeats, matMetal, withRoof: false);

            // Stylized training ground trees around perimeter
            for (int i = -4; i <= 4; i++)
            {
                if (Mathf.Abs(i) <= 1) continue;
                CreateStylizedTree($"Tree_Back_{i}", root.transform, new Vector3(i * 8.5f, 0f, 20f), matConcrete, matFoliage);
            }
            for (int i = 0; i < 4; i++)
            {
                CreateStylizedTree($"Tree_R_{i}", root.transform, new Vector3(39f, 0f, 4f - i * 9f), matConcrete, matFoliage);
            }

            // Team bench
            BuildDugout("Cantera_Bench", root.transform, new Vector3(-28f, 0f, -4f), Quaternion.Euler(0f, 90f, 0f), matMetal, matSeats);

            return root;
        }

        private static GameObject BuildNightVenue(Transform parent, Material matConcrete, Material matStands, Material matBanner, Material matMetal, Material matSeats, out Light[] lights)
        {
            GameObject root = new GameObject("Venue_Night");
            root.transform.SetParent(parent, false);

            BuildLEDBoardsRing(root.transform, matBanner);

            // 4-tier concrete terraced stands with crowd & seating depth
            BuildTerracedStand("Night_Stand_Back", root.transform, new Vector3(0f, 0f, 14f), Quaternion.identity, 64f, 4, 1.1f, 2.4f, matConcrete, matStands, matMetal, withRoof: false);
            BuildTerracedStand("Night_Stand_L", root.transform, new Vector3(-38f, 0f, -12f), Quaternion.Euler(0f, -90f, 0f), 64f, 4, 1.1f, 2.4f, matConcrete, matStands, matMetal, withRoof: false);
            BuildTerracedStand("Night_Stand_R", root.transform, new Vector3(38f, 0f, -12f), Quaternion.Euler(0f, 90f, 0f), 64f, 4, 1.1f, 2.4f, matConcrete, matStands, matMetal, withRoof: false);

            // 4 tall floodlight truss towers
            lights = new Light[4];
            lights[0] = CreateFloodlightTower("Night_Tower_FL", root.transform, new Vector3(-36f, 0f, 16f), new Vector3(45f, 138f, 0f), matMetal);
            lights[1] = CreateFloodlightTower("Night_Tower_FR", root.transform, new Vector3(36f, 0f, 16f), new Vector3(45f, -138f, 0f), matMetal);
            lights[2] = CreateFloodlightTower("Night_Tower_BL", root.transform, new Vector3(-36f, 0f, -34f), new Vector3(45f, 42f, 0f), matMetal);
            lights[3] = CreateFloodlightTower("Night_Tower_BR", root.transform, new Vector3(36f, 0f, -34f), new Vector3(45f, -42f, 0f), matMetal);

            return root;
        }

        private static GameObject BuildProfessionalVenue(Transform parent, Material matConcrete, Material matStands, Material matBanner, Material matMetal, Material matSeats, out Light[] lights)
        {
            GameObject root = new GameObject("Venue_Professional");
            root.transform.SetParent(parent, false);

            BuildLEDBoardsRing(root.transform, matBanner);

            // Large 5-tier covered stands
            BuildTerracedStand("Pro_Stand_Back", root.transform, new Vector3(0f, 0f, 14f), Quaternion.identity, 72f, 5, 1.15f, 2.4f, matConcrete, matStands, matMetal, withRoof: true);
            BuildTerracedStand("Pro_Stand_L", root.transform, new Vector3(-38f, 0f, -14f), Quaternion.Euler(0f, -90f, 0f), 74f, 5, 1.15f, 2.4f, matConcrete, matStands, matMetal, withRoof: true);
            BuildTerracedStand("Pro_Stand_R", root.transform, new Vector3(38f, 0f, -14f), Quaternion.Euler(0f, 90f, 0f), 74f, 5, 1.15f, 2.4f, matConcrete, matStands, matMetal, withRoof: true);

            // Player Entrance Tunnel & Covered Dugouts
            CreateBox("Player_Tunnel_Arch", root.transform, new Vector3(-18f, 1.8f, 11.5f), new Vector3(4.5f, 3.6f, 3.2f), matConcrete, false);
            BuildDugout("Pro_Dugout_L", root.transform, new Vector3(-33f, 0f, -4f), Quaternion.Euler(0f, 90f, 0f), matMetal, matSeats);
            BuildDugout("Pro_Dugout_R", root.transform, new Vector3(-33f, 0f, -14f), Quaternion.Euler(0f, 90f, 0f), matMetal, matSeats);

            // Electronic Scoreboard behind goal
            CreateBox("Scoreboard_Pillar", root.transform, new Vector3(0f, 7.5f, 27f), new Vector3(2.2f, 6f, 1.2f), matMetal, false);
            CreateBox("Scoreboard_Screen", root.transform, new Vector3(0f, 11.5f, 26.5f), new Vector3(14f, 4.8f, 0.8f), matBanner, false);

            lights = new Light[2];
            lights[0] = CreateFloodlightTower("Pro_Tower_L", root.transform, new Vector3(-37f, 0f, 18f), new Vector3(48f, 140f, 0f), matMetal);
            lights[1] = CreateFloodlightTower("Pro_Tower_R", root.transform, new Vector3(37f, 0f, 18f), new Vector3(48f, -140f, 0f), matMetal);

            return root;
        }

        private static GameObject BuildWorldCupVenue(Transform parent, Material matConcrete, Material matStands, Material matBanner, Material matMetal, Material matSeats, Material matGold, out Light[] lights, out ParticleSystem confetti)
        {
            GameObject root = new GameObject("Venue_WorldCup");
            root.transform.SetParent(parent, false);

            BuildLEDBoardsRing(root.transform, matBanner);

            // Towering 6-tier Championship Mega-Stands with Roof Canopies
            BuildTerracedStand("WC_Stand_Back", root.transform, new Vector3(0f, 0f, 14f), Quaternion.identity, 80f, 6, 1.25f, 2.5f, matConcrete, matStands, matMetal, withRoof: true);
            BuildTerracedStand("WC_Stand_L", root.transform, new Vector3(-39f, 0f, -14f), Quaternion.Euler(0f, -90f, 0f), 82f, 6, 1.25f, 2.5f, matConcrete, matStands, matMetal, withRoof: true);
            BuildTerracedStand("WC_Stand_R", root.transform, new Vector3(39f, 0f, -14f), Quaternion.Euler(0f, 90f, 0f), 82f, 6, 1.25f, 2.5f, matConcrete, matStands, matMetal, withRoof: true);

            // Giant Flanking Jumbotrons & Golden Championship Banner Arch
            CreateBox("WC_Jumbotron_L", root.transform, new Vector3(-20f, 13.5f, 27f), new Vector3(12f, 5.2f, 0.8f), matBanner, false);
            CreateBox("WC_Jumbotron_R", root.transform, new Vector3(20f, 13.5f, 27f), new Vector3(12f, 5.2f, 0.8f), matBanner, false);
            CreateBox("WC_GoldArch", root.transform, new Vector3(0f, 15.5f, 26.5f), new Vector3(24f, 1.2f, 0.9f), matGold, false);

            // Championship Trophy Pedestal near tunnel
            CreateBox("WC_Trophy_Pedestal", root.transform, new Vector3(-14f, 0.6f, 6.5f), new Vector3(1.2f, 1.2f, 1.2f), matGold, false);
            GameObject trophyPrefab = Load<GameObject>("Assets/Prefabs/Trophy_Cup.prefab");
            if (trophyPrefab != null)
            {
                GameObject tInst = PrefabUtility.InstantiatePrefab(trophyPrefab, root.transform) as GameObject;
                if (tInst != null) tInst.transform.localPosition = new Vector3(-14f, 1.2f, 6.5f);
            }

            lights = new Light[4];
            lights[0] = CreateFloodlightTower("WC_Tower_FL", root.transform, new Vector3(-37f, 0f, 19f), new Vector3(48f, 138f, 0f), matMetal);
            lights[1] = CreateFloodlightTower("WC_Tower_FR", root.transform, new Vector3(37f, 0f, 19f), new Vector3(48f, -138f, 0f), matMetal);
            lights[2] = CreateFloodlightTower("WC_Tower_BL", root.transform, new Vector3(-37f, 0f, -35f), new Vector3(48f, 42f, 0f), matMetal);
            lights[3] = CreateFloodlightTower("WC_Tower_BR", root.transform, new Vector3(37f, 0f, -35f), new Vector3(48f, -42f, 0f), matMetal);

            // Confetti Cannons
            GameObject confettiObj = new GameObject("WC_ConfettiCannons");
            confettiObj.transform.SetParent(root.transform);
            confettiObj.transform.localPosition = new Vector3(0f, 13f, 2f);
            confetti = confettiObj.AddComponent<ParticleSystem>();
            var cm = confetti.main;
            cm.startLifetime = 4.5f;
            cm.startSpeed = 7.5f;
            cm.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.34f);
            cm.maxParticles = 180;
            cm.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.84f, 0.2f), new Color(0.15f, 0.72f, 1f));
            cm.loop = true;
            var em = confetti.emission;
            em.rateOverTime = 24f;
            var sh = confetti.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(42f, 1f, 1f);
            confetti.Stop();

            return root;
        }

        private static void BuildTerracedStand(string name, Transform parent, Vector3 origin, Quaternion rot, float width, int tiers, float stepH, float stepD, Material matConcrete, Material matCrowd, Material matMetal, bool withRoof)
        {
            GameObject stand = new GameObject(name);
            stand.transform.SetParent(parent, false);
            stand.transform.localPosition = origin;
            stand.transform.localRotation = rot;

            // Front concrete wall
            CreateBox("FrontWall", stand.transform, new Vector3(0f, 0.7f, -0.5f), new Vector3(width, 1.4f, 0.5f), matConcrete, false);

            for (int t = 0; t < tiers; t++)
            {
                float y = 1.4f + t * stepH;
                float z = t * stepD;
                CreateBox($"ConcreteTier_{t}", stand.transform, new Vector3(0f, y * 0.5f, z + stepD * 0.5f), new Vector3(width, y, stepD), matConcrete, false);
                CreateBox($"CrowdTier_{t}", stand.transform, new Vector3(0f, y + 0.35f, z + stepD * 0.5f), new Vector3(width - 1f, 0.7f, stepD * 0.7f), matCrowd, false);
            }

            // Stairs aisles breaking up width visually
            for (int a = -1; a <= 1; a += 2)
            {
                CreateBox($"StairAisle_{a}", stand.transform, new Vector3(a * width * 0.25f, (1.4f + tiers * stepH) * 0.5f, tiers * stepD * 0.5f),
                    new Vector3(1.8f, tiers * stepH + 1.4f, tiers * stepD + 0.5f), matConcrete, false);
            }

            // Safety railing
            CreateBox("TopRail", stand.transform, new Vector3(0f, 1.4f + tiers * stepH + 0.6f, tiers * stepD), new Vector3(width, 0.12f, 0.12f), matMetal, false);

            if (withRoof)
            {
                float roofY = 1.4f + tiers * stepH + 3.8f;
                CreateBox("CanopyRoof", stand.transform, new Vector3(0f, roofY, tiers * stepD * 0.45f), new Vector3(width + 2f, 0.45f, tiers * stepD * 1.15f), matMetal, false);
                CreateBox("RoofPillar_L", stand.transform, new Vector3(-width * 0.45f, roofY * 0.5f, tiers * stepD), new Vector3(0.6f, roofY, 0.6f), matMetal, false);
                CreateBox("RoofPillar_R", stand.transform, new Vector3(width * 0.45f, roofY * 0.5f, tiers * stepD), new Vector3(0.6f, roofY, 0.6f), matMetal, false);
            }
        }

        private static void BuildLEDBoardsRing(Transform parent, Material matBanner)
        {
            CreateBox("LED_Back", parent, new Vector3(0f, 0.55f, 5.8f), new Vector3(52f, 1.0f, 0.22f), matBanner, false);
            CreateBox("LED_Left", parent, new Vector3(-26f, 0.55f, -10f), new Vector3(0.22f, 1.0f, 34f), matBanner, false);
            CreateBox("LED_Right", parent, new Vector3(26f, 0.55f, -10f), new Vector3(0.22f, 1.0f, 34f), matBanner, false);
        }

        private static void BuildDugout(string name, Transform parent, Vector3 pos, Quaternion rot, Material matMetal, Material matSeats)
        {
            GameObject dg = new GameObject(name);
            dg.transform.SetParent(parent, false);
            dg.transform.localPosition = pos;
            dg.transform.localRotation = rot;
            CreateBox("Canopy", dg.transform, new Vector3(0f, 1.85f, 0f), new Vector3(5.5f, 0.12f, 1.4f), matMetal, false);
            CreateBox("BackWall", dg.transform, new Vector3(0f, 0.95f, -0.65f), new Vector3(5.5f, 1.8f, 0.12f), matMetal, false);
            CreateBox("BenchSeats", dg.transform, new Vector3(0f, 0.45f, 0f), new Vector3(5.0f, 0.25f, 0.6f), matSeats, false);
        }

        private static void CreateStylizedTree(string name, Transform parent, Vector3 pos, Material matTrunk, Material matLeaves)
        {
            GameObject t = new GameObject(name);
            t.transform.SetParent(parent, false);
            t.transform.localPosition = pos;
            CreateCylinderPart("Trunk", t.transform, new Vector3(0f, 1.6f, 0f), Quaternion.identity, new Vector3(0.45f, 1.6f, 0.45f), matTrunk, false);
            GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crown.name = "Foliage";
            crown.transform.SetParent(t.transform, false);
            crown.transform.localPosition = new Vector3(0f, 4.1f, 0f);
            crown.transform.localScale = new Vector3(2.8f, 3.0f, 2.8f);
            crown.GetComponent<Renderer>().sharedMaterial = matLeaves;
            Object.DestroyImmediate(crown.GetComponent<Collider>());
        }

        private static void CreateCornerFlag(string name, Transform parent, Vector3 pos, Material matPole, Material matFlag)
        {
            GameObject cf = new GameObject(name);
            cf.transform.SetParent(parent, false);
            cf.transform.localPosition = pos;
            CreateCylinderPart("FlagPole", cf.transform, new Vector3(0f, 0.75f, 0f), Quaternion.identity, new Vector3(0.05f, 0.75f, 0.05f), matPole, false);
            GameObject cloth = CreateBox("FlagCloth", cf.transform, new Vector3(0.18f, 1.38f, 0f), new Vector3(0.32f, 0.22f, 0.03f), matFlag, false);
            cloth.AddComponent<StadiumFlagWave>();
        }

        private static Light CreateFloodlightTower(string name, Transform parent, Vector3 basePos, Vector3 lightRot, Material matMetal)
        {
            GameObject tower = new GameObject(name);
            tower.transform.SetParent(parent, false);
            tower.transform.localPosition = basePos;

            CreateCylinderPart("Mast", tower.transform, new Vector3(0f, 11f, 0f), Quaternion.identity, new Vector3(0.55f, 11f, 0.55f), matMetal, false);
            CreateBox("LampBank", tower.transform, new Vector3(0f, 22.2f, 0f), new Vector3(3.8f, 1.2f, 1.3f), matMetal, false);
            Material matGlow = Load<Material>("Assets/Materials/Mat_Lines_White.mat") ?? matMetal;
            CreateBox("LampFace", tower.transform, new Vector3(0f, 21.7f, 0f), new Vector3(3.3f, 0.7f, 1.36f), matGlow, false);

            GameObject lightObj = new GameObject(name + "_Spot");
            lightObj.transform.SetParent(tower.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 21.5f, 0f);
            lightObj.transform.localRotation = Quaternion.Euler(lightRot);

            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Spot;
            l.color = new Color(0.95f, 0.97f, 1.0f);
            l.intensity = 35f;
            l.range = 85f;
            l.spotAngle = 72f;
            l.shadows = LightShadows.None;
            return l;
        }

        private static void CombineStaticMeshesByMaterial(GameObject container, string assetPrefix, bool castShadows)
        {
            if (container == null) return;

            const string folder = "Assets/Art/Models/Stadium";
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            var groups = new Dictionary<Material, List<CombineInstance>>();
            var toDestroy = new List<GameObject>();
            Matrix4x4 containerWorldToLocal = container.transform.worldToLocalMatrix;

            MeshFilter[] filters = container.GetComponentsInChildren<MeshFilter>(true);
            foreach (var mf in filters)
            {
                if (mf == null || mf.sharedMesh == null) continue;
                GameObject go = mf.gameObject;
                if (go == container) continue;
                if (go.GetComponent<Collider>() != null || go.GetComponent<Light>() != null || go.GetComponent<ParticleSystem>() != null) continue;
                if (PrefabUtility.IsPartOfPrefabInstance(go)) continue;

                MeshRenderer mr = go.GetComponent<MeshRenderer>();
                if (mr == null || mr.sharedMaterial == null) continue;

                Material mat = mr.sharedMaterial;
                if (!groups.TryGetValue(mat, out var list))
                {
                    list = new List<CombineInstance>();
                    groups[mat] = list;
                }

                list.Add(new CombineInstance
                {
                    mesh = mf.sharedMesh,
                    transform = containerWorldToLocal * mf.transform.localToWorldMatrix
                });
                toDestroy.Add(go);
            }

            foreach (var go in toDestroy)
            {
                Object.DestroyImmediate(go);
            }

            int groupIdx = 0;
            foreach (var kvp in groups)
            {
                Material mat = kvp.Key;
                List<CombineInstance> instances = kvp.Value;
                if (instances.Count == 0) continue;

                int totalVerts = 0;
                for (int i = 0; i < instances.Count; i++)
                {
                    totalVerts += instances[i].mesh.vertexCount;
                }

                Mesh combined = new Mesh
                {
                    name = $"{assetPrefix}_{mat.name}_Batch",
                    indexFormat = totalVerts > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16
                };
                combined.CombineMeshes(instances.ToArray(), true, true);
                combined.RecalculateBounds();
                combined.Optimize();

                string safeMatName = mat.name.Replace(" ", "_").Replace("/", "_");
                string assetPath = $"{folder}/Mesh_{assetPrefix}_{groupIdx}_{safeMatName}.asset";
                Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
                if (existing != null)
                {
                    EditorUtility.CopySerialized(combined, existing);
                    EditorUtility.SetDirty(existing);
                    combined = existing;
                }
                else
                {
                    AssetDatabase.CreateAsset(combined, assetPath);
                }

                GameObject batchObj = new GameObject($"StaticBatch_{safeMatName}");
                batchObj.transform.SetParent(container.transform, false);
                batchObj.isStatic = true;
                batchObj.AddComponent<MeshFilter>().sharedMesh = combined;
                MeshRenderer batchRenderer = batchObj.AddComponent<MeshRenderer>();
                batchRenderer.sharedMaterial = mat;
                bool shouldCast = castShadows && !mat.name.Contains("Banner");
                batchRenderer.shadowCastingMode = shouldCast ? ShadowCastingMode.On : ShadowCastingMode.Off;
                groupIdx++;
            }
        }

        // ==================== PRIMITIVE / NET HELPERS ====================
        private static void CreateSubdividedNet(string name, Transform parent, Vector3 pos, Quaternion rot, float width, float height, int segsX, int segsY, Material mat, bool addCollider)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = pos;
            obj.transform.localRotation = rot;

            Mesh mesh = new Mesh { name = name + "_Mesh" };
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            for (int y = 0; y <= segsY; y++)
            {
                float vy = y / (float)segsY;
                for (int x = 0; x <= segsX; x++)
                {
                    float vx = x / (float)segsX;
                    verts.Add(new Vector3((vx - 0.5f) * width, (vy - 0.5f) * height, 0f));
                    uvs.Add(new Vector2(vx, vy));
                }
            }

            int stride = segsX + 1;
            for (int y = 0; y < segsY; y++)
            {
                for (int x = 0; x < segsX; x++)
                {
                    int a = y * stride + x;
                    int b = a + 1;
                    int c = a + stride;
                    int d = c + 1;
                    tris.Add(a); tris.Add(c); tris.Add(b);
                    tris.Add(b); tris.Add(c); tris.Add(d);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            if (!System.IO.Directory.Exists("Assets/Art/Models")) System.IO.Directory.CreateDirectory("Assets/Art/Models");
            string meshPath = $"Assets/Art/Models/Mesh_{name}.asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (existing != null)
            {
                EditorUtility.CopySerialized(mesh, existing);
                EditorUtility.SetDirty(existing);
                mesh = existing;
            }
            else
            {
                AssetDatabase.CreateAsset(mesh, meshPath);
            }

            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            obj.AddComponent<MeshRenderer>().sharedMaterial = mat;
            if (addCollider)
            {
                BoxCollider bc = obj.AddComponent<BoxCollider>();
                bc.size = new Vector3(width, height, 0.06f);
            }
        }

        private static GameObject CreateBox(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool keepCollider)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static GameObject CreateCylinderPart(string name, Transform parent, Vector3 pos, Quaternion rot, Vector3 scale, Material mat, bool keepCollider, string tag = null)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            if (!string.IsNullOrEmpty(tag)) go.tag = tag;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            go.transform.localScale = scale;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);

        private static void SetField(object target, string fieldName, object value)
        {
            var f = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance);
            if (f != null) f.SetValue(target, value);
        }
    }
}
