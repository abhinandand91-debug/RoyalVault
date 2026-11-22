using System.Collections.Generic;
using GameKit;
using PrimeTween;
using SlotKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalVault.Unity
{
    // The pay table window: symbols, features and paylines on tabs, written from the machine so it always matches the maths
    public sealed class PaytableView : DialogView
    {
        private static readonly string[] TabNames = { "SYMBOLS", "FEATURES", "PAYLINES" };
        private static readonly Color ActiveTab = new Color(1f, 0.86f, 0.52f, 1f);
        private static readonly Color IdleTab = new Color(1f, 1f, 1f, 0.55f);

        [SerializeField] private RectTransform content;
        [SerializeField] private Button close;

        private readonly List<PayValues> pays = new List<PayValues>();
        private readonly List<CanvasGroup> pages = new List<CanvasGroup>();
        private readonly List<Image> tabs = new List<Image>();
        private readonly List<TextMeshProUGUI> tabLabels = new List<TextMeshProUGUI>();
        private BetModel bet;
        private ThemeDefinition theme;
        private TextMeshProUGUI betNote;
        private Tween pageFade;
        private int page;

        public void Setup(RectTransform contentRect, Button closeButton)
        {
            content = contentRect;
            close = closeButton;
        }

        protected override void Awake()
        {
            base.Awake();
            close.onClick.AddListener(Close);
        }

        public void Build(MachineDefinition machine, ThemeDefinition themeDefinition, BetModel betModel)
        {
            theme = themeDefinition;
            bet = betModel;
            Clear();
            TabBar();
            RectTransform symbols = Page("Symbols");
            pays.AddRange(PaytableSymbolsPage.Build(symbols, machine, theme));
            betNote = UiFactory.Text("Bet Note", symbols, theme.bodyFont, "", 17f, theme.dimGold, new Vector2(0f, -262f), new Vector2(1100f, 26f));
            PaytableFeaturesPage.Build(Page("Features"), machine, theme);
            PaytableLinesPage.Build(Page("Paylines"), machine, theme);
            ShowPage(0, false);
        }

        public void Open()
        {
            if (!IsOpen)
            {
                Present();
            }
        }

        public void Close()
        {
            RequestClose();
        }

        // The pays are written for the bet being played as the window opens
        protected override void OnOpening()
        {
            WriteValues();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            pageFade.Stop();
        }

        // Shows the tab at index; the tab buttons and the screenshot test call it
        public void ShowPage(int index, bool animate = true)
        {
            page = Mathf.Clamp(index, 0, pages.Count - 1);
            for (int i = 0; i < pages.Count; i++)
            {
                bool shown = i == page;
                pages[i].gameObject.SetActive(shown);
                tabs[i].color = shown ? ActiveTab : IdleTab;
                tabLabels[i].color = shown ? theme.paleGold : theme.dimGold;
            }

            pageFade.Stop();
            if (animate)
            {
                pageFade = Tween.Alpha(pages[page], 0f, 1f, 0.18f, Ease.OutCubic);
            }
            else
            {
                pages[page].alpha = 1f;
            }
        }

        // Pays in credits at the bet being played now
        private void WriteValues()
        {
            if (bet == null)
            {
                return;
            }

            foreach (PayValues values in pays)
            {
                values.Write(bet.LineBet);
            }

            betNote.text = "Wins in credits at your line bet of " + NumberFormat.Credits(bet.LineBet) + " (total bet "
                           + NumberFormat.Credits(bet.TotalBet) + "). Line wins pay left to right.";
        }

        private void Clear()
        {
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                Destroy(content.GetChild(i).gameObject);
            }

            pays.Clear();
            pages.Clear();
            tabs.Clear();
            tabLabels.Clear();
        }

        private void TabBar()
        {
            const float width = 230f;
            for (int i = 0; i < TabNames.Length; i++)
            {
                var position = new Vector2((i - (TabNames.Length - 1) * 0.5f) * (width + 14f), 186f);
                Image pill = UiFactory.Image(TabNames[i] + " Tab", content, theme.pillButton, position, new Vector2(width, 50f));
                pill.type = Image.Type.Sliced;
                Button button = UiFactory.Button(pill);
                int index = i;
                button.onClick.AddListener(() => ShowPage(index));
                TextMeshProUGUI label = UiFactory.Text("Label", pill.transform, theme.headingFont, TabNames[i], 18f, theme.dimGold, Vector2.zero,
                                                       new Vector2(width, 50f), TextAlignmentOptions.Center, 16f);
                tabs.Add(pill);
                tabLabels.Add(label);
            }
        }

        private RectTransform Page(string name)
        {
            RectTransform rect = UiFactory.Rect(name, content, Vector2.zero, content.rect.size);
            pages.Add(rect.gameObject.AddComponent<CanvasGroup>());
            return rect;
        }
    }
}
