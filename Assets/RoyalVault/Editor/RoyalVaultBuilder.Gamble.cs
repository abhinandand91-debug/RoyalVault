using GameKit;
using RoyalVault.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalVault.EditorTools
{
    // Ruby's Last Hand: Ruby at the table, one card to call, the stake, the calls and the cards already turned
    public static partial class RoyalVaultBuilder
    {
        private static readonly Vector2 GambleCardSize = new Vector2(240, 336);

        private static GambleView Gamble(Transform parent, ThemeDefinition theme)
        {
            RectTransform overlay = UiFactory.Stretch("Ruby's Last Hand", parent);
            var group = overlay.gameObject.AddComponent<CanvasGroup>();
            UiFactory.NestedCanvas(overlay.gameObject, true, group);
            Image dim = Stretch("Dim", overlay, theme.white);
            dim.color = new Color(0.01f, 0.01f, 0.02f, 0.86f);
            dim.raycastTarget = true;

            Image ruby = GambleRuby(overlay, theme);
            UiFactory.Text("Title", overlay, theme.headingFont, "RUBY'S LAST HAND", 52, theme.gold, new Vector2(0, 400), new Vector2(1100, 70),
                           TextAlignmentOptions.Center, 18);
            TextMeshProUGUI result = UiFactory.Text("Result", overlay, theme.headingFont, "", 30, theme.paleGold, new Vector2(0, 330), new Vector2(900, 44),
                                                    TextAlignmentOptions.Center, 10);
            GambleView.CardParts card = GambleCard("Card", overlay, theme, new Vector2(0, 80), GambleCardSize);
            CountingText stake = GambleStake(overlay, theme);
            Button[] calls = GambleCalls(overlay, theme);
            Image collect = Pill("Collect", overlay, theme, new Vector2(440, -300), new Vector2(220, 64));
            collect.color = new Color(1f, 0.86f, 0.5f, 1f);
            UiFactory.Text("Label", collect.transform, theme.headingFont, "COLLECT", 20, theme.paleGold, Vector2.zero, new Vector2(220, 64),
                           TextAlignmentOptions.Center, 14);

            var view = overlay.gameObject.AddComponent<GambleView>();
            view.Setup(group, result, card, stake, calls, UiFactory.Button(collect), GambleHistory(overlay, theme), ruby);
            return view;
        }

        // Ruby stands at the left of the table, sized to her baked frames
        private static Image GambleRuby(RectTransform overlay, ThemeDefinition theme)
        {
            const float height = 760f;
            Image ruby = UiFactory.Image("Ruby", overlay, null, new Vector2(-640, -540), new Vector2(300, height));
            ruby.rectTransform.pivot = new Vector2(0.5f, 0f);
            ruby.color = Color.white;
            ruby.preserveAspect = true;
            CharacterAnimation motion = theme.heroRight;
            if (motion != null && motion.IsValid)
            {
                Sprite first = motion.idle.frames[0];
                ruby.rectTransform.sizeDelta = new Vector2(height * first.rect.width / first.rect.height, height);
            }

            return ruby;
        }

        private static GambleView.CardParts GambleCard(string name, Transform parent, ThemeDefinition theme, Vector2 position, Vector2 size)
        {
            RectTransform root = UiFactory.Rect(name, parent, position, size);
            var parts = new GambleView.CardParts
            {
                root = root,
                back = UiFactory.Image("Back", root, theme.cardBack, Vector2.zero, size),
                face = UiFactory.Image("Face", root, theme.cardFace, Vector2.zero, size),
                suit = UiFactory.Image("Suit", root, null, new Vector2(0, -size.y * 0.04f), Vector2.one * size.x * 0.5f),
                rank = UiFactory.Text("Rank", root, theme.headingFont, "", size.x * 0.18f, Color.black,
                                      new Vector2(-size.x * 0.3f, size.y * 0.36f), new Vector2(size.x * 0.34f, size.y * 0.2f)),
            };
            parts.suit.preserveAspect = true;
            return parts;
        }

        private static CountingText GambleStake(RectTransform overlay, ThemeDefinition theme)
        {
            UiFactory.Text("Stake Label", overlay, theme.labelFont, "STAKE", 16, theme.dimGold, new Vector2(0, -128), new Vector2(300, 24),
                           TextAlignmentOptions.Center, 30);
            TextMeshProUGUI value = UiFactory.Text("Stake", overlay, theme.headingFont, "0", 46, theme.ivory, new Vector2(0, -172), new Vector2(500, 60));
            UiFactory.Text("Odds", overlay, theme.bodyFont, "A colour pays x2   ·   a suit pays x4", 21, theme.dimGold, new Vector2(0, -222),
                           new Vector2(800, 30));
            return CountingText.Attach(value);
        }

        // Red, black, then hearts, diamonds, clubs and spades, in GambleChoice order
        private static Button[] GambleCalls(RectTransform overlay, ThemeDefinition theme)
        {
            var calls = new Button[6];
            calls[0] = ColourCall(overlay, theme, "RED  x2", new Vector2(-470, -300), new Color(1f, 0.55f, 0.55f, 1f));
            calls[1] = ColourCall(overlay, theme, "BLACK  x2", new Vector2(-270, -300), new Color(0.75f, 0.75f, 0.8f, 1f));
            for (int i = 0; i < 4; i++)
            {
                calls[2 + i] = SuitCall(overlay, theme, i, new Vector2(-95 + i * 92, -300));
            }

            return calls;
        }

        private static Button ColourCall(RectTransform overlay, ThemeDefinition theme, string label, Vector2 position, Color tint)
        {
            Image pill = Pill(label, overlay, theme, position, new Vector2(180, 64));
            pill.color = tint;
            UiFactory.Text("Label", pill.transform, theme.headingFont, label, 19, theme.paleGold, Vector2.zero, new Vector2(180, 64),
                           TextAlignmentOptions.Center, 10);
            return UiFactory.Button(pill);
        }

        private static Button SuitCall(RectTransform overlay, ThemeDefinition theme, int suit, Vector2 position)
        {
            Image ring = UiFactory.Image("Suit " + suit, overlay, theme.roundButton, position, Vector2.one * 76);
            Image icon = UiFactory.Image("Icon", ring.transform, theme.suits[suit], Vector2.zero, Vector2.one * 40);
            icon.color = suit < 2 ? new Color(0.95f, 0.35f, 0.38f, 1f) : new Color(0.92f, 0.9f, 0.86f, 1f);
            return UiFactory.Button(ring);
        }

        // The last five cards turned, newest first
        private static GambleView.CardParts[] GambleHistory(RectTransform overlay, ThemeDefinition theme)
        {
            UiFactory.Text("Previous", overlay, theme.labelFont, "PREVIOUS CARDS", 14, theme.dimGold, new Vector2(500, 175), new Vector2(360, 22),
                           TextAlignmentOptions.Center, 24);
            var history = new GambleView.CardParts[5];
            for (int i = 0; i < history.Length; i++)
            {
                history[i] = GambleCard("Previous " + (i + 1), overlay, theme, new Vector2(360 + i * 70, 110), new Vector2(60, 84));
                history[i].root.gameObject.SetActive(false);
            }

            return history;
        }
    }
}
