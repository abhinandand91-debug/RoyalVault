using NUnit.Framework;
using RoyalVault.Core;
using SlotKit;

namespace RoyalVault.Tests
{
    // Random wilds (Guardian's Fire): when they may appear, where, and that the grid is judged again afterwards
    public sealed class RandomWildsTests
    {
        private static MachineDefinition Machine(int chancePerMille)
        {
            MachineDefinition machine = MachinePresets.RoyalVault5x3();
            machine.RandomWilds.ChancePerMille = chancePerMille;
            return machine;
        }

        // The first stops (searched in order) whose grid wins nothing
        private static int[] LosingStops(MachineDefinition machine)
        {
            var engine = new SpinEngine(machine, new SeededRandom(1));
            for (int seed = 0; seed < 5000; seed++)
            {
                var stops = new int[machine.Reels];
                for (int reel = 0; reel < machine.Reels; reel++)
                {
                    stops[reel] = (seed * (7 + reel * 3)) % machine.Strips[reel].Length;
                }

                if (!engine.Evaluate(stops, 1).IsWin)
                {
                    return stops;
                }
            }

            Assert.Fail("no losing stops found");
            return null;
        }

        private static SpinContext Spin(MachineDefinition machine, int[] stops, int seed, bool freeSpin = false)
        {
            var session = new GameSession(machine, new MemorySaveStore());
            var context = new SpinContext(session, 20)
            {
                IsFreeSpin = freeSpin,
                Outcome = new SpinEngine(machine, new SeededRandom(1)).Evaluate(stops, 1),
            };
            new RandomWildsFeature(new SeededRandom(seed)).OnOutcome(context);
            return context;
        }

        [Test]
        public void LosingBaseSpinGetsBetweenMinAndMaxWildsOnRegularSymbols()
        {
            MachineDefinition machine = Machine(1000);
            int[] stops = LosingStops(machine);
            SymbolGrid before = new SpinEngine(machine, new SeededRandom(1)).Evaluate(stops, 1).Grid;
            SpinOutcome outcome = Spin(machine, stops, 5).Outcome;

            int wild = machine.FirstIndexOfKind(SymbolKind.Wild);
            Assert.That(outcome.AddedWilds.Count, Is.InRange(machine.RandomWilds.MinWilds, machine.RandomWilds.MaxWilds));
            foreach (GridPosition cell in outcome.AddedWilds)
            {
                Assert.AreEqual(wild, outcome.Grid[cell.Reel, cell.Row]);
                Assert.AreEqual(SymbolKind.Regular, machine.Symbols[before[cell.Reel, cell.Row]].Kind, "only regular symbols burn");
            }
        }

        [Test]
        public void WinsAreEvaluatedAgainOnTheNewGrid()
        {
            MachineDefinition machine = Machine(1000);
            int[] stops = LosingStops(machine);
            SpinOutcome changed = Spin(machine, stops, 5).Outcome;
            var expected = new SpinOutcome(changed.Stops, changed.Grid, changed.LineBet, changed.TotalBet);
            WinEvaluator.Standard().Evaluate(machine, expected);
            Assert.AreEqual(expected.TotalWin, changed.TotalWin);
            Assert.AreEqual(expected.LineWins.Count, changed.LineWins.Count);
        }

        [Test]
        public void WinningSpinsFreeSpinsAndAZeroChanceAreLeftAlone()
        {
            MachineDefinition machine = Machine(1000);
            int[] winning = { 33, 13, 48, 1, 5 };
            Assert.IsEmpty(Spin(machine, winning, 5).Outcome.AddedWilds, "a winning spin");
            Assert.IsEmpty(Spin(machine, LosingStops(machine), 5, true).Outcome.AddedWilds, "a free spin");
            Assert.IsEmpty(Spin(Machine(0), LosingStops(machine), 5).Outcome.AddedWilds, "chance 0");
        }

        [Test]
        public void TheSameSeedBurnsTheSameCells()
        {
            MachineDefinition machine = Machine(1000);
            int[] stops = LosingStops(machine);
            CollectionAssert.AreEqual(Spin(machine, stops, 11).Outcome.AddedWilds, Spin(machine, stops, 11).Outcome.AddedWilds);
        }
    }
}
