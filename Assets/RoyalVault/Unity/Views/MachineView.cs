using System.Collections.Generic;
using System;
using GameKit;
using SlotKit;
using UnityEngine.UI;
using UnityEngine;

namespace RoyalVault.Unity
{
    // The reels inside their frame
    public enum ReelOrder
    {
        // One after another, left to right
        Sequential,
        // One after another, in a new random order every spin
        Random,
        // Every reel together
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

        [Header("Anticipation")]
        [Tooltip("When the landed reels are one seal short of free spins (or one crown short of a jackpot), the " +
                 "remaining reels spin longer, one by one, in a column of light. It only reacts to what has landed.")]
        [SerializeField] private bool anticipation = true;
        [Tooltip("Extra seconds each remaining reel spins while anticipating")]
        [SerializeField, Range(0.3f, 4f)] private float anticipationSeconds = 1.6f;
        [Tooltip("Extra seconds per reel in quick spins (auto play, free spins)")]
        [SerializeField, Range(0f, 2f)] private float fastAnticipationSeconds = 0.8f;

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

        public bool Anticipation { get => anticipation; set => anticipation = value; }
        public ReelOrder StartOrder { get => startOrder; set => startOrder = value; }
        public ReelOrder StopOrder { get => stopOrder; set => stopOrder = value; }

        // The window the reels occupy, in this view's local space
        public Rect Window { get; private set; }

        public void Build(MachineDefinition definition, ThemeDefinition themeDefinition)
        {
            machine = definition;
            theme = themeDefinition;
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);

            Vector2 windowSize = FitWindow();
            BuildCells(windowSize);
            RectTransform fxBack = UiFactory.Rect("Symbol Light", transform, Vector2.zero, Vector2.zero);
            BuildReels(windowSize);
            fxFrontLayer = UiFactory.Rect("Symbol Sparkle", transform, Vector2.zero, Vector2.zero);
            fx = fxFrontLayer.gameObject.AddComponent<SymbolFxLayer>();
            fx.Setup(fxBack, fxFrontLayer, theme);
            BuildAnticipation(windowSize);
            nextGlint = Time.time + 2f;
            paylineLayer = UiFactory.Rect("Paylines", transform, Vector2.zero, ((RectTransform)transform).rect.size);
            BuildFrame(windowSize);
        }

        // The largest cell that fits the reel area, and the window around the cells
        private Vector2 FitWindow()
        {
            Rect area = theme.reelArea;
            gap = theme.cellGap;
            float cellHeight = Mathf.Min((area.height - (machine.Rows - 1) * gap) / machine.Rows,
                                         (area.width - (machine.Reels - 1) * gap) / machine.Reels / theme.cellAspect);
            cell = new Vector2(cellHeight * theme.cellAspect, cellHeight);
            var size = new Vector2(machine.Reels * cell.x + (machine.Reels - 1) * gap, machine.Rows * cell.y + (machine.Rows - 1) * gap);
            windowCentre = area.center;
            Window = new Rect(windowCentre - size * 0.5f, size);
            return size;
        }

        private void BuildCells(Vector2 windowSize)
        {
            RectTransform cells = UiFactory.Rect("Cells", transform, windowCentre, windowSize);
            for (int r = 0; r < machine.Reels; r++)
                for (int row = 0; row < machine.Rows; row++)
                    UiFactory.Image("Cell", cells, theme.reelCell, CellCentre(r, row) - windowCentre, cell);
        }

        private void BuildReels(Vector2 windowSize)
        {
            RectTransform reelRoot = UiFactory.Rect("Reels", transform, windowCentre, windowSize);
            reels = new ReelView[machine.Reels];
            for (int r = 0; r < machine.Reels; r++)
            {
                RectTransform column = UiFactory.Rect("Reel " + (r + 1), reelRoot, new Vector2(CellCentre(r, 0).x - windowCentre.x, 0f), cell);
                reels[r] = column.gameObject.AddComponent<ReelView>();
                int start = UnityEngine.Random.Range(0, machine.Strips[r].Length);
                reels[r].Build(machine.Strips[r], machine.Rows, cell, gap, theme.symbolScale, theme.glow, ArtFor, start, theme.symbolMaterial);
            }
        }

        // The frame wraps the window; its corners keep their size
        private void BuildFrame(Vector2 windowSize)
        {
            Image frame = UiFactory.Image("Frame", transform, theme.reelFrame, windowCentre, windowSize);
            frame.type = Image.Type.Sliced;
            frame.pixelsPerUnitMultiplier = 1f / theme.frameBorderScale;
            if (theme.reelFrame == null) return;
            Vector4 b = theme.reelFrame.border * theme.frameBorderScale; // left, bottom, right, top
            frame.rectTransform.sizeDelta = windowSize + new Vector2(b.x + b.z, b.y + b.w);
            frame.rectTransform.anchoredPosition = windowCentre + new Vector2((b.z - b.x) * 0.5f, (b.w - b.y) * 0.5f);
        }

        private ThemeDefinition.SymbolArt ArtFor(int symbol)
        {
            return theme.Symbol(machine.Symbols[symbol].Id);
        }

        // Centre of a cell in this view's local space
        public Vector2 CellCentre(int reel, int row)
        {
            float x = Window.xMin + cell.x * 0.5f + reel * (cell.x + gap);
            float y = Window.yMax - cell.y * 0.5f - row * (cell.y + gap);
            return new Vector2(x, y);
        }

        // A reel has landed (its index, 0 = leftmost), in the order set by StopOrder
        public event Action<int> ReelStopped;
        // The last reel of a spin has landed
        public event Action AllReelsStopped;
        // Anticipation began (or moved on) and this reel is the one being waited for
        public event Action<int> AnticipationStarted;
        // The last anticipating reel has landed
        public event Action AnticipationEnded;

        // The reels the last spin anticipated, in the order they landed
        public IReadOnlyList<int> AnticipatedReels => anticipated;

        private readonly List<int> anticipated = new List<int>();
        private RectTransform fxFrontLayer;
        private CanvasGroup[] columns = new CanvasGroup[0];
        private UiParticleEmitter[] columnSparks = new UiParticleEmitter[0];
        private int awaited = -1;
        private float awaitedSince;

        // Each reel's place in the given order: 0 for the first, 1 for the next..
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

        // Spins every reel to the outcome's stops and calls back when the last one lands
        public void Spin(SpinOutcome outcome, bool fast, Action onAllStopped)
        {
            ClearWins();
            int stopped = 0;
            int[] starts = Ranks(startOrder);
            int[] stops = Ranks(stopOrder);
            float baseRun = fast ? FastRunSeconds : RunSeconds;
            float gapBetweenStops = fast ? fastStopGap : stopGap;
            float lastStart = (reels.Length - 1) * startGap;
            float[] extra = PlanAnticipation(outcome, stops, fast);
            for (int r = 0; r < reels.Length; r++)
            {
                int reel = r;
                float delay = starts[r] * startGap;
                // landings follow the stop order, whatever the start order
                float landAt = lastStart + baseRun + stops[r] * gapBetweenStops + extra[r];
                float run = Mathf.Max(0.3f, landAt - delay);
                reels[r].Spin(outcome.Stops[r], delay, run, () =>
                {
                    LandSpecials(reel);
                    ReelStopped?.Invoke(reel);
                    stopped++;
                    AnticipateNext(reel);
                    if (stopped == reels.Length) AllReelsStopped?.Invoke();
                    if (stopped == reels.Length) onAllStopped?.Invoke();
                });
            }
        }

        // Lands all reels quickly
        public void Hurry()
        {
            foreach (ReelView reel in reels) reel.Hurry();
        }

        public SymbolView SymbolAt(GridPosition position)
        {
            return reels[position.Reel].ViewAt(position.Row);
        }

        // Celebrates the given cells and dims the rest of the window
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

        // Draws payline line across the whole window, with an amount badge
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

        private int[] landingOrder = new int[0];
        private bool[] anticipating = new bool[0];
        private bool[] landedThisSpin = new bool[0];

        // Slows the remaining reels once one more seal or crown would trigger a feature
        private float[] PlanAnticipation(SpinOutcome outcome, int[] stopRanks, bool fast)
        {
            int n = reels.Length;
            var extra = new float[n];
            anticipating = new bool[n];
            landedThisSpin = new bool[n];
            anticipated.Clear();
            awaited = -1;
            landingOrder = new int[n];
            for (int r = 0; r < n; r++) landingOrder[r] = r;
            System.Array.Sort(landingOrder, (a, b) => stopRanks[a] != stopRanks[b] ? stopRanks[a].CompareTo(stopRanks[b]) : a.CompareTo(b));
            if (!anticipation || stopOrder == ReelOrder.AllAtOnce) return extra;

            int scatter = machine.FirstIndexOfKind(SymbolKind.Scatter);
            int crown = machine.FirstIndexOfKind(SymbolKind.Jackpot);
            int scatterNeed = int.MaxValue;
            foreach (ScatterAward award in machine.ScatterAwards)
                if (award.FreeSpins > 0) scatterNeed = Mathf.Min(scatterNeed, award.Count);
            int crownNeed = machine.JackpotTiers.Length > 0 ? machine.JackpotTiers[0].SymbolCount : int.MaxValue;

            int seals = 0, crowns = 0;
            bool on = false;
            float added = 0f;
            float step = fast ? fastAnticipationSeconds : anticipationSeconds;
            foreach (int r in landingOrder)
            {
                if (on) { added += step; anticipating[r] = true; }
                extra[r] = added;
                for (int row = 0; row < machine.Rows; row++)
                {
                    int symbol = outcome.Grid[r, row];
                    if (symbol == scatter) seals++;
                    if (symbol == crown) crowns++;
                }
                bool sealsClose = scatter >= 0 && scatterNeed != int.MaxValue && scatterNeed > 1 && seals >= scatterNeed - 1;
                bool crownsClose = crown >= 0 && crownNeed != int.MaxValue && crownNeed > 1 && crowns >= crownNeed - 1;
                if (sealsClose || crownsClose) on = true;
            }
            return extra;
        }

        // After a landing: the light leaves that reel and moves to the next reel still being waited for
        private void AnticipateNext(int landedReel)
        {
            if (landedThisSpin.Length != reels.Length) return;
            landedThisSpin[landedReel] = true;
            if (anticipating[landedReel]) anticipated.Add(landedReel);
            int next = -1;
            foreach (int r in landingOrder)
                if (!landedThisSpin[r]) { next = r; break; }
            bool wasOn = awaited >= 0;
            if (next >= 0 && anticipating[next])
            {
                awaited = next;
                awaitedSince = Time.time;
                for (int r = 0; r < columnSparks.Length; r++) columnSparks[r].Intensity = r == next ? 1f : 0f;
                AnticipationStarted?.Invoke(next);
            }
            else
            {
                awaited = -1;
                foreach (UiParticleEmitter sparks in columnSparks) sparks.Intensity = 0f;
                if (wasOn) AnticipationEnded?.Invoke();
            }
        }

        // Light frame, glow and sparks over a reel while it is anticipated
        private void BuildAnticipation(Vector2 windowSize)
        {
            columns = new CanvasGroup[reels.Length];
            columnSparks = new UiParticleEmitter[reels.Length];
            for (int r = 0; r < reels.Length; r++)
            {
                var centre = new Vector2(CellCentre(r, 0).x, windowCentre.y);
                var size = new Vector2(cell.x + gap, windowSize.y + gap);
                columns[r] = AnticipationColumn(r, centre, size);
                columnSparks[r] = ColumnSparks(r, centre - new Vector2(0f, windowSize.y * 0.5f));
            }
        }

        private CanvasGroup AnticipationColumn(int reel, Vector2 centre, Vector2 size)
        {
            RectTransform holder = UiFactory.Rect("Anticipation " + (reel + 1), fxFrontLayer, centre, size);
            var group = holder.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            Image wash = UiFactory.Image("Glow", holder, theme.glow, Vector2.zero, new Vector2(size.x * 1.5f, size.y * 1.15f));
            wash.material = theme.additive;
            wash.color = new Color(1f, 0.82f, 0.45f, 0.45f);
            LightFrame(holder, size);
            return group;
        }

        // Four bars of light with soft halos
        private void LightFrame(RectTransform holder, Vector2 size)
        {
            const float bar = 6f;
            Vector2[] at = { new Vector2(-size.x * 0.5f, 0f), new Vector2(size.x * 0.5f, 0f), new Vector2(0f, size.y * 0.5f), new Vector2(0f, -size.y * 0.5f) };
            Vector2[] dims = { new Vector2(bar, size.y + bar), new Vector2(bar, size.y + bar), new Vector2(size.x + bar, bar), new Vector2(size.x + bar, bar) };
            for (int k = 0; k < 4; k++)
            {
                Image halo = UiFactory.Image("Halo", holder, theme.glow, at[k], dims[k] + Vector2.one * 46f);
                halo.material = theme.additive;
                halo.color = new Color(1f, 0.82f, 0.45f, 0.55f);
                halo.type = Image.Type.Simple;
                Image line = UiFactory.Image("Edge", holder, theme.white, at[k], dims[k]);
                line.material = theme.additive;
                line.color = new Color(1f, 0.93f, 0.7f, 0.95f);
            }
        }

        private UiParticleEmitter ColumnSparks(int reel, Vector2 bottom)
        {
            RectTransform rect = UiFactory.Rect("Anticipation Sparks " + (reel + 1), fxFrontLayer, bottom, Vector2.one * 10f);
            var sparks = rect.gameObject.AddComponent<UiParticleEmitter>();
            sparks.Sprite = theme.sparkle;
            sparks.Material = theme.additive;
            sparks.Rate = 26f;
            sparks.LifeRange = new Vector2(0.8f, 1.5f);
            sparks.SizeRange = new Vector2(10f, 22f);
            sparks.EndSize = 0.2f;
            sparks.Area = new Vector2(cell.x * 0.9f, 10f);
            sparks.VelocityMin = new Vector2(-15f, 220f);
            sparks.VelocityMax = new Vector2(15f, 420f);
            sparks.Swirl = 20f;
            sparks.SwirlFrequency = 3f;
            sparks.MaxParticles = 60;
            sparks.StartColor = new Color(1f, 0.9f, 0.6f, 1f);
            sparks.EndColor = new Color(1f, 0.6f, 0.2f, 0f);
            sparks.Intensity = 0f;
            return sparks;
        }

        // The grand symbols (wild, scatter, jackpot) thump and ring with light as their reel lands
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

        // While the reels rest, a gem, crown or wild now and then catches the light
        private void Update()
        {
            // the awaited reel's frame of light swells in and throbs; the others fade out
            for (int r = 0; r < columns.Length; r++)
            {
                float target = 0f;
                if (r == awaited)
                {
                    float t = Time.time - awaitedSince;
                    target = Mathf.Min(1f, t * 5f) * (0.75f + 0.25f * Mathf.Sin(t * 9f));
                }
                columns[r].alpha = Mathf.MoveTowards(columns[r].alpha, target, Time.deltaTime * 4f);
            }

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
