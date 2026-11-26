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
using UltimatePenaltyCup.UI;
using UltimatePenaltyCup.Audio;
using UltimatePenaltyCup.Economy;
using UltimatePenaltyCup.SaveSystem;
using UltimatePenaltyCup.Customization;

namespace UltimatePenaltyCup.Editor
{
    public static class ProjectSetupBuilder
    {
        // Palette: EA FC / eFootball inspired sports arcade identity
        private static readonly Color ColNavyDeep   = new Color(0.027f, 0.067f, 0.122f, 0.96f); // #07111F
        private static readonly Color ColNavyMid    = new Color(0.063f, 0.141f, 0.231f, 0.95f); // #10243B
        private static readonly Color ColCardBg     = new Color(0.075f, 0.155f, 0.265f, 0.96f);
        private static readonly Color ColElectric   = new Color(0.078f, 0.490f, 1.000f, 1.00f); // #147DFF
        private static readonly Color ColGold       = new Color(1.000f, 0.784f, 0.239f, 1.00f); // #FFC83D
        private static readonly Color ColWhite      = new Color(0.961f, 0.969f, 0.980f, 1.00f); // #F5F7FA
        private static readonly Color ColGreen      = new Color(0.145f, 0.780f, 0.435f, 1.00f); // #25C76F
        private static readonly Color ColRed        = new Color(0.898f, 0.282f, 0.302f, 1.00f); // #E5484D
        private static readonly Color ColMuted      = new Color(0.55f,  0.62f,  0.72f,  1.00f);

        private static Sprite sPanel, sCard, sBtn, sPill, sCircle, sCoin, sLock, sTrophy, sCheck, sPause, sGradient, sReticle;

        [MenuItem("Ultimate Penalty Cup/Execute Complete Project Setup", false, 1)]
        public static void ExecuteCompleteSetup()
        {
            Debug.Log("==========================================================================");
            Debug.Log(">>> STARTING COMPLETE UPGRADED PROJECT SETUP (UltimatePenaltyCup3D) <<<");
            Debug.Log("==========================================================================");

            ConfigureOfficialPlayerSettings();
            ConfigureLogoTexture();
            UIStyleGenerator.GenerateAllUISprites();
            CreateAllMaterials();
            GameTextureGenerator.GenerateAllTextures();
            ConfigureSpecialMaterials();
            CreateAllPrefabs();
            AssetPreviewRenderer.RenderAllPreviews();
            CreateAllScriptableObjects();
            BuildMainMenuScene();
            BuildMatchScene();
            SetupBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("==========================================================================");
            Debug.Log(">>> ULTIMATE PENALTY CUP 3D: COMPLETE PROJECT SETUP FINISHED SUCCESSFULLY! <<<");
            Debug.Log("==========================================================================");
        }

        private static void LoadUISprites()
        {
            sPanel    = LoadSprite("Assets/Art/UI/UI_PanelRounded.png");
            sCard     = LoadSprite("Assets/Art/UI/UI_CardPanel.png");
            sBtn      = LoadSprite("Assets/Art/UI/UI_ButtonRounded.png");
            sPill     = LoadSprite("Assets/Art/UI/UI_BadgePill.png");
            sCircle   = LoadSprite("Assets/Art/UI/UI_Circle.png");
            sCoin     = LoadSprite("Assets/Art/UI/UI_CoinIcon.png");
            sLock     = LoadSprite("Assets/Art/UI/UI_LockIcon.png");
            sTrophy   = LoadSprite("Assets/Art/UI/UI_TrophyIcon.png");
            sCheck    = LoadSprite("Assets/Art/UI/UI_CheckIcon.png");
            sPause    = LoadSprite("Assets/Art/UI/UI_PauseIcon.png");
            sGradient = LoadSprite("Assets/Art/UI/UI_GradientOverlay.png");
            sReticle  = LoadSprite("Assets/Art/UI/UI_ReticleRing.png");
        }

        private static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        public static void ConfigureOfficialPlayerSettings()
        {
            PlayerSettings.companyName = "A&R Game Labs";
            PlayerSettings.productName = "UltimatePenaltyCup3D";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = true;
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
            }
        }
        #endregion

        #region 2. Materials Generation
        public static void CreateAllMaterials()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            // Grass
            CreateOrUpdateMaterial("Assets/Materials/Mat_Grass_Cantera.mat",  litShader, new Color(0.20f, 0.50f, 0.17f), 0.14f, 0.0f);
            CreateOrUpdateMaterial("Assets/Materials/Mat_Grass_Night.mat",    litShader, new Color(0.11f, 0.36f, 0.14f), 0.18f, 0.0f);
            CreateOrUpdateMaterial("Assets/Materials/Mat_Grass_Pro.mat",      litShader, new Color(0.15f, 0.54f, 0.20f), 0.20f, 0.0f);
            CreateOrUpdateMaterial("Assets/Materials/Mat_Grass_WorldCup.mat", litShader, new Color(0.14f, 0.58f, 0.22f), 0.24f, 0.0f);

            // Pitch Lines & Goal
            CreateOrUpdateMaterial("Assets/Materials/Mat_Lines_White.mat", litShader, Color.white, 0.2f, 0.0f);
            CreateOrUpdateMaterial("Assets/Materials/Mat_Goal_Posts.mat",  litShader, new Color(0.96f, 0.96f, 0.98f), 0.72f, 0.25f);
            CreateOrUpdateMaterial("Assets/Materials/Mat_Goal_Net.mat",    litShader, new Color(0.95f, 0.95f, 0.98f, 1f), 0.1f, 0.0f);

            // Balls
            CreateOrUpdateMaterial("Assets/Materials/Mat_Ball_Classic.mat", litShader, Color.white, 0.58f, 0.1f);
            CreateOrUpdateMaterial("Assets/Materials/Mat_Ball_Blue.mat",    litShader, Color.white, 0.78f, 0.3f);
            CreateOrUpdateMaterial("Assets/Materials/Mat_Ball_Fire.mat",    litShader, Color.white, 0.75f, 0.2f);
            CreateOrUpdateMaterial("Assets/Materials/Mat_Ball_Rainbow.mat", litShader, Color.white, 0.85f, 0.25f);
            CreateOrUpdateMaterial("Assets/Materials/Mat_Ball_Gold.mat",    litShader, Color.white, 0.92f, 0.88f);

            // Character Kits & Strictly Separated Skin / Face / Hair / Socks / Boots / Gloves
            CreateOrUpdateMaterial("Assets/Materials/Mat_Player_Kit.mat",     litShader, Color.white, 0.28f, 0.0f);
            CreateOrUpdateMaterial("Assets/Materials/Mat_Player_Shorts.mat",  litShader, new Color(0.04f, 0.12f, 0.36f), 0.25f, 0.0f);
            CreateOrUpdateMaterial("Assets/Materials/Mat_Player_Socks.mat",   litShader, new Color(0.06f, 0.26f, 0.72f), 0.25f, 0.0f);
            CreateOrUpdateMaterial("Assets/Materials/Mat_Boots_Player.mat",   litShader, new Color(0.08f, 0.45f, 0.95f), 0.65f, 0.2f);

            CreateOrUpdateMaterial("Assets/Materials/Mat_Goalkeeper_Kit.mat", litShader, Color.white, 0.28f, 0.0f);
            CreateOrUpdateMaterial("Assets/Materials/Mat_GK_Shorts.mat",      litShader, new Color(0.09f, 0.11f, 0.16f), 0.25f, 0.0f);
            CreateOrUpdateMaterial("Assets/Materials/Mat_GK_Socks.mat",       litShader, new Color(0.95f, 0.40f, 0.05f), 0.25f, 0.0f);
            CreateOrUpdateMaterial("Assets/Materials/Mat_Boots_GK.mat",       litShader, new Color(0.10f, 0.11f, 0.14f), 0.60f, 0.2f);
            CreateOrUpdateMaterial("Assets/Materials/Mat_GK_Gloves.mat",      litShader, Color.white, 0.35f, 0.0f);

            CreateOrUpdateMaterial("Assets/Materials/Mat_Skin.mat",         litShader, new Color(0.90f, 0.72f, 0.58f), 0.22f, 0.0f);
            CreateOrUpdateMaterial("Assets/Materials/Mat_Skin_Body.mat",    litShader, Color.white, 0.22f, 0.0f);
            CreateOrUpdateMaterial("Assets/Materials/Mat_Face_Ruben.mat",   litShader, Color.white, 0.22f, 0.0f);
            CreateOrUpdateMaterial("Assets/Materials/Mat_Face_Malagon.mat", litShader, Color.white, 0.22f, 0.0f);
            CreateOrUpdateMaterial("Assets/Materials/Mat_Hair_Dark.mat",    litShader, new Color(0.11f, 0.08f, 0.06f), 0.32f, 0.0f);

            // Stadium Elements
            CreateOrUpdateMaterial("Assets/Materials/Mat_Stadium_Stands.mat",   litShader, Color.white, 0.2f, 0.0f);
            CreateOrUpdateMaterial("Assets/Materials/Mat_Stadium_Concrete.mat", litShader, new Color(0.24f, 0.27f, 0.33f), 0.15f, 0.0f);
            CreateOrUpdateMaterial("Assets/Materials/Mat_Stadium_Banners.mat",  litShader, Color.white, 0.55f, 0.15f);
            CreateOrUpdateMaterial("Assets/Materials/Mat_Stadium_Metal.mat",    litShader, new Color(0.32f, 0.36f, 0.44f), 0.65f, 0.75f);
            CreateOrUpdateMaterial("Assets/Materials/Mat_Stadium_Seats.mat",    litShader, new Color(0.08f, 0.24f, 0.55f), 0.35f, 0.0f);
            CreateOrUpdateMaterial("Assets/Materials/Mat_Pitch_Fence.mat",      litShader, new Color(0.16f, 0.22f, 0.28f), 0.3f, 0.4f);
            CreateOrUpdateMaterial("Assets/Materials/Mat_Foliage.mat",          litShader, new Color(0.14f, 0.44f, 0.16f), 0.15f, 0.0f);
            CreateOrUpdateMaterial("Assets/Materials/Mat_GoldTrophy.mat",       litShader, Color.white, 0.94f, 0.92f);

            AssetDatabase.SaveAssets();
        }

        private static void ConfigureSpecialMaterials()
        {
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Transparent");
            Material reticleMat = CreateOrUpdateMaterial("Assets/Materials/Mat_AimReticle.mat", unlitShader, ColElectric, 0f, 0f);
            Texture2D reticleTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/UI/UI_ReticleRing.png");
            if (reticleMat != null && reticleTex != null)
            {
                reticleMat.mainTexture = reticleTex;
                if (reticleMat.HasProperty("_BaseMap")) reticleMat.SetTexture("_BaseMap", reticleTex);
                if (reticleMat.HasProperty("_Surface")) reticleMat.SetFloat("_Surface", 1f); // Transparent
                if (reticleMat.HasProperty("_Blend")) reticleMat.SetFloat("_Blend", 0f);
                reticleMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                reticleMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                reticleMat.SetInt("_ZWrite", 0);
                reticleMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                reticleMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                EditorUtility.SetDirty(reticleMat);
            }

            Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? unlitShader;
            Material trajMat = CreateOrUpdateMaterial("Assets/Materials/Mat_TrajectoryLine.mat", particleShader, Color.white, 0f, 0f);
            if (trajMat != null)
            {
                if (trajMat.HasProperty("_Surface")) trajMat.SetFloat("_Surface", 1f);
                trajMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                trajMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                trajMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                EditorUtility.SetDirty(trajMat);
            }
            AssetDatabase.SaveAssets();
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
            mat.enableInstancing = true;
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
            // --- 5 Balanced Rounds ---
            CreateRoundSO("Assets/ScriptableObjects/Rounds/Round1_Cantera.asset", 1, "Cantera",
                "Estadio de entrenamiento. Anota 3 goles en un máximo de 5 intentos para avanzar.",
                3, 5, 500, 300, StadiumType.Cantera, "Portero Principiante", 0.25f, 4.0f, 5.0f, 0.44f);

            CreateRoundSO("Assets/ScriptableObjects/Rounds/Round2_LigaLocal.asset", 2, "Liga Local",
                "Ambiente competitivo nocturno. El arquero gana agilidad lateral y reflejos.",
                3, 5, 500, 450, StadiumType.Night, "Portero Intermedio", 0.20f, 5.2f, 6.5f, 0.32f);

            CreateRoundSO("Assets/ScriptableObjects/Rounds/Round3_LigaProfesional.asset", 3, "Liga Profesional",
                "Estadio profesional a máxima capacidad. Meta exigente: 4 de 5 penales.",
                4, 5, 650, 650, StadiumType.Professional, "Portero Profesional", 0.16f, 6.4f, 8.0f, 0.22f);

            CreateRoundSO("Assets/ScriptableObjects/Rounds/Round4_Semifinal.asset", 4, "Semifinal",
                "Tensión de eliminatoria bajo reflectores. Coloca tus disparos en la escuadra.",
                4, 5, 800, 900, StadiumType.Night, "Portero Avanzado", 0.12f, 7.6f, 9.5f, 0.14f);

            CreateRoundSO("Assets/ScriptableObjects/Rounds/Round5_GranFinal.asset", 5, "Gran Final",
                "¡El título en juego! Supera al Malágon de élite con 4 goles en el Estadio Mundial.",
                4, 5, 1200, 1500, StadiumType.WorldCup, "Portero Élite", 0.09f, 8.8f, 11.0f, 0.08f);

            // --- 5 Ball Items (wired with real rendered 3D preview sprites!) ---
            CreateBallSO("Assets/ScriptableObjects/Items/Ball_Classic.asset", BallType.Classic, "Balón Clásico",
                "Balón reglamentario con paneles pentagonales.", 0, Color.white, Color.black, Color.cyan,
                "Assets/Materials/Mat_Ball_Classic.mat", "Assets/Art/Previews/Preview_Ball_Classic.png");

            CreateBallSO("Assets/ScriptableObjects/Items/Ball_Blue.asset", BallType.Blue, "Balón Azul",
                "Edición eléctrica con estela cian brillante.", 500, new Color(0.1f, 0.6f, 1f), Color.blue, Color.cyan,
                "Assets/Materials/Mat_Ball_Blue.mat", "Assets/Art/Previews/Preview_Ball_Blue.png");

            CreateBallSO("Assets/ScriptableObjects/Items/Ball_Fire.asset", BallType.Fire, "Balón de Fuego",
                "Chispas ardientes y estela ígnea de alta velocidad.", 1000, new Color(1f, 0.4f, 0f), Color.red, new Color(1f, 0.3f, 0f),
                "Assets/Materials/Mat_Ball_Fire.mat", "Assets/Art/Previews/Preview_Ball_Fire.png");

            CreateBallSO("Assets/ScriptableObjects/Items/Ball_Rainbow.asset", BallType.Rainbow, "Balón Arcoíris",
                "Espectro cromático con destellos multicolor.", 2000, new Color(0.8f, 0.2f, 0.9f), Color.yellow, Color.magenta,
                "Assets/Materials/Mat_Ball_Rainbow.mat", "Assets/Art/Previews/Preview_Ball_Rainbow.png");

            CreateBallSO("Assets/ScriptableObjects/Items/Ball_Gold.asset", BallType.Gold, "Balón Dorado",
                "Edición oro campeón 24K con brillo legendario.", 5000, new Color(1f, 0.85f, 0.2f), Color.black, new Color(1f, 0.9f, 0.3f),
                "Assets/Materials/Mat_Ball_Gold.mat", "Assets/Art/Previews/Preview_Ball_Gold.png");

            // --- 4 Stadium Items (wired with real rendered 3D stadium snapshot sprites!) ---
            CreateStadiumSO("Assets/ScriptableObjects/Items/Stadium_Cantera.asset", StadiumType.Cantera, "Estadio Cantera",
                "Cancha de entrenamiento diurna rodeada de gradas y árboles.", 0, new Color(0.35f, 0.6f, 0.9f), 1.35f,
                "Assets/Materials/Mat_Grass_Cantera.mat", "Assets/Art/Previews/Preview_Stadium_Cantera.png");

            CreateStadiumSO("Assets/ScriptableObjects/Items/Stadium_Night.asset", StadiumType.Night, "Estadio Nocturno",
                "Arena nocturna iluminada por cuatro torres de reflectores.", 2000, new Color(0.08f, 0.12f, 0.25f), 0.75f,
                "Assets/Materials/Mat_Grass_Night.mat", "Assets/Art/Previews/Preview_Stadium_Night.png");

            CreateStadiumSO("Assets/ScriptableObjects/Items/Stadium_Professional.asset", StadiumType.Professional, "Estadio Profesional",
                "Estadio techado al atardecer con marcador electrónico y bancas.", 4000, new Color(0.2f, 0.45f, 0.75f), 1.2f,
                "Assets/Materials/Mat_Grass_Pro.mat", "Assets/Art/Previews/Preview_Stadium_Professional.png");

            CreateStadiumSO("Assets/ScriptableObjects/Items/Stadium_WorldCup.asset", StadiumType.WorldCup, "Estadio Mundial",
                "Sede de la Gran Final con pantallas gigantes, confeti y trofeo.", 7500, new Color(0.12f, 0.22f, 0.48f), 1.4f,
                "Assets/Materials/Mat_Grass_WorldCup.mat", "Assets/Art/Previews/Preview_Stadium_WorldCup.png");

            // --- 7 Achievements ---
            CreateAchievementSO("Assets/ScriptableObjects/Items/Ach_FirstGoal.asset", AchievementManager.ID_FIRST_GOAL,
                "Primer Gol", "Marca tu primer gol en cualquier ronda.", 100, 100);

            CreateAchievementSO("Assets/ScriptableObjects/Items/Ach_Sniper10.asset", AchievementManager.ID_SNIPER_10,
                "Francotirador", "Marca 10 goles en total a lo largo de tu carrera.", 300, 250);

            CreateAchievementSO("Assets/ScriptableObjects/Items/Ach_PerfectStreak.asset", AchievementManager.ID_PERFECT_STREAK,
                "Racha Perfecta", "Completa una ronda sin fallar ningún disparo o logra 4 goles seguidos.", 500, 500);

            CreateAchievementSO("Assets/ScriptableObjects/Items/Ach_Champion.asset", AchievementManager.ID_CHAMPION,
                "Campeón", "Supera todas las 5 rondas y gana la Gran Final.", 1500, 1000);

            CreateAchievementSO("Assets/ScriptableObjects/Items/Ach_Millionaire.asset", AchievementManager.ID_MILLIONAIRE,
                "Millonario", "Acumula 10,000 monedas en tu balance.", 2000, 500);

            CreateAchievementSO("Assets/ScriptableObjects/Items/Ach_Collector.asset", AchievementManager.ID_COLLECTOR,
                "Coleccionista", "Compra todos los balones disponibles en la tienda.", 2500, 1000);

            CreateAchievementSO("Assets/ScriptableObjects/Items/Ach_Legend.asset", AchievementManager.ID_LEGEND,
                "Leyenda", "Completa la Gran Final con más de 1,500 puntos.", 3000, 2000);

            AssetDatabase.SaveAssets();
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

        private static void CreateBallSO(string path, BallType type, string name, string desc, int price, Color prim, Color sec, Color trail, string matPath, string iconPath)
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
            so.icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            EditorUtility.SetDirty(so);
        }

        private static void CreateStadiumSO(string path, StadiumType type, string name, string desc, int price, Color sky, float intensity, string grassMatPath, string iconPath)
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
            so.icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
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

        #region 4. Prefabs Generation (Delegates to Upgraded Builders!)
        public static void CreateAllPrefabs()
        {
            CreateTrophyPrefab();
            CharacterAndStadiumUpgrader.RunAllUpgrades();
        }

        private static void CreateTrophyPrefab()
        {
            string prefabPath = "Assets/Prefabs/Trophy_Cup.prefab";
            GameObject trophy = new GameObject("Trophy_UltimatePenaltyCup");
            Material matGold = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_GoldTrophy.mat");

            GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseObj.transform.SetParent(trophy.transform);
            baseObj.transform.localPosition = new Vector3(0f, 0.14f, 0f);
            baseObj.transform.localScale = new Vector3(0.65f, 0.14f, 0.65f);
            baseObj.GetComponent<Renderer>().sharedMaterial = matGold;
            Object.DestroyImmediate(baseObj.GetComponent<Collider>());

            GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stem.transform.SetParent(trophy.transform);
            stem.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            stem.transform.localScale = new Vector3(0.22f, 0.34f, 0.22f);
            stem.GetComponent<Renderer>().sharedMaterial = matGold;
            Object.DestroyImmediate(stem.GetComponent<Collider>());

            GameObject cup = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cup.transform.SetParent(trophy.transform);
            cup.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            cup.transform.localScale = new Vector3(0.85f, 0.90f, 0.85f);
            cup.GetComponent<Renderer>().sharedMaterial = matGold;
            Object.DestroyImmediate(cup.GetComponent<Collider>());

            GameObject handleL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handleL.transform.SetParent(trophy.transform);
            handleL.transform.localPosition = new Vector3(-0.48f, 1.15f, 0f);
            handleL.transform.localRotation = Quaternion.Euler(0f, 0f, 20f);
            handleL.transform.localScale = new Vector3(0.08f, 0.32f, 0.08f);
            handleL.GetComponent<Renderer>().sharedMaterial = matGold;
            Object.DestroyImmediate(handleL.GetComponent<Collider>());

            GameObject handleR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handleR.transform.SetParent(trophy.transform);
            handleR.transform.localPosition = new Vector3(0.48f, 1.15f, 0f);
            handleR.transform.localRotation = Quaternion.Euler(0f, 0f, -20f);
            handleR.transform.localScale = new Vector3(0.08f, 0.32f, 0.08f);
            handleR.GetComponent<Renderer>().sharedMaterial = matGold;
            Object.DestroyImmediate(handleR.GetComponent<Collider>());

            PrefabUtility.SaveAsPrefabAsset(trophy, prefabPath);
            Object.DestroyImmediate(trophy);
        }
        #endregion

        #region 5. Main Menu Scene Builder (Live 3D Stadium + Modern Broadcast UI)
        public static void BuildMainMenuScene()
        {
            LoadUISprites();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            string scenePath = "Assets/Scenes/MainMenu.unity";

            // Live 3D Stadium showcase behind the menu!
            GameObject envPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/StadiumEnvironment.prefab");
            if (envPrefab != null)
            {
                GameObject env = PrefabUtility.InstantiatePrefab(envPrefab) as GameObject;
                var sc = env.GetComponent<StadiumController>();
                if (sc != null) sc.ApplyVenue(StadiumType.Professional);
            }
            GameObject goalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GoalPrefab.prefab");
            if (goalPrefab != null) PrefabUtility.InstantiatePrefab(goalPrefab);
            GameObject ballPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BallPrefab.prefab");
            if (ballPrefab != null)
            {
                GameObject b = PrefabUtility.InstantiatePrefab(ballPrefab) as GameObject;
                b.transform.position = new Vector3(0f, 0.11f, -11f);
            }

            // Orbiting Broadcast Camera
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.07f, 0.16f);
            cam.fieldOfView = 44f;
            camObj.transform.position = new Vector3(4.5f, 3.8f, -16f);
            camObj.transform.LookAt(new Vector3(-1.5f, 1.2f, -4f));
            camObj.AddComponent<AudioListener>();
            camObj.AddComponent<MenuCameraOrbit>();

            // Cinematic Light
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1.0f, 0.95f, 0.88f);
            light.intensity = 1.35f;
            lightObj.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

            // EventSystem
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            // Canvas
            GameObject canvasObj = new GameObject("Canvas_MainMenu");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // Persistent GameManager + Catalogs + RoundConfigs wired directly!
            GameObject gmObj = new GameObject("GameManager");
            GameManager gm = gmObj.AddComponent<GameManager>();
            RoundManager rm = gmObj.AddComponent<RoundManager>();
            gmObj.AddComponent<SaveManager>();
            gmObj.AddComponent<CurrencyManager>();
            gmObj.AddComponent<ScoreManager>();
            gmObj.AddComponent<AudioManager>();
            ShopManager shm = gmObj.AddComponent<ShopManager>();
            gmObj.AddComponent<CustomizationManager>();
            AchievementManager acm = gmObj.AddComponent<AchievementManager>();

            var balls = new List<BallItemSO>
            {
                AssetDatabase.LoadAssetAtPath<BallItemSO>("Assets/ScriptableObjects/Items/Ball_Classic.asset"),
                AssetDatabase.LoadAssetAtPath<BallItemSO>("Assets/ScriptableObjects/Items/Ball_Blue.asset"),
                AssetDatabase.LoadAssetAtPath<BallItemSO>("Assets/ScriptableObjects/Items/Ball_Fire.asset"),
                AssetDatabase.LoadAssetAtPath<BallItemSO>("Assets/ScriptableObjects/Items/Ball_Rainbow.asset"),
                AssetDatabase.LoadAssetAtPath<BallItemSO>("Assets/ScriptableObjects/Items/Ball_Gold.asset")
            };
            var stadiums = new List<StadiumItemSO>
            {
                AssetDatabase.LoadAssetAtPath<StadiumItemSO>("Assets/ScriptableObjects/Items/Stadium_Cantera.asset"),
                AssetDatabase.LoadAssetAtPath<StadiumItemSO>("Assets/ScriptableObjects/Items/Stadium_Night.asset"),
                AssetDatabase.LoadAssetAtPath<StadiumItemSO>("Assets/ScriptableObjects/Items/Stadium_Professional.asset"),
                AssetDatabase.LoadAssetAtPath<StadiumItemSO>("Assets/ScriptableObjects/Items/Stadium_WorldCup.asset")
            };
            shm.SetCatalogs(balls, stadiums);

            var rounds = new List<RoundDataSO>
            {
                AssetDatabase.LoadAssetAtPath<RoundDataSO>("Assets/ScriptableObjects/Rounds/Round1_Cantera.asset"),
                AssetDatabase.LoadAssetAtPath<RoundDataSO>("Assets/ScriptableObjects/Rounds/Round2_LigaLocal.asset"),
                AssetDatabase.LoadAssetAtPath<RoundDataSO>("Assets/ScriptableObjects/Rounds/Round3_LigaProfesional.asset"),
                AssetDatabase.LoadAssetAtPath<RoundDataSO>("Assets/ScriptableObjects/Rounds/Round4_Semifinal.asset"),
                AssetDatabase.LoadAssetAtPath<RoundDataSO>("Assets/ScriptableObjects/Rounds/Round5_GranFinal.asset")
            };
            rm.SetRounds(rounds);
            gm.SetRoundConfigs(rounds.ToArray()); // Serialized directly onto GameManager!

            var achievements = new List<AchievementSO>
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

            MainMenuUI menuUI = canvasObj.AddComponent<MainMenuUI>();
            BuildMainMenuPanels(canvasObj.transform, menuUI, balls, stadiums, rounds, achievements);

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log("[ProjectSetupBuilder] MainMenu.unity rebuilt with live 3D stadium & broadcast UI.");
        }

        private static void BuildMainMenuPanels(Transform canvasT, MainMenuUI menuUI, List<BallItemSO> balls, List<StadiumItemSO> stadiums, List<RoundDataSO> rounds, List<AchievementSO> achievements)
        {
            Sprite logoSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Logo/UltimatePenaltyCup3D_Logo.jpg");

            // ========== PANEL 1: MAIN MENU (Transparent over 3D Stadium with clean left navy column) ==========
            GameObject pnlMain = CreateFullScreenPanel("Panel_MainMenu", canvasT, Color.clear, null);

            GameObject scrim = CreateUIImage("LeftScrim", pnlMain.transform, Vector2.zero, new Vector2(580f, 1080f), new Color(0.027f, 0.067f, 0.122f, 0.84f), null);
            RectTransform scrimRt = scrim.GetComponent<RectTransform>();
            scrimRt.anchorMin = new Vector2(0f, 0f);
            scrimRt.anchorMax = new Vector2(0f, 1f);
            scrimRt.pivot = new Vector2(0f, 0.5f);
            scrimRt.anchoredPosition = Vector2.zero;
            scrimRt.sizeDelta = new Vector2(580f, 0f);

            // Top-Right Currency, Record & Studio Pill Bar
            GameObject topRightBar = CreateContainer("TopRightBadges", pnlMain.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-36f, -28f), new Vector2(540f, 64f));
            topRightBar.GetComponent<RectTransform>().pivot = new Vector2(1f, 1f);
            var hTop = topRightBar.AddComponent<HorizontalLayoutGroup>();
            hTop.spacing = 16f;
            hTop.childAlignment = TextAnchor.MiddleRight;
            hTop.childControlWidth = false;
            hTop.childControlHeight = true;

            GameObject scorePill = CreatePillBadge("ScoreBadge", topRightBar.transform, 270f, ColNavyMid, sTrophy, ColGold, "Récord: 0 pts", out TMP_Text txtHighScore);
            GameObject coinsPill = CreatePillBadge("CoinsBadge", topRightBar.transform, 220f, ColNavyMid, sCoin, ColGold, "0", out TMP_Text txtCoins);

            // Left Column Container (Logo + Buttons)
            GameObject leftCol = CreateContainer("LeftNavigationColumn", pnlMain.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(290f, 0f), new Vector2(460f, 940f));
            var vCol = leftCol.AddComponent<VerticalLayoutGroup>();
            vCol.spacing = 14f;
            vCol.padding = new RectOffset(10, 10, 10, 10);
            vCol.childAlignment = TextAnchor.UpperCenter;
            vCol.childControlWidth = true;
            vCol.childControlHeight = false;

            // Clean Framed Logo (deep navy matching logo backdrop — ZERO grey margins)
            GameObject logoFrame = CreateUIImage("LogoCardFrame", leftCol.transform, Vector2.zero, new Vector2(440f, 250f), new Color(0.025f, 0.055f, 0.12f, 1f), sCard);
            var mask = logoFrame.AddComponent<Mask>();
            mask.showMaskGraphic = true;
            if (logoSprite != null)
            {
                GameObject logoObj = CreateUIImage("LogoImage", logoFrame.transform, Vector2.zero, new Vector2(436f, 250f), Color.white, logoSprite);
                logoObj.GetComponent<Image>().preserveAspect = true;
            }
            else
            {
                CreateUIText("TitleText", logoFrame.transform, Vector2.zero, new Vector2(420f, 120f), "ULTIMATE PENALTY CUP 3D", 42, ColGold, FontStyles.Bold);
            }

            CreateUIText("StudioTagline", leftCol.transform, Vector2.zero, new Vector2(440f, 34f), "A&R GAME LABS  •  TEMPORADA 2026", 17, ColGold, FontStyles.Bold);

            // Primary & Secondary Menu Buttons
            Button btnPlay     = CreateStyledButton("BtnPlay",     leftCol.transform, new Vector2(420f, 74f), "JUGAR",          ColGreen,    ColWhite, 28);
            Button btnShop     = CreateStyledButton("BtnShop",     leftCol.transform, new Vector2(420f, 60f), "TIENDA",         ColElectric, ColWhite, 23);
            Button btnCustom   = CreateStyledButton("BtnCustom",   leftCol.transform, new Vector2(420f, 60f), "PERSONALIZACIÓN",ColNavyMid,  ColWhite, 22);
            Button btnAch      = CreateStyledButton("BtnAch",      leftCol.transform, new Vector2(420f, 60f), "LOGROS",         ColNavyMid,  ColWhite, 22);
            Button btnSettings = CreateStyledButton("BtnSettings", leftCol.transform, new Vector2(420f, 60f), "OPCIONES",       ColNavyMid,  ColWhite, 22);
            Button btnQuit     = CreateStyledButton("BtnQuit",     leftCol.transform, new Vector2(420f, 54f), "SALIR",          ColRed,      ColWhite, 20);

            // Bottom-Right Version & Credits Stamp
            GameObject footer = CreateUIImage("FooterCredits", pnlMain.transform, new Vector2(1f, 0f), new Vector2(460f, 44f), ColNavyMid, sPill);
            RectTransform fRt = footer.GetComponent<RectTransform>();
            fRt.anchorMin = new Vector2(1f, 0f);
            fRt.anchorMax = new Vector2(1f, 0f);
            fRt.pivot = new Vector2(1f, 0f);
            fRt.anchoredPosition = new Vector2(-36f, 24f);
            CreateUIText("TxtFooter", footer.transform, Vector2.zero, new Vector2(440f, 38f), "UltimatePenaltyCup3D  v1.0.0  •  A&R Game Labs", 16, ColMuted, FontStyles.Normal);

            // ========== PANEL 2: ROUND SELECT ==========
            GameObject pnlRoundSelect = CreateFullScreenPanel("Panel_RoundSelect", canvasT, ColNavyDeep, null);
            Button btnBackRounds = CreateHeaderBar(pnlRoundSelect.transform, "SELECCIÓN DE RONDA", "Elige una etapa del torneo — Supera cada meta para desbloquear la siguiente", out _);
            RoundSelectUI roundSelectUI = pnlRoundSelect.AddComponent<RoundSelectUI>();

            GameObject roundsRow = CreateContainer("RoundsRow", pnlRoundSelect.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -35f), new Vector2(1800f, 700f));
            var hRounds = roundsRow.AddComponent<HorizontalLayoutGroup>();
            hRounds.spacing = 22f;
            hRounds.padding = new RectOffset(10, 10, 10, 10);
            hRounds.childControlWidth = true;
            hRounds.childControlHeight = true;
            hRounds.childForceExpandWidth = true;
            hRounds.childForceExpandHeight = true;

            var cardList = new List<RoundSelectUI.RoundCard>();
            for (int i = 0; i < 5; i++)
            {
                int rNum = i + 1;
                RoundDataSO rData = rounds.Count > i ? rounds[i] : null;
                cardList.Add(CreateRoundCard(roundsRow.transform, rNum, rData));
            }

            SetSerializedField(roundSelectUI, "roundCards", cardList.ToArray());
            SetSerializedField(roundSelectUI, "btnBack", btnBackRounds);
            SetSerializedField(roundSelectUI, "mainMenuUI", menuUI);

            // ========== PANEL 3: SHOP ==========
            GameObject pnlShop = CreateFullScreenPanel("Panel_Shop", canvasT, ColNavyDeep, null);
            Button btnBackShop = CreateHeaderBar(pnlShop.transform, "TIENDA OFICIAL", "Adquiere nuevos balones y estadios oficiales", out GameObject shopHeaderRight);
            ShopUI shopUI = pnlShop.AddComponent<ShopUI>();

            GameObject shopCoinPill = CreatePillBadge("ShopCoinsBadge", shopHeaderRight.transform, 230f, ColNavyMid, sCoin, ColGold, "0", out TMP_Text txtShopCoins);

            GameObject shopTabsRow = CreateContainer("ShopTabs", pnlShop.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -155f), new Vector2(560f, 60f));
            var hShopTabs = shopTabsRow.AddComponent<HorizontalLayoutGroup>();
            hShopTabs.spacing = 20f;
            hShopTabs.childControlWidth = true;
            hShopTabs.childControlHeight = true;
            Button btnTabBalls    = CreateStyledButton("BtnTabBalls",    shopTabsRow.transform, new Vector2(260f, 56f), "BALONES",  ColElectric, ColWhite, 22);
            Button btnTabStadiums = CreateStyledButton("BtnTabStadiums", shopTabsRow.transform, new Vector2(260f, 56f), "ESTADIOS", ColNavyMid,  ColWhite, 22);

            GameObject pnlBallsList = CreateContainer("Panel_BallsList", pnlShop.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -55f), new Vector2(1800f, 620f));
            var hBalls = pnlBallsList.AddComponent<HorizontalLayoutGroup>();
            hBalls.spacing = 20f;
            hBalls.childControlWidth = true;
            hBalls.childControlHeight = true;
            foreach (var b in balls) CreateShopCard(pnlBallsList.transform, ShopItemCardUI.CardType.Ball, b, null);

            GameObject pnlStadiumsList = CreateContainer("Panel_StadiumsList", pnlShop.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -55f), new Vector2(1740f, 620f));
            var hStads = pnlStadiumsList.AddComponent<HorizontalLayoutGroup>();
            hStads.spacing = 24f;
            hStads.childControlWidth = true;
            hStads.childControlHeight = true;
            foreach (var s in stadiums) CreateShopCard(pnlStadiumsList.transform, ShopItemCardUI.CardType.Stadium, null, s);

            TMP_Text txtShopFeedback = CreateUIText("TxtShopFeedback", pnlShop.transform, new Vector2(0f, -430f), new Vector2(800f, 46f), "", 24, ColGreen, FontStyles.Bold);
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

            // ========== PANEL 4: CUSTOMIZATION (2 Separate Tabs + Large Preview Panel — ZERO Overlap!) ==========
            GameObject pnlCustom = CreateFullScreenPanel("Panel_Customization", canvasT, ColNavyDeep, null);
            Button btnBackCustom = CreateHeaderBar(pnlCustom.transform, "PERSONALIZACIÓN", "Equipa tus balones y estadios desbloqueados", out _);
            CustomizationUI customUI = pnlCustom.AddComponent<CustomizationUI>();

            GameObject customTabsRow = CreateContainer("CustomTabs", pnlCustom.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-260f, -155f), new Vector2(560f, 58f));
            var hCustomTabs = customTabsRow.AddComponent<HorizontalLayoutGroup>();
            hCustomTabs.spacing = 20f;
            hCustomTabs.childControlWidth = true;
            hCustomTabs.childControlHeight = true;
            Button btnCustomBalls    = CreateStyledButton("BtnCustomTabBalls",    customTabsRow.transform, new Vector2(260f, 54f), "BALONES",  ColElectric, ColWhite, 22);
            Button btnCustomStadiums = CreateStyledButton("BtnCustomTabStadiums", customTabsRow.transform, new Vector2(260f, 54f), "ESTADIOS", ColNavyMid,  ColWhite, 22);

            // Left Grid of Items
            GameObject customBallsTab = CreateContainer("Custom_Balls_Tab", pnlCustom.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-260f, -55f), new Vector2(1240f, 640f));
            var gBalls = customBallsTab.AddComponent<GridLayoutGroup>();
            gBalls.cellSize = new Vector2(230f, 430f);
            gBalls.spacing = new Vector2(18f, 18f);
            gBalls.childAlignment = TextAnchor.UpperCenter;
            foreach (var b in balls) CreateShopCard(customBallsTab.transform, ShopItemCardUI.CardType.Ball, b, null, isCustomizationOnly: true);

            GameObject customStadiumsTab = CreateContainer("Custom_Stadiums_Tab", pnlCustom.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-260f, -55f), new Vector2(1240f, 640f));
            var gStads = customStadiumsTab.AddComponent<GridLayoutGroup>();
            gStads.cellSize = new Vector2(285f, 440f);
            gStads.spacing = new Vector2(20f, 18f);
            gStads.childAlignment = TextAnchor.UpperCenter;
            foreach (var s in stadiums) CreateShopCard(customStadiumsTab.transform, ShopItemCardUI.CardType.Stadium, null, s, isCustomizationOnly: true);

            // Right Large Active Preview Inspector
            GameObject previewCard = CreateUIImage("ActivePreviewCard", pnlCustom.transform, new Vector2(610f, -40f), new Vector2(460f, 670f), ColCardBg, sCard);
            var vPrev = previewCard.AddComponent<VerticalLayoutGroup>();
            vPrev.spacing = 16f;
            vPrev.padding = new RectOffset(28, 28, 30, 30);
            vPrev.childAlignment = TextAnchor.UpperCenter;
            vPrev.childControlWidth = true;
            vPrev.childControlHeight = false;

            TMP_Text txtPrevCat    = CreateUIText("TxtPreviewCat", previewCard.transform, Vector2.zero, new Vector2(400f, 34f), "BALÓN EQUIPADO", 18, ColGold, FontStyles.Bold);
            Sprite defaultBallIcon = balls.Count > 0 && balls[0] != null ? balls[0].icon : null;
            GameObject imgPrevHolder = CreateUIImage("ImgActivePreview", previewCard.transform, Vector2.zero, new Vector2(360f, 280f), Color.white, defaultBallIcon);
            imgPrevHolder.GetComponent<Image>().preserveAspect = true;
            TMP_Text txtPrevTitle  = CreateUIText("TxtPreviewTitle", previewCard.transform, Vector2.zero, new Vector2(400f, 46f), "Balón Clásico", 30, ColWhite, FontStyles.Bold);
            TMP_Text txtPrevDesc   = CreateUIText("TxtPreviewDesc", previewCard.transform, Vector2.zero, new Vector2(400f, 90f), "", 20, ColMuted, FontStyles.Normal);
            TMP_Text txtStadMode   = CreateUIText("TxtStadiumMode", previewCard.transform, Vector2.zero, new Vector2(400f, 40f), "Modo Estadio: Automático", 18, ColElectric, FontStyles.Bold);
            Button btnAutoStad     = CreateStyledButton("BtnAutoRoundStadium", previewCard.transform, new Vector2(360f, 52f), "USAR ESTADIO DE CADA RONDA", ColElectric, ColWhite, 17);

            SetSerializedField(customUI, "btnTabBalls", btnCustomBalls);
            SetSerializedField(customUI, "btnTabStadiums", btnCustomStadiums);
            SetSerializedField(customUI, "panelBallsTab", customBallsTab);
            SetSerializedField(customUI, "panelStadiumsTab", customStadiumsTab);
            SetSerializedField(customUI, "ballsContainer", customBallsTab.transform);
            SetSerializedField(customUI, "stadiumsContainer", customStadiumsTab.transform);
            SetSerializedField(customUI, "imgActivePreview", imgPrevHolder.GetComponent<Image>());
            SetSerializedField(customUI, "txtPreviewTitle", txtPrevTitle);
            SetSerializedField(customUI, "txtPreviewCategory", txtPrevCat);
            SetSerializedField(customUI, "txtPreviewDesc", txtPrevDesc);
            SetSerializedField(customUI, "txtStadiumModeStatus", txtStadMode);
            SetSerializedField(customUI, "btnAutoRoundStadium", btnAutoStad);
            SetSerializedField(customUI, "btnBack", btnBackCustom);
            SetSerializedField(customUI, "mainMenuUI", menuUI);

            // ========== PANEL 5: ACHIEVEMENTS (Scrollable VerticalLayoutGroup + Progress Bars) ==========
            GameObject pnlAch = CreateFullScreenPanel("Panel_Achievements", canvasT, ColNavyDeep, null);
            Button btnBackAch = CreateHeaderBar(pnlAch.transform, "SISTEMA DE LOGROS", "Completa objetivos de carrera para ganar recompensas", out GameObject achHeaderRight);
            AchievementsUI achUI = pnlAch.AddComponent<AchievementsUI>();
            TMP_Text txtAchSummary = CreateUIText("TxtAchSummary", achHeaderRight.transform, Vector2.zero, new Vector2(320f, 44f), "Desbloqueados: 0 / 7", 22, ColGold, FontStyles.Bold);

            GameObject achListContainer = CreateContainer("AchList", pnlAch.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -52f), new Vector2(1420f, 830f));
            var vAch = achListContainer.AddComponent<VerticalLayoutGroup>();
            vAch.spacing = 10f;
            vAch.childControlWidth = true;
            vAch.childControlHeight = true;

            var achCards = new List<AchievementsUI.AchievementCard>();
            foreach (var ach in achievements)
            {
                achCards.Add(CreateAchievementCard(achListContainer.transform, ach));
            }
            SetSerializedField(achUI, "achievementCards", achCards.ToArray());
            SetSerializedField(achUI, "txtSummaryHeader", txtAchSummary);
            SetSerializedField(achUI, "btnBack", btnBackAch);
            SetSerializedField(achUI, "mainMenuUI", menuUI);

            // ========== PANEL 6: SETTINGS (Clean VerticalLayoutGroup Rows — ZERO Overlap!) ==========
            GameObject pnlSettings = CreateFullScreenPanel("Panel_Settings", canvasT, ColNavyDeep, null);
            Button btnBackSettings = CreateHeaderBar(pnlSettings.transform, "OPCIONES Y CONFIGURACIÓN", "Ajustes de audio, controles y gráficos", out _);
            SettingsUI settingsUI = pnlSettings.AddComponent<SettingsUI>();

            GameObject settingsCard = CreateUIImage("SettingsCard", pnlSettings.transform, new Vector2(0f, -40f), new Vector2(1180f, 760f), ColCardBg, sCard);
            var vSet = settingsCard.AddComponent<VerticalLayoutGroup>();
            vSet.spacing = 18f;
            vSet.padding = new RectOffset(48, 48, 34, 34);
            vSet.childAlignment = TextAnchor.UpperCenter;
            vSet.childControlWidth = true;
            vSet.childControlHeight = false;

            Slider sMaster = CreateSettingsSliderRow(settingsCard.transform, "Volumen General", 1.0f, out TMP_Text txtMasterVal);
            Slider sMus    = CreateSettingsSliderRow(settingsCard.transform, "Volumen Música",  0.8f, out TMP_Text txtMusVal);
            Slider sSfx    = CreateSettingsSliderRow(settingsCard.transform, "Efectos de Sonido", 0.9f, out TMP_Text txtSfxVal);
            Slider sSens   = CreateSettingsSliderRow(settingsCard.transform, "Sensibilidad Apuntado", 1.0f, out TMP_Text txtSensVal, 0.5f, 2.0f);

            CreateSettingsStepRow(settingsCard.transform, "Resolución", "1920 x 1080", out Button btnResP, out Button btnResN, out TMP_Text txtResVal);
            CreateSettingsToggleRow(settingsCard.transform, "Modo Pantalla", "PANTALLA COMPLETA", out Button btnToggleFS, out TMP_Text txtFsVal);
            CreateSettingsStepRow(settingsCard.transform, "Calidad Gráfica", "ALTA", out Button btnQualP, out Button btnQualN, out TMP_Text txtQualVal);

            // Restore defaults + Secret Dev Code row
            GameObject bottomSetRow = CreateContainer("SettingsBottomRow", settingsCard.transform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(1060f, 60f));
            var hBotSet = bottomSetRow.AddComponent<HorizontalLayoutGroup>();
            hBotSet.spacing = 18f;
            hBotSet.childControlWidth = false;
            hBotSet.childControlHeight = true;
            hBotSet.childAlignment = TextAnchor.MiddleLeft;

            Button btnResetDef = CreateStyledButton("BtnResetDefaults", bottomSetRow.transform, new Vector2(310f, 52f), "RESTAURAR PREDETERMINADOS", ColNavyMid, ColWhite, 17);
            TMP_InputField inpCode = CreateUIInputField("InputSecretCode", bottomSetRow.transform, Vector2.zero, new Vector2(320f, 52f), "Código promocional...");
            Button btnRedeemCode = CreateStyledButton("BtnRedeemCode", bottomSetRow.transform, new Vector2(160f, 52f), "CANJEAR", ColGold, ColNavyDeep, 19);
            TMP_Text txtCodeMsg = CreateUIText("TxtCodeMsg", settingsCard.transform, Vector2.zero, new Vector2(1040f, 32f), "", 19, ColGreen, FontStyles.Bold);
            txtCodeMsg.gameObject.SetActive(false);

            SetSerializedField(settingsUI, "sliderMaster", sMaster);
            SetSerializedField(settingsUI, "sliderMusic", sMus);
            SetSerializedField(settingsUI, "sliderSfx", sSfx);
            SetSerializedField(settingsUI, "sliderSensitivity", sSens);
            SetSerializedField(settingsUI, "txtMasterVal", txtMasterVal);
            SetSerializedField(settingsUI, "txtMusicVal", txtMusVal);
            SetSerializedField(settingsUI, "txtSfxVal", txtSfxVal);
            SetSerializedField(settingsUI, "txtSensitivityVal", txtSensVal);
            SetSerializedField(settingsUI, "btnResPrev", btnResP);
            SetSerializedField(settingsUI, "btnResNext", btnResN);
            SetSerializedField(settingsUI, "txtResolutionVal", txtResVal);
            SetSerializedField(settingsUI, "btnToggleFullscreen", btnToggleFS);
            SetSerializedField(settingsUI, "txtFullscreenVal", txtFsVal);
            SetSerializedField(settingsUI, "btnQualPrev", btnQualP);
            SetSerializedField(settingsUI, "btnQualNext", btnQualN);
            SetSerializedField(settingsUI, "txtQualityVal", txtQualVal);
            SetSerializedField(settingsUI, "btnResetDefaults", btnResetDef);
            SetSerializedField(settingsUI, "btnBack", btnBackSettings);
            SetSerializedField(settingsUI, "mainMenuUI", menuUI);
            SetSerializedField(settingsUI, "inputSecretCode", inpCode);
            SetSerializedField(settingsUI, "btnRedeemCode", btnRedeemCode);
            SetSerializedField(settingsUI, "txtCodeMessage", txtCodeMsg);

            // Wire MainMenuUI
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
        }
        #endregion

        #region 6. Match Scene Builder (FIFA Proportions + Camera + Broadcast HUD + Always-Active RoundEndManager)
        public static void BuildMatchScene()
        {
            LoadUISprites();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            string scenePath = "Assets/Scenes/MatchScene.unity";

            // Stadium Environment
            GameObject envPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/StadiumEnvironment.prefab");
            if (envPrefab != null) PrefabUtility.InstantiatePrefab(envPrefab);

            // Regulation Goal at z = 0
            GameObject goalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GoalPrefab.prefab");
            if (goalPrefab != null)
            {
                GameObject goalInstance = PrefabUtility.InstantiatePrefab(goalPrefab) as GameObject;
                goalInstance.transform.position = Vector3.zero;
            }

            // FIFA 0.22m Ball resting cleanly on turf at z = -11m (center y = 0.11m)
            GameObject ballPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BallPrefab.prefab");
            if (ballPrefab != null)
            {
                GameObject ballInstance = PrefabUtility.InstantiatePrefab(ballPrefab) as GameObject;
                ballInstance.transform.position = new Vector3(0f, 0.11f, -11f);
            }

            // Player Rubén González (1.80m, slightly left of ball, facing +Z toward goal)
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player_RubenGonzalez.prefab");
            if (playerPrefab != null)
            {
                GameObject playerInstance = PrefabUtility.InstantiatePrefab(playerPrefab) as GameObject;
                playerInstance.transform.position = new Vector3(-0.65f, 0f, -13.15f);
                playerInstance.transform.rotation = Quaternion.identity;
            }

            // Goalkeeper Malágon (1.90m, 0.45m in front of goal line, rotated 180° to face Rubén!)
            GameObject gkPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Goalkeeper_Malagon.prefab");
            if (gkPrefab != null)
            {
                GameObject gkInstance = PrefabUtility.InstantiatePrefab(gkPrefab) as GameObject;
                gkInstance.transform.position = new Vector3(0f, 0f, -0.45f);
                gkInstance.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }

            // Broadcast Penalty Camera WITH MatchCameraController attached!
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.45f, 0.72f, 0.95f);
            cam.fieldOfView = 46f;
            camObj.transform.position = new Vector3(0.55f, 2.05f, -15.4f);
            camObj.transform.LookAt(new Vector3(0f, 1.25f, 0f));
            camObj.AddComponent<AudioListener>();
            camObj.AddComponent<MatchCameraController>();

            // Directional Sun Light
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.97f, 0.90f);
            light.intensity = 1.35f;
            light.shadows = LightShadows.Soft;
            lightObj.transform.rotation = Quaternion.Euler(48f, -30f, 0f);

            // Penalty Manager
            GameObject penaltyMgrObj = new GameObject("PenaltyManager");
            penaltyMgrObj.AddComponent<PenaltyManager>();

            // EventSystem
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            // HUD Canvas
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
            Debug.Log("[ProjectSetupBuilder] MatchScene.unity rebuilt cleanly.");
        }

        private static void BuildMatchHUD(Transform canvasT)
        {
            MatchHUD hud = canvasT.gameObject.AddComponent<MatchHUD>();

            // --- Top Broadcast Scoreboard (Left Pill + Right Pill) ---
            GameObject topLeft = CreateUIImage("Scoreboard_TopLeft", canvasT, Vector2.zero, new Vector2(820f, 82f), ColNavyDeep, sPanel);
            RectTransform tlRt = topLeft.GetComponent<RectTransform>();
            tlRt.anchorMin = new Vector2(0f, 1f);
            tlRt.anchorMax = new Vector2(0f, 1f);
            tlRt.pivot = new Vector2(0f, 1f);
            tlRt.anchoredPosition = new Vector2(28f, -24f);
            var hTL = topLeft.AddComponent<HorizontalLayoutGroup>();
            hTL.spacing = 18f;
            hTL.padding = new RectOffset(22, 22, 10, 10);
            hTL.childAlignment = TextAnchor.MiddleLeft;
            hTL.childControlWidth = false;
            hTL.childControlHeight = true;

            TMP_Text txtRound    = CreateUIText("TxtRoundTitle",     topLeft.transform, Vector2.zero, new Vector2(260f, 52f), "RONDA 1: CANTERA", 22, ColGold, FontStyles.Bold);
            txtRound.alignment = TextAlignmentOptions.Left;
            TMP_Text txtGoals    = CreateUIText("TxtGoalsTarget",    topLeft.transform, Vector2.zero, new Vector2(145f, 52f), "Goles: 0 / 3", 22, ColWhite, FontStyles.Bold);
            TMP_Text txtAttempts = CreateUIText("TxtAttemptCounter", topLeft.transform, Vector2.zero, new Vector2(125f, 52f), "Penal 1 / 5", 20, ColMuted, FontStyles.Bold);

            // 5 Shot Indicator Circles inside Scoreboard
            GameObject dotsGroup = CreateContainer("ShotIndicatorsGroup", topLeft.transform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(190f, 44f));
            var hDots = dotsGroup.AddComponent<HorizontalLayoutGroup>();
            hDots.spacing = 10f;
            hDots.childAlignment = TextAnchor.MiddleCenter;
            hDots.childControlWidth = false;
            hDots.childControlHeight = false;
            Image[] shotDots = new Image[5];
            for (int i = 0; i < 5; i++)
            {
                GameObject dot = CreateUIImage($"Dot_{i}", dotsGroup.transform, Vector2.zero, new Vector2(26f, 26f), new Color(0.28f, 0.33f, 0.42f), sCircle);
                shotDots[i] = dot.GetComponent<Image>();
            }

            // Top Right Bar (Score, Coins, Pause Button)
            GameObject topRight = CreateContainer("Scoreboard_TopRight", canvasT, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -24f), new Vector2(540f, 76f));
            topRight.GetComponent<RectTransform>().pivot = new Vector2(1f, 1f);
            var hTR = topRight.AddComponent<HorizontalLayoutGroup>();
            hTR.spacing = 14f;
            hTR.childAlignment = TextAnchor.MiddleRight;
            hTR.childControlWidth = false;
            hTR.childControlHeight = true;

            CreatePillBadge("MatchScorePill", topRight.transform, 195f, ColNavyDeep, sTrophy, ColElectric, "0 pts", out TMP_Text txtScore);
            CreatePillBadge("MatchCoinsPill", topRight.transform, 185f, ColNavyDeep, sCoin, ColGold, "0", out TMP_Text txtCoins);

            Button btnPause = CreateStyledButton("BtnPause", topRight.transform, new Vector2(72f, 64f), "II", ColNavyMid, ColWhite, 24);
            if (sPause != null)
            {
                btnPause.GetComponentInChildren<TMP_Text>().text = "";
                GameObject pIcon = CreateUIImage("PauseIcon", btnPause.transform, Vector2.zero, new Vector2(30f, 30f), ColWhite, sPause);
                pIcon.GetComponent<Image>().raycastTarget = false;
            }

            // Streak Badge
            TMP_Text txtStreak = CreateUIText("TxtStreak", canvasT, new Vector2(0f, 360f), new Vector2(360f, 52f), "RACHA x2!", 34, ColGold, FontStyles.Bold);
            txtStreak.gameObject.SetActive(false);

            // --- Bottom Power Bar + Hint ---
            GameObject pwrContainer = CreateUIImage("PowerBarContainer", canvasT, new Vector2(0f, -415f), new Vector2(540f, 66f), ColNavyDeep, sPanel);
            TMP_Text txtPowerHint = CreateUIText("TxtPowerHint", pwrContainer.transform, new Vector2(0f, 16f), new Vector2(500f, 26f), "POTENCIA DE TIRO", 17, ColWhite, FontStyles.Bold);
            GameObject pwrTrack = CreateUIImage("PowerTrack", pwrContainer.transform, new Vector2(0f, -12f), new Vector2(500f, 22f), new Color(0.12f, 0.16f, 0.24f), sPill);
            GameObject pwrFillObj = CreateUIImage("PowerFill", pwrTrack.transform, Vector2.zero, new Vector2(494f, 16f), ColGreen, sPill);
            Image pwrFill = pwrFillObj.GetComponent<Image>();
            pwrFill.type = Image.Type.Filled;
            pwrFill.fillMethod = Image.FillMethod.Horizontal;
            pwrFill.fillOrigin = 0;
            pwrFill.fillAmount = 0.5f;

            // Compact Controls Legend Strip (bottom left)
            GameObject ctrlLegend = CreateUIImage("ControlsLegend", canvasT, Vector2.zero, new Vector2(490f, 44f), new Color(0.03f, 0.07f, 0.14f, 0.78f), sPill);
            RectTransform clRt = ctrlLegend.GetComponent<RectTransform>();
            clRt.anchorMin = new Vector2(0f, 0f);
            clRt.anchorMax = new Vector2(0f, 0f);
            clRt.pivot = new Vector2(0f, 0f);
            clRt.anchoredPosition = new Vector2(28f, 20f);
            CreateUIText("TxtLegend", ctrlLegend.transform, Vector2.zero, new Vector2(460f, 36f), "WASD: Mover  |  Mouse: Apuntar  |  Clic: Cargar y Disparar  |  ESC: Pausa", 15, ColWhite, FontStyles.Normal);

            // --- Result Banner ---
            GameObject bannerObj = CreateUIImage("Banner_Result", canvasT, new Vector2(0f, 210f), new Vector2(780f, 128f), ColGreen, sPanel);
            TMP_Text txtResultTitle = CreateUIText("TxtResultTitle", bannerObj.transform, new Vector2(0f, 22f), new Vector2(740f, 56f), "¡GOOOOL!", 44, ColWhite, FontStyles.Bold);
            TMP_Text txtResultSub   = CreateUIText("TxtResultSub",   bannerObj.transform, new Vector2(0f, -28f), new Vector2(740f, 36f), "+100 Pts  |  +100 Monedas", 22, ColGold, FontStyles.Bold);
            bannerObj.SetActive(false);

            // --- Pause Panel (ALWAYS ACTIVE PauseController, toggling panelPause inside!) ---
            GameObject pauseControllerObj = CreateContainer("PauseManager", canvasT, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920f, 1080f));
            PauseUI pauseUI = pauseControllerObj.AddComponent<PauseUI>();

            GameObject pnlPause = CreateFullScreenPanel("Panel_Pause", pauseControllerObj.transform, new Color(0.02f, 0.05f, 0.10f, 0.85f), null);
            GameObject pauseModal = CreateUIImage("PauseModal", pnlPause.transform, Vector2.zero, new Vector2(640f, 560f), ColNavyMid, sCard);
            CreateUIText("TitlePause", pauseModal.transform, new Vector2(0f, 215f), new Vector2(560f, 64f), "PAUSA", 44, ColGold, FontStyles.Bold);

            // Sub-group 1: Main Pause Buttons
            GameObject pauseMainGrp = CreateContainer("PauseMainActions", pauseModal.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -25f), new Vector2(440f, 360f));
            var vPause = pauseMainGrp.AddComponent<VerticalLayoutGroup>();
            vPause.spacing = 18f;
            vPause.childAlignment = TextAnchor.MiddleCenter;
            vPause.childControlWidth = true;
            vPause.childControlHeight = false;

            Button btnResume     = CreateStyledButton("BtnResume",     pauseMainGrp.transform, new Vector2(420f, 64f), "CONTINUAR",       ColGreen,    ColWhite, 24);
            Button btnRestart    = CreateStyledButton("BtnRestart",    pauseMainGrp.transform, new Vector2(420f, 60f), "REINICIAR RONDA", ColElectric, ColWhite, 22);
            Button btnPauseOpts  = CreateStyledButton("BtnOptions",    pauseMainGrp.transform, new Vector2(420f, 60f), "OPCIONES",        ColNavyDeep, ColWhite, 22);
            Button btnQuitToMenu = CreateStyledButton("BtnQuitToMenu", pauseMainGrp.transform, new Vector2(420f, 60f), "VOLVER AL MENÚ",  ColRed,      ColWhite, 22);

            // Sub-group 2: Clean Pause Inline Audio Options (NO overlap!)
            GameObject pauseOptsGrp = CreateContainer("PauseInlineOptions", pauseModal.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -25f), new Vector2(560f, 360f));
            var vPauseOpt = pauseOptsGrp.AddComponent<VerticalLayoutGroup>();
            vPauseOpt.spacing = 22f;
            vPauseOpt.childAlignment = TextAnchor.MiddleCenter;
            vPauseOpt.childControlWidth = true;
            vPauseOpt.childControlHeight = false;

            Slider pmMaster = CreateCompactSliderRow(pauseOptsGrp.transform, "General", 1.0f, out TMP_Text pmMasterTxt);
            Slider pmMus    = CreateCompactSliderRow(pauseOptsGrp.transform, "Música",  0.8f, out TMP_Text pmMusTxt);
            Slider pmSfx    = CreateCompactSliderRow(pauseOptsGrp.transform, "Efectos", 0.9f, out TMP_Text pmSfxTxt);
            Button btnBackOpts = CreateStyledButton("BtnBackFromOptions", pauseOptsGrp.transform, new Vector2(320f, 56f), "VOLVER", ColElectric, ColWhite, 22);
            pauseOptsGrp.SetActive(false);

            SetSerializedField(pauseUI, "panelPause", pnlPause);
            SetSerializedField(pauseUI, "groupMainActions", pauseMainGrp);
            SetSerializedField(pauseUI, "groupInlineOptions", pauseOptsGrp);
            SetSerializedField(pauseUI, "btnResume", btnResume);
            SetSerializedField(pauseUI, "btnRestart", btnRestart);
            SetSerializedField(pauseUI, "btnOptions", btnPauseOpts);
            SetSerializedField(pauseUI, "btnBackFromOptions", btnBackOpts);
            SetSerializedField(pauseUI, "btnQuitToMenu", btnQuitToMenu);
            SetSerializedField(pauseUI, "sliderMaster", pmMaster);
            SetSerializedField(pauseUI, "sliderMusic", pmMus);
            SetSerializedField(pauseUI, "sliderSfx", pmSfx);
            SetSerializedField(pauseUI, "txtMasterVal", pmMasterTxt);
            SetSerializedField(pauseUI, "txtMusicVal", pmMusTxt);
            SetSerializedField(pauseUI, "txtSfxVal", pmSfxTxt);
            pnlPause.SetActive(false);

            // --- Round End / Victory / Defeat / Grand Final (RoundEndUI on ALWAYS-ACTIVE RoundEndManager!) ---
            GameObject roundEndMgr = CreateContainer("RoundEndManager", canvasT, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920f, 1080f));
            RoundEndUI endUI = roundEndMgr.AddComponent<RoundEndUI>();

            GameObject pnlEnd = CreateFullScreenPanel("Panel_RoundEnd", roundEndMgr.transform, new Color(0.02f, 0.05f, 0.10f, 0.90f), null);

            // Normal Result Card
            GameObject pnlNormalResult = CreateUIImage("NormalResultGroup", pnlEnd.transform, Vector2.zero, new Vector2(780f, 740f), ColNavyMid, sCard);
            var vEnd = pnlNormalResult.AddComponent<VerticalLayoutGroup>();
            vEnd.spacing = 14f;
            vEnd.padding = new RectOffset(45, 45, 36, 36);
            vEnd.childAlignment = TextAnchor.UpperCenter;
            vEnd.childControlWidth = true;
            vEnd.childControlHeight = false;

            GameObject trophyImgObj = CreateUIImage("VictoryTrophyIcon", pnlNormalResult.transform, Vector2.zero, new Vector2(90f, 90f), ColGold, sTrophy);
            trophyImgObj.GetComponent<Image>().preserveAspect = true;
            TMP_Text txtEndTitle = CreateUIText("TxtEndTitle", pnlNormalResult.transform, Vector2.zero, new Vector2(680f, 64f), "¡RONDA SUPERADA!", 46, ColGreen, FontStyles.Bold);
            TMP_Text txtEndSub   = CreateUIText("TxtEndSub",   pnlNormalResult.transform, Vector2.zero, new Vector2(680f, 34f), "¡Clasificación asegurada!", 22, ColWhite, FontStyles.Normal);
            TMP_Text txtEndRound = CreateUIText("TxtEndRound", pnlNormalResult.transform, Vector2.zero, new Vector2(680f, 36f), "Ronda 1: Cantera", 24, ColGold, FontStyles.Bold);

            GameObject newRecBadge = CreateUIImage("NewRecordBadge", pnlNormalResult.transform, Vector2.zero, new Vector2(260f, 36f), ColGold, sPill);
            CreateUIText("TxtRec", newRecBadge.transform, Vector2.zero, new Vector2(250f, 32f), "¡NUEVO RÉCORD!", 18, ColNavyDeep, FontStyles.Bold);

            TMP_Text txtEndGoals    = CreateUIText("TxtEndGoals",    pnlNormalResult.transform, Vector2.zero, new Vector2(660f, 36f), "Goles: 3 / 3", 24, ColWhite, FontStyles.Bold);
            TMP_Text txtEndAttempts = CreateUIText("TxtEndAttempts", pnlNormalResult.transform, Vector2.zero, new Vector2(660f, 32f), "Intentos utilizados: 3 de 5", 21, ColMuted, FontStyles.Normal);
            TMP_Text txtEndScore    = CreateUIText("TxtEndScore",    pnlNormalResult.transform, Vector2.zero, new Vector2(660f, 36f), "Puntuación: 950 pts", 24, ColElectric, FontStyles.Bold);
            TMP_Text txtEndCoins    = CreateUIText("TxtEndCoins",    pnlNormalResult.transform, Vector2.zero, new Vector2(660f, 36f), "Monedas ganadas: +500", 24, ColGold, FontStyles.Bold);
            TMP_Text txtEndBonus    = CreateUIText("TxtEndBonus",    pnlNormalResult.transform, Vector2.zero, new Vector2(660f, 32f), "Bono de ronda: +500 pts | +300 monedas", 19, ColGreen, FontStyles.Normal);

            GameObject endBtnsRow = CreateContainer("EndButtonsGroup", pnlNormalResult.transform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(680f, 70f));
            var hEndBtns = endBtnsRow.AddComponent<HorizontalLayoutGroup>();
            hEndBtns.spacing = 14f;
            hEndBtns.childControlWidth = true;
            hEndBtns.childControlHeight = true;
            Button btnNext    = CreateStyledButton("BtnNextRound", endBtnsRow.transform, new Vector2(210f, 62f), "SIGUIENTE RONDA", ColGreen,    ColWhite, 19);
            Button btnRetry   = CreateStyledButton("BtnRetry",     endBtnsRow.transform, new Vector2(210f, 62f), "REPETIR RONDA",   ColElectric, ColWhite, 19);
            Button btnEndMenu = CreateStyledButton("BtnEndMenu",   endBtnsRow.transform, new Vector2(210f, 62f), "VOLVER AL MENÚ",  ColNavyDeep, ColWhite, 19);

            // Grand Champion Celebration Card
            GameObject pnlGrand = CreateUIImage("GrandChampionGroup", pnlEnd.transform, Vector2.zero, new Vector2(960f, 740f), ColNavyMid, sCard);
            var vChamp = pnlGrand.AddComponent<VerticalLayoutGroup>();
            vChamp.spacing = 18f;
            vChamp.padding = new RectOffset(50, 50, 40, 40);
            vChamp.childAlignment = TextAnchor.UpperCenter;
            vChamp.childControlWidth = true;
            vChamp.childControlHeight = false;

            GameObject champTrophy = CreateUIImage("ChampTrophyIcon", pnlGrand.transform, Vector2.zero, new Vector2(150f, 150f), ColGold, sTrophy);
            champTrophy.GetComponent<Image>().preserveAspect = true;
            TMP_Text txtChampTitle = CreateUIText("TxtChampTitle", pnlGrand.transform, Vector2.zero, new Vector2(860f, 76f), "¡CAMPEÓN DE ULTIMATE PENALTY CUP 3D!", 40, ColGold, FontStyles.Bold);
            TMP_Text txtChampScore = CreateUIText("TxtChampScore", pnlGrand.transform, Vector2.zero, new Vector2(860f, 44f), "Puntuación Final: 0 pts", 28, ColWhite, FontStyles.Bold);
            TMP_Text txtChampStats = CreateUIText("TxtChampStats", pnlGrand.transform, Vector2.zero, new Vector2(860f, 40f), "", 22, ColElectric, FontStyles.Normal);
            TMP_Text txtChampAch   = CreateUIText("TxtChampAch",   pnlGrand.transform, Vector2.zero, new Vector2(860f, 64f), "", 21, ColGreen, FontStyles.Bold);

            GameObject champBtnsRow = CreateContainer("ChampButtons", pnlGrand.transform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(820f, 70f));
            var hChampBtns = champBtnsRow.AddComponent<HorizontalLayoutGroup>();
            hChampBtns.spacing = 18f;
            hChampBtns.childControlWidth = true;
            hChampBtns.childControlHeight = true;
            Button btnChampAgain = CreateStyledButton("BtnChampPlayAgain", champBtnsRow.transform, new Vector2(250f, 64f), "VOLVER A JUGAR", ColGreen,    ColWhite, 21);
            Button btnChampAch   = CreateStyledButton("BtnChampAch",       champBtnsRow.transform, new Vector2(250f, 64f), "VER LOGROS",     ColGold,     ColNavyDeep, 21);
            Button btnChampMenu  = CreateStyledButton("BtnChampMenu",      champBtnsRow.transform, new Vector2(250f, 64f), "VOLVER AL MENÚ", ColElectric, ColWhite, 21);

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
            SetSerializedField(endUI, "txtRoundInfo", txtEndRound);
            SetSerializedField(endUI, "txtGoals", txtEndGoals);
            SetSerializedField(endUI, "txtAttempts", txtEndAttempts);
            SetSerializedField(endUI, "txtScore", txtEndScore);
            SetSerializedField(endUI, "txtCoinsGained", txtEndCoins);
            SetSerializedField(endUI, "txtBonusDetails", txtEndBonus);
            SetSerializedField(endUI, "badgeNewRecord", newRecBadge);
            SetSerializedField(endUI, "imgVictoryTrophy", trophyImgObj.GetComponent<Image>());
            SetSerializedField(endUI, "btnNextRound", btnNext);
            SetSerializedField(endUI, "btnRetry", btnRetry);
            SetSerializedField(endUI, "txtBtnRetry", btnRetry.GetComponentInChildren<TMP_Text>());
            SetSerializedField(endUI, "btnMainMenu", btnEndMenu);
            SetSerializedField(endUI, "txtChampionTitle", txtChampTitle);
            SetSerializedField(endUI, "txtChampionScore", txtChampScore);
            SetSerializedField(endUI, "txtChampionStats", txtChampStats);
            SetSerializedField(endUI, "txtChampionAchievements", txtChampAch);
            SetSerializedField(endUI, "btnChampionPlayAgain", btnChampAgain);
            SetSerializedField(endUI, "btnChampionAchievements", btnChampAch);
            SetSerializedField(endUI, "btnChampionMainMenu", btnChampMenu);
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
            SetSerializedField(hud, "txtPowerHint", txtPowerHint);
            SetSerializedField(hud, "bannerResult", bannerObj);
            SetSerializedField(hud, "txtResultTitle", txtResultTitle);
            SetSerializedField(hud, "txtResultSub", txtResultSub);
            SetSerializedField(hud, "imgBannerBg", bannerObj.GetComponent<Image>());
            SetSerializedField(hud, "btnPause", btnPause);
            SetSerializedField(hud, "pauseUI", pauseUI);
            SetSerializedField(hud, "imgShotIndicators", shotDots);
        }
        #endregion

        #region 7. Build Settings
        public static void SetupBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/MatchScene.unity", true)
            };
            Debug.Log("[ProjectSetupBuilder] Registered MainMenu (0) and MatchScene (1) in EditorBuildSettings (SampleScene excluded).");
        }
        #endregion

        #region UI Component Factory Helpers (Responsive Layouts + Zero Overlaps!)
        private static GameObject CreateFullScreenPanel(string name, Transform parent, Color color, Sprite sprite)
        {
            GameObject pnl = new GameObject(name, typeof(RectTransform));
            pnl.transform.SetParent(parent, false);
            RectTransform rt = pnl.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            if (color.a > 0.001f || sprite != null)
            {
                Image img = pnl.AddComponent<Image>();
                img.color = color;
                if (sprite != null) img.sprite = sprite;
            }
            return pnl;
        }

        private static GameObject CreateContainer(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return go;
        }

        private static Button CreateHeaderBar(Transform panelT, string title, string subtitle, out GameObject rightSlot)
        {
            GameObject bar = CreateUIImage("HeaderBar", panelT, Vector2.zero, new Vector2(0f, 110f), ColNavyMid, sPanel);
            RectTransform rt = bar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -12f);
            rt.sizeDelta = new Vector2(-48f, 98f);

            Button btnBack = CreateStyledButton("BtnBack", bar.transform, new Vector2(150f, 54f), "VOLVER", ColNavyDeep, ColWhite, 20);
            RectTransform bRt = btnBack.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0f, 0.5f);
            bRt.anchorMax = new Vector2(0f, 0.5f);
            bRt.pivot = new Vector2(0f, 0.5f);
            bRt.anchoredPosition = new Vector2(24f, 0f);

            TMP_Text tTitle = CreateUIText("HeaderTitle", bar.transform, new Vector2(-120f, 14f), new Vector2(880f, 44f), title, 34, ColGold, FontStyles.Bold);
            tTitle.alignment = TextAlignmentOptions.Left;
            RectTransform tRt = tTitle.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 0.5f);
            tRt.anchorMax = new Vector2(0f, 0.5f);
            tRt.pivot = new Vector2(0f, 0.5f);
            tRt.anchoredPosition = new Vector2(204f, 15f);

            TMP_Text tSub = CreateUIText("HeaderSub", bar.transform, new Vector2(-120f, -18f), new Vector2(960f, 28f), subtitle, 18, ColMuted, FontStyles.Normal);
            tSub.alignment = TextAlignmentOptions.Left;
            RectTransform sRt = tSub.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0f, 0.5f);
            sRt.anchorMax = new Vector2(0f, 0.5f);
            sRt.pivot = new Vector2(0f, 0.5f);
            sRt.anchoredPosition = new Vector2(204f, -18f);

            rightSlot = CreateContainer("HeaderRightSlot", bar.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(360f, 64f));
            rightSlot.GetComponent<RectTransform>().pivot = new Vector2(1f, 0.5f);
            return btnBack;
        }

        private static GameObject CreatePillBadge(string name, Transform parent, float width, Color bg, Sprite icon, Color textCol, string textVal, out TMP_Text txtOut)
        {
            GameObject pill = CreateUIImage(name, parent, Vector2.zero, new Vector2(width, 56f), bg, sPill);
            var h = pill.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 10f;
            h.padding = new RectOffset(16, 16, 6, 6);
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = false;
            h.childControlHeight = false;

            if (icon != null)
            {
                GameObject ic = CreateUIImage("Icon", pill.transform, Vector2.zero, new Vector2(30f, 30f), Color.white, icon);
                ic.GetComponent<Image>().preserveAspect = true;
            }
            txtOut = CreateUIText("TxtValue", pill.transform, Vector2.zero, new Vector2(width - 68f, 40f), textVal, 22, textCol, FontStyles.Bold);
            return pill;
        }

        private static RoundSelectUI.RoundCard CreateRoundCard(Transform parent, int rNum, RoundDataSO rData)
        {
            GameObject cObj = CreateUIImage($"Card_Round_{rNum}", parent, Vector2.zero, new Vector2(330f, 660f), ColCardBg, sCard);
            var v = cObj.AddComponent<VerticalLayoutGroup>();
            v.spacing = 10f;
            v.padding = new RectOffset(18, 18, 18, 18);
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true;
            v.childControlHeight = false;

            // Real Stadium Preview Image
            Sprite stadSprite = AssetDatabase.LoadAssetAtPath<Sprite>(rData != null ? $"Assets/Art/Previews/Preview_Stadium_{rData.stadiumTheme}.png" : "Assets/Art/Previews/Preview_Stadium_Cantera.png");
            GameObject prev = CreateUIImage("ImgStadiumPreview", cObj.transform, Vector2.zero, new Vector2(290f, 170f), Color.white, stadSprite);
            prev.GetComponent<Image>().preserveAspect = false;

            TMP_Text rTitle = CreateUIText("TxtRTitle", cObj.transform, Vector2.zero, new Vector2(290f, 68f), $"RONDA {rNum}\n{rData?.roundName.ToUpper()}", 25, ColGold, FontStyles.Bold);
            TMP_Text rDiff  = CreateUIText("TxtRDiff",  cObj.transform, Vector2.zero, new Vector2(290f, 28f), $"IA: {rData?.goalkeeperTypeTitle}", 17, ColElectric, FontStyles.Bold);
            TMP_Text rDesc  = CreateUIText("TxtRDesc",  cObj.transform, Vector2.zero, new Vector2(290f, 115f), rData?.description ?? "", 17, ColWhite, FontStyles.Normal);
            TMP_Text rTarg  = CreateUIText("TxtRTarget",cObj.transform, Vector2.zero, new Vector2(290f, 32f), $"Meta: {rData?.requiredGoals ?? 3} / {rData?.maxAttempts ?? 5} goles", 19, ColWhite, FontStyles.Bold);
            TMP_Text rRew   = CreateUIText("TxtRRewards",cObj.transform,Vector2.zero, new Vector2(290f, 32f), $"+{rData?.winCoins ?? 300} Monedas", 19, ColGreen, FontStyles.Bold);

            // Clean Lock Banner placed between info & play button (NEVER overlaps text!)
            GameObject lockFooter = CreateUIImage("LockFooter", cObj.transform, Vector2.zero, new Vector2(290f, 44f), new Color(0.14f, 0.16f, 0.22f, 0.96f), sPill);
            var hLock = lockFooter.AddComponent<HorizontalLayoutGroup>();
            hLock.spacing = 8f;
            hLock.childAlignment = TextAnchor.MiddleCenter;
            hLock.childControlWidth = false;
            hLock.childControlHeight = false;
            CreateUIImage("LockIcon", lockFooter.transform, Vector2.zero, new Vector2(22f, 22f), ColRed, sLock);
            TMP_Text txtLockReason = CreateUIText("LockReason", lockFooter.transform, Vector2.zero, new Vector2(210f, 32f), $"Supera Ronda {rNum - 1}", 16, ColRed, FontStyles.Bold);

            Button btnPlay = CreateStyledButton("BtnPlayRound", cObj.transform, new Vector2(280f, 56f), "JUGAR RONDA", ColGreen, ColWhite, 21);

            return new RoundSelectUI.RoundCard
            {
                roundNumber = rNum,
                cardObject = cObj,
                imgStadiumPreview = prev.GetComponent<Image>(),
                txtTitle = rTitle,
                txtDifficulty = rDiff,
                txtDescription = rDesc,
                txtTarget = rTarg,
                txtRewards = rRew,
                lockOverlay = lockFooter,
                txtLockReason = txtLockReason,
                btnPlay = btnPlay,
                txtBtnPlay = btnPlay.GetComponentInChildren<TMP_Text>()
            };
        }

        private static GameObject CreateShopCard(Transform parent, ShopItemCardUI.CardType type, BallItemSO ball, StadiumItemSO stadium, bool isCustomizationOnly = false)
        {
            string itemName = ball != null ? ball.displayName : (stadium != null ? stadium.displayName : "Item");
            int price = ball != null ? ball.price : (stadium != null ? stadium.price : 0);
            string desc = ball != null ? ball.description : (stadium != null ? stadium.description : "");
            Sprite iconSprite = ball != null ? ball.icon : (stadium != null ? stadium.icon : null);

            GameObject cardObj = CreateUIImage($"Card_{itemName}", parent, Vector2.zero, new Vector2(300f, 560f), ColCardBg, sCard);
            ShopItemCardUI cardUI = cardObj.AddComponent<ShopItemCardUI>();

            var v = cardObj.AddComponent<VerticalLayoutGroup>();
            v.spacing = 10f;
            v.padding = new RectOffset(16, 16, 16, 16);
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true;
            v.childControlHeight = false;

            TMP_Text tName = CreateUIText("TxtName", cardObj.transform, Vector2.zero, new Vector2(260f, 40f), itemName, 23, ColGold, FontStyles.Bold);
            GameObject preview = CreateUIImage("ImgPreview", cardObj.transform, Vector2.zero, new Vector2(240f, 180f), Color.white, iconSprite);
            preview.GetComponent<Image>().preserveAspect = true;
            TMP_Text tPrice = CreateUIText("TxtPrice", cardObj.transform, Vector2.zero, new Vector2(260f, 34f), price == 0 ? "GRATIS" : $"{price:N0} Monedas", 21, ColGold, FontStyles.Bold);
            TMP_Text tDesc  = CreateUIText("TxtDesc",  cardObj.transform, Vector2.zero, new Vector2(260f, 76f), desc, 16, ColMuted, FontStyles.Normal);
            Button btnAction = CreateStyledButton("BtnAction", cardObj.transform, new Vector2(240f, 52f), "COMPRAR", ColElectric, ColWhite, 20);

            SetSerializedField(cardUI, "cardType", type);
            SetSerializedField(cardUI, "ballItem", ball);
            SetSerializedField(cardUI, "stadiumItem", stadium);
            SetSerializedField(cardUI, "txtName", tName);
            SetSerializedField(cardUI, "txtPrice", tPrice);
            SetSerializedField(cardUI, "txtDescription", tDesc);
            SetSerializedField(cardUI, "imgPreview", preview.GetComponent<Image>());
            SetSerializedField(cardUI, "btnAction", btnAction);
            SetSerializedField(cardUI, "txtActionButton", btnAction.GetComponentInChildren<TMP_Text>());
            SetSerializedField(cardUI, "imgActionButton", btnAction.GetComponent<Image>());
            SetSerializedField(cardUI, "isCustomizationOnly", isCustomizationOnly);
            return cardObj;
        }

        private static AchievementsUI.AchievementCard CreateAchievementCard(Transform parent, AchievementSO ach)
        {
            GameObject card = CreateUIImage($"Card_Ach_{ach.id}", parent, Vector2.zero, new Vector2(1400f, 94f), ColCardBg, sCard);
            var h = card.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 18f;
            h.padding = new RectOffset(22, 22, 12, 12);
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = false;
            h.childControlHeight = false;

            GameObject medal = CreateUIImage("MedalIcon", card.transform, Vector2.zero, new Vector2(54f, 54f), ColGold, sTrophy);
            medal.GetComponent<Image>().preserveAspect = true;

            GameObject textCol = CreateContainer("InfoCol", card.transform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(580f, 68f));
            var vT = textCol.AddComponent<VerticalLayoutGroup>();
            vT.spacing = 4f;
            vT.childControlWidth = true;
            vT.childControlHeight = false;
            TMP_Text tTitle = CreateUIText("TxtTitle", textCol.transform, Vector2.zero, new Vector2(580f, 30f), ach.title, 22, ColGold, FontStyles.Bold);
            tTitle.alignment = TextAlignmentOptions.Left;
            TMP_Text tDesc  = CreateUIText("TxtDesc",  textCol.transform, Vector2.zero, new Vector2(580f, 30f), ach.description, 16, ColWhite, FontStyles.Normal);
            tDesc.alignment = TextAlignmentOptions.Left;

            // Progress Bar column
            GameObject progCol = CreateContainer("ProgressCol", card.transform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(280f, 60f));
            var vP = progCol.AddComponent<VerticalLayoutGroup>();
            vP.spacing = 6f;
            vP.childAlignment = TextAnchor.MiddleCenter;
            vP.childControlWidth = true;
            vP.childControlHeight = false;
            TMP_Text tProg = CreateUIText("TxtProg", progCol.transform, Vector2.zero, new Vector2(280f, 24f), "0 / 1", 16, ColMuted, FontStyles.Bold);
            GameObject progTrack = CreateUIImage("ProgTrack", progCol.transform, Vector2.zero, new Vector2(280f, 18f), ColNavyDeep, sPill);
            GameObject progFillObj = CreateUIImage("ProgFill", progTrack.transform, Vector2.zero, new Vector2(276f, 14f), ColElectric, sPill);
            Image progFill = progFillObj.GetComponent<Image>();
            progFill.type = Image.Type.Filled;
            progFill.fillMethod = Image.FillMethod.Horizontal;
            progFill.fillAmount = 0f;

            TMP_Text tReward = CreateUIText("TxtReward", card.transform, Vector2.zero, new Vector2(180f, 40f), $"+{ach.rewardCoins} Monedas", 18, ColGreen, FontStyles.Bold);

            GameObject badgeHolder = CreateContainer("StatusSlot", card.transform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(185f, 44f));
            GameObject badgeUnlock = CreateUIImage("BadgeUnlocked", badgeHolder.transform, Vector2.zero, new Vector2(180f, 42f), ColGreen, sPill);
            CreateUIText("TxtU", badgeUnlock.transform, Vector2.zero, new Vector2(170f, 36f), "DESBLOQUEADO", 16, ColWhite, FontStyles.Bold);
            GameObject badgeLock = CreateUIImage("BadgeLocked", badgeHolder.transform, Vector2.zero, new Vector2(180f, 42f), ColNavyDeep, sPill);
            CreateUIText("TxtL", badgeLock.transform, Vector2.zero, new Vector2(170f, 36f), "EN PROGRESO", 16, ColMuted, FontStyles.Bold);

            return new AchievementsUI.AchievementCard
            {
                achievementId = ach.id,
                cardObject = card,
                imgCardBg = card.GetComponent<Image>(),
                imgMedalIcon = medal.GetComponent<Image>(),
                txtTitle = tTitle,
                txtDescription = tDesc,
                imgProgressFill = progFill,
                txtProgress = tProg,
                txtReward = tReward,
                unlockedBadge = badgeUnlock,
                lockedBadge = badgeLock
            };
        }

        private static Slider CreateSettingsSliderRow(Transform parent, string label, float initialVal, out TMP_Text valText, float min = 0f, float max = 1f)
        {
            GameObject row = CreateContainer($"Row_{label}", parent, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(1060f, 54f));
            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 24f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = false;
            h.childControlHeight = false;

            TMP_Text lbl = CreateUIText("Label", row.transform, Vector2.zero, new Vector2(360f, 44f), label, 22, ColWhite, FontStyles.Bold);
            lbl.alignment = TextAlignmentOptions.Left;

            Slider s = CreateProperSlider($"Slider_{label}", row.transform, new Vector2(500f, 32f), min, max, initialVal);
            valText = CreateUIText("Value", row.transform, Vector2.zero, new Vector2(120f, 44f), "100%", 22, ColGold, FontStyles.Bold);
            return s;
        }

        private static Slider CreateCompactSliderRow(Transform parent, string label, float initialVal, out TMP_Text valText)
        {
            GameObject row = CreateContainer($"Row_{label}", parent, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(540f, 48f));
            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 16f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = false;
            h.childControlHeight = false;

            TMP_Text lbl = CreateUIText("Label", row.transform, Vector2.zero, new Vector2(130f, 40f), label, 20, ColWhite, FontStyles.Bold);
            lbl.alignment = TextAlignmentOptions.Left;

            Slider s = CreateProperSlider($"Slider_{label}", row.transform, new Vector2(280f, 28f), 0f, 1f, initialVal);
            valText = CreateUIText("Value", row.transform, Vector2.zero, new Vector2(80f, 40f), "100%", 20, ColGold, FontStyles.Bold);
            return s;
        }

        private static void CreateSettingsStepRow(Transform parent, string label, string defaultVal, out Button btnPrev, out Button btnNext, out TMP_Text valText)
        {
            GameObject row = CreateContainer($"Row_{label}", parent, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(1060f, 54f));
            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 24f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = false;
            h.childControlHeight = false;

            TMP_Text lbl = CreateUIText("Label", row.transform, Vector2.zero, new Vector2(360f, 44f), label, 22, ColWhite, FontStyles.Bold);
            lbl.alignment = TextAlignmentOptions.Left;

            btnPrev = CreateStyledButton("BtnPrev", row.transform, new Vector2(64f, 44f), "<", ColNavyDeep, ColGold, 22);
            valText = CreateUIText("Value", row.transform, Vector2.zero, new Vector2(348f, 44f), defaultVal, 21, ColWhite, FontStyles.Bold);
            btnNext = CreateStyledButton("BtnNext", row.transform, new Vector2(64f, 44f), ">", ColNavyDeep, ColGold, 22);
        }

        private static void CreateSettingsToggleRow(Transform parent, string label, string defaultVal, out Button btnToggle, out TMP_Text valText)
        {
            GameObject row = CreateContainer($"Row_{label}", parent, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(1060f, 54f));
            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 24f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = false;
            h.childControlHeight = false;

            TMP_Text lbl = CreateUIText("Label", row.transform, Vector2.zero, new Vector2(360f, 44f), label, 22, ColWhite, FontStyles.Bold);
            lbl.alignment = TextAlignmentOptions.Left;

            btnToggle = CreateStyledButton("BtnToggle", row.transform, new Vector2(524f, 46f), defaultVal, ColElectric, ColWhite, 20);
            valText = btnToggle.GetComponentInChildren<TMP_Text>();
        }

        private static Slider CreateProperSlider(string name, Transform parent, Vector2 size, float min, float max, float val)
        {
            GameObject sliderObj = new GameObject(name, typeof(RectTransform));
            sliderObj.transform.SetParent(parent, false);
            sliderObj.GetComponent<RectTransform>().sizeDelta = size;

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = val;

            GameObject bg = CreateUIImage("Background", sliderObj.transform, Vector2.zero, new Vector2(size.x, 12f), ColNavyDeep, sPill);
            RectTransform bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0f, 0.5f);
            bgRt.anchorMax = new Vector2(1f, 0.5f);
            bgRt.sizeDelta = new Vector2(0f, 12f);

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform faRt = fillArea.GetComponent<RectTransform>();
            faRt.anchorMin = new Vector2(0f, 0.5f);
            faRt.anchorMax = new Vector2(1f, 0.5f);
            faRt.sizeDelta = new Vector2(-16f, 12f);

            GameObject fill = CreateUIImage("Fill", fillArea.transform, Vector2.zero, Vector2.zero, ColElectric, sPill);
            RectTransform fRt = fill.GetComponent<RectTransform>();
            fRt.anchorMin = Vector2.zero;
            fRt.anchorMax = Vector2.one;
            fRt.sizeDelta = Vector2.zero;
            slider.fillRect = fRt;

            GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderObj.transform, false);
            RectTransform haRt = handleArea.GetComponent<RectTransform>();
            haRt.anchorMin = Vector2.zero;
            haRt.anchorMax = Vector2.one;
            haRt.sizeDelta = new Vector2(-16f, 0f);

            GameObject handle = CreateUIImage("Handle", handleArea.transform, Vector2.zero, new Vector2(26f, 26f), ColGold, sCircle);
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.targetGraphic = handle.GetComponent<Image>();

            return slider;
        }

        private static GameObject CreateUIImage(string name, Transform parent, Vector2 pos, Vector2 size, Color color, Sprite sprite)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            img.color = color;
            if (sprite != null)
            {
                img.sprite = sprite;
                if (sprite.border != Vector4.zero) img.type = Image.Type.Sliced;
            }
            return obj;
        }

        private static TMP_Text CreateUIText(string name, Transform parent, Vector2 pos, Vector2 size, string text, float fontSize, Color color, FontStyles style = FontStyles.Normal)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            return tmp;
        }

        private static Button CreateStyledButton(string name, Transform parent, Vector2 size, string label, Color bgColor, Color textColor, float fontSize)
        {
            GameObject btnObj = CreateUIImage(name, parent, Vector2.zero, size, bgColor, sBtn);
            Button btn = btnObj.AddComponent<Button>();

            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            cb.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            cb.selectedColor = Color.white;
            cb.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.65f);
            btn.colors = cb;

            btnObj.AddComponent<UIButtonAnimator>();
            TMP_Text t = CreateUIText("Text", btnObj.transform, Vector2.zero, size - new Vector2(16f, 8f), label, fontSize, textColor, FontStyles.Bold);
            t.raycastTarget = false;
            return btn;
        }

        private static TMP_InputField CreateUIInputField(string name, Transform parent, Vector2 pos, Vector2 size, string placeholder)
        {
            GameObject inpObj = CreateUIImage(name, parent, pos, size, ColNavyDeep, sBtn);
            TMP_InputField inp = inpObj.AddComponent<TMP_InputField>();

            GameObject textArea = CreateContainer("Text Area", inpObj.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-24f, -12f));
            textArea.AddComponent<RectMask2D>();

            TMP_Text txt = CreateUIText("Text", textArea.transform, Vector2.zero, Vector2.zero, "", 19, ColWhite);
            RectTransform tRt = txt.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one; tRt.sizeDelta = Vector2.zero;
            txt.alignment = TextAlignmentOptions.Left;

            TMP_Text ph = CreateUIText("Placeholder", textArea.transform, Vector2.zero, Vector2.zero, placeholder, 19, ColMuted);
            RectTransform pRt = ph.GetComponent<RectTransform>();
            pRt.anchorMin = Vector2.zero; pRt.anchorMax = Vector2.one; pRt.sizeDelta = Vector2.zero;
            ph.alignment = TextAlignmentOptions.Left;

            inp.textViewport = textArea.GetComponent<RectTransform>();
            inp.textComponent = txt;
            inp.placeholder = ph;
            return inp;
        }

        private static void SetSerializedField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(target, value);
        }
        #endregion
    }
}
