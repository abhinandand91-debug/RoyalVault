using GameKit;
using RoyalVault.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalVault.EditorTools
{
    // The Treasury: twelve urns in two rows over the Treasury painting, with what has been collected, the multiplier and
    // the ankh's shield along the foot
    public static partial class RoyalVaultBuilder
    {
        private const int UrnColumns = 6;
        private const int UrnCount = 12;
        private static readonly Vector2 UrnSize = new Vector2(150, 188);

        private static TreasuryView Treasury(Transform parent, ThemeDefinition theme)
        {
            RectTransform overlay = UiFactory.Stretch("Treasury", parent);
            var group = overlay.gameObject.AddComponent<CanvasGroup>();
            UiFactory.NestedCanvas(overlay.gameObject, true, true);
            TreasuryBackdrop(overlay, theme);

            UiFactory.Text("Title", overlay, theme.headingFont, "THE TREASURY", 56, theme.gold, new Vector2(0, 430), new Vector2(1100, 74),
                           TextAlignmentOptions.Center, 20);
            TextMeshProUGUI prompt = UiFactory.Text("Prompt", overlay, theme.headingFont, "PICK AN URN", 26, Color.white, new Vector2(0, 364),
                                                    new Vector2(1200, 40), TextAlignmentOptions.Center, 10);

            RectTransform board = UiFactory.Rect("Board", overlay, new Vector2(0, -10), new Vector2(1600, 640));
            var urns = new TreasuryView.UrnParts[UrnCount];
            for (int i = 0; i < urns.Length; i++)
            {
                int column = i % UrnColumns;
                int row = i / UrnColumns;
                urns[i] = TreasuryUrn(board, theme, i, new Vector2(-625 + column * 250, 145 - row * 300));
            }

            UiParticleEmitter burst = TreasuryBurst(board, theme);
            (CountingText collected, TextMeshProUGUI multiplier, Image shield) = TreasuryReadouts(overlay, theme);

            Image flash = Stretch("Flash", overlay, theme.white);
            flash.color = new Color(1f, 0.2f, 0.15f, 0f);
            Image tapImage = Stretch("Tap To Continue", overlay, theme.white);
            tapImage.color = Color.clear;
            tapImage.raycastTarget = true;
            Button tap = UiFactory.Button(tapImage);
            tap.transition = Selectable.Transition.None;
            tapImage.gameObject.SetActive(false);

            Sprite[] cracks = { Sprite("Generated/crack_1"), Sprite("Generated/crack_2"), Sprite("Generated/crack_3") };
            var view = overlay.gameObject.AddComponent<TreasuryView>();
            view.Setup(group, board, prompt, collected, multiplier, shield, urns, burst, flash, tap, cracks);
            return view;
        }

        // The Treasury painting, covering the screen, darkened so the urns stand out
        private static void TreasuryBackdrop(RectTransform overlay, ThemeDefinition theme)
        {
            Image block = Stretch("Block", overlay, theme.white);
            block.color = new Color(0.01f, 0.01f, 0.02f, 1f);
            block.raycastTarget = true;
            Image art = UiFactory.Image("Art", overlay, Sprite("Story/scene_5", ".jpg"), Vector2.zero, new Vector2(1920, 1280));
            art.color = new Color(0.5f, 0.46f, 0.42f, 1f);
            Image fade = Stretch("Fade", overlay, Sprite("Generated/fade"));
            fade.color = new Color(1f, 1f, 1f, 0.9f);
        }

        // A glow behind, the urn, its cracks, and the prize that shows once it breaks
        private static TreasuryView.UrnParts TreasuryUrn(RectTransform board, ThemeDefinition theme, int index, Vector2 position)
        {
            RectTransform root = UiFactory.Rect("Urn " + (index + 1), board, position, UrnSize);
            Image glow = UiFactory.Image("Glow", root, theme.glow, new Vector2(0, -6), new Vector2(300, 300));
            glow.material = theme.additive;
            glow.color = new Color(1f, 0.8f, 0.45f, 0.3f);
            Image body = UiFactory.Image("Body", root, theme.urn, Vector2.zero, UrnSize);
            body.preserveAspect = true;
            Image crack = UiFactory.Image("Crack", root, null, new Vector2(0, -10), new Vector2(130, 130));
            crack.color = new Color(1f, 0.85f, 0.5f, 1f);
            crack.material = theme.additive;
            Image icon = UiFactory.Image("Prize Icon", root, null, Vector2.zero, new Vector2(140, 140));
            icon.preserveAspect = true;
            TextMeshProUGUI label = UiFactory.Text("Prize", root, theme.headingFont, "", 44, theme.paleGold, Vector2.zero, new Vector2(240, 120),
                                                   TextAlignmentOptions.Center, 4);
            Image hit = UiFactory.Image("Hit", root, theme.white, Vector2.zero, UrnSize + new Vector2(40, 30), true);
            hit.color = Color.clear;
            Button button = UiFactory.Button(hit);
            button.transition = Selectable.Transition.None;
            return new TreasuryView.UrnParts { root = root, button = button, body = body, crack = crack, glow = glow, icon = icon, label = label };
        }

        // Gold dust thrown out of a broken urn; the view moves its origin to the urn
        private static UiParticleEmitter TreasuryBurst(RectTransform board, ThemeDefinition theme)
        {
            UiParticleEmitter burst = Emitter("Burst", board, theme, theme.sparkle, Vector2.zero);
            burst.Rate = 0f;
            burst.LifeRange = new Vector2(0.5f, 1.1f);
            burst.SizeRange = new Vector2(12f, 30f);
            burst.EndSize = 0.15f;
            burst.VelocityMin = new Vector2(-260f, 120f);
            burst.VelocityMax = new Vector2(260f, 520f);
            burst.Gravity = new Vector2(0f, -620f);
            burst.Drag = 0.6f;
            burst.MaxParticles = 160;
            burst.StartColor = new Color(1f, 0.85f, 0.5f, 1f);
            return burst;
        }

        private static (CountingText collected, TextMeshProUGUI multiplier, Image shield) TreasuryReadouts(RectTransform overlay, ThemeDefinition theme)
        {
            UiFactory.Text("Collected Label", overlay, theme.labelFont, "COLLECTED", 16, theme.dimGold, new Vector2(-260, -392), new Vector2(360, 24),
                           TextAlignmentOptions.Center, 30);
            TextMeshProUGUI value = UiFactory.Text("Collected", overlay, theme.headingFont, "0", 50, theme.ivory, new Vector2(-260, -440),
                                                   new Vector2(520, 64));
            UiFactory.Text("Multiplier Label", overlay, theme.labelFont, "MULTIPLIER", 16, theme.dimGold, new Vector2(220, -392), new Vector2(300, 24),
                           TextAlignmentOptions.Center, 30);
            TextMeshProUGUI multiplier = UiFactory.Text("Multiplier", overlay, theme.headingFont, "x1", 50, theme.gold, new Vector2(220, -440),
                                                        new Vector2(240, 64));
            UiFactory.Text("Shield Label", overlay, theme.labelFont, "SHIELD", 16, theme.dimGold, new Vector2(470, -392), new Vector2(200, 24),
                           TextAlignmentOptions.Center, 30);
            Image shield = UiFactory.Image("Shield", overlay, null, new Vector2(470, -440), new Vector2(76, 76));
            shield.preserveAspect = true;
            return (value.gameObject.AddComponent<CountingText>(), multiplier, shield);
        }
    }
}
