using System.Collections.Generic;

namespace SlotKit
{
    // How a win compares to the bet; the presentation celebrates each tier differently
    public enum WinTier
    {
        None, Normal, Big, Mega, Epic
    }

    // A winning payline
    public sealed class LineWin
    {
        // Index into Paylines
        public readonly int Line;
        public readonly int Symbol;
        public readonly int Count;
        // Credits, before any multiplier
        public readonly long Amount;
        // The winning cells, left to right
        public readonly List<GridPosition> Positions;

        public LineWin(int line, int symbol, int count, long amount, List<GridPosition> positions)
        {
            Line = line;
            Symbol = symbol;
            Count = count;
            Amount = amount;
            Positions = positions;
        }
    }

    // A symbol that covered whole reels and paid on every line, wherever its reels are
    public sealed class ExpandingWin
    {
        public readonly int Symbol;
        // The reels it covered, left to right
        public readonly List<int> Reels;
        // Credits, before any multiplier
        public readonly long Amount;

        public ExpandingWin(int symbol, List<int> reels, long amount)
        {
            Symbol = symbol;
            Reels = reels;
            Amount = amount;
        }
    }

    // A scatter win: credits and free spins for scatters anywhere in the window
    public sealed class ScatterWin
    {
        public readonly int Count;
        public readonly long Amount;
        public readonly int FreeSpins;
        public readonly List<GridPosition> Positions;

        public ScatterWin(int count, long amount, int freeSpins, List<GridPosition> positions)
        {
            Count = count;
            Amount = amount;
            FreeSpins = freeSpins;
            Positions = positions;
        }
    }

    // Everything about one spin
    public sealed class SpinOutcome
    {
        public readonly int[] Stops;
        public readonly SymbolGrid Grid;
        public readonly long LineBet;
        public readonly long TotalBet;

        public readonly List<LineWin> LineWins = new List<LineWin>();
        public ScatterWin Scatter;
        // The jackpot tier index won (see JackpotTiers), or -1
        public int JackpotTier = -1;
        public List<GridPosition> JackpotPositions = new List<GridPosition>();
        // Credits paid from the jackpot pool, set when the tier is awarded
        public long JackpotAmount;
        // Multiplies line and scatter wins (free spins double them)
        public int Multiplier = 1;
        public WinTier Tier;
        // The free spin relic's expanded win, or null
        public ExpandingWin Expanded;
        // Cells a feature turned into wilds after the reels stopped (shown before the wins)
        public readonly List<GridPosition> AddedWilds = new List<GridPosition>();
        // The Treasury round this spin opened, or null; it is paid apart from TotalWin, once its urns are shown
        public TreasuryRound Treasury;

        public SpinOutcome(int[] stops, SymbolGrid grid, long lineBet, long totalBet)
        {
            Stops = stops;
            Grid = grid;
            LineBet = lineBet;
            TotalBet = totalBet;
        }

        // Forgets every win so the rules can evaluate a changed grid again
        public void ClearWins()
        {
            LineWins.Clear();
            Scatter = null;
            Expanded = null;
            JackpotTier = -1;
            JackpotPositions.Clear();
        }

        public long LineAmount
        {
            get
            {
                long sum = 0;
                foreach (LineWin win in LineWins)
                {
                    sum += win.Amount;
                }

                return sum;
            }
        }

        public int FreeSpinsAwarded
        {
            get
            {
                return Scatter != null ? Scatter.FreeSpins : 0;
            }
        }

        // Credits this spin pays: line and scatter wins times the multiplier, plus any jackpot
        public long TotalWin
        {
            get
            {
                long expanded = Expanded != null ? Expanded.Amount : 0;
                return (LineAmount + (Scatter != null ? Scatter.Amount : 0) + expanded) * Multiplier + JackpotAmount;
            }
        }

        public bool IsWin
        {
            get
            {
                return TotalWin > 0 || FreeSpinsAwarded > 0;
            }
        }
    }
}
