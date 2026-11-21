using System;
using System.Collections.Generic;
using RoyalVault.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalVault.Unity
{
    /// <summary>
    /// The reels inside their frame. It builds itself for any grid (5 x 3, 3 x 3, ...): the cells are sized to fit the
    /// theme's reel area, the reels are centred, and the 9-slice frame wraps whatever size results. It spins the reels
    /// to the stops the engine chose and highlights winning cells and lines.
    /// </summary>
    /// <summary>The order the reels start or stop in.</summary>
    public enum ReelOrder
    {
        /// <summary>One after another, left to right.</summary>
        Sequential,
        /// <summary>One after another, in a new random order every spin.</summary>
        Random,
        /// <summary>Every reel together.</summary>
        AllAtOnce,
    }

    public sealed class MachineView : MonoBehaviour
    {
        private const float RunSeconds = 1.25f;
        private const float FastRunSeconds = 0.55f;

        [Header("Reel timing")]
        [Tooltip("The order the reels start spinning in")]
        [SerializeField] private ReelOrder startOrder = ReelOrder.Sequential;
        [Tooltip("Seconds between one reel starting and the next")]
        [SerializeField, Range(0f, 0.4f)] private float startGap = 0.05f;
        [Tooltip("The order the reels land in")]
        [SerializeField] private ReelOrder stopOrder = ReelOrder.Sequential;
        [Tooltip("Seconds between one reel landing and the next")]
        [SerializeField, Range(0f, 0.6f)] private float stopGap = 0.24f;
        [Tooltip("Seconds between landings in quick spins (auto play, free spins)")]
        [SerializeField, Range(0f, 0.3f)] private float fastStopGap = 0.1f;

        private MachineDefinition machine;
        private ThemeDefinition theme;
        private ReelView[] reels;
        private SymbolFxLayer fx;
        private float nextGlint;
        private Vector2 cell;
        private Vector2 windowCentre;
        private float gap;
        private RectTransform paylineLayer;
        private readonly List<PaylineView> paylines = new List<PaylineView>();
        private readonly List<SymbolView> celebrating = new List<SymbolView>();

        public bool IsSpinning
        {
            get
            {
                if (reels == null) return false;
                foreach (ReelView reel in reels) if (reel.IsSpinning) return true;
                return false;
            }
        }

        public ReelOrder StartOrder { get => startOrder; set => startOrder = value; }
        public ReelOrder StopOrder { get => stopOrder; set => stopOrder = value; }

        /// <summary>The window the reels occupy, in this view's local space.</summary>
        public Rect Window { get; private set; }

        public void Build(MachineDefinition definition, ThemeDefinition themeDefinition)
        {
            machine = definition;
            theme = themeDefinition;
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);

            // the largest cell that fits the reel area, then the window around the cells
            Rect area = theme.reelArea;
            gap = theme.cellGap;
            float cellHeight = Mathf.Min((area.height - (machine.Rows - 1) * gap) / machine.Rows,
                                         (area.width - (machine.Reels - 1) * gap) / machine.Reels / theme.cellAspect);
            cell = new Vector2(cellHeight * theme.cellAspect, cellHeight);
            var windowSize = new Vector2(machine.Reels * cell.x + (machine.Reels - 1) * gap, machine.Rows * cell.y + (machine.Rows - 1) * gap);
            windowCentre = area.center;
            Window = new Rect(windowCentre - windowSize * 0.5f, windowSize);

            // cell plates behind the reels
            RectTransform cells = UiFactory.Rect("Cells", transform, windowCentre, windowSize);
            for (int r = 0; r < machine.Reels; r++)
                for (int row = 0; row < machine.Rows; row++)
                    UiFactory.Image("Cell", cells, theme.reelCell, CellCentre(r, row) - windowCentre, cell);

            // light behind winning symbols sits on the plates, outside the reels' masks
            RectTransform fxBack = UiFactory.Rect("Symbol Light", transform, Vector2.zero, Vector2.zero);

            // reels
            RectTransform reelRoot = UiFactory.Rect("Reels", transform, windowCentre, windowSize);
            reels = new ReelView[machine.Reels];
            for (int r = 0; r < machine.Reels; r++)
            {
                RectTransform column = UiFactory.Rect("Reel " + (r + 1), reelRoot, new Vector2(CellCentre(r, 0).x - windowCentre.x, 0f), cell);
                reels[r] = column.gameObject.AddComponent<ReelView>();
                int start = UnityEngine.Random.Range(0, machine.Strips[r].Length);
                reels[r].Build(machine.Strips[r], machine.Rows, cell, gap, theme.symbolScale, theme.glow, ArtFor, start, theme.symbolMaterial);
            }

            // stars, dust and sparks in front of the symbols
            RectTransform fxFront = UiFactory.Rect("Symbol Sparkle", transform, Vector2.zero, Vector2.zero);
            fx = fxFront.gameObject.AddComponent<SymbolFxLayer>();
            fx.Setup(fxBack, fxFront, theme);
            nextGlint = Time.time + 2f;

            paylineLayer = UiFactory.Rect("Paylines", transform, Vector2.zero, ((RectTransform)transform).rect.size);

            // the frame wraps the window; its corners keep their size whatever the window's
            Image frame = UiFactory.Image("Frame", transform, theme.reelFrame, windowCentre, windowSize);
            frame.type = Image.Type.Sliced;
            frame.pixelsPerUnitMultiplier = 1f / theme.frameBorderScale;
            if (theme.reelFrame != null)
            {
                Vector4 b = theme.reelFrame.border * theme.frameBorderScale; // left, bottom, right, top
                frame.rectTransform.sizeDelta = windowSize + new Vector2(b.x + b.z, b.y + b.w);
                frame.rectTransform.anchoredPosition = windowCentre + new Vector2((b.z - b.x) * 0.5f, (b.w - b.y) * 0.5f);
            }
        }

        private ThemeDefinition.SymbolArt ArtFor(int symbol)
        {
            return theme.Symbol(machine.Symbols[symbol].Id);
        }

        /// <summary>Centre of a cell in this view's local space.</summary>
        public Vector2 CellCentre(int reel, int row)
        {
            float x = Window.xMin + cell.x * 0.5f + reel * (cell.x + gap);
            float y = Window.yMax - cell.y * 0.5f - row * (cell.y + gap);
            return new Vector2(x, y);
        }

        /// <summary>A reel has landed (its index, 0 = leftmost), in the order set by <see cref="StopOrder"/>.</summary>
        public event Action<int> ReelStopped;
        /// <summary>The last reel of a spin has landed.</summary>
        public event Action AllReelsStopped;

        /// <summary>
        /// Each reel's place in the given order: 0 for the first, 1 for the next... (all 0 when all at once).
        /// </summary>
        private int[] Ranks(ReelOrder order)
        {
            var ranks = new int[reels.Length];
            for (int r = 0; r < ranks.Length; r++) ranks[r] = order == ReelOrder.AllAtOnce ? 0 : r;
            if (order == ReelOrder.Random)
                for (int i = ranks.Length - 1; i > 0; i--)
                {
                    int j = UnityEngine.Random.Range(0, i + 1);
                    (ranks[i], ranks[j]) = (ranks[j], ranks[i]);
                }
            return ranks;
        }

        /// <summary>Spins every reel to the outcome's stops in the start and stop orders, and calls back when the last one lands.</summary>
        public void Spin(SpinOutcome outcome, bool fast, Action onAllStopped)
        {
            ClearWins();
            int stopped = 0;
            int[] starts = Ranks(startOrder);
            int[] stops = Ranks(stopOrder);
            float baseRun = fast ? FastRunSeconds : RunSeconds;
            float gapBetweenStops = fast ? fastStopGap : stopGap;
            float lastStart = (reels.Length - 1) * startGap;
            for (int r = 0; r < reels.Length; r++)
            {
                int reel = r;
                float delay = starts[r] * startGap;
                // landing times follow the stop order whatever the start order: every reel lands at the same moment
                // after the last one started, plus its place in the stop order
                float landAt = lastStart + baseRun + stops[r] * gapBetweenStops;
                float run = Mathf.Max(0.3f, landAt - delay);
                reels[r].Spin(outcome.Stops[r], delay, run, () =>
                {
                    LandSpecials(reel);
                    ReelStopped?.Invoke(reel);
                    stopped++;
                    if (stopped == reels.Length) AllReelsStopped?.Invoke();
                    if (stopped == reels.Length) onAllStopped?.Invoke();
                });
            }
        }

        /// <summary>Lands all reels quickly.</summary>
        public void Hurry()
        {
            foreach (ReelView reel in reels) reel.Hurry();
        }

        public SymbolView SymbolAt(GridPosition position)
        {
            return reels[position.Reel].ViewAt(position.Row);
        }

        /// <summary>Celebrates the given cells and dims the rest of the window.</summary>
        public void HighlightCells(IEnumerable<GridPosition> positions, float seconds)
        {
            StopCelebrating();
            var lit = new HashSet<SymbolView>();
            foreach (GridPosition position in positions)
            {
                SymbolView view = SymbolAt(position);
                if (lit.Add(view))
                {
                    view.Celebrate(seconds);
                    celebrating.Add(view);
                    if (fx != null) fx.Play(CellCentre(position.Reel, position.Row), cell, view.Art, seconds);
                }
            }
            for (int r = 0; r < reels.Length; r++)
                for (int row = 0; row < machine.Rows; row++)
                {
                    SymbolView view = reels[r].ViewAt(row);
                    view.SetDimmed(lit.Count > 0 && !lit.Contains(view));
                }
        }

        /// <summary>Draws payline <paramref name="line"/> across the whole window, with an amount badge.</summary>
        public void ShowLine(int line, long amount, int slot)
        {
            while (paylines.Count <= slot) paylines.Add(PaylineView.Create(paylineLayer, theme));
            int[] rows = machine.Paylines[line];
            var points = new List<Vector2>(rows.Length + 2);
            Vector2 first = CellCentre(0, rows[0]);
            points.Add(first - new Vector2(cell.x * 0.5f, 0f));
            for (int r = 0; r < rows.Length; r++) points.Add(CellCentre(r, rows[r]));
            points.Add(CellCentre(rows.Length - 1, rows[rows.Length - 1]) + new Vector2(cell.x * 0.5f, 0f));
            paylines[slot].Show(points, amount, theme.paleGold);
        }

        public void HideLines()
        {
            foreach (PaylineView line in paylines) line.Hide();
        }

        public void ClearWins()
        {
            HideLines();
            StopCelebrating();
            if (reels == null) return;
            foreach (ReelView reel in reels)
                for (int row = 0; row < machine.Rows; row++) reel.ViewAt(row).SetDimmed(false);
        }

        /// <summary>The grand symbols (wild, scatter, jackpot) thump and ring with light as their reel lands.</summary>
        private void LandSpecials(int reel)
        {
            for (int row = 0; row < machine.Rows; row++)
            {
                SymbolView view = reels[reel].ViewAt(row);
                SymbolStyle style = view.Style;
                if (view.Art == null || (style != SymbolStyle.Wild && style != SymbolStyle.Scatter && style != SymbolStyle.Jackpot)) continue;
                view.Land();
                if (fx != null) fx.Land(CellCentre(reel, row), cell, view.Art);
            }
        }

        /// <summary>While the reels rest, a gem, crown or wild now and then catches the light.</summary>
        private void Update()
        {
            if (reels == null || Time.time < nextGlint) return;
            nextGlint = Time.time + UnityEngine.Random.Range(2.2f, 4.2f);
            if (IsSpinning || celebrating.Count > 0) return;
            var candidates = new List<SymbolView>();
            foreach (ReelView reel in reels)
                for (int row = 0; row < machine.Rows; row++)
                {
                    SymbolView view = reel.ViewAt(row);
                    if (view.Art != null && view.Style != SymbolStyle.Card) candidates.Add(view);
                }
            if (candidates.Count > 0) candidates[UnityEngine.Random.Range(0, candidates.Count)].Glint();
        }

        private void StopCelebrating()
        {
            if (fx != null) fx.StopAll();
            foreach (SymbolView view in celebrating) view.StopCelebrating();
            celebrating.Clear();
        }
    }
}
