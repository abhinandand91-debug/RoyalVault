using System.Collections.Generic;

namespace RoyalVault.Core
{
    /// <summary>How a win compares to the bet; the presentation celebrates each tier differently.</summary>
    public enum WinTier { None, Normal, Big, Mega, Epic }

    /// <summary>A winning payline.</summary>
    public sealed class LineWin
    {
        /// <summary>Index into <see cref="MachineDefinition.Paylines"/>.</summary>
        public readonly int Line;
        public readonly int Symbol;
        public readonly int Count;
        /// <summary>Credits, before any multiplier.</summary>
        public readonly long Amount;
        /// <summary>The winning cells, left to right.</summary>
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

    /// <summary>A scatter win: credits and free spins for scatters anywhere in the window.</summary>
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

    /// <summary>Everything about one spin. The engine decides it before any reel moves; features may adjust it.</summary>
    public sealed class SpinOutcome
    {
        public readonly int[] Stops;
        public readonly SymbolGrid Grid;
        public readonly long LineBet;
        public readonly long TotalBet;

        public readonly List<LineWin> LineWins = new List<LineWin>();
        public ScatterWin Scatter;
        /// <summary>The jackpot tier index won (see <see cref="MachineDefinition.JackpotTiers"/>), or -1.</summary>
        public int JackpotTier = -1;
        public List<GridPosition> JackpotPositions = new List<GridPosition>();
        /// <summary>Credits paid from the jackpot pool, set when the tier is awarded.</summary>
        public long JackpotAmount;
        /// <summary>Multiplies line and scatter wins (free spins double them).</summary>
        public int Multiplier = 1;
        public WinTier Tier;

        public SpinOutcome(int[] stops, SymbolGrid grid, long lineBet, long totalBet)
        {
            Stops = stops;
            Grid = grid;
            LineBet = lineBet;
            TotalBet = totalBet;
        }

        public long LineAmount
        {
            get
            {
                long sum = 0;
                foreach (LineWin win in LineWins) sum += win.Amount;
                return sum;
            }
        }

        public int FreeSpinsAwarded { get { return Scatter != null ? Scatter.FreeSpins : 0; } }

        /// <summary>Credits this spin pays: line and scatter wins times the multiplier, plus any jackpot.</summary>
        public long TotalWin
        {
            get { return (LineAmount + (Scatter != null ? Scatter.Amount : 0)) * Multiplier + JackpotAmount; }
        }

        public bool IsWin { get { return TotalWin > 0 || FreeSpinsAwarded > 0; } }
    }
}
