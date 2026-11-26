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

                // 1. Grass & Pitch Textures (Natural turf + normal map, NO checkerboard)
                GenerateGrassTexture("Tex_Grass_Cantera", new Color(0.20f, 0.50f, 0.17f), new Color(0.24f, 0.56f, 0.21f), StripeType.SoftHorizontal);
                GenerateGrassTexture("Tex_Grass_Night", new Color(0.11f, 0.36f, 0.14f), new Color(0.14f, 0.42f, 0.18f), StripeType.SoftHorizontal);
                GenerateGrassTexture("Tex_Grass_Pro", new Color(0.15f, 0.54f, 0.20f), new Color(0.19f, 0.62f, 0.24f), StripeType.SoftHorizontal);
                GenerateGrassTexture("Tex_Grass_WorldCup", new Color(0.14f, 0.58f, 0.22f), new Color(0.19f, 0.66f, 0.27f), StripeType.DualCross);
                GenerateGrassNormalMap("Tex_Grass_Normal");

                // 2. Goal Net Cutout Texture
                GenerateGoalNetTexture("Tex_Goal_Net");

                // 3. Soccer Ball Textures (5 Variants)
                GenerateClassicBallTexture("Tex_Ball_Classic");
                GenerateBlueBallTexture("Tex_Ball_Blue");
                GenerateFireBallTexture("Tex_Ball_Fire");
                GenerateRainbowBallTexture("Tex_Ball_Rainbow");
                GenerateGoldBallTexture("Tex_Ball_Gold");

                // 4. Kits & Characters (Strict separation: pure skin body vs frontal face vs front/back jerseys)
                GeneratePlayerJerseyTexture("Tex_Jersey_Ruben");
                GenerateGoalkeeperJerseyTexture("Tex_Jersey_Malagon");
                GenerateSkinBodyTexture("Tex_Skin_Body", new Color(0.90f, 0.72f, 0.58f));
                GeneratePlayerFaceTexture("Tex_Face_Ruben", new Color(0.90f, 0.72f, 0.58f), new Color(0.16f, 0.11f, 0.08f));
                GeneratePlayerFaceTexture("Tex_Face_Malagon", new Color(0.84f, 0.66f, 0.52f), new Color(0.12f, 0.09f, 0.07f));
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
        private enum StripeType { SoftHorizontal, DualCross }

        private static void GenerateGrassTexture(string fileName, Color baseCol, Color stripeCol, StripeType type)
        {
            int width = 512;
            int height = 512;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float stripeVal = Mathf.Sin((float)y / height * Mathf.PI * 4f);
                    float factor = Mathf.SmoothStep(0f, 1f, stripeVal * 0.5f + 0.5f);
                    if (type == StripeType.DualCross)
                    {
                        float xVal = Mathf.Sin((float)x / width * Mathf.PI * 4f) * 0.5f + 0.5f;
                        factor = factor * 0.72f + xVal * 0.28f;
                    }

                    Color c = Color.Lerp(baseCol, stripeCol, factor);

                    // Multi-octave organic grass blade variation (non-repeating natural look)
                    float macro = Mathf.PerlinNoise(x * 0.03f, y * 0.03f) * 0.07f - 0.035f;
                    float mid = Mathf.PerlinNoise(x * 0.16f + 13.7f, y * 0.16f + 5.1f) * 0.06f - 0.03f;
                    float blade = Mathf.PerlinNoise(x * 0.85f, y * 0.85f) * 0.08f - 0.04f;

                    c.r = Mathf.Clamp01(c.r + macro + mid * 0.8f + blade * 0.6f);
                    c.g = Mathf.Clamp01(c.g + macro * 1.1f + mid + blade);
                    c.b = Mathf.Clamp01(c.b + macro * 0.7f + mid * 0.6f + blade * 0.5f);

                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            SaveTextureToFile(tex, fileName);
        }

        private static void GenerateGrassNormalMap(string fileName)
        {
            int size = 512;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float hL = Mathf.PerlinNoise((x - 1) * 0.75f, y * 0.75f);
                    float hR = Mathf.PerlinNoise((x + 1) * 0.75f, y * 0.75f);
                    float hD = Mathf.PerlinNoise(x * 0.75f, (y - 1) * 0.75f);
                    float hU = Mathf.PerlinNoise(x * 0.75f, (y + 1) * 0.75f);

                    Vector3 n = new Vector3((hL - hR) * 1.4f, (hD - hU) * 1.4f, 1.0f).normalized;
                    Color c = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
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

            Color royalBlue = new Color(0.06f, 0.28f, 0.78f);
            Color deepNavy = new Color(0.03f, 0.11f, 0.34f);
            Color goldTrim = new Color(1.0f, 0.78f, 0.18f);
            Color crispWhite = new Color(0.97f, 0.98f, 1.0f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Subtle diagonal athletic weave + center sash
                    bool isDiagonal = ((x + y) / 36) % 2 == 0;
                    Color c = Color.Lerp(royalBlue, deepNavy, isDiagonal ? 0.35f : 0.0f);

                    // Top collar gold & navy rim
                    if (y > 470) c = goldTrim;
                    else if (y > 452) c = deepNavy;

                    // Bottom hem gold stripe
                    if (y < 22) c = goldTrim;

                    // LEFT HALF (x: 0..255) = FRONT OF JERSEY
                    if (x < 256)
                    {
                        // Electric blue chest diagonal stripe
                        if (Mathf.Abs((x - 128) - (y - 260) * 0.4f) < 18)
                            c = Color.Lerp(c, goldTrim, 0.25f);

                        // Gold A&R crest shield on left chest (x: 155..195, y: 310..365)
                        if (x >= 158 && x <= 194 && y >= 312 && y <= 362)
                        {
                            bool border = x <= 162 || x >= 190 || y <= 316 || y >= 358;
                            c = border ? goldTrim : deepNavy;
                            if (x >= 171 && x <= 181 && y >= 325 && y <= 349) c = crispWhite;
                        }
                    }
                    // RIGHT HALF (x: 256..511) = BACK OF JERSEY WITH CRISP #10
                    else
                    {
                        int bx = x - 256; // 0..255 local back X
                        // Number "10" centered on back (bx: 68..188, y: 150..350)
                        // "1"
                        bool isOne = (bx >= 76 && bx <= 98 && y >= 165 && y <= 335) ||
                                     (bx >= 64 && bx <= 76 && y >= 305 && y <= 335);
                        // "0"
                        bool inOuterZero = bx >= 118 && bx <= 188 && y >= 165 && y <= 335;
                        bool inInnerZero = bx >= 140 && bx <= 166 && y >= 190 && y <= 310;
                        bool isZero = inOuterZero && !inInnerZero;

                        if (isOne || isZero) c = crispWhite;

                        // Gold trim box for player nameplate above number (y: 365..392)
                        if (bx >= 75 && bx <= 181 && y >= 368 && y <= 390)
                        {
                            if ((bx / 10) % 2 == 0) c = goldTrim;
                        }
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

            Color vibrantOrange = new Color(0.96f, 0.40f, 0.04f);
            Color darkCharcoal = new Color(0.09f, 0.11f, 0.16f);
            Color goldAccent = new Color(1.0f, 0.80f, 0.22f);
            Color crispWhite = new Color(0.97f, 0.98f, 1.0f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int localX = x < 256 ? x : (x - 256);
                    int chevron = Mathf.Abs(localX - 128) + y;
                    bool isDarkBand = (chevron / 32) % 3 == 0;
                    Color c = isDarkBand ? darkCharcoal : vibrantOrange;

                    if (y > 468) c = darkCharcoal;
                    if (y < 20) c = goldAccent;

                    if (x < 256)
                    {
                        // Front chest crest
                        if (localX >= 158 && localX <= 194 && y >= 312 && y <= 362)
                        {
                            bool border = localX <= 162 || localX >= 190 || y <= 316 || y >= 358;
                            c = border ? goldAccent : darkCharcoal;
                        }
                    }
                    else
                    {
                        // Back of GK jersey: bold "#1" centered (localX: 108..148, y: 165..335)
                        bool isOne = (localX >= 114 && localX <= 140 && y >= 165 && y <= 335) ||
                                     (localX >= 98 && localX <= 114 && y >= 305 && y <= 335);
                        if (isOne) c = crispWhite;
                    }

                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            SaveTextureToFile(tex, fileName);
        }

        private static void GenerateSkinBodyTexture(string fileName, Color skinTone)
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float n = (Mathf.PerlinNoise(x * 0.2f, y * 0.2f) - 0.5f) * 0.025f;
                    Color c = new Color(
                        Mathf.Clamp01(skinTone.r + n),
                        Mathf.Clamp01(skinTone.g + n * 0.9f),
                        Mathf.Clamp01(skinTone.b + n * 0.8f), 1f);
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

                    // Subtle cheek warmth
                    float dCheekL = Vector2.Distance(new Vector2(x, y), new Vector2(78, 108));
                    float dCheekR = Vector2.Distance(new Vector2(x, y), new Vector2(178, 108));
                    if (dCheekL < 22f) c = Color.Lerp(c, new Color(0.95f, 0.58f, 0.52f), (1f - dCheekL / 22f) * 0.25f);
                    if (dCheekR < 22f) c = Color.Lerp(c, new Color(0.95f, 0.58f, 0.52f), (1f - dCheekR / 22f) * 0.25f);

                    // Eyebrows (clean curved arches)
                    if ((x >= 72 && x <= 112 && y >= 149 && y <= 157) ||
                        (x >= 144 && x <= 184 && y >= 149 && y <= 157))
                    {
                        c = hairTone;
                    }

                    // Expressive Eyes (left center 92,132 | right center 164,132)
                    float dEyeL = Vector2.Distance(new Vector2(x, y), new Vector2(92, 132));
                    float dEyeR = Vector2.Distance(new Vector2(x, y), new Vector2(164, 132));
                    if (dEyeL < 12f || dEyeR < 12f)
                    {
                        float d = Mathf.Min(dEyeL, dEyeR);
                        c = d < 6.5f ? new Color(0.12f, 0.08f, 0.06f) : new Color(0.98f, 0.98f, 0.99f);
                        // White glint dot
                        if (Vector2.Distance(new Vector2(x, y), new Vector2(95, 135)) < 2.2f ||
                            Vector2.Distance(new Vector2(x, y), new Vector2(167, 135)) < 2.2f)
                        {
                            c = Color.white;
                        }
                    }

                    // Soft nose bridge shadow
                    if (x >= 122 && x <= 134 && y >= 102 && y <= 126)
                    {
                        c = Color.Lerp(c, skinTone * 0.85f, 0.5f);
                    }

                    // Confident athletic smile (y: 74..83)
                    float smileCurve = Mathf.Pow((x - 128f) / 26f, 2f) * 5f;
                    if (x >= 104 && x <= 152 && y >= (74 + smileCurve) && y <= (82 + smileCurve))
                    {
                        c = new Color(0.72f, 0.34f, 0.34f);
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
                    if (assetPath.Contains("Normal"))
                    {
                        importer.textureType = TextureImporterType.NormalMap;
                    }
                    else
                    {
                        importer.textureType = TextureImporterType.Default;
                    }
                    importer.wrapMode = (assetPath.Contains("Face") || assetPath.Contains("Jersey")) ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.anisoLevel = 8;
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
            // Grass materials with natural 6m mowing band tiling & grass normal map
            Texture2D grassNormal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/Tex_Grass_Normal.png");
            ApplyGrassMaterial("Assets/Materials/Mat_Grass_Cantera.mat", "Tex_Grass_Cantera.png", grassNormal, new Vector2(6f, 8f));
            ApplyGrassMaterial("Assets/Materials/Mat_Grass_Night.mat", "Tex_Grass_Night.png", grassNormal, new Vector2(6f, 8f));
            ApplyGrassMaterial("Assets/Materials/Mat_Grass_Pro.mat", "Tex_Grass_Pro.png", grassNormal, new Vector2(6f, 8f));
            ApplyGrassMaterial("Assets/Materials/Mat_Grass_WorldCup.mat", "Tex_Grass_WorldCup.png", grassNormal, new Vector2(6f, 8f));

            // Net material with double-sided cutout alpha clipping
            Material netMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Goal_Net.mat");
            Texture2D netTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/Tex_Goal_Net.png");
            if (netMat != null && netTex != null)
            {
                netMat.mainTexture = netTex;
                netMat.mainTextureScale = new Vector2(14f, 6f);
                if (netMat.HasProperty("_BaseMap")) netMat.SetTexture("_BaseMap", netTex);
                if (netMat.HasProperty("_AlphaClip")) netMat.SetFloat("_AlphaClip", 1f);
                if (netMat.HasProperty("_Cutoff")) netMat.SetFloat("_Cutoff", 0.35f);
                if (netMat.HasProperty("_Cull")) netMat.SetFloat("_Cull", 0f); // Render both sides
                netMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                EditorUtility.SetDirty(netMat);
            }

            // Balls
            ApplyTextureToMaterial("Assets/Materials/Mat_Ball_Classic.mat", "Tex_Ball_Classic.png", Vector2.one);
            ApplyTextureToMaterial("Assets/Materials/Mat_Ball_Blue.mat", "Tex_Ball_Blue.png", Vector2.one);
            ApplyTextureToMaterial("Assets/Materials/Mat_Ball_Fire.mat", "Tex_Ball_Fire.png", Vector2.one);
            ApplyTextureToMaterial("Assets/Materials/Mat_Ball_Rainbow.mat", "Tex_Ball_Rainbow.png", Vector2.one);
            ApplyTextureToMaterial("Assets/Materials/Mat_Ball_Gold.mat", "Tex_Ball_Gold.png", Vector2.one);

            // Kits & Characters (Strict separation of body skin vs head face!)
            ApplyTextureToMaterial("Assets/Materials/Mat_Player_Kit.mat", "Tex_Jersey_Ruben.png", Vector2.one);
            ApplyTextureToMaterial("Assets/Materials/Mat_Goalkeeper_Kit.mat", "Tex_Jersey_Malagon.png", Vector2.one);
            ApplyTextureToMaterial("Assets/Materials/Mat_Skin.mat", "Tex_Skin_Body.png", Vector2.one);
            ApplyTextureToMaterial("Assets/Materials/Mat_Skin_Body.mat", "Tex_Skin_Body.png", Vector2.one);
            ApplyTextureToMaterial("Assets/Materials/Mat_Face_Ruben.mat", "Tex_Face_Ruben.png", Vector2.one);
            ApplyTextureToMaterial("Assets/Materials/Mat_Face_Malagon.mat", "Tex_Face_Malagon.png", Vector2.one);
            ApplyTextureToMaterial("Assets/Materials/Mat_GK_Gloves.mat", "Tex_Gloves_Malagon.png", Vector2.one);

            // Stadium Surroundings
            ApplyTextureToMaterial("Assets/Materials/Mat_Stadium_Banners.mat", "Tex_Stadium_Banners.png", new Vector2(6f, 1f));
            ApplyTextureToMaterial("Assets/Materials/Mat_Stadium_Stands.mat", "Tex_Stadium_Crowd.png", new Vector2(12f, 3f));
            ApplyTextureToMaterial("Assets/Materials/Mat_Stadium_Concrete.mat", "Tex_Stadium_Stands.png", new Vector2(4f, 4f));
            ApplyTextureToMaterial("Assets/Materials/Mat_GoldTrophy.mat", "Tex_Trophy_Gold.png", new Vector2(2f, 2f));

            AssetDatabase.SaveAssets();
            Debug.Log("[GameTextureGenerator] All materials updated with high-resolution textures.");
        }

        private static void ApplyGrassMaterial(string matPath, string texName, Texture2D normalMap, Vector2 tiling)
        {
            ApplyTextureToMaterial(matPath, texName, tiling);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat != null && normalMap != null)
            {
                if (mat.HasProperty("_BumpMap"))
                {
                    mat.SetTexture("_BumpMap", normalMap);
                    mat.SetFloat("_BumpScale", 0.35f);
                    mat.EnableKeyword("_NORMALMAP");
                }
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.14f);
                EditorUtility.SetDirty(mat);
            }
        }

        private static void ApplyTextureToMaterial(string matPath, string texName, Vector2 tiling)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(TEXTURE_FOLDER, texName).Replace('\\', '/'));

            if (mat != null && tex != null)
            {
                mat.mainTexture = tex;
                mat.mainTextureScale = tiling;
                if (mat.HasProperty("_BaseMap"))
                {
                    mat.SetTexture("_BaseMap", tex);
                    mat.SetTextureScale("_BaseMap", tiling);
                }
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
                mat.color = Color.white;
                EditorUtility.SetDirty(mat);
            }
        }
        #endregion
    }
}
