using System.Collections.Generic;
using GameKit;
using SlotKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalVault.Unity
{
    // Every paying symbol on a card with its pays in credits: wild and premiums on top, gems next, card royals below
    public static class PaytableSymbolsPage
    {
        private const float PremiumHeight = 140f;
        private const float GemHeight = 118f;
        private const float RoyalHeight = 96f;

        public static List<PayValues> Build(RectTransform page, MachineDefinition machine, ThemeDefinition theme)
        {
            var values = new List<PayValues>();
            List<SymbolDefinition> paying = PayingSymbols(machine);
            List<SymbolDefinition> premiums = paying.FindAll(symbol => Tier(symbol, theme) == 0);
            List<SymbolDefinition> gems = paying.FindAll(symbol => Tier(symbol, theme) == 1);
            List<SymbolDefinition> royals = paying.FindAll(symbol => Tier(symbol, theme) == 2);

            Row(page, premiums, theme, 70f, 270f, PremiumHeight, values);
            Row(page, gems, theme, -66f, 270f, GemHeight, values);
            Row(page, royals, theme, -189f, 214f, RoyalHeight, values);
            return values;
        }

        // 0 = wild and treasures, 1 = gems, 2 = card royals
        private static int Tier(SymbolDefinition symbol, ThemeDefinition theme)
        {
            SymbolStyle style = theme.Symbol(symbol.Id) != null ? theme.Symbol(symbol.Id).style : SymbolStyle.Card;
            if (style == SymbolStyle.Card)
            {
                return 2;
            }

            return style == SymbolStyle.Gem ? 1 : 0;
        }

        // Regular symbols and wilds that appear on the reels, best paying first
        private static List<SymbolDefinition> PayingSymbols(MachineDefinition machine)
        {
            var paying = new List<SymbolDefinition>();
            for (int i = 0; i < machine.Symbols.Length; i++)
            {
                SymbolDefinition symbol = machine.Symbols[i];
                bool pays = symbol.Kind == SymbolKind.Regular || symbol.Kind == SymbolKind.Wild;
                if (pays && OnReels(machine, i))
                {
                    paying.Add(symbol);
                }
            }

            paying.Sort((a, b) => b.Pays[b.Pays.Length - 1].CompareTo(a.Pays[a.Pays.Length - 1]));
            return paying;
        }

        private static bool OnReels(MachineDefinition machine, int symbol)
        {
            foreach (int[] strip in machine.Strips)
            {
                if (System.Array.IndexOf(strip, symbol) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static void Row(RectTransform page, List<SymbolDefinition> symbols, ThemeDefinition theme, float y, float width, float height,
                                List<PayValues> values)
        {
            const float gap = 18f;
            float start = -(symbols.Count - 1) * (width + gap) * 0.5f;
            for (int i = 0; i < symbols.Count; i++)
            {
                var centre = new Vector2(start + i * (width + gap), y);
                values.Add(Card(page, symbols[i], theme, centre, new Vector2(width, height)));
            }
        }

        private static PayValues Card(RectTransform page, SymbolDefinition symbol, ThemeDefinition theme, Vector2 centre, Vector2 size)
        {
            bool wild = symbol.Kind == SymbolKind.Wild;
            Image card = UiFactory.Image(symbol.Id, page, theme.card, centre, size);
            card.type = Image.Type.Sliced;
            card.color = wild ? new Color(1f, 0.86f, 0.55f, 1f) : Color.white;
            RectTransform rect = card.rectTransform;

            float iconSize = size.y - 22f;
            float iconX = -size.x * 0.5f + 14f + iconSize * 0.5f;
            Image icon = UiFactory.Image("Icon", rect, theme.Symbol(symbol.Id)?.sprite, new Vector2(iconX, 0f), Vector2.one * iconSize);
            icon.preserveAspect = true;
            Glow(rect, theme, iconX, iconSize, wild);

            bool named = size.y >= GemHeight;
            float textTop = named ? 10f : 0f;
            if (named)
            {
                UiFactory.Text("Name", rect, theme.labelFont, DisplayName(symbol), 13f, wild ? theme.paleGold : theme.dimGold,
                               new Vector2(iconX + iconSize * 0.5f + 70f, size.y * 0.5f - 20f), new Vector2(150f, 22f), TextAlignmentOptions.Center, 30f);
            }

            return Pays(rect, symbol, theme, size, iconX + iconSize * 0.5f, textTop);
        }

        // A soft light behind the art; brighter behind the wild
        private static void Glow(RectTransform card, ThemeDefinition theme, float x, float size, bool wild)
        {
            Image glow = UiFactory.Image("Glow", card, theme.glow, new Vector2(x, 0f), Vector2.one * size * 1.3f);
            glow.material = theme.additive;
            glow.color = new Color(1f, 0.8f, 0.45f, wild ? 0.35f : 0.12f);
            glow.transform.SetSiblingIndex(0);
        }

        private static PayValues Pays(RectTransform card, SymbolDefinition symbol, ThemeDefinition theme, Vector2 size, float left, float top)
        {
            float right = size.x * 0.5f - 16f;
            float height = size.y - 34f;
            float fontSize = size.y >= GemHeight ? 21f : 18f;
            var area = new Vector2(left + 8f, -top * 0.5f - 6f);
            // both columns share font, size and spacing so their lines stay level
            TextMeshProUGUI counts = UiFactory.Text("Counts", card, theme.headingFont, "", fontSize, theme.dimGold,
                                                    area + new Vector2(20f, 0f), new Vector2(40f, height), TextAlignmentOptions.Left);
            TextMeshProUGUI values = UiFactory.Text("Values", card, theme.headingFont, "", fontSize, theme.ivory,
                                                    new Vector2((area.x + 40f + right) * 0.5f, area.y), new Vector2(right - area.x - 40f, height),
                                                    TextAlignmentOptions.Right);
            counts.lineSpacing = -4f;
            values.lineSpacing = -4f;
            return new PayValues(symbol, counts, values);
        }

        // "vault_key" reads as "VAULT KEY"; card royals show only their letter
        private static string DisplayName(SymbolDefinition symbol)
        {
            string id = symbol.Id.StartsWith("card_") ? symbol.Id.Substring(5) : symbol.Id;
            return id.Replace('_', ' ').ToUpperInvariant();
        }
    }
}
