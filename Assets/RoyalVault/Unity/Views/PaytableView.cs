using System.Collections;
using System.Collections.Generic;
using RoyalVault.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalVault.Unity
{
    /// <summary>The pay table window, written from the machine definition so it always matches the maths.</summary>
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

        /// <summary>Writes one row per paying symbol, then the scatter, wild, free spin and jackpot rules.</summary>
        public void Build(MachineDefinition machine, ThemeDefinition theme)
        {
            for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);

            var paying = new List<int>();
            for (int i = 0; i < machine.Symbols.Length; i++)
            {
                SymbolDefinition symbol = machine.Symbols[i];
                if (symbol.Kind != SymbolKind.Regular && symbol.Kind != SymbolKind.Wild) continue;
                bool onReels = false;
                foreach (int[] strip in machine.Strips)
                    if (System.Array.IndexOf(strip, i) >= 0) { onReels = true; break; }
                if (onReels) paying.Add(i);
            }
            paying.Reverse(); // best symbols first

            // the symbol grid fills the panel's dark centre: up to four rows of three, the best symbols first
            const int columns = 3;
            const float cellW = 390f, cellH = 96f, firstRow = 168f;
            for (int n = 0; n < paying.Count; n++)
            {
                SymbolDefinition symbol = machine.Symbols[paying[n]];
                var origin = new Vector2(((n % columns) - (columns - 1) * 0.5f) * cellW, firstRow - (n / columns) * cellH);
                ThemeDefinition.SymbolArt art = theme.Symbol(symbol.Id);
                Image icon = UiFactory.Image(symbol.Id, content, art?.sprite, origin + new Vector2(-90f, 0f), Vector2.one * 86f);
                icon.preserveAspect = true;
                var lines = new List<string>();
                for (int count = symbol.Pays.Length - 1; count >= 0; count--)
                    if (symbol.Pays[count] > 0) lines.Add($"<color=#B39A66>{count}</color>  {symbol.Pays[count]}");
                TextMeshProUGUI pays = UiFactory.Text("Pays", content, theme.headingFont, string.Join("\n", lines), 21f, theme.ivory,
                                                      origin + new Vector2(70f, 0f), new Vector2(180f, cellH), TextAlignmentOptions.Left);
                pays.lineSpacing = -16f;
            }

            // three short lines of rules under the grid
            var rules = new List<string> { $"Pays x line bet, left to right on {machine.Paylines.Length} lines. WILD stands in for all but the scatter and the crown." };
            if (machine.ScatterAwards.Length > 0)
            {
                var counts = new List<string>(); var pays = new List<string>(); var spins = new List<string>();
                foreach (ScatterAward a in machine.ScatterAwards) { counts.Add(a.Count.ToString()); pays.Add(a.TotalBetMultiplier + "x"); spins.Add(a.FreeSpins.ToString()); }
                rules.Add($"{string.Join(" / ", counts)} SCATTERS pay {string.Join(" / ", pays)} the bet with {string.Join(" / ", spins)} free spins, every win x{machine.FreeSpinMultiplier}.");
            }
            if (machine.JackpotTiers.Length > 0)
            {
                var counts = new List<string>(); var names = new List<string>();
                foreach (JackpotTierDefinition t in machine.JackpotTiers) { counts.Add(t.SymbolCount.ToString()); names.Add(t.Name); }
                rules.Add($"{string.Join(" / ", counts)} CROWNS anywhere win the {string.Join(" / ", names)} jackpot.");
            }
            float rulesY = firstRow - Mathf.CeilToInt(paying.Count / (float)columns) * cellH - 22f;
            TextMeshProUGUI ruleText = UiFactory.Text("Rules", content, theme.bodyFont, string.Join("\n", rules), 22f, theme.dimGold,
                                                      new Vector2(0f, rulesY), new Vector2(1150f, 96f));
            ruleText.textWrappingMode = TextWrappingModes.Normal;
            ruleText.lineSpacing = -6f;
        }

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
