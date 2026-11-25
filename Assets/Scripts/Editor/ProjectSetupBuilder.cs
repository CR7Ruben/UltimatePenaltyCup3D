using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UltimatePenaltyCup.Core;
using UltimatePenaltyCup.Levels;
using UltimatePenaltyCup.Shop;
using UltimatePenaltyCup.Achievements;
using UltimatePenaltyCup.Gameplay;
using UltimatePenaltyCup.Player;
using UltimatePenaltyCup.Goalkeeper;
using UltimatePenaltyCup.UI;
using UltimatePenaltyCup.Audio;
using UltimatePenaltyCup.Economy;
using UltimatePenaltyCup.SaveSystem;
using UltimatePenaltyCup.Customization;

namespace UltimatePenaltyCup.Editor
{
    [InitializeOnLoad]
    public static class ProjectSetupBuilder
    {
        private const string AUTO_SETUP_KEY = "UPC_AutoSetup_Ran_v1.0";

        static ProjectSetupBuilder()
        {
            EditorApplication.delayCall += OnEditorReady;
        }

        private static void OnEditorReady()
        {
            if (!SessionState.GetBool(AUTO_SETUP_KEY, false))
            {
                SessionState.SetBool(AUTO_SETUP_KEY, true);
                Debug.Log("[UltimatePenaltyCup] Running automated project setup...");
                ExecuteCompleteSetup();
            }
        }

        [MenuItem("Ultimate Penalty Cup/Execute Complete Project Setup", false, 1)]
        public static void ExecuteCompleteSetup()
        {
            try
            {
                AssetDatabase.StartAssetEditing();

                ConfigureLogoTexture();
                CreateAllMaterials();
                CreateAllScriptableObjects();
                CreateAllPrefabs();
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            BuildMainMenuScene();
            BuildMatchScene();
            SetupBuildSettings();

            Debug.Log("==========================================================================");
            Debug.Log(">>> ULTIMATE PENALTY CUP 3D: COMPLETE PROJECT SETUP FINISHED SUCCESSFULLY! <<<");
            Debug.Log("==========================================================================");
        }

        #region 1. Logo Texture Configuration
        public static void ConfigureLogoTexture()
        {
            string logoPath = "Assets/Art/Logo/UltimatePenaltyCup3D_Logo.jpg";
            if (!File.Exists(logoPath)) return;

            TextureImporter importer = AssetImporter.GetAtPath(logoPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = false;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
                Debug.Log("[ProjectSetupBuilder] Logo texture configured as Sprite.");
            }
        }
        #endregion

        #region 2. Materials Generation
        public static void CreateAllMaterials()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");
            if (litShader == null) litShader = Shader.Find("Mobile/Diffuse");

            // Grass Cantera
            CreateOrUpdateMaterial("Assets/Materials/Mat_Grass_Cantera.mat", litShader, new Color(0.18f, 0.52f, 0.16f), 0.1f, 0.0f);
            // Grass Night
            CreateOrUpdateMaterial("Assets/Materials/Mat_Grass_Night.mat", litShader, new Color(0.08f, 0.32f, 0.12f), 0.2f, 0.0f);
            // Grass Professional
            CreateOrUpdateMaterial("Assets/Materials/Mat_Grass_Pro.mat", litShader, new Color(0.12f, 0.62f, 0.22f), 0.25f, 0.0f);
            // Grass World Cup
            CreateOrUpdateMaterial("Assets/Materials/Mat_Grass_WorldCup.mat", litShader, new Color(0.14f, 0.68f, 0.26f), 0.35f, 0.05f);

            // Pitch Lines
            CreateOrUpdateMaterial("Assets/Materials/Mat_Lines_White.mat", litShader, Color.white, 0.2f, 0.0f);
            // Goal Posts
            CreateOrUpdateMaterial("Assets/Materials/Mat_Goal_Posts.mat", litShader, new Color(0.95f, 0.95f, 0.95f), 0.7f, 0.6f);
            // Goal Net
            CreateOrUpdateMaterial("Assets/Materials/Mat_Goal_Net.mat", litShader, new Color(0.85f, 0.85f, 0.85f, 0.6f), 0.1f, 0.0f);

            // Balls
            CreateOrUpdateMaterial("Assets/Materials/Mat_Ball_Classic.mat", litShader, new Color(0.95f, 0.95f, 0.95f), 0.55f, 0.1f);
            CreateOrUpdateMaterial("Assets/Materials/Mat_Ball_Blue.mat", litShader, new Color(0.05f, 0.65f, 1.0f), 0.8f, 0.4f);
            CreateOrUpdateMaterial("Assets/Materials/Mat_Ball_Fire.mat", litShader, new Color(1.0f, 0.35f, 0.05f), 0.75f, 0.2f);
            CreateOrUpdateMaterial("Assets/Materials/Mat_Ball_Rainbow.mat", litShader, new Color(0.85f, 0.25f, 0.85f), 0.85f, 0.3f);
            CreateOrUpdateMaterial("Assets/Materials/Mat_Ball_Gold.mat", litShader, new Color(1.0f, 0.82f, 0.15f), 0.92f, 0.88f);

            // Character Kits
            CreateOrUpdateMaterial("Assets/Materials/Mat_Player_Kit.mat", litShader, new Color(0.08f, 0.25f, 0.65f), 0.3f, 0.0f);
            CreateOrUpdateMaterial("Assets/Materials/Mat_Goalkeeper_Kit.mat", litShader, new Color(0.95f, 0.45f, 0.05f), 0.3f, 0.0f);
            CreateOrUpdateMaterial("Assets/Materials/Mat_Skin.mat", litShader, new Color(0.92f, 0.75f, 0.62f), 0.2f, 0.0f);

            // Stadium Elements
            CreateOrUpdateMaterial("Assets/Materials/Mat_Stadium_Stands.mat", litShader, new Color(0.15f, 0.2f, 0.3f), 0.2f, 0.1f);
            CreateOrUpdateMaterial("Assets/Materials/Mat_Stadium_Banners.mat", litShader, new Color(0.05f, 0.15f, 0.45f), 0.6f, 0.2f);
            CreateOrUpdateMaterial("Assets/Materials/Mat_GoldTrophy.mat", litShader, new Color(1.0f, 0.85f, 0.2f), 0.95f, 0.95f);
        }

        private static Material CreateOrUpdateMaterial(string path, Shader shader, Color color, float smoothness, float metallic)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            EditorUtility.SetDirty(mat);
            return mat;
        }
        #endregion

        #region 3. ScriptableObjects Generation
        public static void CreateAllScriptableObjects()
        {
            // --- 5 Rounds ---
            CreateRoundSO("Assets/ScriptableObjects/Rounds/Round1_Cantera.asset", 1, "Cantera",
                "Tutorial inicial. Aprende el apuntado y la potencia frente a un portero novato.",
                3, 5, 500, 300, StadiumType.Cantera, "Portero Básico", 0.38f, 3.5f, 4.0f, 0.35f);

            CreateRoundSO("Assets/ScriptableObjects/Rounds/Round2_LigaLocal.asset", 2, "Liga Local",
                "Segunda ronda. El portero adquiere mayor agilidad y cubre mejor los postes.",
                3, 5, 500, 400, StadiumType.Cantera, "Portero Intermedio", 0.30f, 4.5f, 5.5f, 0.25f);

            CreateRoundSO("Assets/ScriptableObjects/Rounds/Round3_LigaProfesional.asset", 3, "Liga Profesional",
                "Tercera ronda. Estadio profesional. Necesitas anotar al menos 4 de 5 goles.",
                4, 5, 500, 500, StadiumType.Professional, "Portero Avanzado", 0.22f, 6.0f, 7.5f, 0.15f);

            CreateRoundSO("Assets/ScriptableObjects/Rounds/Round4_Semifinal.asset", 4, "Semifinal",
                "Ambiente de alta tensión. Malágon reacciona velozmente a disparos bombeados.",
                4, 5, 500, 750, StadiumType.Night, "Portero Difícil", 0.16f, 7.5f, 9.5f, 0.08f);

            CreateRoundSO("Assets/ScriptableObjects/Rounds/Round5_GranFinal.asset", 5, "Gran Final",
                "¡El campeonato está en juego! Solo los disparos colocados a la escuadra vencerán al portero campeón.",
                4, 5, 500, 1000, StadiumType.WorldCup, "Portero Campeón", 0.10f, 9.0f, 12.0f, 0.02f);

            // --- 5 Ball Items ---
            CreateBallSO("Assets/ScriptableObjects/Items/Ball_Classic.asset", BallType.Classic, "Balón Clásico",
                "Balón tradicional de fútbol con paneles pentagonales.", 0, Color.white, Color.black, Color.cyan, "Assets/Materials/Mat_Ball_Classic.mat");

            CreateBallSO("Assets/ScriptableObjects/Items/Ball_Blue.asset", BallType.Blue, "Balón Azul",
                "Balón con tonos eléctricos y estela cian brillante.", 500, new Color(0.1f, 0.6f, 1f), Color.blue, Color.cyan, "Assets/Materials/Mat_Ball_Blue.mat");

            CreateBallSO("Assets/ScriptableObjects/Items/Ball_Fire.asset", BallType.Fire, "Balón de Fuego",
                "En llamas ardientes con estela de fuego viva.", 1000, new Color(1f, 0.4f, 0f), Color.red, new Color(1f, 0.3f, 0f), "Assets/Materials/Mat_Ball_Fire.mat");

            CreateBallSO("Assets/ScriptableObjects/Items/Ball_Rainbow.asset", BallType.Rainbow, "Balón Arcoíris",
                "Espectro cromático con estela iridiscente mágica.", 2000, new Color(0.8f, 0.2f, 0.9f), Color.yellow, Color.magenta, "Assets/Materials/Mat_Ball_Rainbow.mat");

            CreateBallSO("Assets/ScriptableObjects/Items/Ball_Gold.asset", BallType.Gold, "Balón Dorado",
                "Edición de oro puro para campeones legendarios.", 5000, new Color(1f, 0.85f, 0.2f), Color.black, new Color(1f, 0.9f, 0.3f), "Assets/Materials/Mat_Ball_Gold.mat");

            // --- 4 Stadium Items ---
            CreateStadiumSO("Assets/ScriptableObjects/Items/Stadium_Cantera.asset", StadiumType.Cantera, "Estadio Cantera",
                "Cancha tradicional a plena luz del día.", 0, new Color(0.35f, 0.6f, 0.9f), 1.2f, "Assets/Materials/Mat_Grass_Cantera.mat");

            CreateStadiumSO("Assets/ScriptableObjects/Items/Stadium_Night.asset", StadiumType.Night, "Estadio Nocturno",
                "Atmósfera nocturna iluminada por reflectores de alta intensidad.", 2000, new Color(0.08f, 0.12f, 0.25f), 0.9f, "Assets/Materials/Mat_Grass_Night.mat");

            CreateStadiumSO("Assets/ScriptableObjects/Items/Stadium_Professional.asset", StadiumType.Professional, "Estadio Profesional",
                "Arena deportiva moderna con gradas de alta capacidad.", 4000, new Color(0.2f, 0.45f, 0.75f), 1.3f, "Assets/Materials/Mat_Grass_Pro.mat");

            CreateStadiumSO("Assets/ScriptableObjects/Items/Stadium_WorldCup.asset", StadiumType.WorldCup, "Estadio Mundial",
                "El majestuoso estadio de la Gran Final de Ultimate Penalty Cup.", 7500, new Color(0.12f, 0.22f, 0.48f), 1.4f, "Assets/Materials/Mat_Grass_WorldCup.mat");

            // --- 7 Achievements ---
            CreateAchievementSO("Assets/ScriptableObjects/Items/Ach_FirstGoal.asset", AchievementManager.ID_FIRST_GOAL,
                "Primer Gol", "Marca tu primer gol en cualquier ronda.", 100, 100);

            CreateAchievementSO("Assets/ScriptableObjects/Items/Ach_Sniper10.asset", AchievementManager.ID_SNIPER_10,
                "Francotirador", "Marca 10 goles en total a lo largo de tu carrera.", 300, 250);

            CreateAchievementSO("Assets/ScriptableObjects/Items/Ach_PerfectStreak.asset", AchievementManager.ID_PERFECT_STREAK,
                "Racha Perfecta", "Marca 5 goles consecutivos sin fallar en una misma ronda.", 500, 500);

            CreateAchievementSO("Assets/ScriptableObjects/Items/Ach_Champion.asset", AchievementManager.ID_CHAMPION,
                "Campeón", "Supera todas las 5 rondas y gana la Gran Final.", 1500, 1000);

            CreateAchievementSO("Assets/ScriptableObjects/Items/Ach_Millionaire.asset", AchievementManager.ID_MILLIONAIRE,
                "Millonario", "Acumula 10,000 monedas en tu balance.", 2000, 500);

            CreateAchievementSO("Assets/ScriptableObjects/Items/Ach_Collector.asset", AchievementManager.ID_COLLECTOR,
                "Coleccionista", "Compra todos los balones disponibles en la tienda.", 2500, 1000);

            CreateAchievementSO("Assets/ScriptableObjects/Items/Ach_Legend.asset", AchievementManager.ID_LEGEND,
                "Leyenda", "Completa la Gran Final con más de 1,500 puntos.", 3000, 2000);
        }

        private static void CreateRoundSO(string path, int number, string name, string desc, int reqGoals, int maxAtt, int winPts, int winCoins, StadiumType stadium, string keeperTitle, float reaction, float speed, float dive, float error)
        {
            RoundDataSO so = AssetDatabase.LoadAssetAtPath<RoundDataSO>(path);
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<RoundDataSO>();
                AssetDatabase.CreateAsset(so, path);
            }
            so.roundNumber = number;
            so.roundName = name;
            so.description = desc;
            so.requiredGoals = reqGoals;
            so.maxAttempts = maxAtt;
            so.winPoints = winPts;
            so.winCoins = winCoins;
            so.stadiumTheme = stadium;
            so.goalkeeperName = "Malágon";
            so.goalkeeperTypeTitle = keeperTitle;
            so.reactionDelay = reaction;
            so.moveSpeed = speed;
            so.diveSpeed = dive;
            so.errorProbability = error;
            EditorUtility.SetDirty(so);
        }

        private static void CreateBallSO(string path, BallType type, string name, string desc, int price, Color prim, Color sec, Color trail, string matPath)
        {
            BallItemSO so = AssetDatabase.LoadAssetAtPath<BallItemSO>(path);
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<BallItemSO>();
                AssetDatabase.CreateAsset(so, path);
            }
            so.ballType = type;
            so.displayName = name;
            so.description = desc;
            so.price = price;
            so.primaryColor = prim;
            so.secondaryColor = sec;
            so.trailColor = trail;
            so.material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            EditorUtility.SetDirty(so);
        }

        private static void CreateStadiumSO(string path, StadiumType type, string name, string desc, int price, Color sky, float intensity, string grassMatPath)
        {
            StadiumItemSO so = AssetDatabase.LoadAssetAtPath<StadiumItemSO>(path);
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<StadiumItemSO>();
                AssetDatabase.CreateAsset(so, path);
            }
            so.stadiumType = type;
            so.displayName = name;
            so.description = desc;
            so.price = price;
            so.skyColor = sky;
            so.directionalLightIntensity = intensity;
            so.grassMaterial = AssetDatabase.LoadAssetAtPath<Material>(grassMatPath);
            EditorUtility.SetDirty(so);
        }

        private static void CreateAchievementSO(string path, string id, string title, string desc, int coins, int pts)
        {
            AchievementSO so = AssetDatabase.LoadAssetAtPath<AchievementSO>(path);
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<AchievementSO>();
                AssetDatabase.CreateAsset(so, path);
            }
            so.id = id;
            so.title = title;
            so.description = desc;
            so.rewardCoins = coins;
            so.rewardPoints = pts;
            EditorUtility.SetDirty(so);
        }
        #endregion

        #region 4. Prefabs Generation
        public static void CreateAllPrefabs()
        {
            CreateGoalPrefab();
            CreateBallPrefab();
            CreatePlayerPrefab();
            CreateGoalkeeperPrefab();
            CreateStadiumPrefab();
            CreateTrophyPrefab();
        }

        private static void CreateGoalPrefab()
        {
            string prefabPath = "Assets/Prefabs/GoalPrefab.prefab";
            GameObject goalRoot = new GameObject("Goal_Regulation");

            Material matPost = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Goal_Posts.mat");
            Material matNet = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Goal_Net.mat");

            // Standard FIFA Goal dimensions: 7.32m wide, 2.44m high, 1.8m deep
            float postRadius = 0.08f;
            float halfWidth = 3.66f;
            float height = 2.44f;
            float depth = 1.8f;

            // Left Post
            GameObject leftPost = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leftPost.name = "Post_Left";
            leftPost.transform.SetParent(goalRoot.transform);
            leftPost.transform.localPosition = new Vector3(-halfWidth, height * 0.5f, 0f);
            leftPost.transform.localScale = new Vector3(postRadius * 2f, height * 0.5f, postRadius * 2f);
            leftPost.GetComponent<Renderer>().material = matPost;
            leftPost.tag = "Post";

            // Right Post
            GameObject rightPost = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rightPost.name = "Post_Right";
            rightPost.transform.SetParent(goalRoot.transform);
            rightPost.transform.localPosition = new Vector3(halfWidth, height * 0.5f, 0f);
            rightPost.transform.localScale = new Vector3(postRadius * 2f, height * 0.5f, postRadius * 2f);
            rightPost.GetComponent<Renderer>().material = matPost;
            rightPost.tag = "Post";

            // Crossbar
            GameObject crossbar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crossbar.name = "Crossbar";
            crossbar.transform.SetParent(goalRoot.transform);
            crossbar.transform.localPosition = new Vector3(0f, height, 0f);
            crossbar.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            crossbar.transform.localScale = new Vector3(postRadius * 2f, halfWidth, postRadius * 2f);
            crossbar.GetComponent<Renderer>().material = matPost;
            crossbar.tag = "Post";

            // Net Box / Back structure
            GameObject netBack = GameObject.CreatePrimitive(PrimitiveType.Cube);
            netBack.name = "Net_Back";
            netBack.transform.SetParent(goalRoot.transform);
            netBack.transform.localPosition = new Vector3(0f, height * 0.5f, depth);
            netBack.transform.localScale = new Vector3(halfWidth * 2f, height, 0.05f);
            netBack.GetComponent<Renderer>().material = matNet;

            GameObject netTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            netTop.name = "Net_Top";
            netTop.transform.SetParent(goalRoot.transform);
            netTop.transform.localPosition = new Vector3(0f, height, depth * 0.5f);
            netTop.transform.localScale = new Vector3(halfWidth * 2f, 0.05f, depth);
            netTop.GetComponent<Renderer>().material = matNet;

            // Trigger Detector
            GameObject detector = new GameObject("GoalTriggerDetector");
            detector.transform.SetParent(goalRoot.transform);
            detector.transform.localPosition = new Vector3(0f, height * 0.5f, depth * 0.45f);
            BoxCollider col = detector.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(halfWidth * 2f - 0.2f, height - 0.1f, depth * 0.7f);
            detector.AddComponent<GoalDetector>();

            PrefabUtility.SaveAsPrefabAsset(goalRoot, prefabPath);
            GameObject.DestroyImmediate(goalRoot);
        }

        private static void CreateBallPrefab()
        {
            string prefabPath = "Assets/Prefabs/BallPrefab.prefab";
            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "Ball_3D";
            ball.transform.localScale = Vector3.one * 0.32f; // FIFA size 5 ball ~22cm dia + visual scale

            Material matClassic = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Ball_Classic.mat");
            Material matBlue = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Ball_Blue.mat");
            Material matFire = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Ball_Fire.mat");
            Material matRainbow = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Ball_Rainbow.mat");
            Material matGold = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Ball_Gold.mat");

            ball.GetComponent<Renderer>().material = matClassic;

            TrailRenderer trail = ball.AddComponent<TrailRenderer>();
            trail.time = 0.35f;
            trail.startWidth = 0.22f;
            trail.endWidth = 0.02f;
            trail.emitting = false;
            trail.material = matClassic;

            BallController bc = ball.AddComponent<BallController>();
            bc.SetCustomMaterials(matClassic, matBlue, matFire, matRainbow, matGold);

            PrefabUtility.SaveAsPrefabAsset(ball, prefabPath);
            GameObject.DestroyImmediate(ball);
        }

        private static void CreatePlayerPrefab()
        {
            string prefabPath = "Assets/Prefabs/Player_RubenGonzalez.prefab";
            GameObject player = new GameObject("Player_RubenGonzalez");

            Material matKit = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Player_Kit.mat");
            Material matSkin = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Skin.mat");
            Material matPosts = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Goal_Posts.mat");
            Material matBanners = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Stadium_Banners.mat");

            GameObject visuals = new GameObject("Visuals");
            visuals.transform.SetParent(player.transform, false);

            // 1. Torso (Jersey Rubén #10)
            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            torso.name = "Torso";
            torso.transform.SetParent(visuals.transform, false);
            torso.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            torso.transform.localScale = new Vector3(0.52f, 0.42f, 0.32f);
            torso.GetComponent<Renderer>().material = matKit;
            GameObject.DestroyImmediate(torso.GetComponent<Collider>());

            // 2. Head with Facial Features
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(visuals.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.80f, 0f);
            head.transform.localScale = new Vector3(0.32f, 0.34f, 0.32f);
            head.GetComponent<Renderer>().material = matSkin;
            GameObject.DestroyImmediate(head.GetComponent<Collider>());

            // Stylized 3D Hair (Arcade faux-hawk / athletic cut)
            GameObject hair = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            hair.name = "Hair_Stylized";
            hair.transform.SetParent(head.transform, false);
            hair.transform.localPosition = new Vector3(0f, 0.22f, -0.04f);
            hair.transform.localRotation = Quaternion.Euler(75f, 0f, 0f);
            hair.transform.localScale = new Vector3(0.75f, 0.55f, 0.65f);
            hair.GetComponent<Renderer>().material = matBanners; // Dark athletic color
            GameObject.DestroyImmediate(hair.GetComponent<Collider>());

            // 3. Left Arm (Upper arm + Forearm + Hand)
            GameObject armL = new GameObject("Arm_Left");
            armL.transform.SetParent(visuals.transform, false);
            armL.transform.localPosition = new Vector3(-0.35f, 1.45f, 0f);

            GameObject armL_Upper = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            armL_Upper.name = "Sleeve";
            armL_Upper.transform.SetParent(armL.transform, false);
            armL_Upper.transform.localPosition = new Vector3(-0.08f, -0.15f, 0f);
            armL_Upper.transform.localRotation = Quaternion.Euler(0f, 0f, 15f);
            armL_Upper.transform.localScale = new Vector3(0.14f, 0.16f, 0.14f);
            armL_Upper.GetComponent<Renderer>().material = matKit;
            GameObject.DestroyImmediate(armL_Upper.GetComponent<Collider>());

            GameObject armL_Fore = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            armL_Fore.name = "Forearm";
            armL_Fore.transform.SetParent(armL.transform, false);
            armL_Fore.transform.localPosition = new Vector3(-0.14f, -0.38f, 0.05f);
            armL_Fore.transform.localRotation = Quaternion.Euler(-15f, 0f, 20f);
            armL_Fore.transform.localScale = new Vector3(0.12f, 0.15f, 0.12f);
            armL_Fore.GetComponent<Renderer>().material = matSkin;
            GameObject.DestroyImmediate(armL_Fore.GetComponent<Collider>());

            GameObject handL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            handL.name = "Hand";
            handL.transform.SetParent(armL.transform, false);
            handL.transform.localPosition = new Vector3(-0.18f, -0.56f, 0.1f);
            handL.transform.localScale = Vector3.one * 0.14f;
            handL.GetComponent<Renderer>().material = matSkin;
            GameObject.DestroyImmediate(handL.GetComponent<Collider>());

            // 4. Right Arm (Upper arm + Forearm + Hand)
            GameObject armR = new GameObject("Arm_Right");
            armR.transform.SetParent(visuals.transform, false);
            armR.transform.localPosition = new Vector3(0.35f, 1.45f, 0f);

            GameObject armR_Upper = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            armR_Upper.name = "Sleeve";
            armR_Upper.transform.SetParent(armR.transform, false);
            armR_Upper.transform.localPosition = new Vector3(0.08f, -0.15f, 0f);
            armR_Upper.transform.localRotation = Quaternion.Euler(0f, 0f, -15f);
            armR_Upper.transform.localScale = new Vector3(0.14f, 0.16f, 0.14f);
            armR_Upper.GetComponent<Renderer>().material = matKit;
            GameObject.DestroyImmediate(armR_Upper.GetComponent<Collider>());

            GameObject armR_Fore = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            armR_Fore.name = "Forearm";
            armR_Fore.transform.SetParent(armR.transform, false);
            armR_Fore.transform.localPosition = new Vector3(0.14f, -0.38f, 0.05f);
            armR_Fore.transform.localRotation = Quaternion.Euler(-15f, 0f, -20f);
            armR_Fore.transform.localScale = new Vector3(0.12f, 0.15f, 0.12f);
            armR_Fore.GetComponent<Renderer>().material = matSkin;
            GameObject.DestroyImmediate(armR_Fore.GetComponent<Collider>());

            GameObject handR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            handR.name = "Hand";
            handR.transform.SetParent(armR.transform, false);
            handR.transform.localPosition = new Vector3(0.18f, -0.56f, 0.1f);
            handR.transform.localScale = Vector3.one * 0.14f;
            handR.GetComponent<Renderer>().material = matSkin;
            GameObject.DestroyImmediate(handR.GetComponent<Collider>());

            // 5. Shorts (Hips)
            GameObject shorts = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shorts.name = "Shorts";
            shorts.transform.SetParent(visuals.transform, false);
            shorts.transform.localPosition = new Vector3(0f, 0.88f, 0f);
            shorts.transform.localScale = new Vector3(0.48f, 0.28f, 0.32f);
            shorts.GetComponent<Renderer>().material = matKit;
            GameObject.DestroyImmediate(shorts.GetComponent<Collider>());

            // 6. Left Leg (Thigh + Sock + Cleat Boot)
            GameObject legL = new GameObject("Leg_Left");
            legL.transform.SetParent(visuals.transform, false);
            legL.transform.localPosition = new Vector3(-0.16f, 0.78f, 0f);

            GameObject thighL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            thighL.name = "Thigh";
            thighL.transform.SetParent(legL.transform, false);
            thighL.transform.localPosition = new Vector3(0f, -0.18f, 0f);
            thighL.transform.localScale = new Vector3(0.17f, 0.18f, 0.17f);
            thighL.GetComponent<Renderer>().material = matSkin;
            GameObject.DestroyImmediate(thighL.GetComponent<Collider>());

            GameObject shinL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shinL.name = "Shin_Sock";
            shinL.transform.SetParent(legL.transform, false);
            shinL.transform.localPosition = new Vector3(0f, -0.52f, 0f);
            shinL.transform.localScale = new Vector3(0.15f, 0.20f, 0.15f);
            shinL.GetComponent<Renderer>().material = matKit; // Sock
            GameObject.DestroyImmediate(shinL.GetComponent<Collider>());

            GameObject bootL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bootL.name = "Cleat_Boot";
            bootL.transform.SetParent(legL.transform, false);
            bootL.transform.localPosition = new Vector3(0f, -0.74f, 0.08f);
            bootL.transform.localScale = new Vector3(0.16f, 0.10f, 0.30f);
            bootL.GetComponent<Renderer>().material = matBanners; // Dark boot
            GameObject.DestroyImmediate(bootL.GetComponent<Collider>());

            // 7. Right Leg (Kicking Leg)
            GameObject legR = new GameObject("Leg_Right");
            legR.transform.SetParent(visuals.transform, false);
            legR.transform.localPosition = new Vector3(0.16f, 0.78f, 0f);

            GameObject thighR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            thighR.name = "Thigh";
            thighR.transform.SetParent(legR.transform, false);
            thighR.transform.localPosition = new Vector3(0f, -0.18f, 0f);
            thighR.transform.localScale = new Vector3(0.17f, 0.18f, 0.17f);
            thighR.GetComponent<Renderer>().material = matSkin;
            GameObject.DestroyImmediate(thighR.GetComponent<Collider>());

            GameObject shinR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shinR.name = "Shin_Sock";
            shinR.transform.SetParent(legR.transform, false);
            shinR.transform.localPosition = new Vector3(0f, -0.52f, 0f);
            shinR.transform.localScale = new Vector3(0.15f, 0.20f, 0.15f);
            shinR.GetComponent<Renderer>().material = matKit; // Sock
            GameObject.DestroyImmediate(shinR.GetComponent<Collider>());

            GameObject bootR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bootR.name = "Cleat_Boot";
            bootR.transform.SetParent(legR.transform, false);
            bootR.transform.localPosition = new Vector3(0f, -0.74f, 0.08f);
            bootR.transform.localScale = new Vector3(0.16f, 0.10f, 0.30f);
            bootR.GetComponent<Renderer>().material = matBanners; // Dark boot
            GameObject.DestroyImmediate(bootR.GetComponent<Collider>());

            // Components
            PlayerController pc = player.AddComponent<PlayerController>();
            PlayerShooter ps = player.AddComponent<PlayerShooter>();

            // Aim Reticle
            GameObject reticle = GameObject.CreatePrimitive(PrimitiveType.Quad);
            reticle.name = "AimReticle_Indicator";
            reticle.transform.SetParent(player.transform, false);
            reticle.transform.localScale = Vector3.one * 0.75f;
            reticle.GetComponent<Renderer>().material = matPosts;
            GameObject.DestroyImmediate(reticle.GetComponent<Collider>());

            // Trajectory Line
            GameObject lineObj = new GameObject("TrajectoryGuideline");
            lineObj.transform.SetParent(player.transform, false);
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = 0.06f;
            lr.endWidth = 0.02f;
            lr.material = matPosts;
            lr.startColor = new Color(0.1f, 0.85f, 1f, 0.85f);
            lr.endColor = new Color(1f, 0.9f, 0.2f, 0.4f);

            ps.SetAimReticle(reticle.transform, lr);

            PrefabUtility.SaveAsPrefabAsset(player, prefabPath);
            GameObject.DestroyImmediate(player);
        }

        private static void CreateGoalkeeperPrefab()
        {
            string prefabPath = "Assets/Prefabs/Goalkeeper_Malagon.prefab";
            GameObject gk = new GameObject("Goalkeeper_Malagon");
            gk.tag = "Goalkeeper";

            Material matGkKit = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Goalkeeper_Kit.mat");
            Material matSkin = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Skin.mat");
            Material matBanners = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Stadium_Banners.mat");

            GameObject visuals = new GameObject("Visuals");
            visuals.transform.SetParent(gk.transform, false);

            // 1. Torso (Goalkeeper Kit)
            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            torso.name = "Torso";
            torso.tag = "Goalkeeper";
            torso.transform.SetParent(visuals.transform, false);
            torso.transform.localPosition = new Vector3(0f, 1.30f, 0f);
            torso.transform.localScale = new Vector3(0.58f, 0.46f, 0.36f);
            torso.GetComponent<Renderer>().material = matGkKit;

            // 2. Head with Focused Expression
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.tag = "Goalkeeper";
            head.transform.SetParent(visuals.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.88f, 0f);
            head.transform.localScale = new Vector3(0.34f, 0.36f, 0.34f);
            head.GetComponent<Renderer>().material = matSkin;

            // Goalkeeper Hair
            GameObject hair = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hair.name = "Hair_GK";
            hair.transform.SetParent(head.transform, false);
            hair.transform.localPosition = new Vector3(0f, 0.14f, -0.06f);
            hair.transform.localScale = new Vector3(1.05f, 0.8f, 1.05f);
            hair.GetComponent<Renderer>().material = matBanners;
            GameObject.DestroyImmediate(hair.GetComponent<Collider>());

            // 3. Left Arm with Large Goalkeeper Glove
            GameObject armL = new GameObject("Arm_Left");
            armL.transform.SetParent(visuals.transform, false);
            armL.transform.localPosition = new Vector3(-0.38f, 1.50f, 0f);

            GameObject sleeveL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            sleeveL.name = "Sleeve";
            sleeveL.tag = "Goalkeeper";
            sleeveL.transform.SetParent(armL.transform, false);
            sleeveL.transform.localPosition = new Vector3(-0.16f, -0.18f, 0.05f);
            sleeveL.transform.localRotation = Quaternion.Euler(0f, 0f, 35f);
            sleeveL.transform.localScale = new Vector3(0.16f, 0.24f, 0.16f);
            sleeveL.GetComponent<Renderer>().material = matGkKit;

            GameObject gloveL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gloveL.name = "GoalkeeperGlove_Left";
            gloveL.tag = "Goalkeeper";
            gloveL.transform.SetParent(armL.transform, false);
            gloveL.transform.localPosition = new Vector3(-0.35f, -0.38f, 0.12f);
            gloveL.transform.localScale = new Vector3(0.24f, 0.30f, 0.16f);
            gloveL.GetComponent<Renderer>().material = matSkin; // Latex glove grip

            // 4. Right Arm with Large Goalkeeper Glove
            GameObject armR = new GameObject("Arm_Right");
            armR.transform.SetParent(visuals.transform, false);
            armR.transform.localPosition = new Vector3(0.38f, 1.50f, 0f);

            GameObject sleeveR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            sleeveR.name = "Sleeve";
            sleeveR.tag = "Goalkeeper";
            sleeveR.transform.SetParent(armR.transform, false);
            sleeveR.transform.localPosition = new Vector3(0.16f, -0.18f, 0.05f);
            sleeveR.transform.localRotation = Quaternion.Euler(0f, 0f, -35f);
            sleeveR.transform.localScale = new Vector3(0.16f, 0.24f, 0.16f);
            sleeveR.GetComponent<Renderer>().material = matGkKit;

            GameObject gloveR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gloveR.name = "GoalkeeperGlove_Right";
            gloveR.tag = "Goalkeeper";
            gloveR.transform.SetParent(armR.transform, false);
            gloveR.transform.localPosition = new Vector3(0.35f, -0.38f, 0.12f);
            gloveR.transform.localScale = new Vector3(0.24f, 0.30f, 0.16f);
            gloveR.GetComponent<Renderer>().material = matSkin; // Latex glove grip

            // 5. Goalkeeper Shorts & Legs
            GameObject legL = new GameObject("Leg_Left");
            legL.transform.SetParent(visuals.transform, false);
            legL.transform.localPosition = new Vector3(-0.18f, 0.85f, 0f);

            GameObject legColL = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            legColL.name = "LegMesh";
            legColL.tag = "Goalkeeper";
            legColL.transform.SetParent(legL.transform, false);
            legColL.transform.localPosition = new Vector3(0f, -0.42f, 0f);
            legColL.transform.localScale = new Vector3(0.22f, 0.44f, 0.22f);
            legColL.GetComponent<Renderer>().material = matGkKit;

            GameObject legR = new GameObject("Leg_Right");
            legR.transform.SetParent(visuals.transform, false);
            legR.transform.localPosition = new Vector3(0.18f, 0.85f, 0f);

            GameObject legColR = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            legColR.name = "LegMesh";
            legColR.tag = "Goalkeeper";
            legColR.transform.SetParent(legR.transform, false);
            legColR.transform.localPosition = new Vector3(0f, -0.42f, 0f);
            legColR.transform.localScale = new Vector3(0.22f, 0.44f, 0.22f);
            legColR.GetComponent<Renderer>().material = matGkKit;

            gk.AddComponent<GoalkeeperAI>();

            PrefabUtility.SaveAsPrefabAsset(gk, prefabPath);
            GameObject.DestroyImmediate(gk);
        }

        private static void CreateStadiumPrefab()
        {
            string prefabPath = "Assets/Prefabs/StadiumEnvironment.prefab";
            GameObject stadium = new GameObject("StadiumEnvironment");

            Material matGrass = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Grass_Pro.mat");
            Material matLines = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Lines_White.mat");
            Material matStands = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Stadium_Stands.mat");
            Material matBanners = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Stadium_Banners.mat");

            // Pitch Ground
            GameObject pitch = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pitch.name = "Pitch_Grass";
            pitch.transform.SetParent(stadium.transform);
            pitch.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            pitch.transform.localScale = new Vector3(75f, 0.1f, 105f);
            pitch.GetComponent<Renderer>().material = matGrass;

            // White lines
            // Penalty Box (16.5m x 40.3m)
            CreateLinePlane("Line_PenaltyBox_Back", stadium.transform, new Vector3(0f, 0.005f, -16.5f), new Vector3(40.3f, 1f, 0.12f), matLines);
            CreateLinePlane("Line_PenaltyBox_Left", stadium.transform, new Vector3(-20.15f, 0.005f, -8.25f), new Vector3(0.12f, 1f, 16.5f), matLines);
            CreateLinePlane("Line_PenaltyBox_Right", stadium.transform, new Vector3(20.15f, 0.005f, -8.25f), new Vector3(0.12f, 1f, 16.5f), matLines);
            // Goal Line
            CreateLinePlane("Line_GoalLine", stadium.transform, new Vector3(0f, 0.005f, 0f), new Vector3(50f, 1f, 0.12f), matLines);
            // Penalty Spot (11 meters from goal line)
            GameObject spot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spot.name = "PenaltySpot";
            spot.transform.SetParent(stadium.transform);
            spot.transform.localPosition = new Vector3(0f, 0.006f, -11f);
            spot.transform.localScale = new Vector3(0.35f, 0.005f, 0.35f);
            spot.GetComponent<Renderer>().material = matLines;
            GameObject.DestroyImmediate(spot.GetComponent<Collider>());

            // Grandstands (Behind goal, left, right)
            CreateStand("Stand_BehindGoal", stadium.transform, new Vector3(0f, 4.5f, 25f), new Vector3(80f, 9f, 20f), matStands);
            CreateStand("Stand_KickerSide", stadium.transform, new Vector3(0f, 4.5f, -50f), new Vector3(80f, 9f, 20f), matStands);
            CreateStand("Stand_SideLeft", stadium.transform, new Vector3(-45f, 4.5f, -10f), new Vector3(20f, 9f, 90f), matStands);
            CreateStand("Stand_SideRight", stadium.transform, new Vector3(45f, 4.5f, -10f), new Vector3(20f, 9f, 90f), matStands);

            // Perimeter Advertising Boards (LED style)
            CreateLinePlane("Banner_BehindGoal", stadium.transform, new Vector3(0f, 0.6f, 6.5f), new Vector3(40f, 1.2f, 0.2f), matBanners);
            CreateLinePlane("Banner_Left", stadium.transform, new Vector3(-25f, 0.6f, -5f), new Vector3(0.2f, 1.2f, 25f), matBanners);
            CreateLinePlane("Banner_Right", stadium.transform, new Vector3(25f, 0.6f, -5f), new Vector3(0.2f, 1.2f, 25f), matBanners);

            // 4 Floodlights
            CreateFloodlight("Light_Tower_FL", stadium.transform, new Vector3(-35f, 16f, 15f), new Vector3(50f, 45f, 0f));
            CreateFloodlight("Light_Tower_FR", stadium.transform, new Vector3(35f, 16f, 15f), new Vector3(50f, -45f, 0f));
            CreateFloodlight("Light_Tower_BL", stadium.transform, new Vector3(-35f, 16f, -35f), new Vector3(50f, 135f, 0f));
            CreateFloodlight("Light_Tower_BR", stadium.transform, new Vector3(35f, 16f, -35f), new Vector3(50f, -135f, 0f));

            PrefabUtility.SaveAsPrefabAsset(stadium, prefabPath);
            GameObject.DestroyImmediate(stadium);
        }

        private static void CreateLinePlane(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            quad.name = name;
            quad.transform.SetParent(parent);
            quad.transform.localPosition = pos;
            quad.transform.localScale = scale;
            quad.GetComponent<Renderer>().material = mat;
            GameObject.DestroyImmediate(quad.GetComponent<Collider>());
        }

        private static void CreateStand(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject stand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stand.name = name;
            stand.transform.SetParent(parent);
            stand.transform.localPosition = pos;
            stand.transform.localScale = scale;
            stand.GetComponent<Renderer>().material = mat;
        }

        private static void CreateFloodlight(string name, Transform parent, Vector3 pos, Vector3 rotation)
        {
            GameObject tower = new GameObject(name);
            tower.transform.SetParent(parent);
            tower.transform.localPosition = pos;
            tower.transform.localRotation = Quaternion.Euler(rotation);

            Light light = tower.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = new Color(0.95f, 0.98f, 1.0f);
            light.intensity = 3.5f;
            light.range = 55f;
            light.spotAngle = 65f;
        }

        private static void CreateTrophyPrefab()
        {
            string prefabPath = "Assets/Prefabs/Trophy_Cup.prefab";
            GameObject trophy = new GameObject("Trophy_UltimatePenaltyCup");

            Material matGold = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_GoldTrophy.mat");

            // Base
            GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseObj.transform.SetParent(trophy.transform);
            baseObj.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            baseObj.transform.localScale = new Vector3(0.7f, 0.15f, 0.7f);
            baseObj.GetComponent<Renderer>().material = matGold;

            // Stem
            GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stem.transform.SetParent(trophy.transform);
            stem.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            stem.transform.localScale = new Vector3(0.25f, 0.35f, 0.25f);
            stem.GetComponent<Renderer>().material = matGold;

            // Cup Body
            GameObject cup = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cup.transform.SetParent(trophy.transform);
            cup.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            cup.transform.localScale = new Vector3(0.9f, 0.95f, 0.9f);
            cup.GetComponent<Renderer>().material = matGold;

            // Handles
            GameObject handleL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handleL.transform.SetParent(trophy.transform);
            handleL.transform.localPosition = new Vector3(-0.55f, 1.25f, 0f);
            handleL.transform.localRotation = Quaternion.Euler(0f, 0f, 25f);
            handleL.transform.localScale = new Vector3(0.08f, 0.35f, 0.08f);
            handleL.GetComponent<Renderer>().material = matGold;

            GameObject handleR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handleR.transform.SetParent(trophy.transform);
            handleR.transform.localPosition = new Vector3(0.55f, 1.25f, 0f);
            handleR.transform.localRotation = Quaternion.Euler(0f, 0f, -25f);
            handleR.transform.localScale = new Vector3(0.08f, 0.35f, 0.08f);
            handleR.GetComponent<Renderer>().material = matGold;

            PrefabUtility.SaveAsPrefabAsset(trophy, prefabPath);
            GameObject.DestroyImmediate(trophy);
        }
        #endregion

        #region 5. Main Menu Scene Builder
        public static void BuildMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            string scenePath = "Assets/Scenes/MainMenu.unity";

            // Camera
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.1f, 0.22f); // Deep navy blue
            camObj.transform.position = new Vector3(0f, 2.5f, -10f);
            camObj.transform.rotation = Quaternion.Euler(10f, 0f, 0f);
            camObj.AddComponent<AudioListener>();

            // Directional Light
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.9f, 0.95f, 1.0f);
            light.intensity = 1.1f;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Canvas & EventSystem
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            GameObject canvasObj = new GameObject("Canvas_MainMenu");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // Persistent Core GameManager
            GameObject gmObj = new GameObject("GameManager");
            GameManager gm = gmObj.AddComponent<GameManager>();
            RoundManager rm = gmObj.AddComponent<RoundManager>();
            SaveManager sm = gmObj.AddComponent<SaveManager>();
            CurrencyManager cm = gmObj.AddComponent<CurrencyManager>();
            ScoreManager scm = gmObj.AddComponent<ScoreManager>();
            AudioManager am = gmObj.AddComponent<AudioManager>();
            ShopManager shm = gmObj.AddComponent<ShopManager>();
            CustomizationManager csm = gmObj.AddComponent<CustomizationManager>();
            AchievementManager acm = gmObj.AddComponent<AchievementManager>();

            // Hook catalogs into ShopManager
            List<BallItemSO> balls = new List<BallItemSO>
            {
                AssetDatabase.LoadAssetAtPath<BallItemSO>("Assets/ScriptableObjects/Items/Ball_Classic.asset"),
                AssetDatabase.LoadAssetAtPath<BallItemSO>("Assets/ScriptableObjects/Items/Ball_Blue.asset"),
                AssetDatabase.LoadAssetAtPath<BallItemSO>("Assets/ScriptableObjects/Items/Ball_Fire.asset"),
                AssetDatabase.LoadAssetAtPath<BallItemSO>("Assets/ScriptableObjects/Items/Ball_Rainbow.asset"),
                AssetDatabase.LoadAssetAtPath<BallItemSO>("Assets/ScriptableObjects/Items/Ball_Gold.asset")
            };
            List<StadiumItemSO> stadiums = new List<StadiumItemSO>
            {
                AssetDatabase.LoadAssetAtPath<StadiumItemSO>("Assets/ScriptableObjects/Items/Stadium_Cantera.asset"),
                AssetDatabase.LoadAssetAtPath<StadiumItemSO>("Assets/ScriptableObjects/Items/Stadium_Night.asset"),
                AssetDatabase.LoadAssetAtPath<StadiumItemSO>("Assets/ScriptableObjects/Items/Stadium_Professional.asset"),
                AssetDatabase.LoadAssetAtPath<StadiumItemSO>("Assets/ScriptableObjects/Items/Stadium_WorldCup.asset")
            };
            shm.SetCatalogs(balls, stadiums);

            List<RoundDataSO> rounds = new List<RoundDataSO>
            {
                AssetDatabase.LoadAssetAtPath<RoundDataSO>("Assets/ScriptableObjects/Rounds/Round1_Cantera.asset"),
                AssetDatabase.LoadAssetAtPath<RoundDataSO>("Assets/ScriptableObjects/Rounds/Round2_LigaLocal.asset"),
                AssetDatabase.LoadAssetAtPath<RoundDataSO>("Assets/ScriptableObjects/Rounds/Round3_LigaProfesional.asset"),
                AssetDatabase.LoadAssetAtPath<RoundDataSO>("Assets/ScriptableObjects/Rounds/Round4_Semifinal.asset"),
                AssetDatabase.LoadAssetAtPath<RoundDataSO>("Assets/ScriptableObjects/Rounds/Round5_GranFinal.asset")
            };
            rm.SetRounds(rounds);

            List<AchievementSO> achievements = new List<AchievementSO>
            {
                AssetDatabase.LoadAssetAtPath<AchievementSO>("Assets/ScriptableObjects/Items/Ach_FirstGoal.asset"),
                AssetDatabase.LoadAssetAtPath<AchievementSO>("Assets/ScriptableObjects/Items/Ach_Sniper10.asset"),
                AssetDatabase.LoadAssetAtPath<AchievementSO>("Assets/ScriptableObjects/Items/Ach_PerfectStreak.asset"),
                AssetDatabase.LoadAssetAtPath<AchievementSO>("Assets/ScriptableObjects/Items/Ach_Champion.asset"),
                AssetDatabase.LoadAssetAtPath<AchievementSO>("Assets/ScriptableObjects/Items/Ach_Millionaire.asset"),
                AssetDatabase.LoadAssetAtPath<AchievementSO>("Assets/ScriptableObjects/Items/Ach_Collector.asset"),
                AssetDatabase.LoadAssetAtPath<AchievementSO>("Assets/ScriptableObjects/Items/Ach_Legend.asset")
            };
            acm.SetAchievements(achievements);

            // Build UI Panels
            MainMenuUI menuUI = canvasObj.AddComponent<MainMenuUI>();
            BuildMainMenuPanels(canvasObj.transform, menuUI, balls, stadiums, rounds, achievements);

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log("[ProjectSetupBuilder] MainMenu.unity built successfully.");
        }

        private static void BuildMainMenuPanels(Transform canvasT, MainMenuUI menuUI, List<BallItemSO> balls, List<StadiumItemSO> stadiums, List<RoundDataSO> rounds, List<AchievementSO> achievements)
        {
            Sprite logoSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Logo/UltimatePenaltyCup3D_Logo.jpg");

            // --- Panel 1: Main Menu ---
            GameObject pnlMain = CreateUIPanel("Panel_MainMenu", canvasT, new Color(0.04f, 0.08f, 0.18f, 0.95f));

            // Logo Header
            if (logoSprite != null)
            {
                GameObject logoObj = CreateUIImage("LogoImage", pnlMain.transform, new Vector2(0f, 260f), new Vector2(520f, 320f), Color.white);
                logoObj.GetComponent<Image>().sprite = logoSprite;
            }
            else
            {
                CreateUIText("TitleText", pnlMain.transform, new Vector2(0f, 320f), new Vector2(900f, 100f), "ULTIMATE PENALTY CUP 3D", 54, Color.yellow);
            }

            // Top Bar: Coins & High Score
            GameObject coinsBadge = CreateUIImage("CoinsBadge", pnlMain.transform, new Vector2(720f, 480f), new Vector2(260f, 50f), new Color(0.1f, 0.15f, 0.3f, 0.9f));
            TMP_Text txtCoins = CreateUIText("TxtCoins", coinsBadge.transform, Vector2.zero, new Vector2(240f, 45f), "0 Monedas", 24, new Color(1f, 0.85f, 0.2f));

            GameObject scoreBadge = CreateUIImage("ScoreBadge", pnlMain.transform, new Vector2(-720f, 480f), new Vector2(280f, 50f), new Color(0.1f, 0.15f, 0.3f, 0.9f));
            TMP_Text txtHighScore = CreateUIText("TxtHighScore", scoreBadge.transform, Vector2.zero, new Vector2(260f, 45f), "Récord: 0 pts", 22, Color.white);

            // 6 Menu Buttons
            float startY = 80f;
            float stepY = -70f;
            Button btnPlay = CreateUIButton("BtnPlay", pnlMain.transform, new Vector2(0f, startY + stepY * 0), new Vector2(360f, 58f), "JUGAR", new Color(0.15f, 0.6f, 0.25f));
            Button btnShop = CreateUIButton("BtnShop", pnlMain.transform, new Vector2(0f, startY + stepY * 1), new Vector2(360f, 58f), "TIENDA", new Color(0.12f, 0.35f, 0.75f));
            Button btnCustom = CreateUIButton("BtnCustom", pnlMain.transform, new Vector2(0f, startY + stepY * 2), new Vector2(360f, 58f), "PERSONALIZACIÓN", new Color(0.12f, 0.35f, 0.75f));
            Button btnAch = CreateUIButton("BtnAch", pnlMain.transform, new Vector2(0f, startY + stepY * 3), new Vector2(360f, 58f), "LOGROS", new Color(0.12f, 0.35f, 0.75f));
            Button btnSettings = CreateUIButton("BtnSettings", pnlMain.transform, new Vector2(0f, startY + stepY * 4), new Vector2(360f, 58f), "OPCIONES", new Color(0.12f, 0.35f, 0.75f));
            Button btnQuit = CreateUIButton("BtnQuit", pnlMain.transform, new Vector2(0f, startY + stepY * 5), new Vector2(360f, 58f), "SALIR", new Color(0.7f, 0.18f, 0.18f));

            // Dev Code Box at bottom
            GameObject devBox = CreateUIImage("DevCodeBox", pnlMain.transform, new Vector2(0f, -460f), new Vector2(480f, 55f), new Color(0.08f, 0.12f, 0.22f, 0.9f));
            TMP_InputField inpCode = CreateUIInputField("InpCode", devBox.transform, new Vector2(-80f, 0f), new Vector2(280f, 40f), "Código secreto...");
            Button btnSubmitCode = CreateUIButton("BtnSubmitCode", devBox.transform, new Vector2(160f, 0f), new Vector2(140f, 40f), "CANJEAR", new Color(0.9f, 0.7f, 0.15f));
            TMP_Text txtCodeFeedback = CreateUIText("TxtCodeFeedback", pnlMain.transform, new Vector2(0f, -410f), new Vector2(600f, 35f), "", 20, Color.green);
            txtCodeFeedback.gameObject.SetActive(false);

            // --- Panel 2: Round Select ---
            GameObject pnlRoundSelect = CreateUIPanel("Panel_RoundSelect", canvasT, new Color(0.05f, 0.1f, 0.2f, 0.98f));
            CreateUIText("TitleRounds", pnlRoundSelect.transform, new Vector2(0f, 440f), new Vector2(800f, 60f), "SELECCIÓN DE RONDA", 42, new Color(1f, 0.85f, 0.2f));
            RoundSelectUI roundSelectUI = pnlRoundSelect.AddComponent<RoundSelectUI>();

            List<RoundSelectUI.RoundCard> cardList = new List<RoundSelectUI.RoundCard>();
            float cardSpacingX = 350f;
            float startCardX = -2f * cardSpacingX;

            for (int i = 0; i < 5; i++)
            {
                int rNum = i + 1;
                RoundDataSO rData = rounds.Count > i ? rounds[i] : null;

                GameObject cObj = CreateUIImage($"Card_Round_{rNum}", pnlRoundSelect.transform, new Vector2(startCardX + i * cardSpacingX, 20f), new Vector2(320f, 520f), new Color(0.1f, 0.18f, 0.35f));
                TMP_Text rTitle = CreateUIText("TxtRTitle", cObj.transform, new Vector2(0f, 210f), new Vector2(290f, 50f), $"Ronda {rNum}", 28, Color.yellow);
                TMP_Text rTarget = CreateUIText("TxtRTarget", cObj.transform, new Vector2(0f, 150f), new Vector2(290f, 40f), $"Objetivo: {rData?.requiredGoals ?? 3} de 5", 22, Color.white);
                TMP_Text rDesc = CreateUIText("TxtRDesc", cObj.transform, new Vector2(0f, 40f), new Vector2(280f, 140f), rData?.description ?? "", 18, new Color(0.85f, 0.85f, 0.9f));
                TMP_Text rRewards = CreateUIText("TxtRRewards", cObj.transform, new Vector2(0f, -90f), new Vector2(280f, 45f), $"+{rData?.winCoins ?? 300} Monedas", 20, new Color(0.3f, 0.9f, 0.4f));

                Button btnPlayR = CreateUIButton("BtnPlayRound", cObj.transform, new Vector2(0f, -190f), new Vector2(240f, 55f), "JUGAR", new Color(0.15f, 0.65f, 0.25f));
                GameObject lockOverlay = CreateUIImage("LockOverlay", cObj.transform, Vector2.zero, new Vector2(320f, 520f), new Color(0f, 0f, 0f, 0.75f));
                CreateUIText("LockText", lockOverlay.transform, Vector2.zero, new Vector2(200f, 60f), "BLOQUEADO", 26, new Color(0.85f, 0.3f, 0.3f));

                cardList.Add(new RoundSelectUI.RoundCard
                {
                    roundNumber = rNum,
                    cardObject = cObj,
                    txtTitle = rTitle,
                    txtDescription = rDesc,
                    txtTarget = rTarget,
                    txtRewards = rRewards,
                    lockOverlay = lockOverlay,
                    btnPlay = btnPlayR
                });
            }

            Button btnBackRounds = CreateUIButton("BtnBackRounds", pnlRoundSelect.transform, new Vector2(-750f, 440f), new Vector2(160f, 50f), "VOLVER", new Color(0.5f, 0.5f, 0.5f));

            // Set serialized fields on RoundSelectUI
            SetSerializedField(roundSelectUI, "roundCards", cardList.ToArray());
            SetSerializedField(roundSelectUI, "btnBack", btnBackRounds);
            SetSerializedField(roundSelectUI, "mainMenuUI", menuUI);

            // --- Panel 3: Shop UI ---
            GameObject pnlShop = CreateUIPanel("Panel_Shop", canvasT, new Color(0.05f, 0.1f, 0.2f, 0.98f));
            CreateUIText("TitleShop", pnlShop.transform, new Vector2(0f, 440f), new Vector2(600f, 60f), "TIENDA OFICIAL", 42, new Color(1f, 0.85f, 0.2f));
            ShopUI shopUI = pnlShop.AddComponent<ShopUI>();

            Button btnTabBalls = CreateUIButton("BtnTabBalls", pnlShop.transform, new Vector2(-200f, 370f), new Vector2(220f, 45f), "BALONES", new Color(0.15f, 0.45f, 0.85f));
            Button btnTabStadiums = CreateUIButton("BtnTabStadiums", pnlShop.transform, new Vector2(200f, 370f), new Vector2(220f, 45f), "ESTADIOS", new Color(0.15f, 0.45f, 0.85f));

            GameObject pnlBallsList = new GameObject("Panel_BallsList");
            pnlBallsList.transform.SetParent(pnlShop.transform, false);
            GameObject pnlStadiumsList = new GameObject("Panel_StadiumsList");
            pnlStadiumsList.transform.SetParent(pnlShop.transform, false);

            float shopItemSpacing = 310f;
            float shopStartX = -2f * shopItemSpacing;

            for (int i = 0; i < balls.Count; i++)
            {
                var b = balls[i];
                GameObject card = CreateShopItemCard(pnlBallsList.transform, new Vector2(shopStartX + i * shopItemSpacing, -20f), b.displayName, b.price, b.description, b.primaryColor, ShopItemCardUI.CardType.Ball, b, null);
            }

            float stadiumStartX = -1.5f * 360f;
            for (int i = 0; i < stadiums.Count; i++)
            {
                var s = stadiums[i];
                GameObject card = CreateShopItemCard(pnlStadiumsList.transform, new Vector2(stadiumStartX + i * 360f, -20f), s.displayName, s.price, s.description, s.skyColor, ShopItemCardUI.CardType.Stadium, null, s);
            }

            Button btnBackShop = CreateUIButton("BtnBackShop", pnlShop.transform, new Vector2(-750f, 440f), new Vector2(160f, 50f), "VOLVER", new Color(0.5f, 0.5f, 0.5f));
            TMP_Text txtShopCoins = CreateUIText("TxtShopCoins", pnlShop.transform, new Vector2(720f, 440f), new Vector2(240f, 50f), "0 Monedas", 26, Color.yellow);
            TMP_Text txtShopFeedback = CreateUIText("TxtShopFeedback", pnlShop.transform, new Vector2(0f, -380f), new Vector2(600f, 40f), "", 22, Color.green);
            txtShopFeedback.gameObject.SetActive(false);

            SetSerializedField(shopUI, "btnTabBalls", btnTabBalls);
            SetSerializedField(shopUI, "btnTabStadiums", btnTabStadiums);
            SetSerializedField(shopUI, "panelBallsList", pnlBallsList);
            SetSerializedField(shopUI, "panelStadiumsList", pnlStadiumsList);
            SetSerializedField(shopUI, "ballsContainer", pnlBallsList.transform);
            SetSerializedField(shopUI, "stadiumsContainer", pnlStadiumsList.transform);
            SetSerializedField(shopUI, "txtCoins", txtShopCoins);
            SetSerializedField(shopUI, "txtFeedback", txtShopFeedback);
            SetSerializedField(shopUI, "btnBack", btnBackShop);
            SetSerializedField(shopUI, "mainMenuUI", menuUI);

            // --- Panel 4: Customization ---
            GameObject pnlCustom = CreateUIPanel("Panel_Customization", canvasT, new Color(0.05f, 0.1f, 0.2f, 0.98f));
            CreateUIText("TitleCustom", pnlCustom.transform, new Vector2(0f, 440f), new Vector2(800f, 60f), "PERSONALIZACIÓN", 42, new Color(1f, 0.85f, 0.2f));
            CustomizationUI customUI = pnlCustom.AddComponent<CustomizationUI>();

            GameObject customBallsContainer = new GameObject("Custom_Balls_Container");
            customBallsContainer.transform.SetParent(pnlCustom.transform, false);
            for (int i = 0; i < balls.Count; i++)
            {
                var b = balls[i];
                CreateShopItemCard(customBallsContainer.transform, new Vector2(shopStartX + i * shopItemSpacing, 120f), b.displayName, b.price, b.description, b.primaryColor, ShopItemCardUI.CardType.Ball, b, null);
            }

            GameObject customStadiumsContainer = new GameObject("Custom_Stadiums_Container");
            customStadiumsContainer.transform.SetParent(pnlCustom.transform, false);
            for (int i = 0; i < stadiums.Count; i++)
            {
                var s = stadiums[i];
                CreateShopItemCard(customStadiumsContainer.transform, new Vector2(stadiumStartX + i * 360f, -220f), s.displayName, s.price, s.description, s.skyColor, ShopItemCardUI.CardType.Stadium, null, s);
            }

            Button btnBackCustom = CreateUIButton("BtnBackCustom", pnlCustom.transform, new Vector2(-750f, 440f), new Vector2(160f, 50f), "VOLVER", new Color(0.5f, 0.5f, 0.5f));
            SetSerializedField(customUI, "ballsContainer", customBallsContainer.transform);
            SetSerializedField(customUI, "stadiumsContainer", customStadiumsContainer.transform);
            SetSerializedField(customUI, "btnBack", btnBackCustom);
            SetSerializedField(customUI, "mainMenuUI", menuUI);

            // --- Panel 5: Achievements ---
            GameObject pnlAch = CreateUIPanel("Panel_Achievements", canvasT, new Color(0.05f, 0.1f, 0.2f, 0.98f));
            CreateUIText("TitleAch", pnlAch.transform, new Vector2(0f, 440f), new Vector2(600f, 60f), "SISTEMA DE LOGROS", 42, new Color(1f, 0.85f, 0.2f));
            AchievementsUI achUI = pnlAch.AddComponent<AchievementsUI>();

            List<AchievementsUI.AchievementCard> achCardList = new List<AchievementsUI.AchievementCard>();
            float achStartY = 280f;
            float achStepY = -95f;

            for (int i = 0; i < achievements.Count; i++)
            {
                var ach = achievements[i];
                GameObject card = CreateUIImage($"Card_Ach_{ach.id}", pnlAch.transform, new Vector2(0f, achStartY + i * achStepY), new Vector2(980f, 78f), new Color(0.12f, 0.18f, 0.32f));
                TMP_Text tTitle = CreateUIText("TxtTitle", card.transform, new Vector2(-280f, 15f), new Vector2(360f, 35f), ach.title, 24, Color.yellow);
                TMP_Text tDesc = CreateUIText("TxtDesc", card.transform, new Vector2(-200f, -18f), new Vector2(520f, 30f), ach.description, 18, Color.white);
                TMP_Text tReward = CreateUIText("TxtReward", card.transform, new Vector2(250f, 0f), new Vector2(200f, 35f), $"+{ach.rewardCoins} Monedas", 20, new Color(0.3f, 0.9f, 0.4f));

                GameObject badgeUnlock = CreateUIImage("BadgeUnlocked", card.transform, new Vector2(420f, 0f), new Vector2(80f, 40f), new Color(0.2f, 0.8f, 0.3f));
                CreateUIText("Txt", badgeUnlock.transform, Vector2.zero, new Vector2(80f, 40f), "LISTO", 16, Color.white);

                GameObject badgeLock = CreateUIImage("BadgeLocked", card.transform, new Vector2(420f, 0f), new Vector2(80f, 40f), new Color(0.4f, 0.4f, 0.4f));
                CreateUIText("Txt", badgeLock.transform, Vector2.zero, new Vector2(80f, 40f), "FALTA", 16, Color.white);

                achCardList.Add(new AchievementsUI.AchievementCard
                {
                    achievementId = ach.id,
                    cardObject = card,
                    txtTitle = tTitle,
                    txtDescription = tDesc,
                    txtReward = tReward,
                    unlockedBadge = badgeUnlock,
                    lockedBadge = badgeLock
                });
            }

            Button btnBackAch = CreateUIButton("BtnBackAch", pnlAch.transform, new Vector2(-750f, 440f), new Vector2(160f, 50f), "VOLVER", new Color(0.5f, 0.5f, 0.5f));
            SetSerializedField(achUI, "achievementCards", achCardList.ToArray());
            SetSerializedField(achUI, "btnBack", btnBackAch);
            SetSerializedField(achUI, "mainMenuUI", menuUI);

            // --- Panel 6: Settings ---
            GameObject pnlSettings = CreateUIPanel("Panel_Settings", canvasT, new Color(0.05f, 0.1f, 0.2f, 0.98f));
            CreateUIText("TitleSettings", pnlSettings.transform, new Vector2(0f, 440f), new Vector2(600f, 60f), "OPCIONES", 42, new Color(1f, 0.85f, 0.2f));
            SettingsUI settingsUI = pnlSettings.AddComponent<SettingsUI>();

            CreateUIText("LblMusic", pnlSettings.transform, new Vector2(-200f, 180f), new Vector2(250f, 40f), "Volumen Música:", 24, Color.white);
            Slider sliderMus = CreateUISlider("SliderMusic", pnlSettings.transform, new Vector2(120f, 180f), new Vector2(350f, 30f));
            TMP_Text txtMusVal = CreateUIText("TxtMusicVal", pnlSettings.transform, new Vector2(360f, 180f), new Vector2(80f, 40f), "80%", 22, Color.yellow);

            CreateUIText("LblSfx", pnlSettings.transform, new Vector2(-200f, 90f), new Vector2(250f, 40f), "Efectos de Sonido:", 24, Color.white);
            Slider sliderSfx = CreateUISlider("SliderSfx", pnlSettings.transform, new Vector2(120f, 90f), new Vector2(350f, 30f));
            TMP_Text txtSfxVal = CreateUIText("TxtSfxVal", pnlSettings.transform, new Vector2(360f, 90f), new Vector2(80f, 40f), "90%", 22, Color.yellow);

            Button btnBackSettings = CreateUIButton("BtnBackSettings", pnlSettings.transform, new Vector2(-750f, 440f), new Vector2(160f, 50f), "VOLVER", new Color(0.5f, 0.5f, 0.5f));
            SetSerializedField(settingsUI, "sliderMusic", sliderMus);
            SetSerializedField(settingsUI, "sliderSfx", sliderSfx);
            SetSerializedField(settingsUI, "txtMusicVal", txtMusVal);
            SetSerializedField(settingsUI, "txtSfxVal", txtSfxVal);
            SetSerializedField(settingsUI, "btnBack", btnBackSettings);
            SetSerializedField(settingsUI, "mainMenuUI", menuUI);

            // Wire MainMenuUI serialized fields
            SetSerializedField(menuUI, "panelMainMenu", pnlMain);
            SetSerializedField(menuUI, "panelRoundSelect", pnlRoundSelect);
            SetSerializedField(menuUI, "panelShop", pnlShop);
            SetSerializedField(menuUI, "panelCustomization", pnlCustom);
            SetSerializedField(menuUI, "panelAchievements", pnlAch);
            SetSerializedField(menuUI, "panelSettings", pnlSettings);

            SetSerializedField(menuUI, "btnPlay", btnPlay);
            SetSerializedField(menuUI, "btnShop", btnShop);
            SetSerializedField(menuUI, "btnCustomization", btnCustom);
            SetSerializedField(menuUI, "btnAchievements", btnAch);
            SetSerializedField(menuUI, "btnSettings", btnSettings);
            SetSerializedField(menuUI, "btnQuit", btnQuit);

            SetSerializedField(menuUI, "txtCoins", txtCoins);
            SetSerializedField(menuUI, "txtHighScore", txtHighScore);
            SetSerializedField(menuUI, "inputSecretCode", inpCode);
            SetSerializedField(menuUI, "btnSubmitCode", btnSubmitCode);
            SetSerializedField(menuUI, "txtCodeFeedback", txtCodeFeedback);
        }
        #endregion

        #region 6. Match Scene Builder
        public static void BuildMatchScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            string scenePath = "Assets/Scenes/MatchScene.unity";

            // Spawn Environment
            GameObject envPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/StadiumEnvironment.prefab");
            if (envPrefab != null) PrefabUtility.InstantiatePrefab(envPrefab);

            // Spawn Goal
            GameObject goalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GoalPrefab.prefab");
            if (goalPrefab != null)
            {
                GameObject goalInstance = PrefabUtility.InstantiatePrefab(goalPrefab) as GameObject;
                goalInstance.transform.position = Vector3.zero;
            }

            // Spawn Ball
            GameObject ballPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BallPrefab.prefab");
            if (ballPrefab != null)
            {
                GameObject ballInstance = PrefabUtility.InstantiatePrefab(ballPrefab) as GameObject;
                ballInstance.transform.position = new Vector3(0f, 0.15f, -11f);
            }

            // Spawn Player (Ruben Gonzalez)
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player_RubenGonzalez.prefab");
            if (playerPrefab != null)
            {
                GameObject playerInstance = PrefabUtility.InstantiatePrefab(playerPrefab) as GameObject;
                playerInstance.transform.position = new Vector3(0f, 0f, -13.5f);
            }

            // Spawn Goalkeeper (Malagon)
            GameObject gkPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Goalkeeper_Malagon.prefab");
            if (gkPrefab != null)
            {
                GameObject gkInstance = PrefabUtility.InstantiatePrefab(gkPrefab) as GameObject;
                gkInstance.transform.position = new Vector3(0f, 0f, 0f);
            }

            // Main Camera
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            camObj.transform.position = new Vector3(0f, 2.8f, -16.8f);
            camObj.transform.rotation = Quaternion.Euler(9f, 0f, 0f);
            camObj.AddComponent<AudioListener>();

            // Directional Light
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.98f, 0.92f);
            light.intensity = 1.3f;
            lightObj.transform.rotation = Quaternion.Euler(48f, -25f, 0f);

            // Penalty Manager Coordinator
            GameObject penaltyMgrObj = new GameObject("PenaltyManager");
            PenaltyManager pm = penaltyMgrObj.AddComponent<PenaltyManager>();

            // UI Canvas & EventSystem
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            GameObject canvasObj = new GameObject("Canvas_MatchHUD");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            BuildMatchHUD(canvasObj.transform);

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log("[ProjectSetupBuilder] MatchScene.unity built successfully.");
        }

        private static void BuildMatchHUD(Transform canvasT)
        {
            MatchHUD hud = canvasT.gameObject.AddComponent<MatchHUD>();

            // --- Top Bar HUD ---
            GameObject topBar = CreateUIImage("TopBar", canvasT, new Vector2(0f, 480f), new Vector2(1700f, 75f), new Color(0.05f, 0.1f, 0.22f, 0.88f));

            TMP_Text txtRound = CreateUIText("TxtRoundTitle", topBar.transform, new Vector2(-600f, 0f), new Vector2(380f, 50f), "RONDA 1: CANTERA", 28, new Color(1f, 0.85f, 0.2f));
            TMP_Text txtGoals = CreateUIText("TxtGoalsTarget", topBar.transform, new Vector2(-200f, 0f), new Vector2(250f, 50f), "Goles: 0 / 3", 26, Color.white);
            TMP_Text txtAttempts = CreateUIText("TxtAttemptCounter", topBar.transform, new Vector2(100f, 0f), new Vector2(250f, 50f), "Intento 1 / 5", 26, Color.white);
            TMP_Text txtScore = CreateUIText("TxtScore", topBar.transform, new Vector2(400f, 0f), new Vector2(250f, 50f), "Puntos: 0", 26, Color.cyan);
            TMP_Text txtCoins = CreateUIText("TxtCoins", topBar.transform, new Vector2(680f, 0f), new Vector2(200f, 50f), "Monedas: 0", 26, Color.yellow);
            TMP_Text txtStreak = CreateUIText("TxtStreak", canvasT, new Vector2(0f, 380f), new Vector2(400f, 50f), "¡Racha x2!", 32, new Color(1f, 0.45f, 0.1f));

            Button btnPause = CreateUIButton("BtnPause", topBar.transform, new Vector2(800f, 0f), new Vector2(90f, 50f), "II", new Color(0.3f, 0.35f, 0.5f));

            // --- Power Bar ---
            GameObject pwrContainer = CreateUIImage("PowerBarContainer", canvasT, new Vector2(0f, -420f), new Vector2(500f, 36f), new Color(0.1f, 0.1f, 0.1f, 0.85f));
            GameObject pwrFillObj = CreateUIImage("PowerFill", pwrContainer.transform, Vector2.zero, new Vector2(492f, 28f), Color.green);
            Image pwrFill = pwrFillObj.GetComponent<Image>();
            pwrFill.type = Image.Type.Filled;
            pwrFill.fillMethod = Image.FillMethod.Horizontal;
            pwrFill.fillOrigin = 0;
            pwrFill.fillAmount = 0.5f;

            // --- Result Banner ---
            GameObject bannerObj = CreateUIImage("Banner_Result", canvasT, new Vector2(0f, 150f), new Vector2(850f, 140f), new Color(0.1f, 0.65f, 0.25f, 0.95f));
            TMP_Text txtResultTitle = CreateUIText("TxtResultTitle", bannerObj.transform, new Vector2(0f, 28f), new Vector2(800f, 60f), "¡GOOOOL!", 46, Color.white);
            TMP_Text txtResultSub = CreateUIText("TxtResultSub", bannerObj.transform, new Vector2(0f, -32f), new Vector2(800f, 40f), "+100 Pts | +100 Monedas", 24, Color.yellow);
            bannerObj.SetActive(false);

            // --- Pause Panel ---
            GameObject pnlPause = CreateUIPanel("Panel_Pause", canvasT, new Color(0f, 0f, 0f, 0.88f));
            PauseUI pauseUI = pnlPause.AddComponent<PauseUI>();
            CreateUIText("TitlePause", pnlPause.transform, new Vector2(0f, 280f), new Vector2(400f, 70f), "PAUSA", 52, Color.white);

            Button btnResume = CreateUIButton("BtnResume", pnlPause.transform, new Vector2(0f, 140f), new Vector2(360f, 60f), "CONTINUAR", new Color(0.2f, 0.65f, 0.3f));
            Button btnRestart = CreateUIButton("BtnRestart", pnlPause.transform, new Vector2(0f, 50f), new Vector2(360f, 60f), "REINICIAR PENALES", new Color(0.25f, 0.45f, 0.8f));
            Button btnQuitToMenu = CreateUIButton("BtnQuitToMenu", pnlPause.transform, new Vector2(0f, -40f), new Vector2(360f, 60f), "SALIR AL MENÚ", new Color(0.7f, 0.2f, 0.2f));

            Slider sMus = CreateUISlider("SliderMusPause", pnlPause.transform, new Vector2(50f, -150f), new Vector2(300f, 30f));
            CreateUIText("LblMus", pnlPause.transform, new Vector2(-220f, -150f), new Vector2(200f, 30f), "Música:", 22, Color.white);

            Slider sSfx = CreateUISlider("SliderSfxPause", pnlPause.transform, new Vector2(50f, -210f), new Vector2(300f, 30f));
            CreateUIText("LblSfx", pnlPause.transform, new Vector2(-220f, -210f), new Vector2(200f, 30f), "Efectos:", 22, Color.white);

            SetSerializedField(pauseUI, "panelPause", pnlPause);
            SetSerializedField(pauseUI, "btnResume", btnResume);
            SetSerializedField(pauseUI, "btnRestart", btnRestart);
            SetSerializedField(pauseUI, "btnQuitToMenu", btnQuitToMenu);
            SetSerializedField(pauseUI, "sliderMusic", sMus);
            SetSerializedField(pauseUI, "sliderSfx", sSfx);
            pnlPause.SetActive(false);

            // --- Round End / Victory / Defeat Panel ---
            GameObject pnlEnd = CreateUIPanel("Panel_RoundEnd", canvasT, new Color(0.04f, 0.08f, 0.18f, 0.96f));
            RoundEndUI endUI = pnlEnd.AddComponent<RoundEndUI>();

            GameObject pnlNormalResult = new GameObject("NormalResultGroup");
            pnlNormalResult.transform.SetParent(pnlEnd.transform, false);

            TMP_Text txtEndTitle = CreateUIText("TxtEndTitle", pnlNormalResult.transform, new Vector2(0f, 260f), new Vector2(800f, 70f), "¡RONDA SUPERADA!", 52, new Color(0.2f, 0.9f, 0.35f));
            TMP_Text txtEndSub = CreateUIText("TxtEndSub", pnlNormalResult.transform, new Vector2(0f, 190f), new Vector2(800f, 40f), "¡Excelente temple y puntería!", 26, Color.white);
            TMP_Text txtEndGoals = CreateUIText("TxtEndGoals", pnlNormalResult.transform, new Vector2(0f, 110f), new Vector2(600f, 45f), "Goles: 4 de 5", 30, Color.yellow);
            TMP_Text txtEndScore = CreateUIText("TxtEndScore", pnlNormalResult.transform, new Vector2(0f, 50f), new Vector2(600f, 45f), "Puntuación: 950 pts", 28, Color.cyan);
            TMP_Text txtEndCoins = CreateUIText("TxtEndCoins", pnlNormalResult.transform, new Vector2(0f, -10f), new Vector2(600f, 45f), "Monedas Ganadas: +500", 28, Color.yellow);

            Button btnNext = CreateUIButton("BtnNextRound", pnlNormalResult.transform, new Vector2(0f, -110f), new Vector2(380f, 65f), "SIGUIENTE RONDA", new Color(0.18f, 0.65f, 0.28f));
            Button btnRetry = CreateUIButton("BtnRetry", pnlNormalResult.transform, new Vector2(0f, -190f), new Vector2(380f, 65f), "REPETIR RONDA", new Color(0.25f, 0.45f, 0.8f));
            Button btnEndMenu = CreateUIButton("BtnEndMenu", pnlNormalResult.transform, new Vector2(0f, -270f), new Vector2(380f, 65f), "MENÚ PRINCIPAL", new Color(0.6f, 0.2f, 0.2f));

            // Grand Champion Celebration
            GameObject pnlGrand = new GameObject("GrandChampionGroup");
            pnlGrand.transform.SetParent(pnlEnd.transform, false);

            TMP_Text txtChampTitle = CreateUIText("TxtChampTitle", pnlGrand.transform, new Vector2(0f, 320f), new Vector2(1200f, 80f), "¡ERES EL CAMPEÓN DE ULTIMATE PENALTY CUP!", 54, new Color(1f, 0.85f, 0.2f));
            TMP_Text txtChampScore = CreateUIText("TxtChampScore", pnlGrand.transform, new Vector2(0f, 220f), new Vector2(900f, 70f), "¡Felicidades Rubén González!", 34, Color.white);

            // 3D Trophy inside UI or Trophy Icon
            GameObject trophyCard = CreateUIImage("TrophyDisplay", pnlGrand.transform, new Vector2(0f, 20f), new Vector2(320f, 320f), new Color(1f, 0.85f, 0.2f, 0.2f));
            CreateUIText("TrophyCupText", trophyCard.transform, Vector2.zero, new Vector2(300f, 150f), "🏆\nCOPA ULTIMATE", 48, new Color(1f, 0.85f, 0.2f));

            Button btnChampContinue = CreateUIButton("BtnChampContinue", pnlGrand.transform, new Vector2(0f, -240f), new Vector2(420f, 70f), "CELEBRAR Y VOLVER AL MENÚ", new Color(0.95f, 0.75f, 0.15f));

            // Confetti Particle System
            GameObject confettiObj = new GameObject("ConfettiParticles");
            confettiObj.transform.SetParent(pnlGrand.transform, false);
            ParticleSystem ps = confettiObj.AddComponent<ParticleSystem>();
            var mainPs = ps.main;
            mainPs.startLifetime = 4.0f;
            mainPs.startSpeed = 8f;
            mainPs.startSize = 0.25f;
            mainPs.maxParticles = 500;
            var em = ps.emission;
            em.rateOverTime = 80f;

            SetSerializedField(endUI, "panelEndContainer", pnlEnd);
            SetSerializedField(endUI, "panelNormalResult", pnlNormalResult);
            SetSerializedField(endUI, "panelGrandChampionCelebration", pnlGrand);
            SetSerializedField(endUI, "txtStatusTitle", txtEndTitle);
            SetSerializedField(endUI, "txtStatusSub", txtEndSub);
            SetSerializedField(endUI, "txtGoals", txtEndGoals);
            SetSerializedField(endUI, "txtScore", txtEndScore);
            SetSerializedField(endUI, "txtCoinsGained", txtEndCoins);
            SetSerializedField(endUI, "btnNextRound", btnNext);
            SetSerializedField(endUI, "btnRetry", btnRetry);
            SetSerializedField(endUI, "btnMainMenu", btnEndMenu);
            SetSerializedField(endUI, "txtChampionTitle", txtChampTitle);
            SetSerializedField(endUI, "txtChampionScore", txtChampScore);
            SetSerializedField(endUI, "btnChampionContinue", btnChampContinue);
            SetSerializedField(endUI, "confettiParticles", ps);
            pnlEnd.SetActive(false);

            // Wire MatchHUD
            SetSerializedField(hud, "txtRoundTitle", txtRound);
            SetSerializedField(hud, "txtGoalsTarget", txtGoals);
            SetSerializedField(hud, "txtAttemptCounter", txtAttempts);
            SetSerializedField(hud, "txtScore", txtScore);
            SetSerializedField(hud, "txtCoins", txtCoins);
            SetSerializedField(hud, "txtStreak", txtStreak);
            SetSerializedField(hud, "powerBarContainer", pwrContainer);
            SetSerializedField(hud, "imgPowerFill", pwrFill);
            SetSerializedField(hud, "bannerResult", bannerObj);
            SetSerializedField(hud, "txtResultTitle", txtResultTitle);
            SetSerializedField(hud, "txtResultSub", txtResultSub);
            SetSerializedField(hud, "imgBannerBg", bannerObj.GetComponent<Image>());
            SetSerializedField(hud, "btnPause", btnPause);
            SetSerializedField(hud, "pauseUI", pauseUI);
        }
        #endregion

        #region 7. Build Settings
        public static void SetupBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/MatchScene.unity", true)
            };
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[ProjectSetupBuilder] Registered MainMenu and MatchScene in EditorBuildSettings.");
        }
        #endregion

        #region UI Creation Helpers
        private static GameObject CreateUIPanel(string name, Transform parent, Color color)
        {
            GameObject pnl = new GameObject(name, typeof(RectTransform));
            pnl.transform.SetParent(parent, false);
            RectTransform rt = pnl.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            Image img = pnl.AddComponent<Image>();
            img.color = color;
            return pnl;
        }

        private static GameObject CreateUIImage(string name, Transform parent, Vector2 pos, Vector2 size, Color color)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            img.color = color;
            return obj;
        }

        private static TMP_Text CreateUIText(string name, Transform parent, Vector2 pos, Vector2 size, string text, float fontSize, Color color)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            return tmp;
        }

        private static Button CreateUIButton(string name, Transform parent, Vector2 pos, Vector2 size, string label, Color bgColor)
        {
            GameObject btnObj = CreateUIImage(name, parent, pos, size, bgColor);
            Button btn = btnObj.AddComponent<Button>();

            ColorBlock cb = btn.colors;
            cb.normalColor = bgColor;
            cb.highlightedColor = bgColor * 1.25f;
            cb.pressedColor = bgColor * 0.75f;
            cb.selectedColor = bgColor;
            cb.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);
            btn.colors = cb;

            CreateUIText("Text", btnObj.transform, Vector2.zero, size, label, 22, Color.white);
            return btn;
        }

        private static Slider CreateUISlider(string name, Transform parent, Vector2 pos, Vector2 size)
        {
            GameObject sliderObj = new GameObject(name, typeof(RectTransform));
            sliderObj.transform.SetParent(parent, false);
            RectTransform rt = sliderObj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;

            // Background
            GameObject bg = CreateUIImage("Background", sliderObj.transform, Vector2.zero, size, new Color(0.2f, 0.2f, 0.2f, 0.8f));
            // Fill Area
            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRt = fillArea.GetComponent<RectTransform>();
            fillAreaRt.sizeDelta = size;
            GameObject fill = CreateUIImage("Fill", fillArea.transform, Vector2.zero, size, new Color(0.2f, 0.7f, 1f));
            slider.fillRect = fill.GetComponent<RectTransform>();

            return slider;
        }

        private static TMP_InputField CreateUIInputField(string name, Transform parent, Vector2 pos, Vector2 size, string placeholder)
        {
            GameObject inpObj = CreateUIImage(name, parent, pos, size, new Color(0.15f, 0.2f, 0.35f));
            TMP_InputField inp = inpObj.AddComponent<TMP_InputField>();

            TMP_Text txt = CreateUIText("Text", inpObj.transform, Vector2.zero, size - new Vector2(20f, 0f), "", 20, Color.white);
            txt.alignment = TextAlignmentOptions.Left;
            TMP_Text ph = CreateUIText("Placeholder", inpObj.transform, Vector2.zero, size - new Vector2(20f, 0f), placeholder, 20, new Color(0.7f, 0.7f, 0.7f, 0.6f));
            ph.alignment = TextAlignmentOptions.Left;

            inp.textComponent = txt;
            inp.placeholder = ph;
            return inp;
        }

        private static GameObject CreateShopItemCard(Transform parent, Vector2 pos, string name, int price, string desc, Color previewCol, ShopItemCardUI.CardType type, BallItemSO ball, StadiumItemSO stadium)
        {
            GameObject cardObj = CreateUIImage($"Card_{name}", parent, pos, new Vector2(290f, 440f), new Color(0.1f, 0.16f, 0.3f));
            ShopItemCardUI cardUI = cardObj.AddComponent<ShopItemCardUI>();

            TMP_Text tName = CreateUIText("TxtName", cardObj.transform, new Vector2(0f, 175f), new Vector2(270f, 40f), name, 24, Color.yellow);
            GameObject preview = CreateUIImage("ImgPreview", cardObj.transform, new Vector2(0f, 75f), new Vector2(130f, 130f), previewCol);
            TMP_Text tPrice = CreateUIText("TxtPrice", cardObj.transform, new Vector2(0f, -20f), new Vector2(270f, 35f), price == 0 ? "GRATIS" : $"{price:N0} Monedas", 22, new Color(1f, 0.85f, 0.2f));
            TMP_Text tDesc = CreateUIText("TxtDesc", cardObj.transform, new Vector2(0f, -75f), new Vector2(260f, 70f), desc, 16, new Color(0.85f, 0.85f, 0.9f));

            Button btnAction = CreateUIButton("BtnAction", cardObj.transform, new Vector2(0f, -165f), new Vector2(230f, 50f), "COMPRAR", new Color(0.2f, 0.5f, 0.9f));
            TMP_Text tBtn = btnAction.GetComponentInChildren<TMP_Text>();
            Image imgBtn = btnAction.GetComponent<Image>();

            SetSerializedField(cardUI, "cardType", type);
            SetSerializedField(cardUI, "ballItem", ball);
            SetSerializedField(cardUI, "stadiumItem", stadium);
            SetSerializedField(cardUI, "txtName", tName);
            SetSerializedField(cardUI, "txtPrice", tPrice);
            SetSerializedField(cardUI, "txtDescription", tDesc);
            SetSerializedField(cardUI, "imgPreview", preview.GetComponent<Image>());
            SetSerializedField(cardUI, "btnAction", btnAction);
            SetSerializedField(cardUI, "txtActionButton", tBtn);
            SetSerializedField(cardUI, "imgActionButton", imgBtn);

            return cardObj;
        }

        private static void SetSerializedField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }
        #endregion
    }
}
