// ===========================================================================
// CharacterAndStadiumUpgrader.cs
// ULTIMATE PENALTY CUP 3D - Visual Upgrade System
// A&R Game Labs | Developers: Rubén González & Xitzia Aimara Fierro Rivera
// ===========================================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UltimatePenaltyCup.Player;
using UltimatePenaltyCup.Goalkeeper;
using UltimatePenaltyCup.Gameplay;
using UltimatePenaltyCup.Core;

namespace UltimatePenaltyCup.Editor
{
    public static class CharacterAndStadiumUpgrader
    {
        // ----------------------------------------------------------------
        // Menu Items
        // ----------------------------------------------------------------
        [MenuItem("Ultimate Penalty Cup/Visual Upgrade/01 - Rebuild Player (Rubén González)", false, 100)]
        public static void RebuildPlayerPrefab()
        {
            const string path = "Assets/Prefabs/Player_RubenGonzalez.prefab";
            Material matKit  = Load<Material>("Assets/Materials/Mat_Player_Kit.mat");
            Material matSkin = Load<Material>("Assets/Materials/Mat_Skin.mat");
            Material matBoot = Load<Material>("Assets/Materials/Mat_Stadium_Banners.mat");

            // ---- Build the character in memory ----
            GameObject root = new GameObject("Player_RubenGonzalez");

            // --- Visuals container ---
            GameObject vis = new GameObject("Visuals");
            vis.transform.SetParent(root.transform, false);

            // ---- TORSO ----
            // Main body capsule - slender athletic build
            GameObject torso = MakePart("Torso", vis.transform,
                new Vector3(0f, 1.22f, 0f), Quaternion.identity,
                new Vector3(0.50f, 0.44f, 0.30f),
                PrimitiveType.Capsule, matKit);

            // Collar / neck
            GameObject neck = MakePart("Neck", vis.transform,
                new Vector3(0f, 1.65f, 0f), Quaternion.identity,
                new Vector3(0.14f, 0.09f, 0.14f),
                PrimitiveType.Cylinder, matSkin);

            // ---- HEAD ----
            GameObject head = MakePart("Head", vis.transform,
                new Vector3(0f, 1.82f, 0f), Quaternion.identity,
                new Vector3(0.32f, 0.34f, 0.32f),
                PrimitiveType.Sphere, matSkin);

            // Hair - stylised faux-hawk
            GameObject hair = MakePart("Hair_Top", head.transform,
                new Vector3(0f, 0.28f, -0.05f), Quaternion.Euler(85f, 0f, 0f),
                new Vector3(0.70f, 0.42f, 0.60f),
                PrimitiveType.Capsule, matBoot);
            MakePart("Hair_Back", head.transform,
                new Vector3(0f, 0.14f, 0.18f), Quaternion.Euler(10f, 0f, 0f),
                new Vector3(0.68f, 0.22f, 0.45f),
                PrimitiveType.Sphere, matBoot);

            // ---- LEFT ARM ----
            GameObject armL = new GameObject("Arm_Left");
            armL.transform.SetParent(vis.transform, false);
            armL.transform.localPosition = new Vector3(-0.33f, 1.47f, 0f);

            MakePart("Sleeve_L", armL.transform,
                new Vector3(-0.10f, -0.14f, 0f), Quaternion.Euler(0f, 0f, 18f),
                new Vector3(0.13f, 0.17f, 0.13f), PrimitiveType.Cylinder, matKit);
            MakePart("Forearm_L", armL.transform,
                new Vector3(-0.18f, -0.38f, 0.04f), Quaternion.Euler(-12f, 0f, 22f),
                new Vector3(0.11f, 0.16f, 0.11f), PrimitiveType.Cylinder, matSkin);
            MakePart("Hand_L", armL.transform,
                new Vector3(-0.24f, -0.58f, 0.08f), Quaternion.Euler(-8f, 0f, 15f),
                new Vector3(0.13f, 0.08f, 0.09f), PrimitiveType.Sphere, matSkin);

            // ---- RIGHT ARM ----
            GameObject armR = new GameObject("Arm_Right");
            armR.transform.SetParent(vis.transform, false);
            armR.transform.localPosition = new Vector3(0.33f, 1.47f, 0f);

            MakePart("Sleeve_R", armR.transform,
                new Vector3(0.10f, -0.14f, 0f), Quaternion.Euler(0f, 0f, -18f),
                new Vector3(0.13f, 0.17f, 0.13f), PrimitiveType.Cylinder, matKit);
            MakePart("Forearm_R", armR.transform,
                new Vector3(0.18f, -0.38f, 0.04f), Quaternion.Euler(-12f, 0f, -22f),
                new Vector3(0.11f, 0.16f, 0.11f), PrimitiveType.Cylinder, matSkin);
            MakePart("Hand_R", armR.transform,
                new Vector3(0.24f, -0.58f, 0.08f), Quaternion.Euler(-8f, 0f, -15f),
                new Vector3(0.13f, 0.08f, 0.09f), PrimitiveType.Sphere, matSkin);

            // ---- SHORTS / HIPS ----
            MakePart("Shorts", vis.transform,
                new Vector3(0f, 0.86f, 0f), Quaternion.identity,
                new Vector3(0.46f, 0.26f, 0.30f), PrimitiveType.Cube, matKit);

            // ---- LEFT LEG ----
            GameObject legL = new GameObject("Leg_Left");
            legL.transform.SetParent(vis.transform, false);
            legL.transform.localPosition = new Vector3(-0.15f, 0.80f, 0f);

            MakePart("Thigh_L", legL.transform,
                new Vector3(0f, -0.17f, 0f), Quaternion.identity,
                new Vector3(0.17f, 0.19f, 0.17f), PrimitiveType.Cylinder, matSkin);
            MakePart("Shin_Sock_L", legL.transform,
                new Vector3(0f, -0.50f, 0f), Quaternion.identity,
                new Vector3(0.15f, 0.21f, 0.15f), PrimitiveType.Cylinder, matKit);
            // Ankle guard bump
            MakePart("AnkleGuard_L", legL.transform,
                new Vector3(0f, -0.70f, 0.02f), Quaternion.identity,
                new Vector3(0.14f, 0.07f, 0.13f), PrimitiveType.Sphere, matKit);
            // Boot with pointed toe
            MakePart("Boot_L", legL.transform,
                new Vector3(0f, -0.76f, 0.09f), Quaternion.Euler(10f, 0f, 0f),
                new Vector3(0.15f, 0.09f, 0.33f), PrimitiveType.Cube, matBoot);

            // ---- RIGHT LEG (kicking) ----
            GameObject legR = new GameObject("Leg_Right");
            legR.transform.SetParent(vis.transform, false);
            legR.transform.localPosition = new Vector3(0.15f, 0.80f, 0f);

            MakePart("Thigh_R", legR.transform,
                new Vector3(0f, -0.17f, 0f), Quaternion.identity,
                new Vector3(0.17f, 0.19f, 0.17f), PrimitiveType.Cylinder, matSkin);
            MakePart("Shin_Sock_R", legR.transform,
                new Vector3(0f, -0.50f, 0f), Quaternion.identity,
                new Vector3(0.15f, 0.21f, 0.15f), PrimitiveType.Cylinder, matKit);
            MakePart("AnkleGuard_R", legR.transform,
                new Vector3(0f, -0.70f, 0.02f), Quaternion.identity,
                new Vector3(0.14f, 0.07f, 0.13f), PrimitiveType.Sphere, matKit);
            MakePart("Boot_R", legR.transform,
                new Vector3(0f, -0.76f, 0.09f), Quaternion.Euler(10f, 0f, 0f),
                new Vector3(0.15f, 0.09f, 0.33f), PrimitiveType.Cube, matBoot);

            // ---- JERSEY NUMBER "10" (extra badge on torso back/front) ----
            // The texture Tex_Jersey_Ruben.png carries the #10 print on the material
            // so no additional geometry needed beyond using the correct material.

            // ---- COMPONENTS ----
            PlayerController pc = root.AddComponent<PlayerController>();
            PlayerShooter ps = root.AddComponent<PlayerShooter>();

            // Aim Reticle crosshair
            Material matLines = Load<Material>("Assets/Materials/Mat_Lines_White.mat");
            GameObject reticle = GameObject.CreatePrimitive(PrimitiveType.Quad);
            reticle.name = "AimReticle_Indicator";
            reticle.transform.SetParent(root.transform, false);
            reticle.transform.localScale = Vector3.one * 0.70f;
            if (matLines != null) reticle.GetComponent<Renderer>().material = matLines;
            Object.DestroyImmediate(reticle.GetComponent<Collider>());

            // Trajectory guideline
            GameObject lineObj = new GameObject("TrajectoryGuideline");
            lineObj.transform.SetParent(root.transform, false);
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.positionCount = 10;
            lr.startWidth = 0.05f;
            lr.endWidth = 0.01f;
            if (matLines != null) lr.material = matLines;
            lr.startColor = new Color(0.1f, 0.9f, 1f, 0.9f);
            lr.endColor = new Color(1f, 0.9f, 0.2f, 0.2f);
            lr.useWorldSpace = true;

            ps.SetAimReticle(reticle.transform, lr);

            // Wire visual body parts to PlayerController
            SetField(pc, "characterVisuals", vis.transform);
            SetField(pc, "head",    head.transform);
            SetField(pc, "torso",   torso.transform);
            SetField(pc, "leftArm", armL.transform);
            SetField(pc, "rightArm",armR.transform);
            SetField(pc, "leftLeg", legL.transform);
            SetField(pc, "rightLeg",legR.transform);

            // ---- SAVE ----
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log("[Upgrader] Player_RubenGonzalez.prefab upgraded.");
        }

        // ----------------------------------------------------------------
        [MenuItem("Ultimate Penalty Cup/Visual Upgrade/02 - Rebuild Goalkeeper (Malágon)", false, 101)]
        public static void RebuildGoalkeeperPrefab()
        {
            const string path = "Assets/Prefabs/Goalkeeper_Malagon.prefab";
            Material matGk   = Load<Material>("Assets/Materials/Mat_Goalkeeper_Kit.mat");
            Material matSkin = Load<Material>("Assets/Materials/Mat_Skin.mat");
            Material matBoot = Load<Material>("Assets/Materials/Mat_Stadium_Banners.mat");

            GameObject root = new GameObject("Goalkeeper_Malagon");
            root.tag = "Goalkeeper";

            GameObject vis = new GameObject("Visuals");
            vis.transform.SetParent(root.transform, false);

            // ---- TORSO - Bulkier goalkeeper build ----
            GameObject torso = MakePart("Torso", vis.transform,
                new Vector3(0f, 1.28f, 0f), Quaternion.identity,
                new Vector3(0.57f, 0.47f, 0.36f), PrimitiveType.Capsule, matGk);
            torso.tag = "Goalkeeper";

            // Neck
            MakePart("Neck", vis.transform,
                new Vector3(0f, 1.70f, 0f), Quaternion.identity,
                new Vector3(0.15f, 0.09f, 0.15f), PrimitiveType.Cylinder, matSkin);

            // ---- HEAD ----
            GameObject head = MakePart("Head", vis.transform,
                new Vector3(0f, 1.90f, 0f), Quaternion.identity,
                new Vector3(0.34f, 0.36f, 0.34f), PrimitiveType.Sphere, matSkin);
            head.tag = "Goalkeeper";

            // Hair - short dark
            MakePart("Hair_GK", head.transform,
                new Vector3(0f, 0.18f, -0.04f), Quaternion.identity,
                new Vector3(1.05f, 0.60f, 1.05f), PrimitiveType.Sphere, matBoot);

            // ---- LEFT ARM + BIG GOALKEEPER GLOVE ----
            GameObject armL = new GameObject("Arm_Left");
            armL.transform.SetParent(vis.transform, false);
            armL.transform.localPosition = new Vector3(-0.40f, 1.52f, 0f);

            GameObject sleeveL = MakePart("Sleeve_L", armL.transform,
                new Vector3(-0.14f, -0.16f, 0.04f), Quaternion.Euler(0f, 0f, 32f),
                new Vector3(0.16f, 0.25f, 0.16f), PrimitiveType.Cylinder, matGk);
            sleeveL.tag = "Goalkeeper";

            // Large goalie glove
            GameObject gloveL = MakePart("GoalkeeperGlove_L", armL.transform,
                new Vector3(-0.32f, -0.42f, 0.14f), Quaternion.Euler(-10f, 0f, 15f),
                new Vector3(0.26f, 0.08f, 0.22f), PrimitiveType.Cube, matSkin);
            gloveL.tag = "Goalkeeper";
            // Glove palm bump
            MakePart("GlovePalm_L", armL.transform,
                new Vector3(-0.32f, -0.44f, 0.21f), Quaternion.Euler(-10f, 0f, 15f),
                new Vector3(0.24f, 0.07f, 0.14f), PrimitiveType.Sphere, matSkin);
            // Wrist strap
            MakePart("WristStrap_L", armL.transform,
                new Vector3(-0.28f, -0.33f, 0.10f), Quaternion.Euler(0f, 0f, 15f),
                new Vector3(0.20f, 0.07f, 0.18f), PrimitiveType.Cylinder, matGk);

            // ---- RIGHT ARM + BIG GOALKEEPER GLOVE ----
            GameObject armR = new GameObject("Arm_Right");
            armR.transform.SetParent(vis.transform, false);
            armR.transform.localPosition = new Vector3(0.40f, 1.52f, 0f);

            GameObject sleeveR = MakePart("Sleeve_R", armR.transform,
                new Vector3(0.14f, -0.16f, 0.04f), Quaternion.Euler(0f, 0f, -32f),
                new Vector3(0.16f, 0.25f, 0.16f), PrimitiveType.Cylinder, matGk);
            sleeveR.tag = "Goalkeeper";

            GameObject gloveR = MakePart("GoalkeeperGlove_R", armR.transform,
                new Vector3(0.32f, -0.42f, 0.14f), Quaternion.Euler(-10f, 0f, -15f),
                new Vector3(0.26f, 0.08f, 0.22f), PrimitiveType.Cube, matSkin);
            gloveR.tag = "Goalkeeper";
            MakePart("GlovePalm_R", armR.transform,
                new Vector3(0.32f, -0.44f, 0.21f), Quaternion.Euler(-10f, 0f, -15f),
                new Vector3(0.24f, 0.07f, 0.14f), PrimitiveType.Sphere, matSkin);
            MakePart("WristStrap_R", armR.transform,
                new Vector3(0.28f, -0.33f, 0.10f), Quaternion.Euler(0f, 0f, -15f),
                new Vector3(0.20f, 0.07f, 0.18f), PrimitiveType.Cylinder, matGk);

            // ---- SHORTS ----
            MakePart("Shorts", vis.transform,
                new Vector3(0f, 0.90f, 0f), Quaternion.identity,
                new Vector3(0.52f, 0.28f, 0.34f), PrimitiveType.Cube, matGk);

            // ---- LEFT LEG ----
            GameObject legL = new GameObject("Leg_Left");
            legL.transform.SetParent(vis.transform, false);
            legL.transform.localPosition = new Vector3(-0.17f, 0.84f, 0f);

            GameObject legMeshL = MakePart("LegMesh_L", legL.transform,
                new Vector3(0f, -0.40f, 0f), Quaternion.identity,
                new Vector3(0.21f, 0.44f, 0.21f), PrimitiveType.Capsule, matGk);
            legMeshL.tag = "Goalkeeper";
            MakePart("Boot_L_GK", legL.transform,
                new Vector3(0f, -0.74f, 0.07f), Quaternion.Euler(8f, 0f, 0f),
                new Vector3(0.18f, 0.09f, 0.34f), PrimitiveType.Cube, matBoot);

            // ---- RIGHT LEG ----
            GameObject legR = new GameObject("Leg_Right");
            legR.transform.SetParent(vis.transform, false);
            legR.transform.localPosition = new Vector3(0.17f, 0.84f, 0f);

            GameObject legMeshR = MakePart("LegMesh_R", legR.transform,
                new Vector3(0f, -0.40f, 0f), Quaternion.identity,
                new Vector3(0.21f, 0.44f, 0.21f), PrimitiveType.Capsule, matGk);
            legMeshR.tag = "Goalkeeper";
            MakePart("Boot_R_GK", legR.transform,
                new Vector3(0f, -0.74f, 0.07f), Quaternion.Euler(8f, 0f, 0f),
                new Vector3(0.18f, 0.09f, 0.34f), PrimitiveType.Cube, matBoot);

            // ---- COMPONENTS ----
            GoalkeeperAI gkAI = root.AddComponent<GoalkeeperAI>();
            SetField(gkAI, "bodyRoot", vis.transform);
            SetField(gkAI, "torso",   torso.transform);
            SetField(gkAI, "head",    head.transform);
            SetField(gkAI, "leftArm", armL.transform);
            SetField(gkAI, "rightArm",armR.transform);
            SetField(gkAI, "leftLeg", legL.transform);
            SetField(gkAI, "rightLeg",legR.transform);

            // ---- SAVE ----
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log("[Upgrader] Goalkeeper_Malagon.prefab upgraded.");
        }

        // ----------------------------------------------------------------
        [MenuItem("Ultimate Penalty Cup/Visual Upgrade/03 - Rebuild Stadium Prefab", false, 102)]
        public static void RebuildStadiumPrefab()
        {
            const string path = "Assets/Prefabs/StadiumEnvironment.prefab";

            Material matGrass  = Load<Material>("Assets/Materials/Mat_Grass_Cantera.mat");
            Material matLines  = Load<Material>("Assets/Materials/Mat_Lines_White.mat");
            Material matStands = Load<Material>("Assets/Materials/Mat_Stadium_Stands.mat");
            Material matBanner = Load<Material>("Assets/Materials/Mat_Stadium_Banners.mat");

            GameObject root = new GameObject("StadiumEnvironment");
            root.AddComponent<StadiumController>();

            // ========== PITCH ==========
            GameObject pitch = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pitch.name = "Pitch_Grass";
            pitch.transform.SetParent(root.transform);
            pitch.transform.localPosition = new Vector3(0f, -0.05f, -5f);
            pitch.transform.localScale    = new Vector3(80f, 0.10f, 110f);
            pitch.GetComponent<Renderer>().material = matGrass;
            Object.DestroyImmediate(pitch.GetComponent<BoxCollider>());
            // Invisible floor collider (player/ball needs it)
            GameObject floor = new GameObject("FloorCollider");
            floor.transform.SetParent(root.transform);
            floor.transform.localPosition = new Vector3(0f, 0f, -5f);
            BoxCollider floorCol = floor.AddComponent<BoxCollider>();
            floorCol.size = new Vector3(80f, 0.1f, 110f);

            // ========== PITCH LINES ==========
            // Penalty box
            CreateLine("Line_PenaltyBox_Back",  root.transform, new Vector3(0f, 0.005f, -16.5f), new Vector3(40.3f, 0.01f, 0.12f), matLines);
            CreateLine("Line_PenaltyBox_Left",  root.transform, new Vector3(-20.15f, 0.005f, -8.25f), new Vector3(0.12f, 0.01f, 16.5f), matLines);
            CreateLine("Line_PenaltyBox_Right", root.transform, new Vector3(20.15f, 0.005f, -8.25f), new Vector3(0.12f, 0.01f, 16.5f), matLines);
            // Goal box (5.5m x 18.3m)
            CreateLine("Line_GoalBox_Back",  root.transform, new Vector3(0f, 0.005f, -5.5f), new Vector3(18.3f, 0.01f, 0.12f), matLines);
            CreateLine("Line_GoalBox_Left",  root.transform, new Vector3(-9.15f, 0.005f, -2.75f), new Vector3(0.12f, 0.01f, 5.5f), matLines);
            CreateLine("Line_GoalBox_Right", root.transform, new Vector3(9.15f, 0.005f, -2.75f), new Vector3(0.12f, 0.01f, 5.5f), matLines);
            // Goal line
            CreateLine("Line_GoalLine", root.transform, new Vector3(0f, 0.005f, 0f), new Vector3(50f, 0.01f, 0.12f), matLines);
            // Penalty arc (D) - simplified as a few curved line segments
            for (int i = -4; i <= 4; i++)
            {
                float ang = i * 15f * Mathf.Deg2Rad;
                float r = 9.15f;
                float x = Mathf.Sin(ang) * r;
                float z = -11f + Mathf.Cos(ang) * r;
                if (z < -16.5f) continue; // only outside penalty box
                CreateLine($"PenArc_{i+4}", root.transform, new Vector3(x, 0.005f, z), new Vector3(0.12f, 0.01f, 0.12f), matLines);
            }
            // Penalty spot
            GameObject spot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spot.name = "PenaltySpot";
            spot.transform.SetParent(root.transform);
            spot.transform.localPosition = new Vector3(0f, 0.006f, -11f);
            spot.transform.localScale    = new Vector3(0.35f, 0.005f, 0.35f);
            spot.GetComponent<Renderer>().material = matLines;
            Object.DestroyImmediate(spot.GetComponent<Collider>());

            // ========== GRANDSTANDS ==========
            // Multi-tier structure for each stand side
            BuildStand("Stand_BehindGoal", root.transform, new Vector3(0f, 0f, 30f),
                new Vector3(90f, 1f, 24f), matStands, matBanner, 4);
            BuildStand("Stand_KickerSide", root.transform, new Vector3(0f, 0f, -60f),
                new Vector3(90f, 1f, 20f), matStands, matBanner, 3);
            BuildStand("Stand_SideLeft",  root.transform, new Vector3(-52f, 0f, -15f),
                new Vector3(22f, 1f, 100f), matStands, matBanner, 4, true);
            BuildStand("Stand_SideRight", root.transform, new Vector3(52f, 0f, -15f),
                new Vector3(22f, 1f, 100f), matStands, matBanner, 4, true);

            // ========== LED PERIMETER BOARDS ==========
            CreateLEDBoard("Board_GoalLine_L",  root.transform, new Vector3(-12f, 0.6f, 1.5f), new Vector3(16f, 1.1f, 0.18f), matBanner);
            CreateLEDBoard("Board_GoalLine_R",  root.transform, new Vector3(12f, 0.6f, 1.5f), new Vector3(16f, 1.1f, 0.18f), matBanner);
            CreateLEDBoard("Board_SideLeft_F",  root.transform, new Vector3(-25f, 0.6f, -8f), new Vector3(0.18f, 1.1f, 30f), matBanner, true);
            CreateLEDBoard("Board_SideRight_F", root.transform, new Vector3(25f, 0.6f, -8f), new Vector3(0.18f, 1.1f, 30f), matBanner, true);

            // ========== FLOODLIGHT TOWERS ==========
            Light[] lights = new Light[4];
            lights[0] = CreateFloodlightTower("Light_FL", root.transform, new Vector3(-38f, 0f, 22f), new Vector3(55f, 42f, 0f));
            lights[1] = CreateFloodlightTower("Light_FR", root.transform, new Vector3(38f, 0f, 22f), new Vector3(55f, -42f, 0f));
            lights[2] = CreateFloodlightTower("Light_BL", root.transform, new Vector3(-38f, 0f, -40f), new Vector3(55f, 138f, 0f));
            lights[3] = CreateFloodlightTower("Light_BR", root.transform, new Vector3(38f, 0f, -40f), new Vector3(55f, -138f, 0f));

            // ========== CONFETTI SYSTEM (mundo final) ==========
            GameObject confettiObj = new GameObject("ConfettiCannons");
            confettiObj.transform.SetParent(root.transform);
            confettiObj.transform.localPosition = new Vector3(0f, 12f, 0f);
            ParticleSystem confetti = confettiObj.AddComponent<ParticleSystem>();
            var cm = confetti.main;
            cm.startLifetime = 5.5f;
            cm.startSpeed    = 10f;
            cm.startSize     = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
            cm.maxParticles  = 800;
            cm.startColor    = new ParticleSystem.MinMaxGradient(Color.yellow, Color.cyan);
            cm.loop          = true;
            var em = confetti.emission;
            em.rateOverTime = 0f;        // disabled until Mundial stadium
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 300) });
            var sh = confetti.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(80f, 1f, 1f);
            confetti.Stop();

            // Wire StadiumController references
            StadiumController sc = root.GetComponent<StadiumController>();
            SetField(sc, "pitchRenderer", pitch.GetComponent<Renderer>());
            SetField(sc, "floodlights", lights);
            SetField(sc, "confettiSystem", confetti);
            SetField(sc, "matGrassCantera", Load<Material>("Assets/Materials/Mat_Grass_Cantera.mat"));
            SetField(sc, "matGrassNight",   Load<Material>("Assets/Materials/Mat_Grass_Night.mat"));
            SetField(sc, "matGrassPro",     Load<Material>("Assets/Materials/Mat_Grass_Pro.mat"));
            SetField(sc, "matGrassWorldCup",Load<Material>("Assets/Materials/Mat_Grass_WorldCup.mat"));

            // ---- SAVE ----
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log("[Upgrader] StadiumEnvironment.prefab rebuilt.");
        }

        // ----------------------------------------------------------------
        [MenuItem("Ultimate Penalty Cup/Visual Upgrade/04 - Rebuild Ball Prefab (VFX)", false, 103)]
        public static void RebuildBallPrefab()
        {
            const string path = "Assets/Prefabs/BallPrefab.prefab";

            Material matClassic = Load<Material>("Assets/Materials/Mat_Ball_Classic.mat");
            Material matBlue    = Load<Material>("Assets/Materials/Mat_Ball_Blue.mat");
            Material matFire    = Load<Material>("Assets/Materials/Mat_Ball_Fire.mat");
            Material matRainbow = Load<Material>("Assets/Materials/Mat_Ball_Rainbow.mat");
            Material matGold    = Load<Material>("Assets/Materials/Mat_Ball_Gold.mat");

            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "Ball_3D";
            ball.transform.localScale = Vector3.one * 0.32f;
            ball.GetComponent<Renderer>().material = matClassic;

            // ---- Trail ----
            TrailRenderer trail = ball.AddComponent<TrailRenderer>();
            trail.time        = 0.4f;
            trail.startWidth  = 0.26f;
            trail.endWidth    = 0.02f;
            trail.emitting    = false;
            trail.material    = matClassic;

            // ---- Ball VFX Particle System ----
            GameObject vfxObj = new GameObject("BallVFX_Sparks");
            vfxObj.transform.SetParent(ball.transform, false);
            ParticleSystem vfx = vfxObj.AddComponent<ParticleSystem>();
            var vm = vfx.main;
            vm.startLifetime = 0.4f;
            vm.startSpeed    = 2.5f;
            vm.startSize     = new ParticleSystem.MinMaxCurve(0.04f, 0.14f);
            vm.maxParticles  = 60;
            vm.startColor    = new Color(1f, 0.9f, 0.3f, 0.9f);
            vm.loop          = true;
            var vem = vfx.emission;
            vem.rateOverTime = 35f;
            vfx.Stop();

            // ---- BallController ----
            BallController bc = ball.AddComponent<BallController>();
            bc.SetCustomMaterials(matClassic, matBlue, matFire, matRainbow, matGold);
            SetField(bc, "ballVfxParticles", vfx);

            PrefabUtility.SaveAsPrefabAsset(ball, path);
            Object.DestroyImmediate(ball);
            Debug.Log("[Upgrader] BallPrefab.prefab rebuilt with VFX.");
        }

        // ----------------------------------------------------------------
        [MenuItem("Ultimate Penalty Cup/Visual Upgrade/05 - Run ALL Upgrades", false, 110)]
        public static void RunAllUpgrades()
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                RebuildPlayerPrefab();
                RebuildGoalkeeperPrefab();
                RebuildStadiumPrefab();
                RebuildBallPrefab();
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            Debug.Log("[Upgrader] ALL VISUAL UPGRADES COMPLETE.");
        }

        // ================================================================
        // HELPERS
        // ================================================================
        private static GameObject MakePart(string name, Transform parent, Vector3 pos, Quaternion rot, Vector3 scale, PrimitiveType prim, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(prim);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            go.transform.localScale    = scale;
            if (mat != null) go.GetComponent<Renderer>().material = mat;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static void CreateLine(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localPosition = pos;
            go.transform.localScale    = scale;
            go.GetComponent<Renderer>().material = mat;
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        /// <summary>Multi-tier grandstand: base + 3 tier steps + crowd texture plane on face.</summary>
        private static void BuildStand(string name, Transform parent, Vector3 origin, Vector3 baseScale, Material matStand, Material matBanner, int tiers, bool rotated = false)
        {
            GameObject standRoot = new GameObject(name);
            standRoot.transform.SetParent(parent);
            standRoot.transform.localPosition = origin;
            if (rotated) standRoot.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            float tierH = 2.8f;
            float tierD = 5.5f;
            for (int t = 0; t < tiers; t++)
            {
                GameObject tier = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tier.name = $"Tier_{t}";
                tier.transform.SetParent(standRoot.transform);
                float yPos = tierH * 0.5f + tierH * t;
                float zOff = -tierD * 0.5f - tierD * t;
                tier.transform.localPosition = new Vector3(0f, yPos, zOff);
                tier.transform.localScale    = new Vector3(baseScale.x, tierH, tierD);
                tier.GetComponent<Renderer>().material = matStand;
                // Crowd face panel (front face)
                GameObject crowd = GameObject.CreatePrimitive(PrimitiveType.Cube);
                crowd.name = $"Crowd_{t}";
                crowd.transform.SetParent(standRoot.transform);
                crowd.transform.localPosition = new Vector3(0f, yPos, zOff + tierD * 0.5f);
                crowd.transform.localScale    = new Vector3(baseScale.x, tierH, 0.08f);
                crowd.GetComponent<Renderer>().material = matBanner;
                Object.DestroyImmediate(crowd.GetComponent<Collider>());
            }
            // Roof cap
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Roof";
            roof.transform.SetParent(standRoot.transform);
            roof.transform.localPosition = new Vector3(0f, tierH * tiers + 0.6f, -tierD * tiers * 0.4f);
            roof.transform.localScale    = new Vector3(baseScale.x + 4f, 0.5f, tierD * tiers * 0.5f);
            roof.GetComponent<Renderer>().material = matStand;
            Object.DestroyImmediate(roof.GetComponent<Collider>());
        }

        private static void CreateLEDBoard(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool rotated = false)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localPosition = pos;
            go.transform.localScale    = scale;
            if (rotated) go.transform.localRotation = Quaternion.identity;
            go.GetComponent<Renderer>().material = mat;
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        private static Light CreateFloodlightTower(string name, Transform parent, Vector3 basePos, Vector3 lightRot)
        {
            GameObject tower = new GameObject(name + "_Tower");
            tower.transform.SetParent(parent);
            tower.transform.localPosition = basePos;

            // Pole
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(tower.transform, false);
            pole.transform.localPosition = new Vector3(0f, 10f, 0f);
            pole.transform.localScale    = new Vector3(0.4f, 10f, 0.4f);
            Material poleMat = Load<Material>("Assets/Materials/Mat_Goal_Posts.mat");
            if (poleMat != null) pole.GetComponent<Renderer>().material = poleMat;
            Object.DestroyImmediate(pole.GetComponent<Collider>());

            // Lamp housing
            GameObject lamp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lamp.name = "LampHousing";
            lamp.transform.SetParent(tower.transform, false);
            lamp.transform.localPosition = new Vector3(0f, 20.5f, 0f);
            lamp.transform.localScale    = new Vector3(2.5f, 0.5f, 1.2f);
            if (poleMat != null) lamp.GetComponent<Renderer>().material = poleMat;
            Object.DestroyImmediate(lamp.GetComponent<Collider>());

            // Spotlight
            GameObject lightObj = new GameObject(name + "_Light");
            lightObj.transform.SetParent(tower.transform);
            lightObj.transform.localPosition = new Vector3(0f, 20f, 0f);
            lightObj.transform.localRotation = Quaternion.Euler(lightRot);

            Light l = lightObj.AddComponent<Light>();
            l.type      = LightType.Spot;
            l.color     = new Color(0.96f, 0.98f, 1.0f);
            l.intensity = 3.5f;
            l.range     = 60f;
            l.spotAngle = 68f;
            l.shadows   = LightShadows.Soft;
            return l;
        }

        private static T Load<T>(string path) where T : Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var f = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance);
            if (f != null)
                f.SetValue(target, value);
            else
                Debug.LogWarning($"[Upgrader] Field '{fieldName}' not found on {target.GetType().Name}");
        }
    }
}
