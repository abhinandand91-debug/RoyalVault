using System.Collections.Generic;

namespace RoyalVault.Core
{
    /// <summary>A cell of the grid: reel (column, 0 = left) and row (0 = top).</summary>
    public struct GridPosition
    {
        public readonly int Reel;
        public readonly int Row;

        public GridPosition(int reel, int row)
        {
            Reel = reel;
            Row = row;
        }

        public override string ToString() { return $"({Reel},{Row})"; }
    }

    /// <summary>The symbols showing in the window after a spin, as symbol indices.</summary>
    public sealed class SymbolGrid
    {
        private readonly int[,] cells;

        public int Reels { get; }
        public int Rows { get; }

        public SymbolGrid(int reels, int rows)
        {
            Reels = reels;
            Rows = rows;
            cells = new int[reels, rows];
        }

        public int this[int reel, int row]
        {
            get { return cells[reel, row]; }
            set { cells[reel, row] = value; }
        }

        /// <summary>Every cell holding <paramref name="symbol"/>.</summary>
        public List<GridPosition> PositionsOf(int symbol)
        {
            var found = new List<GridPosition>();
            for (int reel = 0; reel < Reels; reel++)
                for (int row = 0; row < Rows; row++)
                    if (cells[reel, row] == symbol) found.Add(new GridPosition(reel, row));
            return found;
        }

        /// <summary>
        /// The grid for these reel stops: reel <c>r</c> shows its strip from <c>stops[r]</c> downwards, wrapping
        /// around the end of the strip.
        /// </summary>
        public static SymbolGrid FromStops(MachineDefinition machine, int[] stops)
        {
            var grid = new SymbolGrid(machine.Reels, machine.Rows);
            for (int reel = 0; reel < machine.Reels; reel++)
            {
                int[] strip = machine.Strips[reel];
                for (int row = 0; row < machine.Rows; row++)
                    grid[reel, row] = strip[(stops[reel] + row) % strip.Length];
            }
            return grid;
        }
    }
}
