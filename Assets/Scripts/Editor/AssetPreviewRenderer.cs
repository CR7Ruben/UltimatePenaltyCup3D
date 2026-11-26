using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UltimatePenaltyCup.Core;
using UltimatePenaltyCup.Gameplay;

namespace UltimatePenaltyCup.Editor
{
    /// <summary>
    /// Renders real 3D studio & stadium snapshot PNG sprites for the Shop, Round Select,
    /// and Customization panels so zero items ever appear as plain colored squares.
    /// Must run with GPU active (no -nographics).
    /// </summary>
    public static class AssetPreviewRenderer
    {
        private const string PREVIEWS_DIR = "Assets/Art/Previews";

        [MenuItem("Ultimate Penalty Cup/Visual Upgrade/Render 3D Shop & Stadium Previews", false, 99)]
        public static void RenderAllPreviews()
        {
            if (!Directory.Exists(PREVIEWS_DIR)) Directory.CreateDirectory(PREVIEWS_DIR);

            string currentScenePath = EditorSceneManager.GetActiveScene().path;
            var previewScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            try
            {
                RenderBallPreviews();
                RenderStadiumPreviews();
            }
            finally
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                if (!string.IsNullOrEmpty(currentScenePath) && File.Exists(currentScenePath))
                {
                    EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
                }
            }

            Debug.Log("[AssetPreviewRenderer] Rendered all real 3D ball and stadium preview sprites.");
        }

        private static void RenderBallPreviews()
        {
            GameObject camObj = new GameObject("PreviewCam");
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.11f, 0.22f, 1f);
            cam.fieldOfView = 30f;
            camObj.transform.position = new Vector3(0f, 0.12f, -1.05f);
            camObj.transform.LookAt(Vector3.zero);

            GameObject keyLightObj = new GameObject("KeyLight");
            Light key = keyLightObj.AddComponent<Light>();
            key.type = LightType.Directional;
            key.intensity = 1.6f;
            key.color = Color.white;
            keyLightObj.transform.rotation = Quaternion.Euler(35f, -35f, 0f);

            GameObject rimLightObj = new GameObject("RimLight");
            Light rim = rimLightObj.AddComponent<Light>();
            rim.type = LightType.Directional;
            rim.intensity = 0.8f;
            rim.color = new Color(0.35f, 0.75f, 1.0f);
            rimLightObj.transform.rotation = Quaternion.Euler(-20f, 145f, 0f);

            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.transform.localScale = Vector3.one * 0.42f;
            ball.transform.rotation = Quaternion.Euler(18f, -28f, 12f);

            var ballConfigs = new (string file, string matPath)[]
            {
                ("Preview_Ball_Classic.png", "Assets/Materials/Mat_Ball_Classic.mat"),
                ("Preview_Ball_Blue.png",    "Assets/Materials/Mat_Ball_Blue.mat"),
                ("Preview_Ball_Fire.png",    "Assets/Materials/Mat_Ball_Fire.mat"),
                ("Preview_Ball_Rainbow.png", "Assets/Materials/Mat_Ball_Rainbow.mat"),
                ("Preview_Ball_Gold.png",    "Assets/Materials/Mat_Ball_Gold.mat")
            };

            cam.backgroundColor = new Color(0f, 0f, 0f, 0f); // Transparent studio cutout!
            foreach (var cfg in ballConfigs)
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>(cfg.matPath);
                if (m != null) ball.GetComponent<Renderer>().sharedMaterial = m;
                CaptureCameraToSprite(cam, 512, 512, cfg.file, transparent: true);
            }

            Object.DestroyImmediate(ball);
            Object.DestroyImmediate(keyLightObj);
            Object.DestroyImmediate(rimLightObj);
            Object.DestroyImmediate(camObj);
        }

        private static void RenderStadiumPreviews()
        {
            GameObject stadiumPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/StadiumEnvironment.prefab");
            GameObject goalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GoalPrefab.prefab");
            if (stadiumPrefab == null) return;

            GameObject stadiumInst = PrefabUtility.InstantiatePrefab(stadiumPrefab) as GameObject;
            GameObject goalInst = goalPrefab != null ? PrefabUtility.InstantiatePrefab(goalPrefab) as GameObject : null;
            if (goalInst != null) goalInst.transform.position = Vector3.zero;

            GameObject lightObj = new GameObject("Directional Light");
            Light sun = lightObj.AddComponent<Light>();
            sun.type = LightType.Directional;

            GameObject camObj = new GameObject("StadiumPreviewCam");
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.fieldOfView = 50f;
            camObj.transform.position = new Vector3(-16f, 11.5f, -26f);
            camObj.transform.LookAt(new Vector3(0f, 2.5f, 8f));

            StadiumController sc = stadiumInst.GetComponent<StadiumController>();
            var venues = new (StadiumType type, string file, Color bg, Color sunCol, float sunInt, Vector3 sunRot)[]
            {
                (StadiumType.Cantera,      "Preview_Stadium_Cantera.png",      new Color(0.45f, 0.72f, 0.95f), new Color(1.0f, 0.97f, 0.90f), 1.35f, new Vector3(48f, -30f, 0f)),
                (StadiumType.Night,        "Preview_Stadium_Night.png",        new Color(0.03f, 0.06f, 0.14f), new Color(0.65f, 0.78f, 1.0f), 0.75f, new Vector3(65f, 25f, 0f)),
                (StadiumType.Professional, "Preview_Stadium_Professional.png", new Color(0.16f, 0.26f, 0.46f), new Color(1.0f, 0.90f, 0.78f), 1.2f,  new Vector3(36f, -55f, 0f)),
                (StadiumType.WorldCup,     "Preview_Stadium_WorldCup.png",     new Color(0.06f, 0.12f, 0.28f), new Color(1.0f, 0.96f, 0.88f), 1.4f,  new Vector3(52f, -32f, 0f))
            };

            foreach (var v in venues)
            {
                sun.color = v.sunCol;
                sun.intensity = v.sunInt;
                sun.transform.rotation = Quaternion.Euler(v.sunRot);
                cam.backgroundColor = v.bg;
                if (sc != null) sc.ApplyVenue(v.type);
                CaptureCameraToSprite(cam, 640, 360, v.file);
            }

            Object.DestroyImmediate(camObj);
            Object.DestroyImmediate(lightObj);
            if (goalInst != null) Object.DestroyImmediate(goalInst);
            if (stadiumInst != null) Object.DestroyImmediate(stadiumInst);
        }

        private static void CaptureCameraToSprite(Camera cam, int width, int height, string fileName, bool transparent = false)
        {
            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);

            string path = Path.Combine(PREVIEWS_DIR, fileName).Replace('\\', '/');
            byte[] pngBytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            for (int attempt = 0; attempt < 4; attempt++)
            {
                try
                {
                    File.WriteAllBytes(path, pngBytes);
                    break;
                }
                catch (IOException) when (attempt < 3)
                {
                    System.GC.Collect();
                    System.GC.WaitForPendingFinalizers();
                    AssetDatabase.ReleaseCachedFileHandles();
                    System.Threading.Thread.Sleep(60);
                }
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = transparent;
                imp.mipmapEnabled = false;
                imp.SaveAndReimport();
            }
        }
    }
}
