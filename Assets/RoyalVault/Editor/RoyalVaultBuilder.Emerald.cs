using GameKit;
using RoyalVault.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalVault.EditorTools
{
    // The Emerald Sanctum look from the Figma file: its art, its colours, and the parts drawn differently from the Art Deco look
    public static partial class RoyalVaultBuilder
    {
        private static readonly Color EmeraldGold = new Color32(217, 164, 65, 255);
        private static readonly Color EmeraldInk = new Color32(3, 18, 14, 255);

        #region textures
        // Plain shapes the Emerald look needs; the painted art comes from the Figma file
        private static void WriteEmeraldTextures()
        {
            Color stroke = new Color32(201, 162, 78, 255);
            Write("emerald_cell", 64, 64, EmeraldCellPixel);
            Write("emerald_bar", 4, 256, (x, y) => Color.Lerp(new Color32(4, 16, 12, 255), new Color32(12, 40, 30, 255), y / 255f));
            Write("emerald_pill", 256, 128, (x, y) => RoundedRect(x, y, 256, 128, 63.5f, new Color32(13, 44, 33, 255), stroke, 3f));
            Write("emerald_ring", 128, 128, (x, y) => RoundedRect(x, y, 128, 128, 63.5f, new Color32(12, 40, 30, 255), stroke, 3f));
            // a light fill the game tints per jackpot tier, and its gold edge on its own so the tint leaves it bright
            Write("emerald_plaque", 128, 128, (x, y) => RoundedRect(x, y, 128, 128, 18f, Color.Lerp(new Color(0.72f, 0.72f, 0.72f, 1f), Color.white, y / 127f),
                                                                 Color.clear, 0f));
            Write("emerald_plaque_edge", 128, 128, (x, y) => RoundedRect(x, y, 128, 128, 18f, Color.clear, new Color32(214, 170, 92, 255), 3f));
            Write("emerald_disc", 256, 256, EmeraldDiscPixel);
            Write("spin_wings", 512, 192, SpinWingsPixel);
        }

        // A dark emerald cell with a faint lighter edge, like the reel bands in the mockup
        private static Color EmeraldCellPixel(int x, int y)
        {
            Color top = new Color32(14, 46, 35, 255);
            Color bottom = new Color32(7, 28, 21, 255);
            Color colour = Color.Lerp(bottom, top, y / 63f);
            bool edge = x == 0 || y == 0 || x == 63 || y == 63;
            return edge ? Color.Lerp(colour, new Color32(40, 92, 70, 255), 0.5f) : colour;
        }

        // The spin button's face: emerald deepening to its rim, a gold edge and a soft highlight at the top
        private static Color EmeraldDiscPixel(int x, int y)
        {
            float u = (x + 0.5f) / 128f - 1f;
            float v = (y + 0.5f) / 128f - 1f;
            float d = Mathf.Sqrt(u * u + v * v);
            if (d > 1f)
            {
                return Color.clear;
            }

            Color face = Color.Lerp(new Color32(24, 150, 108, 255), new Color32(5, 52, 37, 255), d * d);
            float highlight = Mathf.Clamp01(1f - Mathf.Sqrt(u * u + (v - 0.55f) * (v - 0.55f)) / 0.55f) * 0.25f;
            face = Color.Lerp(face, Color.white, highlight);
            if (d > 0.92f)
            {
                face = Color.Lerp(new Color32(240, 205, 120, 255), new Color32(150, 104, 40, 255), (d - 0.92f) / 0.08f);
            }

            float edge = Mathf.Clamp01((1f - d) * 128f);
            face.a = edge;
            return face;
        }

        // Two rounded gold wings with an emerald inlay, one either side of the rotor
        private static Color SpinWingsPixel(int x, int y)
        {
            const int wingWidth = 190;
            const int wingHeight = 92;
            int localX = x < 256 ? x - 6 : 506 - x;
            int localY = y - (192 - wingHeight) / 2;
            if (localX < 0 || localX >= wingWidth || localY < 0 || localY >= wingHeight)
            {
                return Color.clear;
            }

            Color gold = Color.Lerp(new Color32(150, 104, 40, 255), new Color32(244, 210, 128, 255), localY / (float)wingHeight);
            Color inlay = Color.Lerp(new Color32(6, 48, 34, 255), new Color32(16, 92, 66, 255), localY / (float)wingHeight);
            Color outer = RoundedRect(localX, localY, wingWidth, wingHeight, 26f, gold, gold, 0f);
            Color inner = RoundedRect(localX - 14, localY - 14, wingWidth - 28, wingHeight - 28, 14f, inlay, inlay, 0f);
            return inner.a > 0f ? Color.Lerp(outer, inner, inner.a) : outer;
        }
        #endregion

        // The Figma file's art and the design system's colours
        private static void SetEmeraldArt(ThemeDefinition theme)
        {
            theme.background = Sprite("Emerald/Environment/bg", ".jpg");
            theme.reelFrame = null;
            // set here as well as on import: art copied in before the importer compiles is imported without it
            EnsureSpriteBorder(ArtPath + "Emerald/Environment/reel_housing.png", new Vector4(231, 127, 231, 135));
            theme.reelHousing = Sprite("Emerald/Environment/reel_housing");
            theme.housingBorderScale = 1f;
            theme.reelCell = Sprite("Generated/emerald_cell");
            theme.niche = null;
            theme.consoleBar = Sprite("Generated/emerald_bar");
            theme.pillButton = Sprite("Generated/emerald_pill");
            theme.roundButton = Sprite("Generated/emerald_ring");
            theme.spinButton = Sprite("Generated/emerald_disc");
            theme.spinRotor = Sprite("Emerald/Environment/ring");
            theme.spinWings = Sprite("Generated/spin_wings");
            theme.jackpotPlaque = Sprite("Generated/emerald_plaque");
            theme.jackpotIcons = new[] { Sprite("Emerald/UI/gem_grand"), Sprite("Emerald/UI/gem_major"), Sprite("Emerald/UI/gem_minor") };
            theme.jackpotTints = new Color[] { new Color32(10, 18, 15, 255), new Color32(12, 58, 42, 255), new Color32(13, 33, 76, 255) };
            theme.gold = EmeraldGold;
            theme.paleGold = new Color32(255, 225, 160, 255);
            theme.ivory = new Color32(243, 233, 210, 255);
            theme.dimGold = new Color32(189, 209, 192, 255);
            theme.ink = EmeraldInk;
            theme.surface = new Color32(11, 38, 28, 255);
            theme.accent = new Color32(53, 216, 232, 255);
            // a reel window shaped like the housing's, sitting a little low so the jackpots clear its lintel
            theme.reelArea = new Rect(-494, -285, 988, 480);
            theme.cellAspect = 1.23f;
            theme.cellGap = 4f;
        }

        // 9-slice borders (left, bottom, right, top, in the art's pixels), reimporting only when they differ
        private static void EnsureSpriteBorder(string path, Vector4 border)
        {
            if (UnityEditor.AssetImporter.GetAtPath(path) is UnityEditor.TextureImporter importer && importer.spriteBorder != border)
            {
                importer.spriteBorder = border;
                importer.SaveAndReimport();
            }
        }

        // The Art Deco look's colours and reel window, so rebuilding it after the Emerald look restores them
        private static void SetArtDecoLayout(ThemeDefinition theme)
        {
            theme.reelHousing = null;
            theme.gold = new Color32(232, 199, 122, 255);
            theme.paleGold = new Color32(255, 231, 166, 255);
            theme.ivory = new Color32(243, 233, 210, 255);
            theme.dimGold = new Color32(179, 154, 102, 255);
            theme.ink = new Color32(10, 9, 13, 255);
            theme.reelArea = new Rect(-555, -251, 1110, 541);
            theme.cellAspect = 1.23f;
            theme.cellGap = 4f;
        }

        // The title set in Cinzel with a gold gradient, and the sanctum's name beneath it
        private static void EmeraldLogo(Transform parent, ThemeDefinition theme)
        {
            TextMeshProUGUI title = UiFactory.Text("Logo", parent, theme.headingFont, "ROYAL VAULT", 50, Color.white, new Vector2(0, 503), new Vector2(700, 64),
                                                   TextAlignmentOptions.Center, 6);
            title.enableVertexGradient = true;
            title.colorGradient = new VertexGradient(theme.paleGold, theme.paleGold, theme.gold, theme.gold);
            UiFactory.Text("Subtitle", parent, theme.labelFont, "THE<space=1.2em>EMERALD<space=1.2em>SANCTUM", 13, theme.dimGold, new Vector2(0, 468), new Vector2(500, 20),
                           TextAlignmentOptions.Center, 24);
        }

        // Three plaques: the top tier in the middle, each with its gem, its name and its growing value
        private static JackpotPanelView EmeraldJackpots(Transform parent, ThemeDefinition theme)
        {
            RectTransform panel = UiFactory.Stretch("Jackpots", parent);
            UiFactory.NestedCanvas(panel.gameObject, false);
            var plaques = new Image[3];
            var names = new TextMeshProUGUI[3];
            var values = new CountingText[3];
            Vector2[] positions = { new Vector2(0, 390), new Vector2(-575, 392), new Vector2(575, 392) };
            Vector2[] sizes = { new Vector2(500, 120), new Vector2(330, 100), new Vector2(330, 100) };
            for (int i = 0; i < 3; i++)
            {
                bool top = i == 0;
                plaques[i] = JackpotPlaque(panel, theme, i, positions[i], sizes[i]);
                float iconSize = top ? 74f : 60f;
                Image icon = UiFactory.Image("Gem", plaques[i].transform, IconFor(theme, i), new Vector2(-sizes[i].x * 0.5f + iconSize * 0.5f + 22f, 0),
                                             Vector2.one * iconSize);
                icon.preserveAspect = true;
                float textX = iconSize * 0.5f;
                float textWidth = sizes[i].x - iconSize - 40f;
                names[i] = UiFactory.Text("Name", plaques[i].transform, theme.labelFont, "GRAND", top ? 15 : 13, theme.dimGold,
                                          new Vector2(textX, top ? 30 : 24), new Vector2(textWidth, 22), TextAlignmentOptions.Center, 30);
                TextMeshProUGUI value = UiFactory.Text("Value", plaques[i].transform, theme.headingFont, "0", top ? 44 : 32, top ? theme.paleGold : theme.ivory,
                                                       new Vector2(textX, top ? -12 : -10), new Vector2(textWidth, top ? 56 : 44));
                values[i] = value.gameObject.AddComponent<CountingText>();
            }

            var view = panel.gameObject.AddComponent<JackpotPanelView>();
            view.Setup(plaques, names, values);
            return view;
        }

        private static Sprite IconFor(ThemeDefinition theme, int tier)
        {
            return tier < theme.jackpotIcons.Length ? theme.jackpotIcons[tier] : null;
        }

        private static Image JackpotPlaque(RectTransform panel, ThemeDefinition theme, int tier, Vector2 position, Vector2 size)
        {
            Image plaque = UiFactory.Image(tier == 0 ? "Top Tier" : tier == 1 ? "Second Tier" : "Third Tier", panel, theme.jackpotPlaque, position, size);
            plaque.type = Image.Type.Sliced;
            plaque.color = tier < theme.jackpotTints.Length ? theme.jackpotTints[tier] : theme.surface;
            Image edge = UiFactory.Image("Edge", plaque.transform, Sprite("Generated/emerald_plaque_edge"), Vector2.zero, size);
            edge.type = Image.Type.Sliced;
            edge.color = tier == 0 ? Color.white : new Color(1f, 1f, 1f, 0.55f);
            return plaque;
        }

        // The control deck: deep emerald, a gold edge along the top and a fainter line inside it
        private static void EmeraldDeck(RectTransform console, ThemeDefinition theme)
        {
            RectTransform bar = BottomStrip("Deck", console, theme.consoleBar, 150, 0, 0).rectTransform;
            Image edge = BottomStrip("Gold Edge", bar, Sprite("Generated/rule"), 2, 148, 64);
            edge.color = theme.gold;
            Image inner = BottomStrip("Inner Line", bar, theme.white, 1, 140, 160);
            inner.color = new Color(theme.gold.r, theme.gold.g, theme.gold.b, 0.25f);
        }

        // A cyan glow, the gold wings, the bronze rotor that turns (faster while the reels run) and the emerald face
        private static SpinButtonView EmeraldSpinButton(Transform parent, ThemeDefinition theme, Vector2 position)
        {
            Image glow = UiFactory.Image("Spin Glow", parent, theme.glow, position, new Vector2(330, 330));
            glow.material = theme.additive;
            glow.color = new Color(theme.accent.r, theme.accent.g, theme.accent.b, 0.35f);
            Image wings = UiFactory.Image("Spin Wings", parent, theme.spinWings, position, new Vector2(270, 101));
            wings.preserveAspect = true;
            Image rotor = UiFactory.Image("Spin Rotor", parent, theme.spinRotor, position, new Vector2(212, 212));
            rotor.preserveAspect = true;

            Image face = UiFactory.Image("Spin", parent, theme.spinButton, position, new Vector2(150, 150));
            face.material = theme.symbolMaterial;
            Button button = UiFactory.Button(face);
            button.transition = Selectable.Transition.None;
            TextMeshProUGUI label = UiFactory.Text("Label", face.transform, theme.headingFont, "SPIN", 30, Color.white, new Vector2(0, -2), new Vector2(130, 48));
            label.enableVertexGradient = true;
            label.colorGradient = new VertexGradient(theme.paleGold, theme.paleGold, theme.gold, theme.gold);
            label.characterSpacing = 6f;

            var view = face.gameObject.AddComponent<SpinButtonView>();
            view.Setup(button, glow, rotor, face, label);
            view.SetRayAlpha(1f, 1f);
            return view;
        }
    }
}
