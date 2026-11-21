using SlotKit;

namespace RoyalVault.Core
{
    // The machines that ship with the game
    public static class MachinePresets
    {
        // symbol order shared by both machines (the theme maps each id to its art)
        private static SymbolDefinition[] Symbols(int[][] pays)
        {
            string[] ids = { "card_10", "card_J", "card_Q", "card_K", "card_A", "emerald", "sapphire", "ruby", "ankh", "gold_bars", "vault_key", "wild" };
            var symbols = new SymbolDefinition[14];
            for (int i = 0; i < ids.Length; i++)
                symbols[i] = new SymbolDefinition(ids[i], i == 11 ? SymbolKind.Wild : SymbolKind.Regular, pays[i]);
            symbols[12] = new SymbolDefinition("vault_seal", SymbolKind.Scatter);
            symbols[13] = new SymbolDefinition("crown", SymbolKind.Jackpot);
            return symbols;
        }

        private static int[] Pay5(int three, int four, int five) { return new[] { 0, 0, 0, three, four, five }; }
        private static int[] Pay3(int three) { return new[] { 0, 0, 0, three }; }

        // 5 x 3, 20 lines, free spins and three progressive jackpots
        public static MachineDefinition RoyalVault5x3()
        {
            return new MachineDefinition
            {
                Name = "Royal Vault",
                Reels = 5,
                Rows = 3,
                Symbols = Symbols(new[]
                {
                    Pay5(20, 50, 200), Pay5(20, 50, 200), Pay5(20, 70, 300), Pay5(35, 90, 350), Pay5(35, 110, 450),
                    Pay5(55, 150, 550), Pay5(55, 180, 750), Pay5(75, 220, 900), Pay5(90, 300, 1100), Pay5(110, 370, 1800),
                    Pay5(150, 550, 3700), Pay5(180, 900, 9000),
                }),
                Strips = new[]
                {
                    new[] { 8, 1, 3, 5, 1, 2, 10, 0, 5, 4, 3, 2, 7, 0, 8, 3, 1, 2, 4, 2, 3, 5, 7, 0, 1, 6, 8, 6, 2, 13, 5, 4, 1, 12, 10, 9, 0, 6, 3, 4, 0, 7, 4, 6, 9, 0, 1 },
                    new[] { 5, 2, 0, 3, 0, 3, 4, 10, 4, 9, 3, 0, 3, 8, 11, 1, 6, 8, 9, 4, 5, 2, 0, 10, 5, 7, 11, 3, 13, 7, 2, 4, 1, 6, 2, 1, 6, 4, 0, 2, 8, 1, 7, 0, 5, 1, 6, 1, 12 },
                    new[] { 11, 4, 10, 0, 4, 1, 3, 13, 1, 12, 3, 2, 6, 3, 8, 1, 7, 10, 2, 0, 7, 2, 6, 1, 4, 1, 6, 7, 4, 3, 8, 0, 5, 6, 0, 5, 0, 9, 8, 2, 0, 3, 5, 2, 4, 11, 5, 1, 9 },
                    new[] { 4, 1, 5, 2, 9, 11, 2, 3, 13, 1, 4, 12, 6, 5, 0, 4, 3, 8, 9, 11, 4, 7, 8, 0, 10, 3, 8, 1, 5, 1, 0, 4, 0, 2, 10, 1, 6, 2, 5, 0, 3, 1, 6, 0, 2, 6, 7, 3, 7 },
                    new[] { 7, 0, 3, 4, 1, 3, 4, 1, 12, 0, 9, 1, 6, 8, 0, 10, 2, 6, 2, 7, 0, 1, 10, 3, 6, 9, 1, 5, 2, 4, 5, 8, 3, 2, 1, 5, 6, 0, 3, 2, 0, 7, 4, 5, 8, 4, 13 },
                },
                Paylines = new[]
                {
                    new[] { 1, 1, 1, 1, 1 }, new[] { 0, 0, 0, 0, 0 }, new[] { 2, 2, 2, 2, 2 }, new[] { 0, 1, 2, 1, 0 }, new[] { 2, 1, 0, 1, 2 },
                    new[] { 0, 0, 1, 2, 2 }, new[] { 2, 2, 1, 0, 0 }, new[] { 1, 0, 0, 0, 1 }, new[] { 1, 2, 2, 2, 1 }, new[] { 1, 0, 1, 0, 1 },
                    new[] { 1, 2, 1, 2, 1 }, new[] { 0, 1, 0, 1, 0 }, new[] { 2, 1, 2, 1, 2 }, new[] { 1, 1, 0, 1, 1 }, new[] { 1, 1, 2, 1, 1 },
                    new[] { 0, 1, 1, 1, 0 }, new[] { 2, 1, 1, 1, 2 }, new[] { 0, 2, 0, 2, 0 }, new[] { 2, 0, 2, 0, 2 }, new[] { 0, 2, 2, 2, 0 },
                },
                ScatterAwards = new[] { new ScatterAward(3, 2, 8), new ScatterAward(4, 10, 12), new ScatterAward(5, 50, 20) },
                JackpotTiers = new[]
                {
                    new JackpotTierDefinition("MINOR", 3, 500, 0.5),
                    new JackpotTierDefinition("MAJOR", 4, 5000, 0.4),
                    new JackpotTierDefinition("GRAND", 5, 250000, 0.6),
                },
                LineBets = new[] { 1, 2, 5, 10, 25, 50, 100 },
                DefaultLineBetIndex = 2,
                StartBalance = 50000,
                FreeSpinMultiplier = 2,
            };
        }

        // A classic 3 x 3 machine with 5 paylines, on the same symbols and theme
        public static MachineDefinition Classic3x3()
        {
            return new MachineDefinition
            {
                Name = "Royal Vault Classic",
                Reels = 3,
                Rows = 3,
                Symbols = Symbols(new[]
                {
                    Pay3(26), Pay3(26), Pay3(31), Pay3(42), Pay3(52), Pay3(72), Pay3(72), Pay3(95), Pay3(95), Pay3(160),
                    Pay3(160), Pay3(260),
                }),
                Strips = new[]
                {
                    new[] { 3, 0, 9, 2, 4, 0, 4, 7, 1, 2, 1, 0, 3, 13, 5, 12, 1, 3, 0, 2, 4, 7, 5, 9, 1, 2 },
                    new[] { 2, 3, 12, 4, 3, 5, 11, 13, 0, 4, 2, 4, 1, 2, 3, 9, 0, 11, 1, 7, 1, 7, 1, 5, 0, 9, 2, 0 },
                    new[] { 1, 9, 7, 0, 1, 4, 0, 3, 1, 2, 3, 2, 4, 9, 0, 1, 2, 5, 0, 13, 7, 4, 5, 2, 12, 3 },
                },
                Paylines = new[] { new[] { 1, 1, 1 }, new[] { 0, 0, 0 }, new[] { 2, 2, 2 }, new[] { 0, 1, 2 }, new[] { 2, 1, 0 } },
                ScatterAwards = new[] { new ScatterAward(3, 3, 6) },
                JackpotTiers = new[] { new JackpotTierDefinition("MINOR", 3, 300, 0.8) },
                LineBets = new[] { 1, 2, 5, 10, 25, 50, 100 },
                DefaultLineBetIndex = 2,
                StartBalance = 20000,
                FreeSpinMultiplier = 2,
            };
        }
    }
}

