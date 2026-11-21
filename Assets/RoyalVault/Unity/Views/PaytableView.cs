using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RoyalVault.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalVault.Unity
{
    // The pay table window, written from the machine definition so it always matches the maths
    public sealed class PaytableView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform content;
        [SerializeField] private Button close;
        [SerializeField] private Button backdrop;

        public bool IsOpen { get; private set; }

        public void Setup(CanvasGroup canvasGroup, RectTransform contentRect, Button closeButton, Button backdropButton)
        {
            group = canvasGroup;
            content = contentRect;
            close = closeButton;
            backdrop = backdropButton;
        }

        private void Awake()
        {
            close.onClick.AddListener(Close);
            backdrop.onClick.AddListener(Close);
            if (!IsOpen) SetVisible(false);
        }

        private const int Columns = 3;
        private const float CellWidth = 390f, CellHeight = 96f, FirstRow = 168f;

        // Writes one row per paying symbol, then the scatter, wild, free spin and jackpot rules
        public void Build(MachineDefinition machine, ThemeDefinition theme)
        {
            for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);
            List<int> paying = PayingSymbols(machine);
            for (int n = 0; n < paying.Count; n++) PayRow(machine.Symbols[paying[n]], n, theme);

            float rulesY = FirstRow - Mathf.CeilToInt(paying.Count / (float)Columns) * CellHeight - 22f;
            TextMeshProUGUI rules = UiFactory.Text("Rules", content, theme.bodyFont, string.Join("\n", Rules(machine)), 22f, theme.dimGold,
                                                   new Vector2(0f, rulesY), new Vector2(1150f, 96f));
            rules.textWrappingMode = TextWrappingModes.Normal;
            rules.lineSpacing = -6f;
        }

        // Regular symbols and wilds found on the reels, best first
        private static List<int> PayingSymbols(MachineDefinition machine)
        {
            var paying = new List<int>();
            for (int i = 0; i < machine.Symbols.Length; i++)
            {
                SymbolKind kind = machine.Symbols[i].Kind;
                bool pays = kind == SymbolKind.Regular || kind == SymbolKind.Wild;
                if (pays && machine.Strips.Any(strip => System.Array.IndexOf(strip, i) >= 0)) paying.Add(i);
            }
            paying.Reverse();
            return paying;
        }

        private void PayRow(SymbolDefinition symbol, int index, ThemeDefinition theme)
        {
            var origin = new Vector2((index % Columns - (Columns - 1) * 0.5f) * CellWidth, FirstRow - index / Columns * CellHeight);
            Image icon = UiFactory.Image(symbol.Id, content, theme.Symbol(symbol.Id)?.sprite, origin + new Vector2(-90f, 0f), Vector2.one * 86f);
            icon.preserveAspect = true;
            var lines = new List<string>();
            for (int count = symbol.Pays.Length - 1; count >= 0; count--)
                if (symbol.Pays[count] > 0) lines.Add($"<color=#B39A66>{count}</color>  {symbol.Pays[count]}");
            TextMeshProUGUI pays = UiFactory.Text("Pays", content, theme.headingFont, string.Join("\n", lines), 21f, theme.ivory,
                                                  origin + new Vector2(70f, 0f), new Vector2(180f, CellHeight), TextAlignmentOptions.Left);
            pays.lineSpacing = -16f;
        }

        private static List<string> Rules(MachineDefinition machine)
        {
            var rules = new List<string> { $"Pays x line bet, left to right on {machine.Paylines.Length} lines. WILD stands in for all but the scatter and the crown." };
            if (machine.ScatterAwards.Length > 0)
            {
                ScatterAward[] a = machine.ScatterAwards;
                rules.Add($"{Join(a, x => x.Count)} SCATTERS pay {Join(a, x => x.TotalBetMultiplier + "x")} the bet with {Join(a, x => x.FreeSpins)} free spins, every win x{machine.FreeSpinMultiplier}.");
            }
            if (machine.JackpotTiers.Length > 0)
                rules.Add($"{Join(machine.JackpotTiers, t => t.SymbolCount)} CROWNS anywhere win the {Join(machine.JackpotTiers, t => t.Name)} jackpot.");
            return rules;
        }

        private static string Join<T>(IEnumerable<T> items, System.Func<T, object> part) => string.Join(" / ", items.Select(part));

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            StopAllCoroutines();
            StartCoroutine(Fade(1f));
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            StopAllCoroutines();
            StartCoroutine(Fade(0f));
        }

        private IEnumerator Fade(float to)
        {
            group.blocksRaycasts = to > 0f;
            float from = group.alpha;
            yield return Tween.Run(0.22f, k => group.alpha = Mathf.Lerp(from, to, k), Ease.OutCubic);
            if (to <= 0f) SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            group.alpha = visible ? 1f : 0f;
            group.blocksRaycasts = visible;
            gameObject.SetActive(visible);
        }
    }
}
