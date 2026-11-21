using System.Collections.Generic;

namespace SlotKit
{
    // A cell of the grid: reel (column, 0 = left) and row (0 = top)
    public struct GridPosition
    {
        public readonly int Reel;
        public readonly int Row;

        public GridPosition(int reel, int row)
        {
            Reel = reel;
            Row = row;
        }

        public override string ToString()
        {
            return $"({Reel},{Row})";
        }
    }

    // The symbols showing in the window after a spin, as symbol indices
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
            get
            {
                return cells[reel, row];
            }
            set
            {
                cells[reel, row] = value;
            }
        }

        // Every cell holding symbol
        public List<GridPosition> PositionsOf(int symbol)
        {
            var found = new List<GridPosition>();
            for (int reel = 0; reel < Reels; reel++)
            {
                for (int row = 0; row < Rows; row++)
                {
                    if (cells[reel, row] == symbol)
                    {
                        found.Add(new GridPosition(reel, row));
                    }
                }
            }

            return found;
        }

        // The visible grid for these stops, wrapping around each strip
        public static SymbolGrid FromStops(MachineDefinition machine, int[] stops)
        {
            var grid = new SymbolGrid(machine.Reels, machine.Rows);
            for (int reel = 0; reel < machine.Reels; reel++)
            {
                int[] strip = machine.Strips[reel];
                for (int row = 0; row < machine.Rows; row++)
                {
                    grid[reel, row] = strip[(stops[reel] + row) % strip.Length];
                }
            }
            return grid;
        }
    }
}
