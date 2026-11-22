using System.Text;
using GameKit;
using SlotKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalVault.Unity
{
    // The special symbols side by side: the wild, the Vault Seal's free spins and the Crown's jackpots
    public static class PaytableFeaturesPage
    {
        private static readonly Vector2 PanelSize = new Vector2(360f, 360f);

        public static void Build(RectTransform page, MachineDefinition machine, ThemeDefinition theme)
        {
            Panel(page, theme, -385f, IdOf(machine, SymbolKind.Wild), "WILD", WildText());
            Panel(page, theme, 0f, IdOf(machine, SymbolKind.Scatter), "VAULT SEAL", SealText(machine));
            Panel(page, theme, 385f, IdOf(machine, SymbolKind.Jackpot), "CROWN JACKPOTS", CrownText(machine));
            float noteY = -246f;
            if (machine.Treasury.Enabled)
            {
                string note = "A Vault Key on reels 1, 3 and 5 opens the Treasury: pick urns for credits, a multiplier or an ankh's shield, "
                              + "until the guardian's eye is found.";
                UiFactory.Text("Treasury", page, theme.bodyFont, note, 17f, theme.dimGold, new Vector2(0f, noteY), new Vector2(1200f, 26f));
                noteY -= 26f;
            }

            if (machine.Gamble.Enabled)
            {
                string note = "After a win, gamble it on Ruby's Last Hand: call a colour to double it or a suit to quadruple it, up to "
                              + machine.Gamble.MaxRounds + " times.";
                UiFactory.Text("Gamble", page, theme.bodyFont, note, 17f, theme.dimGold, new Vector2(0f, noteY), new Vector2(1100f, 26f));
            }
        }

        private static string IdOf(MachineDefinition machine, SymbolKind kind)
        {
            foreach (SymbolDefinition symbol in machine.Symbols)
            {
                if (symbol.Kind == kind)
                {
                    return symbol.Id;
                }
            }

            return null;
        }

        private static void Panel(RectTransform page, ThemeDefinition theme, float x, string symbolId, string heading, string body)
        {
            Image card = UiFactory.Image(heading, page, theme.card, new Vector2(x, -48f), PanelSize);
            card.type = Image.Type.Sliced;
            RectTransform rect = card.rectTransform;

            Image glow = UiFactory.Image("Glow", rect, theme.glow, new Vector2(0f, 108f), Vector2.one * 180f);
            glow.material = theme.additive;
            glow.color = new Color(1f, 0.8f, 0.45f, 0.25f);
            Image icon = UiFactory.Image("Icon", rect, symbolId != null ? theme.Symbol(symbolId)?.sprite : null, new Vector2(0f, 108f), Vector2.one * 120f);
            icon.preserveAspect = true;

            UiFactory.Text("Heading", rect, theme.headingFont, heading, 24f, theme.gold, new Vector2(0f, 28f), new Vector2(320f, 34f),
                           TextAlignmentOptions.Center, 12f);
            Image rule = UiFactory.Image("Rule", rect, theme.line, new Vector2(0f, 7f), new Vector2(220f, 6f));
            rule.color = new Color(1f, 0.85f, 0.5f, 0.6f);
            TextMeshProUGUI text = UiFactory.Text("Body", rect, theme.bodyFont, body, 19f, theme.ivory, new Vector2(0f, -88f), new Vector2(316f, 150f),
                                                  TextAlignmentOptions.Top);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.lineSpacing = -4f;
        }

        private static string WildText()
        {
            return "Stands in for every symbol except the Vault Seal and the Crown, completing the best win on each line.\n\n" +
                   "<color=#E8C77A>A line of wilds pays the top prize.</color>";
        }

        private static string SealText(MachineDefinition machine)
        {
            var text = new StringBuilder();
            foreach (ScatterAward award in machine.ScatterAwards)
            {
                text.Append("<color=#E8C77A>").Append(award.Count).Append(" seals</color>  ")
                    .Append(award.FreeSpins).Append(" free spins + ").Append(award.TotalBetMultiplier).Append("× bet\n");
            }

            text.Append("\nSeals pay anywhere.");
            if (machine.ExpandingFreeSpinSymbol)
            {
                text.Append(" Each round reveals a <color=#E8C77A>Sealed Relic</color> that covers whole reels and pays on every line.");
            }

            if (machine.FreeSpinMultiplier > 1)
            {
                text.Append(" Every free spin win is multiplied by ").Append(machine.FreeSpinMultiplier).Append('.');
            }

            return text.ToString();
        }

        private static string CrownText(MachineDefinition machine)
        {
            var text = new StringBuilder();
            for (int i = machine.JackpotTiers.Length - 1; i >= 0; i--)
            {
                JackpotTierDefinition tier = machine.JackpotTiers[i];
                text.Append("<color=#E8C77A>").Append(tier.SymbolCount).Append(" crowns</color>  ").Append(tier.Name).Append(" jackpot\n");
            }

            text.Append("\nCrowns count anywhere on the reels. Jackpots grow with every bet until won.");
            return text.ToString();
        }
    }
}
