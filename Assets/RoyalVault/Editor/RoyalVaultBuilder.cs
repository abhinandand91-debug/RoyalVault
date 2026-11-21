using System.IO;
using System.Linq;
using RoyalVault.Core;
using RoyalVault.Unity;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace RoyalVault.EditorTools
{
    // One-click setup: fonts, textures, presets, theme, prefab and scenes
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

        // Entry point for batch builds
        public static void BuildAllBatch()
        {
            BuildAll();
        }

        // Batch mode, first run only: imports the TextMesh Pro essentials (shaders and settings)
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

        // A dynamic SDF font asset for a TTF in the Fonts folder (glyphs are added as text needs them)
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
            theme.symbols = SymbolArts();
            SetInterfaceArt(theme);
            SetCharacters(theme);
            SetEffects(theme);
            theme.displayFont = fonts.Display;
            theme.headingFont = fonts.Heading;
            theme.labelFont = fonts.Label;
            theme.bodyFont = fonts.Body;
            if (created) AssetDatabase.CreateAsset(theme, path);
            EditorUtility.SetDirty(theme);
            return theme;
        }

        private static ThemeDefinition.SymbolArt[] SymbolArts()
        {
            string[] ids = { "card_10", "card_J", "card_Q", "card_K", "card_A", "emerald", "sapphire", "ruby", "ankh", "gold_bars", "vault_key", "wild", "vault_seal", "crown" };
            return ids.Select(id =>
            {
                (SymbolStyle style, Color tint) = SymbolLook(id);
                return new ThemeDefinition.SymbolArt
                {
                    id = id, sprite = Sprite("Symbols/" + id), blur = Sprite("Symbols/Blur/" + id),
                    style = style, tint = tint, winAnimation = SymbolWinAnimation(id),
                };
            }).ToArray();
        }

        private static void SetInterfaceArt(ThemeDefinition theme)
        {
            theme.background = Sprite("Machine/bg", ".jpg");
            theme.vignette = Sprite("Generated/vignette");
            theme.reelFrame = Sprite("Machine/reel_frame");
            theme.reelCell = Sprite("Machine/reel_cell");
            theme.logo = Sprite("Machine/logo");
            theme.niche = Sprite("Machine/niche");
            theme.consoleBar = Sprite("Generated/bar");
            theme.spinButton = Sprite("UI/spin_medallion");
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
            theme.rays = Sprite("Generated/rays");
        }

        private static void SetCharacters(ThemeDefinition theme)
        {
            theme.character = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Art.Private/Characters/anubis.png");
            theme.characterAnimation = CreateCharacterAnimation(Root + "Art.Private/Characters/Anubis");
            theme.story = CreateStory();
            theme.sounds = CreateSounds();
            theme.heroLeft = CreateCharacterAnimation(Root + "Art.Private/Characters/Cole");
            theme.heroRight = CreateCharacterAnimation(Root + "Art.Private/Characters/Ruby");
        }

        private static void SetEffects(ThemeDefinition theme)
        {
            theme.additive = AdditiveMaterial();
            theme.symbolMaterial = SymbolMaterial();
            // Hovl Studio's Magic Effects FREE, when imported (git-ignored: its licence allows no redistribution)
            theme.clashImpactVfx = StorePrefab("Assets/Hovl Studio", "Electro hit");
            theme.clashSparksVfx = StorePrefab("Assets/Hovl Studio", "Sparks explode blue");
            theme.sealBreakVfx = StorePrefab("Assets/Hovl Studio", "Holy hit");
            theme.vfxScale = 170f;
        }

        // A prefab by exact name anywhere under folder, or null when the pack is absent
        private static GameObject StorePrefab(string folder, string name)
        {
            if (!AssetDatabase.IsValidFolder(folder)) return null;
            foreach (string guid in AssetDatabase.FindAssets(name + " t:Prefab", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == name) return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            Debug.LogWarning("Royal Vault: store effect not found: " + folder + " / " + name);
            return null;
        }

        // How each symbol celebrates, and the colour of its light
        private static (SymbolStyle, Color) SymbolLook(string id)
        {
            switch (id)
            {
                case "emerald": return (SymbolStyle.Gem, new Color(0.35f, 1f, 0.55f, 1f));
                case "sapphire": return (SymbolStyle.Gem, new Color(0.4f, 0.6f, 1f, 1f));
                case "ruby": return (SymbolStyle.Gem, new Color(1f, 0.3f, 0.35f, 1f));
                case "ankh":
                case "gold_bars":
                case "vault_key": return (SymbolStyle.Treasure, new Color(1f, 0.8f, 0.4f, 1f));
                case "wild": return (SymbolStyle.Wild, new Color(0.85f, 0.95f, 1f, 1f));
                case "vault_seal": return (SymbolStyle.Scatter, new Color(0.4f, 0.75f, 1f, 1f));
                case "crown": return (SymbolStyle.Jackpot, new Color(1f, 0.82f, 0.4f, 1f));
                default: return (SymbolStyle.Card, new Color(1f, 0.85f, 0.5f, 1f));
            }
        }

        // The reel symbols' material: the shine sweep and the flash (created once)
        private static Material SymbolMaterial()
        {
            string path = Generated + "UISymbol.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("RoyalVault/UI Symbol")) { name = "UISymbol" };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // The additive UI material glows and particles use (created once)
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
        // Soft shapes drawn in code: glow, payline, vignette, light rays, the console's pill and ring buttons
        private static void GenerateTextures()
        {
            EnsureFolder(Generated);
            Write("white", 4, 4, (x, y) => Color.white);
            Write("glow", 256, 256, GlowPixel);
            Write("line", 64, 24, LinePixel);
            Write("vignette", 512, 288, VignettePixel);
            Write("rays", 1024, 1024, RaysPixel);
            for (int stage = 1; stage <= 3; stage++) WriteCrack(stage);
            // dark at the foot of the screen, clear from 70% up: lays text over a painting
            Write("fade", 4, 256, (x, y) => new Color(0f, 0f, 0f, Mathf.Pow(Mathf.Clamp01(1f - y / 180f), 1.4f)));
            Write("bar", 4, 256, (x, y) => Color.Lerp(new Color32(6, 6, 8, 255), new Color32(23, 21, 28, 255), y / 255f));
            Write("rule", 1024, 8, RulePixel);
            Write("socket", 320, 320, SocketPixel);
            Color fill = new Color32(18, 17, 24, 255), stroke = new Color32(184, 135, 59, 255);
            Write("pill", 256, 128, (x, y) => RoundedRect(x, y, 256, 128, 63.5f, fill, stroke, 3f));
            Write("ring", 128, 128, (x, y) => RoundedRect(x, y, 128, 128, 63.5f, new Color32(28, 24, 32, 255), new Color32(232, 199, 122, 255), 3f));
            AssetDatabase.Refresh();
        }

        private static Color GlowPixel(int x, int y)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(127.5f, 127.5f)) / 128f;
            return new Color(1f, 1f, 1f, Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f));
        }

        // A bright core with a warm halo, for paylines
        private static Color LinePixel(int x, int y)
        {
            float d = Mathf.Abs(y - 11.5f) / 12f;
            float core = Mathf.Clamp01(1f - d * 4f);
            float halo = Mathf.Pow(Mathf.Clamp01(1f - d), 2f) * 0.55f;
            return new Color(1f, 1f - 0.15f * (1f - core), 1f - 0.4f * (1f - core), Mathf.Max(core, halo));
        }

        private static Color VignettePixel(int x, int y)
        {
            float dx = (x - 255.5f) / 256f, dy = (y - 143.5f) / 144f;
            float d = Mathf.Sqrt(dx * dx + dy * dy) / 1.41f;
            return new Color(0.01f, 0.01f, 0.015f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1f, d)) * 0.82f);
        }

        private static Color RaysPixel(int x, int y)
        {
            var p = new Vector2(x - 511.5f, y - 511.5f);
            float r = p.magnitude / 512f;
            float ray = Mathf.Pow(Mathf.Abs(Mathf.Sin(Mathf.Atan2(p.y, p.x) * 9f)), 6f);
            return new Color(1f, 0.86f, 0.52f, ray * Mathf.Clamp01(1f - r) * Mathf.Clamp01(r * 6f) * 0.55f);
        }

        // A gold rule, brightest in the middle, fading at its ends
        private static Color RulePixel(int x, int y)
        {
            float u = x / 1023f;
            float ends = Mathf.SmoothStep(0f, 1f, Mathf.Min(u, 1f - u) * 8f);
            float centre = Mathf.Lerp(0.72f, 1f, 1f - Mathf.Abs(u - 0.5f) * 2f);
            Color gold = Color.Lerp(new Color32(184, 135, 59, 255), new Color32(255, 241, 193, 255), centre - 0.72f);
            gold.a = ends * Mathf.Clamp01(1f - Mathf.Abs(y - 3.5f) / 4f);
            return gold;
        }

        // A dark disc in a gold ring that fades towards the bottom, with a soft glow around
        private static Color SocketPixel(int x, int y)
        {
            const float radius = 122f;
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(160f, 160f));
            if (d > radius + 1f)
                return d < radius + 36f ? new Color(0.91f, 0.78f, 0.48f, Mathf.Pow(Mathf.Clamp01(1f - (d - radius) / 36f), 2f) * 0.28f) : Color.clear;
            float top = Mathf.Clamp01((y - 40f) / 240f);
            Color ring = Color.Lerp(new Color(0.48f, 0.33f, 0.09f, 0.25f), new Color(1f, 0.945f, 0.757f, 1f), top);
            Color c = d > radius - 2.5f ? ring : (Color)new Color32(10, 9, 13, 255);
            c.a *= Mathf.Clamp01(radius + 1f - d);
            return c;
        }

        // A rounded rectangle with a stroke, antialiased
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

        // Jagged glowing fractures running out from near the centre of a 128 x 128 seal
        private static void WriteCrack(int stage)
        {
            var random = new System.Random(stage * 7919);
            var segments = new System.Collections.Generic.List<(Vector2 a, Vector2 b)>();
            float[][] angles = { new[] { 25f, 150f, 268f }, new[] { 92f, 205f, 330f }, new[] { 55f, 120f, 238f, 300f } };
            var centre = new Vector2(64f, 64f);
            foreach (float degrees in angles[stage - 1])
            {
                float angle = degrees * Mathf.Deg2Rad;
                float start = stage == 3 ? 22f : 5f;
                float length = stage == 3 ? 30f : 54f;
                Vector2 p = centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * start;
                for (int i = 1; i <= 5; i++)
                {
                    float a = angle + (float)(random.NextDouble() - 0.5) * 0.9f;
                    float r = start + length * i / 5f;
                    Vector2 q = centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                    segments.Add((p, q));
                    p = q;
                }
            }
            Write("crack_" + stage, 128, 128, (x, y) =>
            {
                var point = new Vector2(x + 0.5f, y + 0.5f);
                float d = float.MaxValue;
                foreach (var (a, b) in segments)
                {
                    Vector2 ab = b - a;
                    float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / ab.sqrMagnitude);
                    d = Mathf.Min(d, Vector2.Distance(point, a + ab * t));
                }
                float core = Mathf.Clamp01(1.4f - d * 0.8f);
                float halo = Mathf.Exp(-d / 2.5f) * 0.45f;
                return new Color(1f, 1f, 1f, Mathf.Max(core, halo));
            });
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
