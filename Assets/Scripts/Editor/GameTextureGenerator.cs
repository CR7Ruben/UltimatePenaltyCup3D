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

                // 4. Kits & Characters (Strict separation: pure skin body vs frontal face vs front/back jerseys + #7)
                GeneratePlayerJerseyTexture("Tex_Jersey_Ruben");
                GenerateGoalkeeperJerseyTexture("Tex_Jersey_Malagon");
                GenerateSkinBodyTexture("Tex_Skin_Body", new Color(0.89f, 0.70f, 0.56f));
                GeneratePlayerFaceTexture("Tex_Face_Ruben", new Color(0.89f, 0.70f, 0.56f), new Color(0.15f, 0.10f, 0.08f), new Color(0.24f, 0.15f, 0.09f), hasStubble: false);
                GeneratePlayerFaceTexture("Tex_Face_Malagon", new Color(0.83f, 0.65f, 0.51f), new Color(0.12f, 0.09f, 0.07f), new Color(0.18f, 0.14f, 0.10f), hasStubble: true);
                GenerateGlovesTexture("Tex_Gloves_Malagon");
                GenerateFabricNormalMap("Tex_Fabric_Normal");
                GenerateSkinNormalMap("Tex_Skin_Normal");

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
                    // Micro athletic knit weave + soft diagonal jacquard pattern
                    bool isDiagonal = ((x + y) / 34) % 2 == 0;
                    float knit = ((x % 4 == 0 || y % 4 == 0) ? -0.015f : 0.01f);
                    Color c = Color.Lerp(royalBlue, deepNavy, isDiagonal ? 0.28f : 0.0f);

                    // Subtle side-seam & shoulder athletic shading (kept inside panel interiors)
                    int localX = x < 256 ? x : (x - 256);
                    float centerShade = 1f - Mathf.Pow(Mathf.Abs(localX - 128) / 128f, 2f) * 0.12f;
                    // Soft realistic lower-torso jersey crease waves
                    float fold = Mathf.Sin(y * 0.11f + localX * 0.05f) * 0.016f * Mathf.Clamp01((260f - y) / 200f);
                    c = new Color(
                        Mathf.Clamp01(c.r * centerShade + knit + fold),
                        Mathf.Clamp01(c.g * centerShade + knit + fold),
                        Mathf.Clamp01(c.b * centerShade + knit + fold), 1f);

                    // Top collar V-trim + navy/gold rim
                    if (y > 472) c = goldTrim;
                    else if (y > 454) c = deepNavy;

                    // Bottom hem gold stripe & stitching line
                    if (y < 20) c = goldTrim;
                    else if (y >= 20 && y <= 23) c = deepNavy;

                    // LEFT HALF (x: 0..255) = FRONT OF JERSEY (clean sleeve region x < 64)
                    if (x < 256)
                    {
                        if (x >= 68 && x <= 222 && Mathf.Abs((x - 130) - (y - 260) * 0.42f) < 16)
                            c = Color.Lerp(c, goldTrim, 0.28f);

                        // Front subtle small athletic chest number accent? Keep classic A&R shield crest cleanly on left chest
                        if (x >= 158 && x <= 194 && y >= 312 && y <= 362)
                        {
                            bool border = x <= 162 || x >= 190 || y <= 316 || y >= 358;
                            c = border ? goldTrim : deepNavy;
                            if (x >= 171 && x <= 181 && y >= 325 && y <= 349) c = crispWhite;
                        }
                    }
                    // RIGHT HALF (x: 256..511) = BACK OF JERSEY WITH CRISP #7
                    else
                    {
                        int bx = x - 256; // 0..255 local back X (left->right when viewed from behind -Z)
                        // Sporty block/angled "7" centered horizontally around bx=128 (bx: 84..174, y: 156..340)
                        // Top horizontal crossbar (y: 304..340, bx: 84..174) + left tip drop serif (bx: 84..100, y: 282..306)
                        bool topBar = bx >= 84 && bx <= 174 && y >= 304 && y <= 340;
                        bool topLeftSerif = bx >= 84 && bx <= 102 && y >= 282 && y <= 306;

                        // Angled diagonal stem descending from upper-right (bx=172, y=304) to lower-left (bx=114, y=156)
                        bool diagFill = false;
                        bool diagOutline = false;
                        if (y >= 156 && y <= 316)
                        {
                            float tDiag = (y - 156f) / (316f - 156f); // 0 bottom .. 1 top
                            float centerX = Mathf.Lerp(116f, 160f, tDiag);
                            float halfStroke = Mathf.Lerp(15.5f, 17.0f, tDiag);
                            diagFill = Mathf.Abs(bx - centerX) <= halfStroke;
                            diagOutline = Mathf.Abs(bx - centerX) <= halfStroke + 4.5f;
                        }

                        bool isSeven = topBar || topLeftSerif || diagFill;
                        bool inSevenOuterBox = (bx >= 80 && bx <= 178 && y >= 152 && y <= 344) &&
                                               ((bx >= 80 && bx <= 178 && y >= 300 && y <= 344) ||
                                                (bx >= 80 && bx <= 106 && y >= 278 && y <= 308) ||
                                                diagOutline);

                        if (isSeven)
                        {
                            // Crisp white athletic print with tiny inner jersey pinstripe texture removed for clean legibility
                            c = crispWhite;
                        }
                        else if (inSevenOuterBox)
                        {
                            // Deep navy + gold athletic keyline outline framing the #7
                            c = deepNavy;
                        }

                        // Clean horizontal nameplate bar above #7 ("GONZALEZ" styling stripe)
                        if (bx >= 78 && bx <= 178 && y >= 372 && y <= 388)
                        {
                            c = Color.Lerp(c, goldTrim, 0.88f);
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

            Color vibrantOrange = new Color(0.95f, 0.39f, 0.05f);
            Color deepAmber = new Color(0.76f, 0.24f, 0.03f);
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
                    float knit = ((x % 4 == 0 || y % 4 == 0) ? -0.015f : 0.01f);
                    Color baseCol = Color.Lerp(vibrantOrange, deepAmber, ((x / 18 + y / 18) % 2 == 0) ? 0.20f : 0f);
                    Color c = isDarkBand ? darkCharcoal : baseCol;
                    c = new Color(Mathf.Clamp01(c.r + knit), Mathf.Clamp01(c.g + knit), Mathf.Clamp01(c.b + knit), 1f);

                    if (y > 468) c = darkCharcoal;
                    if (y < 20) c = goldAccent;

                    if (x < 256)
                    {
                        if (localX >= 158 && localX <= 194 && y >= 312 && y <= 362)
                        {
                            bool border = localX <= 162 || localX >= 190 || y <= 316 || y >= 358;
                            c = border ? goldAccent : darkCharcoal;
                        }
                    }
                    else
                    {
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
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float pore = (Mathf.PerlinNoise(x * 0.25f, y * 0.25f) - 0.5f) * 0.022f;
                    float warmth = Mathf.Sin((y / (float)size) * Mathf.PI) * 0.018f;
                    Color c = new Color(
                        Mathf.Clamp01(skinTone.r + pore + warmth),
                        Mathf.Clamp01(skinTone.g + pore * 0.85f),
                        Mathf.Clamp01(skinTone.b + pore * 0.7f - warmth * 0.5f), 1f);
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            SaveTextureToFile(tex, fileName);
        }

        private static void GeneratePlayerFaceTexture(string fileName, Color skinTone, Color hairTone, Color irisColor, bool hasStubble)
        {
            int size = 512;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float pore = (Mathf.PerlinNoise(x * 0.18f, y * 0.18f) - 0.5f) * 0.018f;
                    Color c = new Color(
                        Mathf.Clamp01(skinTone.r + pore),
                        Mathf.Clamp01(skinTone.g + pore * 0.85f),
                        Mathf.Clamp01(skinTone.b + pore * 0.75f), 1f);

                    // Soft under-jaw / temple ambient contour
                    float edgeDist = Mathf.Abs(x - 256f) / 256f;
                    if (edgeDist > 0.65f) c *= Mathf.Lerp(1f, 0.90f, (edgeDist - 0.65f) / 0.35f);

                    // Cheekbone warmth
                    float dCheekL = Vector2.Distance(new Vector2(x, y), new Vector2(154, 214));
                    float dCheekR = Vector2.Distance(new Vector2(x, y), new Vector2(358, 214));
                    if (dCheekL < 46f) c = Color.Lerp(c, new Color(0.92f, 0.56f, 0.50f), (1f - dCheekL / 46f) * 0.22f);
                    if (dCheekR < 46f) c = Color.Lerp(c, new Color(0.92f, 0.56f, 0.50f), (1f - dCheekR / 46f) * 0.22f);

                    // Subtle stubble jaw/chin shading for Malagon
                    if (hasStubble && y < 192 && Mathf.Abs(x - 256f) < 135f)
                    {
                        float mask = Mathf.SmoothStep(0f, 1f, (192f - y) / 90f) * (1f - Mathf.Pow(Mathf.Abs(x - 256f) / 135f, 2f));
                        float stipple = Mathf.PerlinNoise(x * 0.6f, y * 0.6f) * 0.65f + 0.35f;
                        c = Color.Lerp(c, skinTone * 0.68f, mask * stipple * 0.35f);
                    }

                    // Natural arched eyebrows (left & right)
                    float browArchL = -Mathf.Pow((x - 182f) / 40f, 2f) * 7f;
                    float browArchR = -Mathf.Pow((x - 330f) / 40f, 2f) * 7f;
                    if ((x >= 138 && x <= 224 && y >= 296 + browArchL && y <= 311 + browArchL) ||
                        (x >= 288 && x <= 374 && y >= 296 + browArchR && y <= 311 + browArchR))
                    {
                        c = hairTone;
                    }

                    // Soft eye socket shade around eyes
                    float dSockL = Vector2.Distance(new Vector2(x, y), new Vector2(182, 260));
                    float dSockR = Vector2.Distance(new Vector2(x, y), new Vector2(330, 260));
                    if (dSockL < 34f && dSockL >= 20f) c = Color.Lerp(c, skinTone * 0.84f, 0.25f);
                    if (dSockR < 34f && dSockR >= 20f) c = Color.Lerp(c, skinTone * 0.84f, 0.25f);

                    // Expressive Human Eyes (almond sclera + colored iris + pupil + upper lash + catchlights)
                    DrawHumanEye(ref c, x, y, 182f, 260f, irisColor, hairTone);
                    DrawHumanEye(ref c, x, y, 330f, 260f, irisColor, hairTone);

                    // Sculpted nose bridge highlight + side shadow + nostril recesses
                    if (Mathf.Abs(x - 256) < 16 && y >= 198 && y <= 258)
                    {
                        float center = 1f - Mathf.Abs(x - 256f) / 16f;
                        c = Color.Lerp(c, skinTone * 1.07f, center * 0.35f);
                    }
                    if (Vector2.Distance(new Vector2(x, y), new Vector2(242, 198)) < 6f ||
                        Vector2.Distance(new Vector2(x, y), new Vector2(270, 198)) < 6f)
                    {
                        c = Color.Lerp(c, skinTone * 0.65f, 0.55f);
                    }

                    // Natural lips + mouth line (y: 142..166)
                    float mouthCurve = Mathf.Pow((x - 256f) / 50f, 2f) * 7f;
                    if (x >= 210 && x <= 302 && y >= (144 + mouthCurve) && y <= (164 + mouthCurve))
                    {
                        float midY = 154f + mouthCurve;
                        bool crease = Mathf.Abs(y - midY) < 2.2f;
                        c = crease ? new Color(0.45f, 0.20f, 0.20f) : new Color(0.76f, 0.42f, 0.40f);
                    }

                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            SaveTextureToFile(tex, fileName);
        }

        private static void DrawHumanEye(ref Color c, int x, int y, float cx, float cy, Color irisCol, Color lashCol)
        {
            float dx = (x - cx) / 24f;
            float dy = (y - cy) / 15f;
            float almond = dx * dx + dy * dy;
            if (almond <= 1.0f)
            {
                c = new Color(0.96f, 0.96f, 0.97f); // Sclera
                float irisR = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                if (irisR < 11.5f)
                {
                    c = irisR < 5.8f ? new Color(0.06f, 0.05f, 0.05f) : irisCol;
                    // Catchlight glints
                    if (Vector2.Distance(new Vector2(x, y), new Vector2(cx + 4f, cy + 4f)) < 2.8f)
                        c = Color.white;
                }
                // Upper lash line
                if (dy > 0.52f) c = lashCol;
            }
        }

        private static void GenerateFabricNormalMap(string fileName)
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float wx = Mathf.Sin(x * 1.4f) * 0.22f + Mathf.Cos(y * 1.4f) * 0.14f;
                    float wy = Mathf.Sin(y * 1.4f) * 0.22f + (Mathf.PerlinNoise(x * 0.12f, y * 0.12f) - 0.5f) * 0.18f;
                    Vector3 n = new Vector3(wx, wy, 1f).normalized;
                    tex.SetPixel(x, y, new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f));
                }
            }
            tex.Apply();
            SaveTextureToFile(tex, fileName);
        }

        private static void GenerateSkinNormalMap(string fileName)
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float hx = (Mathf.PerlinNoise((x + 1) * 0.45f, y * 0.45f) - Mathf.PerlinNoise((x - 1) * 0.45f, y * 0.45f)) * 0.25f;
                    float hy = (Mathf.PerlinNoise(x * 0.45f, (y + 1) * 0.45f) - Mathf.PerlinNoise(x * 0.45f, (y - 1) * 0.45f)) * 0.25f;
                    Vector3 n = new Vector3(-hx, -hy, 1f).normalized;
                    tex.SetPixel(x, y, new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f));
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
            for (int attempt = 0; attempt < 4; attempt++)
            {
                try
                {
                    File.WriteAllBytes(fullPath, bytes);
                    break;
                }
                catch (IOException)
                {
                    if (attempt == 3) throw;
                    AssetDatabase.ReleaseCachedFileHandles();
                    GC.Collect();
                    System.Threading.Thread.Sleep(120);
                }
            }
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

            // Kits & Characters (Strict separation of body skin vs head face + non-plastic athletic PBR!)
            Texture2D fabricNormal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/Tex_Fabric_Normal.png");
            Texture2D skinNormal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/Tex_Skin_Normal.png");

            ApplyTextureToMaterial("Assets/Materials/Mat_Player_Kit.mat", "Tex_Jersey_Ruben.png", Vector2.one, fabricNormal, 0.25f, 0.18f);
            ApplyTextureToMaterial("Assets/Materials/Mat_Goalkeeper_Kit.mat", "Tex_Jersey_Malagon.png", Vector2.one, fabricNormal, 0.25f, 0.18f);
            ApplyTextureToMaterial("Assets/Materials/Mat_Skin.mat", "Tex_Skin_Body.png", Vector2.one, skinNormal, 0.18f, 0.26f);
            ApplyTextureToMaterial("Assets/Materials/Mat_Skin_Body.mat", "Tex_Skin_Body.png", Vector2.one, skinNormal, 0.18f, 0.26f);
            ApplyTextureToMaterial("Assets/Materials/Mat_Face_Ruben.mat", "Tex_Face_Ruben.png", Vector2.one, skinNormal, 0.12f, 0.26f);
            ApplyTextureToMaterial("Assets/Materials/Mat_Face_Malagon.mat", "Tex_Face_Malagon.png", Vector2.one, skinNormal, 0.14f, 0.26f);
            ApplyTextureToMaterial("Assets/Materials/Mat_GK_Gloves.mat", "Tex_Gloves_Malagon.png", Vector2.one, fabricNormal, 0.22f, 0.34f);

            SetMaterialSurfaceProps("Assets/Materials/Mat_Player_Shorts.mat", fabricNormal, 0.22f, 0.16f, clearBaseMap: true);
            SetMaterialSurfaceProps("Assets/Materials/Mat_GK_Shorts.mat", fabricNormal, 0.22f, 0.16f, clearBaseMap: true);
            SetMaterialSurfaceProps("Assets/Materials/Mat_Player_Socks.mat", fabricNormal, 0.20f, 0.18f, clearBaseMap: true);
            SetMaterialSurfaceProps("Assets/Materials/Mat_GK_Socks.mat", fabricNormal, 0.20f, 0.18f, clearBaseMap: true);
            SetMaterialSurfaceProps("Assets/Materials/Mat_Hair_Dark.mat", null, 0f, 0.22f);
            SetMaterialSurfaceProps("Assets/Materials/Mat_Boots_Player.mat", null, 0f, 0.42f);
            SetMaterialSurfaceProps("Assets/Materials/Mat_Boots_GK.mat", null, 0f, 0.42f);

            // Stadium Surroundings
            ApplyTextureToMaterial("Assets/Materials/Mat_Stadium_Banners.mat", "Tex_Stadium_Banners.png", new Vector2(6f, 1f));
            ApplyTextureToMaterial("Assets/Materials/Mat_Stadium_Stands.mat", "Tex_Stadium_Crowd.png", new Vector2(12f, 3f));
            ApplyTextureToMaterial("Assets/Materials/Mat_Stadium_Concrete.mat", "Tex_Stadium_Stands.png", new Vector2(4f, 4f));
            ApplyTextureToMaterial("Assets/Materials/Mat_GoldTrophy.mat", "Tex_Trophy_Gold.png", new Vector2(2f, 2f));

            AssetDatabase.SaveAssets();
            Debug.Log("[GameTextureGenerator] All materials updated with high-resolution PBR textures.");
        }

        private static void ApplyGrassMaterial(string matPath, string texName, Texture2D normalMap, Vector2 tiling)
        {
            ApplyTextureToMaterial(matPath, texName, tiling, normalMap, 0.35f, 0.14f);
        }

        private static void ApplyTextureToMaterial(string matPath, string texName, Vector2 tiling, Texture2D normalMap = null, float bumpScale = 0.25f, float smoothness = -1f)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(TEXTURE_FOLDER, texName).Replace('\\', '/'));

            if (mat != null && tex != null)
            {
                mat.mainTexture = tex;
                mat.mainTextureScale = tiling;
                mat.mainTextureOffset = Vector2.zero;
                if (mat.HasProperty("_BaseMap"))
                {
                    mat.SetTexture("_BaseMap", tex);
                    mat.SetTextureScale("_BaseMap", tiling);
                    mat.SetTextureOffset("_BaseMap", Vector2.zero);
                }
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
                mat.color = Color.white;
                if (normalMap != null || smoothness >= 0f)
                {
                    SetMaterialSurfaceProps(matPath, normalMap, bumpScale, smoothness);
                }
                EditorUtility.SetDirty(mat);
            }
        }

        private static void SetMaterialSurfaceProps(string matPath, Texture2D normalMap, float bumpScale, float smoothness, bool clearBaseMap = false)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null) return;
            if (clearBaseMap)
            {
                mat.mainTexture = null;
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", null);
            }
            if (normalMap != null && mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", normalMap);
                if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", bumpScale);
                mat.EnableKeyword("_NORMALMAP");
            }
            if (smoothness >= 0f && mat.HasProperty("_Smoothness"))
            {
                mat.SetFloat("_Smoothness", smoothness);
            }
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(mat);
        }
        #endregion
    }
}
