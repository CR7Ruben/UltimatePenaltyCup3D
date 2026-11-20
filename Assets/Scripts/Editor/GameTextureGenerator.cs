using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UltimatePenaltyCup.Core;
using UltimatePenaltyCup.Gameplay;

namespace UltimatePenaltyCup.Editor
{
    public static class GameTextureGenerator
    {
        private const string TEXTURE_FOLDER = "Assets/Art/Textures";

        [MenuItem("Ultimate Penalty Cup/Generate All Game Textures", false, 3)]
        public static void GenerateAllTextures()
        {
            Debug.Log("==========================================================");
            Debug.Log(">>> GENERATING COMPLETE HIGH-RESOLUTION GAME TEXTURES <<<");
            Debug.Log("==========================================================");

            if (!Directory.Exists(TEXTURE_FOLDER))
            {
                Directory.CreateDirectory(TEXTURE_FOLDER);
            }

            try
            {
                AssetDatabase.StartAssetEditing();

                // 1. Grass & Pitch Textures
                GenerateGrassTexture("Tex_Grass_Cantera", new Color(0.18f, 0.52f, 0.16f), new Color(0.22f, 0.58f, 0.20f), StripeType.Vertical);
                GenerateGrassTexture("Tex_Grass_Night", new Color(0.08f, 0.32f, 0.12f), new Color(0.11f, 0.38f, 0.15f), StripeType.MownCheckers);
                GenerateGrassTexture("Tex_Grass_Pro", new Color(0.12f, 0.62f, 0.22f), new Color(0.16f, 0.70f, 0.26f), StripeType.Striped);
                GenerateGrassTexture("Tex_Grass_WorldCup", new Color(0.14f, 0.68f, 0.26f), new Color(0.18f, 0.76f, 0.32f), StripeType.MownCheckers);

                // 2. Goal Net Cutout Texture
                GenerateGoalNetTexture("Tex_Goal_Net");

                // 3. Soccer Ball Textures (5 Variants)
                GenerateClassicBallTexture("Tex_Ball_Classic");
                GenerateBlueBallTexture("Tex_Ball_Blue");
                GenerateFireBallTexture("Tex_Ball_Fire");
                GenerateRainbowBallTexture("Tex_Ball_Rainbow");
                GenerateGoldBallTexture("Tex_Ball_Gold");

                // 4. Kits & Characters
                GeneratePlayerJerseyTexture("Tex_Jersey_Ruben");
                GenerateGoalkeeperJerseyTexture("Tex_Jersey_Malagon");
                GeneratePlayerFaceTexture("Tex_Face_Ruben", new Color(0.92f, 0.76f, 0.64f), new Color(0.18f, 0.12f, 0.08f));
                GeneratePlayerFaceTexture("Tex_Face_Malagon", new Color(0.88f, 0.72f, 0.60f), new Color(0.12f, 0.10f, 0.08f));
                GenerateGlovesTexture("Tex_Gloves_Malagon");

                // 5. Stadium Environment
                GenerateStadiumBannersTexture("Tex_Stadium_Banners");
                GenerateCrowdTexture("Tex_Stadium_Crowd");
                GenerateStandsConcreteTexture("Tex_Stadium_Stands");
                GenerateTrophyTexture("Tex_Trophy_Gold");
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            ConfigureTextureImportSettings();
            ApplyTexturesToAllMaterials();

            Debug.Log("==========================================================");
            Debug.Log(">>> ALL TEXTURES SUCCESSFULLY GENERATED & APPLIED! <<<");
            Debug.Log("==========================================================");
        }

        #region 1. Grass Textures
        private enum StripeType { Striped, Vertical, MownCheckers }

        private static void GenerateGrassTexture(string fileName, Color baseCol, Color stripeCol, StripeType type)
        {
            int width = 512;
            int height = 512;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float factor = 0f;
                    if (type == StripeType.Striped)
                    {
                        factor = Mathf.Sin((float)y / height * Mathf.PI * 16f) * 0.5f + 0.5f;
                    }
                    else if (type == StripeType.Vertical)
                    {
                        factor = Mathf.Sin((float)x / width * Mathf.PI * 16f) * 0.5f + 0.5f;
                    }
                    else // Checkers
                    {
                        bool cx = (x / 32) % 2 == 0;
                        bool cy = (y / 32) % 2 == 0;
                        factor = (cx ^ cy) ? 1f : 0f;
                    }

                    Color c = Color.Lerp(baseCol, stripeCol, factor);

                    // Add micro turf noise (grass blades)
                    float noise = Mathf.PerlinNoise(x * 0.25f, y * 0.25f) * 0.14f - 0.07f;
                    float speckle = (UnityEngine.Random.value - 0.5f) * 0.04f;
                    c.r = Mathf.Clamp01(c.r + noise + speckle);
                    c.g = Mathf.Clamp01(c.g + noise * 1.2f + speckle);
                    c.b = Mathf.Clamp01(c.b + noise * 0.8f + speckle);

                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            SaveTextureToFile(tex, fileName);
        }
        #endregion

        #region 2. Goal Net Texture
        private static void GenerateGoalNetTexture(string fileName)
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color netColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            Color clearColor = new Color(0f, 0f, 0f, 0f);

            int cellSize = 32;
            int cordThickness = 3;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Diamond net pattern: (x + y) % cellSize < cordThickness || (x - y + size) % cellSize < cordThickness
                    int d1 = (x + y) % cellSize;
                    int d2 = ((x - y) % cellSize + cellSize) % cellSize;

                    bool isCord = d1 < cordThickness || d2 < cordThickness;
                    // Knots at intersections
                    bool isKnot = (d1 < cordThickness + 1) && (d2 < cordThickness + 1);

                    if (isKnot)
                    {
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, 1f));
                    }
                    else if (isCord)
                    {
                        tex.SetPixel(x, y, netColor);
                    }
                    else
                    {
                        tex.SetPixel(x, y, clearColor);
                    }
                }
            }
            tex.Apply();
            SaveTextureToFile(tex, fileName);
        }
        #endregion

        #region 3. Ball Textures
        private static void GenerateClassicBallTexture(string fileName)
        {
            int size = 512;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color whiteLeather = new Color(0.96f, 0.96f, 0.96f);
            Color blackPatch = new Color(0.1f, 0.1f, 0.12f);
            Color seamCol = new Color(0.7f, 0.7f, 0.72f);

            Vector2[] patchCenters = new Vector2[]
            {
                new Vector2(128, 128), new Vector2(384, 128),
                new Vector2(256, 256),
                new Vector2(128, 384), new Vector2(384, 384),
                new Vector2(0, 256), new Vector2(512, 256),
                new Vector2(256, 0), new Vector2(256, 512)
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    float minDist = float.MaxValue;

                    foreach (var c in patchCenters)
                    {
                        float d = Vector2.Distance(p, c);
                        if (d < minDist) minDist = d;
                    }

                    Color pixel;
                    if (minDist < 48f)
                    {
                        pixel = blackPatch;
                    }
                    else if (minDist < 54f)
                    {
                        pixel = seamCol; // Seam line
                    }
                    else
                    {
                        // Subtle leather grain
                        float grain = (Mathf.PerlinNoise(x * 0.4f, y * 0.4f) - 0.5f) * 0.06f;
                        pixel = new Color(whiteLeather.r + grain, whiteLeather.g + grain, whiteLeather.b + grain);
                    }

                    // Gold brand stamp in center patch
                    if (Vector2.Distance(p, new Vector2(256, 256)) < 24f)
                    {
                        pixel = new Color(1f, 0.82f, 0.15f); // Golden star / logo
                    }

                    tex.SetPixel(x, y, pixel);
                }
            }
            tex.Apply();
            SaveTextureToFile(tex, fileName);
        }

        private static void GenerateBlueBallTexture(string fileName)
        {
            int size = 512;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color deepBlue = new Color(0.04f, 0.15f, 0.45f);
            Color brightCyan = new Color(0.1f, 0.85f, 1.0f);
            Color neonWhite = new Color(0.9f, 0.98f, 1.0f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float gridX = Mathf.Abs(((x % 64) - 32) / 32f);
                    float gridY = Mathf.Abs(((y % 64) - 32) / 32f);
                    float hexDist = Mathf.Max(gridX, gridY);

                    Color pixel;
                    if (hexDist > 0.88f)
                    {
                        pixel = neonWhite; // Glowing wireframe seams
                    }
                    else if (hexDist > 0.72f)
                    {
                        pixel = brightCyan;
                    }
                    else
                    {
                        float glow = (1f - hexDist) * 0.35f;
                        pixel = new Color(deepBlue.r + glow, deepBlue.g + glow * 1.5f, deepBlue.b + glow * 2f);
                    }

                    tex.SetPixel(x, y, pixel);
                }
            }
            tex.Apply();
            SaveTextureToFile(tex, fileName);
        }

        private static void GenerateFireBallTexture(string fileName)
        {
            int size = 512;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color darkMagma = new Color(0.2f, 0.05f, 0.02f);
            Color brightFire = new Color(1f, 0.45f, 0.05f);
            Color coreGold = new Color(1f, 0.9f, 0.2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float n1 = Mathf.PerlinNoise(x * 0.03f, y * 0.03f);
                    float n2 = Mathf.PerlinNoise(x * 0.08f + 12f, y * 0.08f + 50f);
                    float fireIntensity = Mathf.Clamp01(n1 * 0.7f + n2 * 0.5f);

                    Color pixel;
                    if (fireIntensity > 0.75f)
                    {
                        pixel = Color.Lerp(brightFire, coreGold, (fireIntensity - 0.75f) * 4f);
                    }
                    else if (fireIntensity > 0.35f)
                    {
                        pixel = Color.Lerp(darkMagma, brightFire, (fireIntensity - 0.35f) * 2.5f);
                    }
                    else
                    {
                        pixel = darkMagma;
                    }

                    // Seam cuts
                    if ((x % 64 < 3) || (y % 64 < 3))
                    {
                        pixel = coreGold * 1.2f; // Glowing crack seams
                    }

                    tex.SetPixel(x, y, pixel);
                }
            }
            tex.Apply();
            SaveTextureToFile(tex, fileName);
        }

        private static void GenerateRainbowBallTexture(string fileName)
        {
            int size = 512;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float angle = Mathf.Atan2(y - 256, x - 256) / (Mathf.PI * 2f) + 0.5f;
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(256, 256)) / 256f;
                    float hue = (angle + dist * 0.5f) % 1f;

                    Color rainbow = Color.HSVToRGB(hue, 0.85f, 0.95f);

                    // Add geometric faceted sheen
                    if ((x % 32 < 2) || (y % 32 < 2))
                    {
                        rainbow = Color.white;
                    }

                    tex.SetPixel(x, y, rainbow);
                }
            }
            tex.Apply();
            SaveTextureToFile(tex, fileName);
        }

        private static void GenerateGoldBallTexture(string fileName)
        {
            int size = 512;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color deepGold = new Color(0.72f, 0.52f, 0.12f);
            Color brightGold = new Color(1.0f, 0.86f, 0.25f);
            Color shineWhite = new Color(1.0f, 0.98f, 0.8f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float hex = Mathf.Sin((x + y) * 0.15f) * Mathf.Cos((x - y) * 0.15f);
                    float shine = Mathf.Clamp01(hex * 0.5f + 0.5f);

                    Color pixel = Color.Lerp(deepGold, brightGold, shine);

                    // Engraved pentagon edges
                    if (x % 48 < 3 || y % 48 < 3)
                    {
                        pixel = Color.Lerp(pixel, shineWhite, 0.6f);
                    }

                    tex.SetPixel(x, y, pixel);
                }
            }
            tex.Apply();
            SaveTextureToFile(tex, fileName);
        }
        #endregion

        #region 4. Jerseys & Character Textures
        private static void GeneratePlayerJerseyTexture(string fileName)
        {
            int width = 512;
            int height = 512;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);

            Color royalBlue = new Color(0.08f, 0.28f, 0.72f);
            Color darkBlue = new Color(0.04f, 0.14f, 0.42f);
            Color goldTrim = new Color(1.0f, 0.82f, 0.15f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Vertical athletic stripes
                    bool isStripe = (x / 24) % 2 == 0;
                    Color c = isStripe ? royalBlue : darkBlue;

                    // Gold collar and shoulders
                    if (y > 440)
                    {
                        c = goldTrim;
                    }
                    // Gold side panels
                    if (x < 30 || x > 482)
                    {
                        c = goldTrim;
                    }

                    // Number 10 on back (center area x: 210..302, y: 180..320)
                    if (x >= 210 && x <= 236 && y >= 190 && y <= 310) c = Color.white; // "1"
                    if (x >= 260 && x <= 302 && y >= 190 && y <= 310)
                    {
                        // "0" outline
                        if (x <= 270 || x >= 292 || y <= 205 || y >= 295) c = Color.white;
                    }

                    // A&R Crest on chest (x: 120..160, y: 350..390)
                    if (x >= 120 && x <= 160 && y >= 350 && y <= 390)
                    {
                        c = goldTrim;
                    }

                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            SaveTextureToFile(tex, fileName);
        }

        private static void GenerateGoalkeeperJerseyTexture(string fileName)
        {
            int width = 512;
            int height = 512;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);

            Color brightOrange = new Color(1.0f, 0.45f, 0.05f);
            Color darkCarbon = new Color(0.12f, 0.12f, 0.15f);
            Color neonYellow = new Color(0.95f, 1.0f, 0.2f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Chevron pattern (V-shape)
                    int chevron = Mathf.Abs((x - 256)) + y;
                    bool isBand = (chevron / 28) % 2 == 0;

                    Color c = isBand ? brightOrange : darkCarbon;

                    // Neon collar and sleeves
                    if (y > 450 || x < 25 || x > 487)
                    {
                        c = neonYellow;
                    }

                    // Goalkeeper "#1" on back
                    if (x >= 244 && x <= 268 && y >= 190 && y <= 310) c = Color.white;

                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            SaveTextureToFile(tex, fileName);
        }

        private static void GeneratePlayerFaceTexture(string fileName, Color skinTone, Color hairTone)
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Color c = skinTone;

                    // Hair on top
                    if (y > 180)
                    {
                        c = hairTone;
                    }

                    // Eyebrows (y: 135..145)
                    bool leftBrow = x >= 65 && x <= 110 && y >= 135 && y <= 145;
                    bool rightBrow = x >= 146 && x <= 191 && y >= 135 && y <= 145;
                    if (leftBrow || rightBrow) c = hairTone;

                    // Eyes (y: 115..128)
                    bool leftEye = x >= 72 && x <= 103 && y >= 115 && y <= 128;
                    bool rightEye = x >= 153 && x <= 184 && y >= 115 && y <= 128;
                    if (leftEye || rightEye)
                    {
                        // Pupil
                        bool isPupil = (x >= 82 && x <= 93) || (x >= 163 && x <= 174);
                        c = isPupil ? new Color(0.1f, 0.08f, 0.05f) : Color.white;
                    }

                    // Mouth / Smile (y: 60..70)
                    if (x >= 95 && x <= 161 && y >= 60 && y <= 68)
                    {
                        c = new Color(0.78f, 0.35f, 0.38f);
                    }

                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            SaveTextureToFile(tex, fileName);
        }

        private static void GenerateGlovesTexture(string fileName)
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color latexWhite = new Color(0.95f, 0.95f, 0.95f);
            Color gripNeon = new Color(0.1f, 0.9f, 0.3f);
            Color strapBlack = new Color(0.12f, 0.12f, 0.15f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Color c = latexWhite;

                    // Silicone grip ridges
                    if ((y % 16 < 4) && y < 190)
                    {
                        c = gripNeon;
                    }

                    // Wrist strap
                    if (y < 45)
                    {
                        c = strapBlack;
                    }

                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            SaveTextureToFile(tex, fileName);
        }
        #endregion

        #region 5. Stadium & Surroundings Textures
        private static void GenerateStadiumBannersTexture(string fileName)
        {
            int width = 1024;
            int height = 128;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);

            Color navyBg = new Color(0.04f, 0.10f, 0.28f);
            Color cyanGlow = new Color(0.05f, 0.85f, 1.0f);
            Color goldCol = new Color(1.0f, 0.85f, 0.15f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color c = navyBg;

                    // Top and bottom LED neon glow strips
                    if (y < 8 || y > 120)
                    {
                        c = cyanGlow;
                    }

                    // Section dividers
                    if (x % 256 < 6)
                    {
                        c = goldCol;
                    }

                    // Digital scanline pattern
                    if (y % 4 == 0)
                    {
                        c *= 0.85f;
                    }

                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            SaveTextureToFile(tex, fileName);
        }

        private static void GenerateCrowdTexture(string fileName)
        {
            int width = 512;
            int height = 256;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);

            Color[] fanColors = new Color[]
            {
                new Color(0.08f, 0.32f, 0.85f), // Blue jersey
                new Color(0.95f, 0.82f, 0.15f), // Gold scarf
                new Color(0.92f, 0.92f, 0.95f), // White shirt
                new Color(0.85f, 0.22f, 0.22f), // Red banner
                new Color(0.12f, 0.12f, 0.15f)  // Dark jacket
            };

            for (int y = 0; y < height; y++)
            {
                int row = y / 24;
                for (int x = 0; x < width; x++)
                {
                    int person = (x / 16) + row * 17;
                    Color fan = fanColors[Mathf.Abs(person) % fanColors.Length];

                    // Head/Hair silhouette
                    int localY = y % 24;
                    int localX = x % 16;
                    bool isHead = (localX >= 5 && localX <= 10 && localY >= 14 && localY <= 21);

                    Color pixel;
                    if (isHead)
                    {
                        pixel = new Color(0.85f, 0.68f, 0.55f); // Skin/face
                        if (localY > 18) pixel = new Color(0.15f, 0.12f, 0.10f); // Hair
                    }
                    else if (localY < 14)
                    {
                        pixel = fan; // Torso / clothes
                    }
                    else
                    {
                        pixel = new Color(0.12f, 0.15f, 0.22f); // Stand background
                    }

                    tex.SetPixel(x, y, pixel);
                }
            }
            tex.Apply();
            SaveTextureToFile(tex, fileName);
        }

        private static void GenerateStandsConcreteTexture(string fileName)
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color concreteBase = new Color(0.25f, 0.28f, 0.35f);
            Color seatBlue = new Color(0.12f, 0.25f, 0.55f);

            for (int y = 0; y < size; y++)
            {
                bool isSeatRow = (y % 32) < 20;
                for (int x = 0; x < size; x++)
                {
                    float noise = (Mathf.PerlinNoise(x * 0.1f, y * 0.1f) - 0.5f) * 0.08f;
                    Color c = isSeatRow ? seatBlue : concreteBase;
                    c.r += noise;
                    c.g += noise;
                    c.b += noise;

                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            SaveTextureToFile(tex, fileName);
        }

        private static void GenerateTrophyTexture(string fileName)
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color goldBase = new Color(0.95f, 0.80f, 0.18f);
            Color goldShine = new Color(1.0f, 0.96f, 0.55f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float v = Mathf.Sin(x * 0.1f) * Mathf.Cos(y * 0.1f) * 0.5f + 0.5f;
                    Color c = Color.Lerp(goldBase, goldShine, v);

                    // Decorative bands
                    if (y == 50 || y == 120 || y == 200)
                    {
                        c = Color.white;
                    }

                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            SaveTextureToFile(tex, fileName);
        }
        #endregion

        #region 6. Save & Import Settings
        private static void SaveTextureToFile(Texture2D tex, string fileName)
        {
            byte[] bytes = tex.EncodeToPNG();
            string fullPath = Path.Combine(TEXTURE_FOLDER, fileName + ".png");
            File.WriteAllBytes(fullPath, bytes);
            GameObject.DestroyImmediate(tex);
        }

        public static void ConfigureTextureImportSettings()
        {
            string[] files = Directory.GetFiles(TEXTURE_FOLDER, "*.png");
            foreach (var file in files)
            {
                string assetPath = file.Replace('\\', '/');
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Default;
                    importer.wrapMode = TextureWrapMode.Repeat;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.anisoLevel = 4;
                    if (assetPath.Contains("Net"))
                    {
                        importer.alphaIsTransparency = true;
                    }
                    importer.SaveAndReimport();
                }
            }
            Debug.Log("[GameTextureGenerator] Texture import settings configured.");
        }
        #endregion

        #region 7. Apply to Materials
        public static void ApplyTexturesToAllMaterials()
        {
            // Grass materials with realistic field tiling
            ApplyTextureToMaterial("Assets/Materials/Mat_Grass_Cantera.mat", "Tex_Grass_Cantera.png", new Vector2(25f, 35f));
            ApplyTextureToMaterial("Assets/Materials/Mat_Grass_Night.mat", "Tex_Grass_Night.png", new Vector2(25f, 35f));
            ApplyTextureToMaterial("Assets/Materials/Mat_Grass_Pro.mat", "Tex_Grass_Pro.png", new Vector2(25f, 35f));
            ApplyTextureToMaterial("Assets/Materials/Mat_Grass_WorldCup.mat", "Tex_Grass_WorldCup.png", new Vector2(25f, 35f));

            // Net material with cutout alpha clipping
            Material netMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Goal_Net.mat");
            Texture2D netTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/Tex_Goal_Net.png");
            if (netMat != null && netTex != null)
            {
                netMat.mainTexture = netTex;
                netMat.mainTextureScale = new Vector2(16f, 8f);
                // Enable Alpha Clipping on URP Lit
                if (netMat.HasProperty("_AlphaClip")) netMat.SetFloat("_AlphaClip", 1f);
                if (netMat.HasProperty("_Cutoff")) netMat.SetFloat("_Cutoff", 0.5f);
                netMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                EditorUtility.SetDirty(netMat);
            }

            // Balls
            ApplyTextureToMaterial("Assets/Materials/Mat_Ball_Classic.mat", "Tex_Ball_Classic.png", Vector2.one);
            ApplyTextureToMaterial("Assets/Materials/Mat_Ball_Blue.mat", "Tex_Ball_Blue.png", Vector2.one);
            ApplyTextureToMaterial("Assets/Materials/Mat_Ball_Fire.mat", "Tex_Ball_Fire.png", Vector2.one);
            ApplyTextureToMaterial("Assets/Materials/Mat_Ball_Rainbow.mat", "Tex_Ball_Rainbow.png", Vector2.one);
            ApplyTextureToMaterial("Assets/Materials/Mat_Ball_Gold.mat", "Tex_Ball_Gold.png", Vector2.one);

            // Kits & Characters
            ApplyTextureToMaterial("Assets/Materials/Mat_Player_Kit.mat", "Tex_Jersey_Ruben.png", Vector2.one);
            ApplyTextureToMaterial("Assets/Materials/Mat_Goalkeeper_Kit.mat", "Tex_Jersey_Malagon.png", Vector2.one);

            // Stadium Surroundings
            ApplyTextureToMaterial("Assets/Materials/Mat_Stadium_Banners.mat", "Tex_Stadium_Banners.png", new Vector2(10f, 1f));
            ApplyTextureToMaterial("Assets/Materials/Mat_Stadium_Stands.mat", "Tex_Stadium_Crowd.png", new Vector2(15f, 4f));
            ApplyTextureToMaterial("Assets/Materials/Mat_GoldTrophy.mat", "Tex_Trophy_Gold.png", new Vector2(2f, 2f));

            AssetDatabase.SaveAssets();
            Debug.Log("[GameTextureGenerator] All materials updated with high-resolution textures.");
        }

        private static void ApplyTextureToMaterial(string matPath, string texName, Vector2 tiling)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(TEXTURE_FOLDER, texName).Replace('\\', '/'));

            if (mat != null && tex != null)
            {
                mat.mainTexture = tex;
                mat.mainTextureScale = tiling;
                EditorUtility.SetDirty(mat);
            }
        }
        #endregion
    }
}
