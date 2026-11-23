using System.Globalization;
using SlotKit;
using TMPro;
using UnityEngine;

namespace RoyalVault.Unity
{
    // The strip under the reels: the lines in play, what just won and what it pays
    public sealed class WinStripView : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI lines;
        [SerializeField]
        private TextMeshProUGUI message;
        [SerializeField]
        private TextMeshProUGUI pays;

        private string[] names = new string[0];

        public void Setup(TextMeshProUGUI linesText, TextMeshProUGUI messageText, TextMeshProUGUI paysText)
        {
            lines = linesText;
            message = messageText;
            pays = paysText;
        }

        public void Bind(MachineDefinition machine)
        {
            lines.text = machine.Paylines.Length + " LINES";
            names = new string[machine.Symbols.Length];
            for (int i = 0; i < names.Length; i++)
            {
                names[i] = machine.Symbols[i].Id.Replace('_', ' ').ToUpperInvariant();
            }

            Clear();
        }

        public void Clear()
        {
            message.text = "";
            pays.text = "";
        }

        // Everything the spin won, together
        public void ShowTotal(int lineCount, long amount)
        {
            message.text = lineCount == 0 ? "BONUS WIN" : lineCount == 1 ? "1 WINNING LINE" : lineCount + " WINNING LINES";
            pays.text = Pays(amount);
        }

        // One winning line
        public void ShowLine(LineWin line, long multiplier)
        {
            message.text = line.Count + " OF A KIND  ·  " + names[line.Symbol] + " WIN";
            pays.text = Pays(line.Amount * multiplier);
        }

        private static string Pays(long amount) => "PAYS " + amount.ToString("N0", CultureInfo.InvariantCulture);
    }
}
