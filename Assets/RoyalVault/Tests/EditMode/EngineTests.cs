using System.Collections.Generic;
using NUnit.Framework;
using RoyalVault.Core;

namespace RoyalVault.Tests
{
    /// <summary>The engine and win rules on a tiny hand-made machine, so every expected value is easy to check by eye.</summary>
    public sealed class EngineTests
    {
        // symbols: 0 = A, 1 = K, 2 = wild, 3 = scatter, 4 = crown
        private static MachineDefinition Tiny(int reels = 5)
        {
            var strips = new int[reels][];
            for (int r = 0; r < reels; r++) strips[r] = new[] { 0, 0, 0, 1, 1, 1, 2, 3, 4, 0 };
            var lines = new List<int[]> { Filled(reels, 1), Filled(reels, 0), Filled(reels, 2) };
            return new MachineDefinition
            {
                Name = "Tiny",
                Reels = reels,
                Rows = 3,
                Symbols = new[]
                {
                    new SymbolDefinition("A", SymbolKind.Regular, new[] { 0, 0, 0, 5, 10, 50 }),
                    new SymbolDefinition("K", SymbolKind.Regular, new[] { 0, 0, 0, 2, 4, 20 }),
                    new SymbolDefinition("wild", SymbolKind.Wild, new[] { 0, 0, 0, 30, 60, 300 }),
                    new SymbolDefinition("scatter", SymbolKind.Scatter),
                    new SymbolDefinition("crown", SymbolKind.Jackpot),
                },
                Strips = strips,
                Paylines = lines.ToArray(),
                ScatterAwards = new[] { new ScatterAward(3, 2, 8) },
                JackpotTiers = new[] { new JackpotTierDefinition("MINOR", 3, 100, 1), new JackpotTierDefinition("GRAND", 5, 10000, 1) },
                LineBets = new[] { 1, 2 },
            };
        }

        private static int[] Filled(int length, int value)
        {
            var a = new int[length];
            for (int i = 0; i < length; i++) a[i] = value;
            return a;
        }

        [Test]
        public void Grid_ShowsStripFromEachStop()
        {
            MachineDefinition m = Tiny();
            SymbolGrid grid = SymbolGrid.FromStops(m, new[] { 0, 3, 8, 9, 6 });
            Assert.AreEqual(0, grid[0, 0]);
            Assert.AreEqual(1, grid[1, 2]);
            Assert.AreEqual(4, grid[2, 0]);
            Assert.AreEqual(0, grid[2, 1]); // wraps around the end of the strip
            Assert.AreEqual(2, grid[4, 0]);
        }

        [Test]
        public void FiveOfAKind_PaysTheTopLine()
        {
            var engine = new SpinEngine(Tiny(), new SeededRandom(1));
            SpinOutcome outcome = engine.Evaluate(new[] { 0, 0, 0, 0, 0 }, 1);
            // stop 0 shows A, A, A on every reel: three lines of five aces
            Assert.AreEqual(3, outcome.LineWins.Count);
            Assert.AreEqual(150, outcome.TotalWin);
            Assert.AreEqual(5, outcome.LineWins[0].Count);
        }

        [Test]
        public void Wild_ExtendsARunButNotPastAnotherSymbol()
        {
            MachineDefinition m = Tiny();
            int[] ids = { 0, 2, 0, 1, 0 }; // A, wild, A, K, A
            Assert.IsTrue(PaylineRule.BestRun(m, ids, out int symbol, out int count, out int pay));
            Assert.AreEqual(0, symbol);
            Assert.AreEqual(3, count);
            Assert.AreEqual(5, pay);
        }

        [Test]
        public void LeadingWilds_PayAsWildsWhenThatIsWorthMore()
        {
            MachineDefinition m = Tiny();
            int[] ids = { 2, 2, 2, 1, 0 }; // three wilds then K: three wilds (30) beat four kings (4)
            Assert.IsTrue(PaylineRule.BestRun(m, ids, out int symbol, out int count, out int pay));
            Assert.AreEqual(2, symbol);
            Assert.AreEqual(30, pay);
        }

        [Test]
        public void Wild_DoesNotStandInForTheScatter()
        {
            MachineDefinition m = Tiny();
            Assert.IsFalse(PaylineRule.BestRun(m, new[] { 3, 2, 3, 3, 3 }, out _, out _, out _));
        }

        [Test]
        public void ThreeScatters_PayTheTotalBetAndAwardFreeSpins()
        {
            var engine = new SpinEngine(Tiny(3), new SeededRandom(1));
            // stop 7 puts the scatter in the top row of every reel
            SpinOutcome outcome = engine.Evaluate(new[] { 7, 7, 7 }, 2);
            Assert.IsNotNull(outcome.Scatter);
            Assert.AreEqual(3, outcome.Scatter.Count);
            Assert.AreEqual(8, outcome.FreeSpinsAwarded);
            Assert.AreEqual(2 * 6, outcome.Scatter.Amount); // 2 x total bet of 3 lines x 2
        }

        [Test]
        public void JackpotSymbols_WinTheHighestTierReached()
        {
            var engine = new SpinEngine(Tiny(), new SeededRandom(1));
            Assert.AreEqual(0, engine.Evaluate(new[] { 8, 8, 8, 0, 0 }, 1).JackpotTier);
            Assert.AreEqual(1, engine.Evaluate(new[] { 8, 8, 8, 8, 8 }, 1).JackpotTier);
            Assert.AreEqual(-1, engine.Evaluate(new[] { 8, 8, 0, 0, 0 }, 1).JackpotTier);
        }

        [Test]
        public void ThreeByThree_IsJustAnotherDefinition()
        {
            MachineDefinition m = MachinePresets.Classic3x3();
            Assert.IsEmpty(m.Validate());
            SpinOutcome outcome = new SpinEngine(m, new SeededRandom(3)).Spin(1);
            Assert.AreEqual(3, outcome.Grid.Reels);
            Assert.AreEqual(3, outcome.Grid.Rows);
            Assert.AreEqual(5, outcome.TotalBet);
        }

        [Test]
        public void SameSeed_SameSpins()
        {
            MachineDefinition m = MachinePresets.RoyalVault5x3();
            var a = new SpinEngine(m, new SeededRandom(42));
            var b = new SpinEngine(m, new SeededRandom(42));
            for (int i = 0; i < 50; i++) CollectionAssert.AreEqual(a.Spin(1).Stops, b.Spin(1).Stops);
        }

        [Test]
        public void Validate_ReportsBrokenMachines()
        {
            MachineDefinition m = Tiny();
            m.Paylines = new[] { new[] { 0, 1, 5, 1, 0 } };
            m.Strips = new[] { new[] { 0, 1, 99 } };
            List<string> errors = m.Validate();
            Assert.That(errors, Has.Some.Contains("reel strips"));
            Assert.That(errors, Has.Some.Contains("row 5"));
        }
    }
}
