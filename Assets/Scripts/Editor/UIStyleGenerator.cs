using System.IO;
using UnityEngine;
using UnityEditor;

namespace UltimatePenaltyCup.Editor
{
    public static class UIStyleGenerator
    {
        private const string UI_DIR = "Assets/Art/UI";

        [MenuItem("Ultimate Penalty Cup/Visual Upgrade/Generate UI Sprites", false, 97)]
        public static void GenerateAllUISprites()
        {
            if (!Directory.Exists(UI_DIR)) Directory.CreateDirectory(UI_DIR);

            // 9-sliced rounded rectangles
            SaveSprite("UI_PanelRounded.png", CreateRoundedRect(128, 128, 22, 3, new Color(1f, 1f, 1f, 1f), new Color(1f, 1f, 1f, 0.35f)), 28);
            SaveSprite("UI_CardPanel.png", CreateRoundedRect(128, 128, 18, 2, Color.white, new Color(0.8f, 0.9f, 1f, 0.45f)), 24);
            SaveSprite("UI_ButtonRounded.png", CreateRoundedRect(128, 64, 14, 2, Color.white, new Color(1f, 1f, 1f, 0.25f)), 18);
            SaveSprite("UI_BadgePill.png", CreateRoundedRect(128, 64, 30, 0, Color.white, Color.clear), 31);

            // Circular & reticle
            SaveSprite("UI_Circle.png", CreateCircle(128, Color.white), 0);
            SaveSprite("UI_ReticleRing.png", CreateReticle(256), 0);
            SaveSprite("UI_GradientOverlay.png", CreateHorizontalGradient(256, 64), 0);

            // Crisp sports icons
            SaveSprite("UI_CoinIcon.png", CreateCoinIcon(128), 0);
            SaveSprite("UI_LockIcon.png", CreateLockIcon(128), 0);
            SaveSprite("UI_TrophyIcon.png", CreateTrophyIcon(128), 0);
            SaveSprite("UI_CheckIcon.png", CreateCheckIcon(128), 0);
            SaveSprite("UI_PauseIcon.png", CreatePauseIcon(128), 0);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[UIStyleGenerator] Generated all UI sprites.");
        }

        private static Texture2D CreateRoundedRect(int w, int h, int radius, int border, Color fill, Color borderCol)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Max(radius - x, x - (w - 1 - radius)));
                    float dy = Mathf.Max(0, Mathf.Max(radius - y, y - (h - 1 - radius)));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius + 0.5f - dist);

                    Color c = fill;
                    if (border > 0)
                    {
                        float edgeDist = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                        float cornerInset = radius - dist;
                        float innerDist = (dx > 0 && dy > 0) ? cornerInset : edgeDist;
                        if (innerDist < border) c = Color.Lerp(fill, borderCol, borderCol.a);
                    }
                    c.a *= alpha;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateCircle(int size, Color col)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = (size - 1) * 0.5f;
            float r = center - 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float a = Mathf.Clamp01(r + 1f - d);
                    tex.SetPixel(x, y, new Color(col.r, col.g, col.b, col.a * a));
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateReticle(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float c = (size - 1) * 0.5f;
            float outerR = size * 0.44f;
            float innerR = size * 0.36f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - c;
                    float dy = y - c;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);

                    float a = 0f;
                    // Outer glowing ring
                    if (d <= outerR && d >= innerR)
                    {
                        float edgeA = Mathf.Clamp01((outerR - d) * 0.6f) * Mathf.Clamp01((d - innerR) * 0.6f);
                        a = Mathf.Max(a, edgeA);
                    }
                    // Inner precision dot
                    if (d <= size * 0.06f)
                    {
                        a = Mathf.Max(a, Mathf.Clamp01((size * 0.06f - d) * 0.7f));
                    }
                    // 4 cardinal ticks
                    bool hTick = Mathf.Abs(dy) < size * 0.025f && Mathf.Abs(dx) > size * 0.16f && Mathf.Abs(dx) < size * 0.47f;
                    bool vTick = Mathf.Abs(dx) < size * 0.025f && Mathf.Abs(dy) > size * 0.16f && Mathf.Abs(dy) < size * 0.47f;
                    if (hTick || vTick) a = 1f;

                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateHorizontalGradient(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float t = x / (float)(w - 1);
                    float alpha = Mathf.Lerp(0.92f, 0.12f, Mathf.SmoothStep(0f, 1f, t));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateCoinIcon(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float c = (size - 1) * 0.5f;
            float r = size * 0.44f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                    float a = Mathf.Clamp01(r + 1f - d);
                    Color col = new Color(1.0f, 0.78f, 0.15f, a);
                    if (d < r * 0.75f && d > r * 0.62f) col = new Color(0.85f, 0.55f, 0.05f, a);
                    if (Mathf.Abs(x - c) < size * 0.06f && Mathf.Abs(y - c) < r * 0.42f) col = new Color(1f, 0.96f, 0.65f, a);
                    tex.SetPixel(x, y, col);
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateLockIcon(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool body = x >= size * 0.24f && x <= size * 0.76f && y >= size * 0.14f && y <= size * 0.56f;
                    float arcD = Vector2.Distance(new Vector2(x, y), new Vector2(c, size * 0.56f));
                    bool shackle = y >= size * 0.54f && arcD <= size * 0.24f && arcD >= size * 0.14f;
                    bool keyhole = Vector2.Distance(new Vector2(x, y), new Vector2(c, size * 0.36f)) < size * 0.06f ||
                                   (Mathf.Abs(x - c) < size * 0.03f && y >= size * 0.24f && y <= size * 0.36f);
                    float a = (body || shackle) && !keyhole ? 1f : 0f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateTrophyIcon(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float ny = y / (float)size;
                    float nx = Mathf.Abs(x - c) / (float)size;
                    bool cup = ny >= 0.42f && ny <= 0.86f && nx <= Mathf.Lerp(0.10f, 0.26f, (ny - 0.42f) / 0.44f);
                    bool stem = ny >= 0.22f && ny < 0.42f && nx <= 0.06f;
                    bool basePlinth = ny >= 0.12f && ny < 0.22f && nx <= 0.22f;
                    bool handles = ny >= 0.56f && ny <= 0.78f && nx >= 0.22f && nx <= 0.34f &&
                                   (ny > 0.72f || ny < 0.62f || nx > 0.28f);
                    float a = (cup || stem || basePlinth || handles) ? 1f : 0f;
                    tex.SetPixel(x, y, new Color(1f, 0.84f, 0.22f, a));
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateCheckIcon(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float v = y / (float)size;
                    float d1 = Mathf.Abs((v - 0.32f) + (u - 0.42f));
                    bool leg1 = u >= 0.20f && u <= 0.44f && d1 < 0.07f;
                    float d2 = Mathf.Abs((v - 0.32f) - (u - 0.42f));
                    bool leg2 = u >= 0.40f && u <= 0.82f && d2 < 0.07f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, (leg1 || leg2) ? 1f : 0f));
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreatePauseIcon(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float v = y / (float)size;
                    bool bars = v >= 0.20f && v <= 0.80f && ((u >= 0.24f && u <= 0.42f) || (u >= 0.58f && u <= 0.76f));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, bars ? 1f : 0f));
                }
            }
            tex.Apply();
            return tex;
        }

        private static void SaveSprite(string fileName, Texture2D tex, int border)
        {
            string path = Path.Combine(UI_DIR, fileName).Replace('\\', '/');
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                if (border > 0)
                {
                    imp.spriteBorder = new Vector4(border, border, border, border);
                }
                imp.SaveAndReimport();
            }
        }
    }
}
