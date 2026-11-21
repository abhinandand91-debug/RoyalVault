using System.Text;
using GameKit;
using SlotKit;
using TMPro;

namespace RoyalVault.Unity
{
    // A symbol's pays written in credits at the current line bet, best count first
    public sealed class PayValues
    {
        private readonly TextMeshProUGUI counts;
        private readonly TextMeshProUGUI values;
        private readonly SymbolDefinition symbol;

        public PayValues(SymbolDefinition paying, TextMeshProUGUI countText, TextMeshProUGUI valueText)
        {
            symbol = paying;
            counts = countText;
            values = valueText;
        }

        public void Write(long lineBet)
        {
            var countLines = new StringBuilder();
            var valueLines = new StringBuilder();
            for (int count = symbol.Pays.Length - 1; count >= 0; count--)
            {
                if (symbol.Pays[count] <= 0)
                {
                    continue;
                }

                if (countLines.Length > 0)
                {
                    countLines.Append('\n');
                    valueLines.Append('\n');
                }

                countLines.Append(count).Append('×');
                valueLines.Append(NumberFormat.Credits(symbol.Pays[count] * lineBet));
            }

            counts.text = countLines.ToString();
            values.text = valueLines.ToString();
        }
    }
}
