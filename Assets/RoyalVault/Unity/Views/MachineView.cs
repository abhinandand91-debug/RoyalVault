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
    public sealed class MachineView : MonoBehaviour
    {
        private const float RunSeconds = 1.25f;
        private const float StopStagger = 0.24f;

        private MachineDefinition machine;
        private ThemeDefinition theme;
        private ReelView[] reels;
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

            // reels
            RectTransform reelRoot = UiFactory.Rect("Reels", transform, windowCentre, windowSize);
            reels = new ReelView[machine.Reels];
            for (int r = 0; r < machine.Reels; r++)
            {
                RectTransform column = UiFactory.Rect("Reel " + (r + 1), reelRoot, new Vector2(CellCentre(r, 0).x - windowCentre.x, 0f), cell);
                reels[r] = column.gameObject.AddComponent<ReelView>();
                int start = UnityEngine.Random.Range(0, machine.Strips[r].Length);
                reels[r].Build(machine.Strips[r], machine.Rows, cell, gap, theme.symbolScale, theme.glow, ArtFor, start);
            }

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

        /// <summary>Spins every reel to the outcome's stops, left to right, and calls back when the last one lands.</summary>
        public void Spin(SpinOutcome outcome, bool fast, Action onAllStopped)
        {
            ClearWins();
            int stopped = 0;
            for (int r = 0; r < reels.Length; r++)
            {
                float run = (fast ? 0.55f : RunSeconds) + r * (fast ? 0.1f : StopStagger);
                reels[r].Spin(outcome.Stops[r], r * 0.05f, run, () =>
                {
                    stopped++;
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
                if (lit.Add(view)) { view.Celebrate(seconds); celebrating.Add(view); }
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

        private void StopCelebrating()
        {
            foreach (SymbolView view in celebrating) view.StopCelebrating();
            celebrating.Clear();
        }
    }
}
