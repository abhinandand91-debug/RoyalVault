using GameKit;
using SlotKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalVault.Unity
{
    // Every payline as a small grid of the reels with its cells lit
    public static class PaytableLinesPage
    {
        private const int Columns = 5;
        private static readonly Vector2 Diagram = new Vector2(206f, 90f);
        private static readonly Vector2 Spacing = new Vector2(226f, 100f);

        public static void Build(RectTransform page, MachineDefinition machine, ThemeDefinition theme)
        {
            int rows = Mathf.CeilToInt(machine.Paylines.Length / (float)Columns);
            float top = (rows - 1) * Spacing.y * 0.5f - 52f;
            for (int i = 0; i < machine.Paylines.Length; i++)
            {
                int column = i % Columns;
                int row = i / Columns;
                var centre = new Vector2((column - (Columns - 1) * 0.5f) * Spacing.x, top - row * Spacing.y);
                Line(page, machine, theme, i, centre);
            }

            string note = "Wins pay left to right from the first reel, on all " + machine.Paylines.Length + " lines. Only the highest win on each line pays.";
            UiFactory.Text("Note", page, theme.bodyFont, note, 17f, theme.dimGold, new Vector2(0f, -264f), new Vector2(1100f, 26f));
        }

        private static void Line(RectTransform page, MachineDefinition machine, ThemeDefinition theme, int index, Vector2 centre)
        {
            Image card = UiFactory.Image("Line " + (index + 1), page, theme.card, centre, Diagram);
            card.type = Image.Type.Sliced;
            RectTransform rect = card.rectTransform;
            UiFactory.Text("Number", rect, theme.headingFont, (index + 1).ToString(), 18f, theme.gold, new Vector2(-82f, 0f), new Vector2(30f, 30f));

            int[] rows = machine.Paylines[index];
            var cell = new Vector2(24f, 18f);
            for (int reel = 0; reel < machine.Reels; reel++)
            {
                for (int row = 0; row < machine.Rows; row++)
                {
                    bool lit = rows[reel] == row;
                    var position = new Vector2(-38f + reel * (cell.x + 4f), (machine.Rows - 1) * 0.5f * (cell.y + 4f) - row * (cell.y + 4f));
                    Image square = UiFactory.Image("Cell", rect, theme.white, position, cell);
                    square.color = lit ? new Color(1f, 0.82f, 0.42f, 1f) : new Color(1f, 1f, 1f, 0.08f);
                }
            }
        }
    }
}
