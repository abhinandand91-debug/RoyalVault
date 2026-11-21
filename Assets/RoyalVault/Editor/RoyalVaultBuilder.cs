using System.IO;
using RoyalVault.Core;
using RoyalVault.Unity;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace RoyalVault.EditorTools
{
    /// <summary>
    /// One-click project setup: font assets, generated UI textures, the shipped machine presets, the theme, the game
    /// prefab and the Boot and Game scenes. Run it from the "Royal Vault" menu after changing the art or presets; it is
    /// also callable in batch mode (<see cref="BuildAllBatch"/>). Machine assets that already exist are left alone so
    /// edits made in the Inspector survive.
    /// </summary>
    public static partial class RoyalVaultBuilder
    {
        private const string Root = "Assets/RoyalVault/";
        private const string ArtPath = Root + "Art/";
        private const string Generated = ArtPath + "Generated/";
        private const string FontPath = Root + "Fonts/";
        private const string DataPath = Root + "Data/";
        private const string TmpEssentials = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";

        [MenuItem("Royal Vault/Build Everything", priority = 1)]
        public static void BuildAll()
        {
            if (!AssetDatabase.IsValidFolder("Assets/TextMesh Pro"))
            {
                Debug.Log("Royal Vault: importing the TextMesh Pro essentials first; run Build Everything again when it finishes.");
                AssetDatabase.ImportPackage(TmpEssentials, false);
                return;
            }
            GenerateTextures();
            ThemeDefinition theme = CreateTheme(CreateFonts());
            MachineDefinitionAsset main = CreateMachine("RoyalVault_5x3", MachinePresets.RoyalVault5x3());
            MachineDefinitionAsset classic = CreateMachine("Classic_3x3", MachinePresets.Classic3x3());
            GameObject prefab = BuildGamePrefab(main, theme);
            BuildScenes(prefab, theme, classic);
            AssetDatabase.SaveAssets();
            Debug.Log("Royal Vault: build complete (Boot and Game scenes, RoyalVaultGame prefab, presets, theme).");
        }

        /// <summary>Batch mode: Unity -batchmode -quit -executeMethod RoyalVault.EditorTools.RoyalVaultBuilder.BuildAllBatch</summary>
        public static void BuildAllBatch()
        {
            BuildAll();
        }

        /// <summary>Batch mode, first run only: imports the TextMesh Pro essentials (shaders and settings).</summary>
        public static void ImportTmpEssentialsBatch()
        {
            if (!AssetDatabase.IsValidFolder("Assets/TextMesh Pro")) AssetDatabase.ImportPackage(TmpEssentials, false);
        }

        #region machines and theme
        private static MachineDefinitionAsset CreateMachine(string name, MachineDefinition preset)
        {
            EnsureFolder(DataPath);
            string path = DataPath + name + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<MachineDefinitionAsset>(path);
            if (asset != null) return asset; // keep Inspector edits
            asset = ScriptableObject.CreateInstance<MachineDefinitionAsset>();
            asset.CopyFrom(preset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private struct Fonts
        {
            public TMP_FontAsset Display, Heading, Label, Body;
        }

        private static Fonts CreateFonts()
        {
            return new Fonts
            {
                Display = FontAsset("Cinzel-Black"),
                Heading = FontAsset("Cinzel-Bold"),
                Label = FontAsset("Cinzel-Regular"),
                Body = FontAsset("CormorantGaramond-SemiBold"),
            };
        }

        /// <summary>A dynamic SDF font asset for a TTF in the Fonts folder (glyphs are added as text needs them).</summary>
        private static TMP_FontAsset FontAsset(string name)
        {
            string path = FontPath + name + " SDF.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null) return existing;
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath + name + ".ttf");
            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            asset.name = name + " SDF";
            AssetDatabase.CreateAsset(asset, path);
            asset.atlasTextures[0].name = name + " Atlas";
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            asset.material.name = name + " Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            // the common characters up front, so the first frame does not stall on glyph generation
            asset.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,:;%+-x×/…!?'()&", out _);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static ThemeDefinition CreateTheme(Fonts fonts)
        {
            EnsureFolder(DataPath);
            string path = DataPath + "RoyalVault_Theme.asset";
            var theme = AssetDatabase.LoadAssetAtPath<ThemeDefinition>(path);
            bool created = theme == null;
            if (created) theme = ScriptableObject.CreateInstance<ThemeDefinition>();

            string[] ids = { "card_10", "card_J", "card_Q", "card_K", "card_A", "emerald", "sapphire", "ruby", "ankh", "gold_bars", "vault_key", "wild", "vault_seal", "crown" };
            theme.symbols = new ThemeDefinition.SymbolArt[ids.Length];
            for (int i = 0; i < ids.Length; i++)
                theme.symbols[i] = new ThemeDefinition.SymbolArt { id = ids[i], sprite = Sprite("Symbols/" + ids[i]), blur = Sprite("Symbols/Blur/" + ids[i]) };

            theme.background = Sprite("Machine/bg", ".jpg");
            theme.vignette = Sprite("Generated/vignette");
            theme.reelFrame = Sprite("Machine/reel_frame");
            theme.reelCell = Sprite("Machine/reel_cell");
            theme.logo = Sprite("Machine/logo");
            theme.niche = Sprite("Machine/niche");
            theme.character = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Art.Private/Characters/anubis.png");
            theme.consoleBar = Sprite("Generated/bar");
            theme.spinButton = Sprite("UI/spin_btn");
            theme.spinButtonPressed = Sprite("UI/spin_btn_pressed");
            theme.pillButton = Sprite("Generated/pill");
            theme.roundButton = Sprite("Generated/ring");
            theme.jackpotGrand = Sprite("UI/jackpot_grand");
            theme.jackpotMinor = Sprite("UI/jackpot_minor");
            theme.popupPanel = Sprite("UI/popup_panel");
            theme.bigWinBanner = Sprite("UI/bigwin_banner");
            theme.coin = Sprite("UI/coin");
            theme.sparkle = Sprite("UI/sparkle");
            theme.glow = Sprite("Generated/glow");
            theme.line = Sprite("Generated/line");
            theme.white = Sprite("Generated/white");
            theme.levelBar = Sprite("UI/level_bar");
            theme.levelFill = Sprite("UI/level_fill");
            theme.iconMenu = Sprite("UI/Icons/icon_menu");
            theme.iconInfo = Sprite("UI/Icons/icon_info");
            theme.iconSound = Sprite("UI/Icons/icon_sound");
            theme.iconClose = Sprite("UI/Icons/icon_close");
            theme.additive = AdditiveMaterial();
            theme.displayFont = fonts.Display;
            theme.headingFont = fonts.Heading;
            theme.labelFont = fonts.Label;
            theme.bodyFont = fonts.Body;

            if (created) AssetDatabase.CreateAsset(theme, path);
            EditorUtility.SetDirty(theme);
            return theme;
        }

        /// <summary>The additive UI material glows and particles use (created once).</summary>
        private static Material AdditiveMaterial()
        {
            string path = Generated + "UIAdditive.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("RoyalVault/UI Additive")) { name = "UIAdditive" };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Sprite Sprite(string relative, string extension = ".png")
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + relative + extension);
            if (sprite == null) Debug.LogWarning("Royal Vault: missing art " + ArtPath + relative + extension);
            return sprite;
        }
        #endregion machines and theme

        #region generated textures
        /// <summary>Soft shapes drawn in code: glow, payline, vignette, light rays, the console's pill and ring buttons.</summary>
        private static void GenerateTextures()
        {
            EnsureFolder(Generated);
            Write("white", 4, 4, (x, y) => Color.white);
            Write("glow", 256, 256, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(127.5f, 127.5f)) / 128f;
                float a = Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f);
                return new Color(1f, 1f, 1f, a);
            });
            Write("line", 64, 24, (x, y) =>
            {
                float d = Mathf.Abs(y - 11.5f) / 12f;
                float core = Mathf.Clamp01(1f - d * 4f);
                float halo = Mathf.Pow(Mathf.Clamp01(1f - d), 2f) * 0.55f;
                float a = Mathf.Max(core, halo);
                return new Color(1f, 1f - 0.15f * (1f - core), 1f - 0.4f * (1f - core), a);
            });
            Write("vignette", 512, 288, (x, y) =>
            {
                float dx = (x - 255.5f) / 256f, dy = (y - 143.5f) / 144f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / 1.41f;
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1f, d)) * 0.82f;
                return new Color(0.01f, 0.01f, 0.015f, a);
            });
            Write("rays", 1024, 1024, (x, y) =>
            {
                Vector2 p = new Vector2(x - 511.5f, y - 511.5f);
                float r = p.magnitude / 512f;
                float angle = Mathf.Atan2(p.y, p.x);
                float ray = Mathf.Pow(Mathf.Abs(Mathf.Sin(angle * 9f)), 6f);
                float a = ray * Mathf.Clamp01(1f - r) * Mathf.Clamp01(r * 6f) * 0.55f;
                return new Color(1f, 0.86f, 0.52f, a);
            });
            // the console: a lacquer gradient, a gold rule that fades at its ends, and the raised socket for spin
            Write("bar", 4, 256, (x, y) => Color.Lerp(new Color32(6, 6, 8, 255), new Color32(23, 21, 28, 255), y / 255f));
            Write("rule", 1024, 8, (x, y) =>
            {
                float u = x / 1023f;
                float ends = Mathf.SmoothStep(0f, 1f, Mathf.Min(u, 1f - u) * 8f);
                float centre = Mathf.Lerp(0.72f, 1f, 1f - Mathf.Abs(u - 0.5f) * 2f);
                Color gold = Color.Lerp(new Color32(184, 135, 59, 255), new Color32(255, 241, 193, 255), centre - 0.72f);
                gold.a = ends * Mathf.Clamp01(1f - Mathf.Abs(y - 3.5f) / 4f);
                return gold;
            });
            Write("socket", 320, 320, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(160f, 160f));
                const float radius = 122f;
                Color glowColor = new Color(0.91f, 0.78f, 0.48f, Mathf.Pow(Mathf.Clamp01(1f - (d - radius) / 36f), 2f) * 0.28f);
                if (d > radius + 1f) return d < radius + 36f ? glowColor : Color.clear;
                // the gold ring is brightest at the top and fades towards the bottom
                float top = Mathf.Clamp01((y - 40f) / 240f);
                Color ring = Color.Lerp(new Color(0.48f, 0.33f, 0.09f, 0.25f), new Color(1f, 0.945f, 0.757f, 1f), top);
                Color disc = new Color32(10, 9, 13, 255);
                float edge = Mathf.Clamp01(radius + 1f - d);
                Color c = d > radius - 2.5f ? ring : disc;
                c.a *= edge;
                return c;
            });
            Color fill = new Color32(18, 17, 24, 255), stroke = new Color32(184, 135, 59, 255);
            Write("pill", 256, 128, (x, y) => RoundedRect(x, y, 256, 128, 63.5f, fill, stroke, 3f));
            Write("ring", 128, 128, (x, y) => RoundedRect(x, y, 128, 128, 63.5f, new Color32(28, 24, 32, 255), new Color32(232, 199, 122, 255), 3f));
            AssetDatabase.Refresh();
        }

        /// <summary>A rounded rectangle with a stroke, antialiased.</summary>
        private static Color RoundedRect(int x, int y, int w, int h, float radius, Color fill, Color stroke, float strokeWidth)
        {
            float px = x + 0.5f, py = y + 0.5f;
            float cx = Mathf.Clamp(px, radius, w - radius), cy = Mathf.Clamp(py, radius, h - radius);
            float d = radius - Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy)); // distance inside the edge
            if (d < 0f) return Color.clear;
            float edge = Mathf.Clamp01(d);
            Color c = d < strokeWidth ? stroke : Color.Lerp(stroke, fill, Mathf.Clamp01(d - strokeWidth));
            c.a *= edge;
            return c;
        }

        private static void Write(string name, int width, int height, System.Func<int, int, Color> pixel)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++) texture.SetPixel(x, y, pixel(x, y));
            texture.Apply();
            File.WriteAllBytes(Generated + name + ".png", texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }
        #endregion generated textures

        private static void EnsureFolder(string path)
        {
            path = path.TrimEnd('/');
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
