using NUnit.Framework;
using SlotKit;

namespace RoyalVault.Tests
{
    // Ruby's Last Hand: double-or-nothing on a win, with fair odds and firm limits
    public sealed class GambleTests
    {
        private static GambleSettings Settings()
        {
            return new GambleSettings { Enabled = true, MaxRounds = 5, MaxWinTotalBets = 50 };
        }

        // A wallet that already holds the win, as it does after a spin pays
        private static (Wallet wallet, GambleSession gamble) Start(long win, int seed, GambleSettings settings = null)
        {
            var wallet = new Wallet(1000 + win);
            var gamble = new GambleSession(wallet, new SeededRandom(seed), settings ?? Settings());
            gamble.Start(win, 100);
            return (wallet, gamble);
        }

        [Test]
        public void ARightColourDoublesTheStakeAndAWrongOneTakesItBack()
        {
            for (int seed = 0; seed < 40; seed++)
            {
                var (wallet, gamble) = Start(200, seed);
                Card card = gamble.Call(GambleChoice.Red);
                if (card.IsRed)
                {
                    Assert.AreEqual(400, gamble.Stake);
                    Assert.AreEqual(1400, wallet.Balance);
                    Assert.IsTrue(gamble.Active);
                }
                else
                {
                    Assert.AreEqual(0, gamble.Stake);
                    Assert.AreEqual(1000, wallet.Balance);
                    Assert.IsFalse(gamble.Active);
                }
            }
        }

        [Test]
        public void ARightSuitPaysFourTimes()
        {
            for (int seed = 0; seed < 60; seed++)
            {
                var (wallet, gamble) = Start(100, seed);
                Card card = gamble.Call(GambleChoice.Spades);
                long expected = card.Suit == 3 ? 400 : 0;
                Assert.AreEqual(expected, gamble.Stake);
                Assert.AreEqual(1000 + expected, wallet.Balance);
            }
        }

        [Test]
        public void TheGambleEndsAfterItsLastRoundOrAtItsLimit()
        {
            var settings = Settings();
            settings.MaxRounds = 1;
            var (_, oneRound) = Start(100, 3, settings);
            oneRound.Call(GambleChoice.Black);
            Assert.IsFalse(oneRound.Active, "one round only");

            var (_, nearLimit) = Start(2000, 3);
            Assert.IsTrue(nearLimit.CanCall(GambleChoice.Red), "4,000 is within 50 total bets");
            Assert.IsFalse(nearLimit.CanCall(GambleChoice.Hearts), "8,000 would pass the limit");
            Assert.IsFalse(new GambleSession(new Wallet(0), new SeededRandom(1), Settings()).Offers(3000, 100), "too big to offer");
            Assert.IsFalse(new GambleSession(new Wallet(0), new SeededRandom(1), new GambleSettings()).Offers(100, 100), "turned off");
        }

        [Test]
        public void CollectingKeepsTheStake()
        {
            var (wallet, gamble) = Start(300, 1);
            long kept = -1;
            gamble.Ended += stake => kept = stake;
            gamble.Collect();
            Assert.AreEqual(300, kept);
            Assert.AreEqual(1300, wallet.Balance);
            Assert.IsFalse(gamble.Active);
        }

        [Test]
        public void EveryCallIsFair()
        {
            var random = new SeededRandom(7);
            long returned = 0;
            const int calls = 200000;
            for (int i = 0; i < calls; i++)
            {
                var gamble = new GambleSession(new Wallet(1000), random, Settings());
                gamble.Start(100, 100);
                GambleChoice choice = (GambleChoice)(i % 6);
                gamble.Call(choice);
                returned += gamble.Stake;
            }

            double rate = returned / (100.0 * calls);
            Assert.That(rate, Is.InRange(0.98, 1.02), "average return per unit staked");
        }
    }
}
