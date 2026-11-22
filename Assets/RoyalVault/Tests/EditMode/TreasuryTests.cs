using System.Collections.Generic;
using NUnit.Framework;
using RoyalVault.Core;
using SlotKit;

namespace RoyalVault.Tests
{
    // The Treasury: a Vault Key on reels 1, 3 and 5 opens 12 urns; picks reveal credits, a multiplier or a shield until an
    // eye gets through
    public sealed class TreasuryTests
    {
        private static MachineDefinition Machine()
        {
            MachineDefinition machine = MachinePresets.RoyalVault5x3();
            machine.RandomWilds.ChancePerMille = 0;
            return machine;
        }

        // A grid with no keys, then a key on the top row of each given reel
        private static SpinOutcome Grid(MachineDefinition machine, params int[] keyReels)
        {
            var grid = new SymbolGrid(machine.Reels, machine.Rows);
            for (int reel = 0; reel < machine.Reels; reel++)
            {
                for (int row = 0; row < machine.Rows; row++)
                {
                    grid[reel, row] = (reel + row) % 2;
                }
            }

            foreach (int reel in keyReels)
            {
                grid[reel, 0] = machine.SymbolIndex("vault_key");
            }

            return new SpinOutcome(new int[machine.Reels], grid, 5, 5 * machine.Paylines.Length);
        }

        private static SpinOutcome Open(MachineDefinition machine, GameSession session, bool freeSpin, params int[] keyReels)
        {
            SpinOutcome outcome = Grid(machine, keyReels);
            var context = new SpinContext(session, 100) { IsFreeSpin = freeSpin, Outcome = outcome };
            new TreasuryFeature(new SeededRandom(3)).OnOutcome(context);
            return outcome;
        }

        [Test]
        public void KeysOnReelsOneThreeAndFiveOpenItInTheBaseGameOnly()
        {
            MachineDefinition machine = Machine();
            var session = new GameSession(machine, new MemorySaveStore());

            SpinOutcome opened = Open(machine, session, false, 0, 2, 4);
            Assert.IsNotNull(opened.Treasury);
            Assert.AreEqual(3, opened.Treasury.Keys.Count);

            Assert.IsNull(Open(machine, session, false, 0, 1, 2).Treasury, "keys on the wrong reels");
            Assert.IsNull(Open(machine, session, false, 0, 2).Treasury, "a key reel without a key");
            Assert.IsNull(Open(machine, session, true, 0, 2, 4).Treasury, "free spins");
        }

        [Test]
        public void EveryRoundHoldsTheSettingsUrnsStartsWithCreditsAndEndsOnAnEyeThatGetsThrough()
        {
            var settings = new TreasurySettings { Enabled = true };
            var random = new SeededRandom(5);
            for (int i = 0; i < 2000; i++)
            {
                TreasuryRound round = TreasuryRound.Draw(settings, random, 100, new List<GridPosition>());
                var counts = new int[4];
                foreach (Urn urn in round.Order)
                {
                    counts[(int)urn.Prize]++;
                }

                Assert.AreEqual(settings.Urns, round.Order.Length);
                Assert.AreEqual(settings.Eyes, counts[(int)UrnPrize.Eye]);
                Assert.AreEqual(settings.Shields, counts[(int)UrnPrize.Shield]);
                Assert.AreEqual(settings.Multipliers, counts[(int)UrnPrize.Multiplier]);
                Assert.AreEqual(UrnPrize.Credits, round.Order[0].Prize, "the first pick always pays");
                Assert.Greater(round.Amount, 0);

                bool endsEarly = round.Picks < round.Order.Length;
                if (endsEarly)
                {
                    Assert.AreEqual(UrnPrize.Eye, round.Order[round.Picks - 1].Prize, "only an eye ends a round early");
                    Assert.AreEqual(0, round.ShieldsAfter(round.Picks - 1), "a shield would have blocked it");
                }

                Assert.AreEqual(round.CreditsAfter(round.Picks) * round.MultiplierAfter(round.Picks), round.Amount);
            }
        }

        [Test]
        public void AShieldBlocksOneEyeAndAMultiplierScalesEverythingCollected()
        {
            var settings = new TreasurySettings
            {
                Enabled = true, Urns = 6, Eyes = 2, Shields = 1, Multipliers = 1, CreditValues = new[] { 4 }, CreditWeights = new[] { 1 },
            };
            var random = new SeededRandom(8);
            bool sawShieldSave = false;
            for (int i = 0; i < 500; i++)
            {
                TreasuryRound round = TreasuryRound.Draw(settings, random, 10, new List<GridPosition>());
                int eyesSeen = 0;
                for (int pick = 0; pick < round.Picks; pick++)
                {
                    if (round.Order[pick].Prize == UrnPrize.Eye)
                    {
                        eyesSeen++;
                    }
                }

                sawShieldSave |= eyesSeen == 2;
                Assert.LessOrEqual(eyesSeen, 2);
                Assert.AreEqual(round.CreditsAfter(round.Picks) * round.MultiplierAfter(round.Picks), round.Amount);
                Assert.AreEqual(0, round.CreditsAfter(round.Picks) % 40, "credit urns hold 4 total bets");
            }

            Assert.IsTrue(sawShieldSave, "a shield let a round carry on past an eye");
        }

        [Test]
        public void TheRoundIsPaidWhenCollectedOrOnTheNextLaunchButNeverTwice()
        {
            MachineDefinition machine = Machine();
            var store = new MemorySaveStore();
            var session = new GameSession(machine, store);
            long start = session.Wallet.Balance;
            SpinOutcome outcome = Open(machine, session, false, 0, 2, 4);
            var context = new SpinContext(session, 100) { Outcome = outcome };
            new TreasuryFeature(new SeededRandom(3)).OnPaid(context);

            long amount = outcome.Treasury.Amount;
            Assert.AreEqual(amount, session.Treasury.Pending);
            Assert.AreEqual(start, session.Wallet.Balance, "held until the urns are shown");

            session.Save();
            var relaunched = new GameSession(machine, store);
            Assert.AreEqual(start + amount, relaunched.Wallet.Balance, "paid on the next launch");
            var again = new GameSession(machine, store);
            Assert.AreEqual(start + amount, again.Wallet.Balance, "and only once");

            Assert.AreEqual(amount, session.Treasury.Collect(session.Wallet));
            Assert.AreEqual(start + amount, session.Wallet.Balance);
            Assert.AreEqual(0, session.Treasury.Pending);
        }

        [Test]
        public void SavesFromBeforeTheTreasuryLoadWithNothingPending()
        {
            var store = new MemorySaveStore { Data = new SaveData { balance = 500 } };
            var session = new GameSession(Machine(), store);
            Assert.AreEqual(500, session.Wallet.Balance);
            Assert.AreEqual(0, session.Treasury.Pending);
        }
    }
}
