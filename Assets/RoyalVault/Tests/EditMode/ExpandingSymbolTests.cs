using NUnit.Framework;
using RoyalVault.Core;
using SlotKit;

namespace RoyalVault.Tests
{
    // The Sealed Relic: picked when free spins start, kept for the round, expanding over whole reels to pay on every line
    public sealed class ExpandingSymbolTests
    {
        private static MachineDefinition Machine()
        {
            MachineDefinition machine = MachinePresets.RoyalVault5x3();
            machine.RandomWilds.ChancePerMille = 0;
            return machine;
        }

        // A grid of one filler symbol with the relic on the given reels (top row)
        private static SpinOutcome Grid(MachineDefinition machine, int filler, int relic, params int[] relicReels)
        {
            var grid = new SymbolGrid(machine.Reels, machine.Rows);
            for (int reel = 0; reel < machine.Reels; reel++)
            {
                for (int row = 0; row < machine.Rows; row++)
                {
                    grid[reel, row] = (reel + row) % 2 == 0 ? filler : filler + 1;
                }
            }

            foreach (int reel in relicReels)
            {
                grid[reel, 0] = relic;
            }

            return new SpinOutcome(new int[machine.Reels], grid, 5, 5 * machine.Paylines.Length);
        }

        private static SpinContext FreeSpin(GameSession session, SpinOutcome outcome)
        {
            return new SpinContext(session, 0) { IsFreeSpin = true, Outcome = outcome };
        }

        [Test]
        public void RelicCoversItsReelsAndPaysOnEveryLineWhereverTheReelsAre()
        {
            MachineDefinition machine = Machine();
            var session = new GameSession(machine, new MemorySaveStore());
            int relic = machine.SymbolIndex("ruby");
            session.FreeSpins.Add(5);
            session.FreeSpins.SetRelic(relic);

            SpinOutcome outcome = Grid(machine, machine.SymbolIndex("card_10"), relic, 0, 2, 4);
            new ExpandingSymbolFeature(new SeededRandom(1)).OnOutcome(FreeSpin(session, outcome));

            Assert.IsNotNull(outcome.Expanded);
            CollectionAssert.AreEqual(new[] { 0, 2, 4 }, outcome.Expanded.Reels);
            long expected = (long)machine.Symbols[relic].PayFor(3) * 5 * machine.Paylines.Length;
            Assert.AreEqual(expected, outcome.Expanded.Amount);
        }

        [Test]
        public void TooFewReelsTheBaseGameOrNoRelicPayNothingExtra()
        {
            MachineDefinition machine = Machine();
            var session = new GameSession(machine, new MemorySaveStore());
            int relic = machine.SymbolIndex("ruby");
            int filler = machine.SymbolIndex("card_10");
            var feature = new ExpandingSymbolFeature(new SeededRandom(1));

            session.FreeSpins.Add(5);
            SpinOutcome noRelic = Grid(machine, filler, relic, 0, 1, 2);
            feature.OnOutcome(FreeSpin(session, noRelic));
            Assert.IsNull(noRelic.Expanded, "no relic chosen");

            session.FreeSpins.SetRelic(relic);
            SpinOutcome twoReels = Grid(machine, filler, relic, 0, 3);
            feature.OnOutcome(FreeSpin(session, twoReels));
            Assert.IsNull(twoReels.Expanded, "two reels do not pay");

            SpinOutcome baseGame = Grid(machine, filler, relic, 0, 1, 2);
            feature.OnOutcome(new SpinContext(session, 100) { Outcome = baseGame });
            Assert.IsNull(baseGame.Expanded, "base game");
        }

        [Test]
        public void ARoundPicksItsRelicFromThePoolKeepsItAndForgetsItAtTheEnd()
        {
            MachineDefinition machine = Machine();
            machine.ExpandingSymbolPool = new[] { "ankh" };
            var store = new MemorySaveStore();
            var random = new SeededRandom(4);
            var session = new GameSession(machine, store);
            var flow = new SpinFlow(session, new SpinEngine(machine, random), StandardFeatures.Create(random));

            var trigger = new SpinContext(session, 100) { Outcome = Grid(machine, 0, 1) };
            trigger.Outcome.Scatter = new ScatterWin(3, 0, 8, new System.Collections.Generic.List<GridPosition>());
            flow.Features.Paid(trigger);
            Assert.AreEqual(machine.SymbolIndex("ankh"), session.FreeSpins.Relic, "picked from the pool");

            session.Save();
            var restored = new GameSession(machine, store);
            Assert.AreEqual(session.FreeSpins.Relic, restored.FreeSpins.Relic, "kept across sessions");

            session.FreeSpins.EndRound();
            Assert.AreEqual(-1, session.FreeSpins.Relic, "forgotten when the round ends");
        }

        [Test]
        public void SavesFromBeforeRelicsLoadWithoutOne()
        {
            MachineDefinition machine = Machine();
            var store = new MemorySaveStore { Data = new SaveData { balance = 500, freeSpins = 3 } };
            var session = new GameSession(machine, store);
            Assert.AreEqual(3, session.FreeSpins.Remaining);
            Assert.AreEqual(-1, session.FreeSpins.Relic);
        }
    }
}
