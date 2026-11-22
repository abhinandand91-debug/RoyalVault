using System.Collections.Generic;

namespace SlotKit
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
    // How often a losing base-game spin is rescued with extra wilds, and how many
    public sealed class RandomWildSettings
    {
        // Losing base-game spins that get wilds, per thousand
        public int ChancePerMille;
        public int MinWilds = 2;
        public int MaxWilds = 4;
    }

    // The gamble after a win: how many guesses in a row, and the most it may grow to
    public sealed class GambleSettings
    {
        public bool Enabled;
        public int MaxRounds = 5;
        // The stake may not grow past this many total bets
        public int MaxWinTotalBets = 50;
    }

    // The Treasury pick bonus: which symbol opens it on which reels, and what its urns hold
    public sealed class TreasurySettings
    {
        public bool Enabled;
        public string KeySymbol = "vault_key";
        // Reels (0 = leftmost) that must each show a key
        public int[] KeyReels = { 0, 2, 4 };
        public int Urns = 12;
        // Urns that end the round
        public int Eyes = 2;
        // Urns that block the next eye
        public int Shields = 1;
        // Urns that add one to the round's multiplier
        public int Multipliers = 1;
        // The rest hold credits: one of these many total bets, picked by the matching weight
        public int[] CreditValues = { 1, 2, 3, 5, 10, 20 };
        public int[] CreditWeights = { 30, 25, 20, 14, 8, 3 };
    }

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
        // Random wilds on a losing base-game spin (0 percent turns the feature off)
        public RandomWildSettings RandomWilds = new RandomWildSettings();
        // Free spin rounds pick one regular symbol that expands over whole reels and pays on every line
        public bool ExpandingFreeSpinSymbol;
        // The symbol ids the relic is picked from; empty means any regular symbol
        public string[] ExpandingSymbolPool = new string[0];
        // Double-or-nothing on a base-game win (turn off where gambles are not allowed)
        public GambleSettings Gamble = new GambleSettings();
        public TreasurySettings Treasury = new TreasurySettings();

        public int SymbolIndex(string id)
        {
            for (int i = 0; i < Symbols.Length; i++)
            {
                if (Symbols[i].Id == id)
                {
                    return i;
                }
            }

            return -1;
        }

        public int FirstIndexOfKind(SymbolKind kind)
        {
            for (int i = 0; i < Symbols.Length; i++)
            {
                if (Symbols[i].Kind == kind)
                {
                    return i;
                }
            }

            return -1;
        }

        // Problems that would stop the machine from working; empty when the definition is sound
        public List<string> Validate()
        {
            var errors = new List<string>();
            if (Reels < 1 || Rows < 1)
            {
                errors.Add("The grid needs at least one reel and one row.");
            }

            if (Strips.Length != Reels)
            {
                errors.Add($"There are {Strips.Length} reel strips for {Reels} reels.");
            }

            for (int r = 0; r < Strips.Length; r++)
            {
                if (Strips[r] == null || Strips[r].Length < Rows)
                {
                    errors.Add($"Reel {r + 1} has fewer symbols than rows.");
                    continue;
                }
                foreach (int id in Strips[r])
                {
                    if (id < 0 || id >= Symbols.Length)
                    {
                        errors.Add($"Reel {r + 1} uses unknown symbol index {id}.");
                        break;
                    }
                }
            }
            for (int l = 0; l < Paylines.Length; l++)
            {
                if (Paylines[l] == null || Paylines[l].Length != Reels)
                {
                    errors.Add($"Payline {l + 1} does not cross every reel.");
                    continue;
                }
                foreach (int row in Paylines[l])
                {
                    if (row < 0 || row >= Rows)
                    {
                        errors.Add($"Payline {l + 1} uses row {row}, outside the grid.");
                        break;
                    }
                }
            }
            if (LineBets.Length == 0)
            {
                errors.Add("There are no line bets.");
            }
            else if (DefaultLineBetIndex < 0 || DefaultLineBetIndex >= LineBets.Length)
            {
                errors.Add("The default line bet is out of range.");
            }

            ValidateTreasury(errors);
            return errors;
        }

        private void ValidateTreasury(List<string> errors)
        {
            if (!Treasury.Enabled)
            {
                return;
            }

            if (SymbolIndex(Treasury.KeySymbol) < 0)
            {
                errors.Add($"The Treasury key symbol {Treasury.KeySymbol} is not on this machine.");
            }

            foreach (int reel in Treasury.KeyReels)
            {
                if (reel < 0 || reel >= Reels)
                {
                    errors.Add($"The Treasury key reel {reel + 1} is outside the grid.");
                }
            }

            int special = Treasury.Eyes + Treasury.Shields + Treasury.Multipliers;
            if (Treasury.Eyes < 1 || special >= Treasury.Urns)
            {
                errors.Add("The Treasury needs at least one eye and at least one urn of credits.");
            }

            if (Treasury.CreditValues.Length == 0 || Treasury.CreditValues.Length != Treasury.CreditWeights.Length)
            {
                errors.Add("The Treasury credit values and weights do not match.");
            }
        }
    }
}
