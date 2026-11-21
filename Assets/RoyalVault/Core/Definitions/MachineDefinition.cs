using System.Collections.Generic;

namespace RoyalVault.Core
{
    // Scatter award: Count or more scatters anywhere pay the total bet times a multiplier and free spins
    public sealed class ScatterAward
    {
        public readonly int Count;
        public readonly int TotalBetMultiplier;
        public readonly int FreeSpins;

        public ScatterAward(int count, int totalBetMultiplier, int freeSpins)
        {
            Count = count;
            TotalBetMultiplier = totalBetMultiplier;
            FreeSpins = freeSpins;
        }
    }

    // A progressive jackpot tier (Minor, Major, Grand), won by SymbolCount jackpot symbols anywhere
    public sealed class JackpotTierDefinition
    {
        public readonly string Name;
        public readonly int SymbolCount;
        // The value the tier starts at and resets to after it is won
        public readonly long Seed;
        // Share of every paid bet added to this tier, in percent
        public readonly double ContributionPercent;

        public JackpotTierDefinition(string name, int symbolCount, long seed, double contributionPercent)
        {
            Name = name;
            SymbolCount = symbolCount;
            Seed = seed;
            ContributionPercent = contributionPercent;
        }
    }

    // Grid, symbols, strips, paylines, pays, features and bets of one machine
    public sealed class MachineDefinition
    {
        public string Name = "Machine";
        public int Reels = 5;
        public int Rows = 3;
        public SymbolDefinition[] Symbols = new SymbolDefinition[0];
        // One strip of symbol indices per reel; a reel shows Rows consecutive entries
        public int[][] Strips = new int[0][];
        // For each payline, the row (0 = top) it crosses on each reel
        public int[][] Paylines = new int[0][];
        public ScatterAward[] ScatterAwards = new ScatterAward[0];
        // Jackpot tiers, smallest first
        public JackpotTierDefinition[] JackpotTiers = new JackpotTierDefinition[0];
        // The line bets the player can choose from
        public int[] LineBets = { 1 };
        public int DefaultLineBetIndex;
        public long StartBalance = 50000;
        // Wins during free spins are multiplied by this
        public int FreeSpinMultiplier = 2;
        // Win tiers as multiples of the total bet: Big, Mega, Epic
        public int[] WinTierMultipliers = { 10, 25, 50 };

        public int SymbolIndex(string id)
        {
            for (int i = 0; i < Symbols.Length; i++)
                if (Symbols[i].Id == id) return i;
            return -1;
        }

        public int FirstIndexOfKind(SymbolKind kind)
        {
            for (int i = 0; i < Symbols.Length; i++)
                if (Symbols[i].Kind == kind) return i;
            return -1;
        }

        // Problems that would stop the machine from working; empty when the definition is sound
        public List<string> Validate()
        {
            var errors = new List<string>();
            if (Reels < 1 || Rows < 1) errors.Add("The grid needs at least one reel and one row.");
            if (Strips.Length != Reels) errors.Add($"There are {Strips.Length} reel strips for {Reels} reels.");
            for (int r = 0; r < Strips.Length; r++)
            {
                if (Strips[r] == null || Strips[r].Length < Rows) { errors.Add($"Reel {r + 1} has fewer symbols than rows."); continue; }
                foreach (int id in Strips[r])
                    if (id < 0 || id >= Symbols.Length) { errors.Add($"Reel {r + 1} uses unknown symbol index {id}."); break; }
            }
            for (int l = 0; l < Paylines.Length; l++)
            {
                if (Paylines[l] == null || Paylines[l].Length != Reels) { errors.Add($"Payline {l + 1} does not cross every reel."); continue; }
                foreach (int row in Paylines[l])
                    if (row < 0 || row >= Rows) { errors.Add($"Payline {l + 1} uses row {row}, outside the grid."); break; }
            }
            if (LineBets.Length == 0) errors.Add("There are no line bets.");
            else if (DefaultLineBetIndex < 0 || DefaultLineBetIndex >= LineBets.Length) errors.Add("The default line bet is out of range.");
            return errors;
        }
    }
}
