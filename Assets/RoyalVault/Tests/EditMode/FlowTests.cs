using NUnit.Framework;
using RoyalVault.Core;

namespace RoyalVault.Tests
{
    /// <summary>The spin flow, the session and the standard features, played without any presentation.</summary>
    public sealed class FlowTests
    {
        private static (GameSession session, SpinFlow flow, SpinEngine engine) Create(long balance = 100000)
        {
            MachineDefinition machine = MachinePresets.RoyalVault5x3();
            machine.StartBalance = balance;
            var session = new GameSession(machine, new MemorySaveStore());
            var engine = new SpinEngine(machine, new SeededRandom(7));
            return (session, new SpinFlow(session, engine, StandardFeatures.Create()), engine);
        }

        private static SpinContext Play(SpinFlow flow)
        {
            SpinContext played = null;
            flow.SpinPaid += Capture;
            void Capture(SpinContext c) { played = c; }
            flow.RequestSpin();
            flow.ReelsStopped();
            flow.PresentationDone();
            flow.SpinPaid -= Capture;
            return played;
        }

        [Test]
        public void Spin_TakesTheBetAndPaysTheWin()
        {
            var (session, flow, _) = Create();
            long before = session.Wallet.Balance;
            SpinContext spin = Play(flow);
            Assert.AreEqual(before - session.Bet.TotalBet + spin.Outcome.TotalWin, session.Wallet.Balance);
            Assert.AreEqual(SpinState.Idle, flow.State);
        }

        [Test]
        public void Spin_IsRejectedWithoutEnoughCredits()
        {
            var (session, flow, _) = Create(10);
            string reason = null;
            flow.SpinRejected += c => reason = c.Rejection;
            Assert.IsFalse(flow.RequestSpin());
            Assert.IsNotNull(reason);
            Assert.AreEqual(10, session.Wallet.Balance);
        }

        [Test]
        public void FlowWaitsForThePresentation()
        {
            var (_, flow, _) = Create();
            Assert.IsTrue(flow.RequestSpin());
            Assert.IsFalse(flow.RequestSpin(), "a second spin cannot start while the reels run");
            flow.ReelsStopped();
            Assert.AreEqual(SpinState.Presenting, flow.State);
            flow.PresentationDone();
            Assert.AreEqual(SpinState.Idle, flow.State);
        }

        [Test]
        public void FreeSpins_CostNothingAndDoubleWins()
        {
            var (session, flow, engine) = Create();
            session.FreeSpins.Add(2);
            long before = session.Wallet.Balance;
            SpinContext spin = Play(flow);
            Assert.IsTrue(spin.IsFreeSpin);
            Assert.AreEqual(2, spin.Outcome.Multiplier);
            Assert.AreEqual(before + spin.Outcome.TotalWin, session.Wallet.Balance);
            Assert.IsTrue(spin.ContinueAutomatically, "one free spin is still waiting");
            _ = engine;
        }

        [Test]
        public void ScatterSpin_StartsAFreeSpinRound()
        {
            var (session, flow, engine) = Create();
            engine.ForcedStops = new[] { 33, 48, 5, 22, 6 }; // three vault seals
            SpinContext spin = Play(flow);
            Assert.AreEqual(8, spin.Outcome.FreeSpinsAwarded);
            Assert.AreEqual(8, session.FreeSpins.Remaining);
            Assert.IsTrue(spin.ContinueAutomatically);
        }

        [Test]
        public void ThreeCrowns_PayTheMinorJackpotAndResetIt()
        {
            var (session, flow, engine) = Create();
            for (int i = 0; i < 20; i++) session.Jackpots.Contribute(1000);
            long minor = session.Jackpots.Value(0);
            Assert.Greater(minor, 500);
            engine.ForcedStops = new[] { 12, 27, 3, 7, 46 }; // three crowns
            SpinContext spin = Play(flow);
            Assert.AreEqual(0, spin.Outcome.JackpotTier);
            // the value includes the contribution of the spin itself
            Assert.GreaterOrEqual(spin.Outcome.JackpotAmount, minor);
            Assert.AreEqual(500, session.Jackpots.Value(0));
            Assert.AreEqual(WinTier.Epic, spin.Outcome.Tier);
        }

        [Test]
        public void BigWin_IsLabelledByItsSizeAgainstTheBet()
        {
            var (_, flow, engine) = Create();
            engine.ForcedStops = new[] { 33, 13, 48, 1, 5 }; // 370 x the line bet = 18.5 x the total bet
            Assert.AreEqual(WinTier.Big, Play(flow).Outcome.Tier);
            engine.ForcedStops = new[] { 43, 9, 48, 5, 25 }; // 790 x the line bet = 39.5 x the total bet
            Assert.AreEqual(WinTier.Mega, Play(flow).Outcome.Tier);
        }

        [Test]
        public void AutoPlay_CountsDownAndStopsOnABigWin()
        {
            var (_, flow, engine) = Create();
            var auto = flow.Features.Get<AutoPlayFeature>();
            auto.Start(5);
            SpinContext first = Play(flow);
            Assert.AreEqual(4, auto.Remaining);
            Assert.IsTrue(first.ContinueAutomatically || first.Outcome.Tier >= WinTier.Big);
            engine.ForcedStops = new[] { 33, 13, 48, 1, 5 };
            Play(flow);
            Assert.IsFalse(auto.Active);
        }

        [Test]
        public void Session_IsSavedAndRestored()
        {
            MachineDefinition machine = MachinePresets.RoyalVault5x3();
            var store = new MemorySaveStore();
            var session = new GameSession(machine, store);
            session.Bet.SetLevel(4);
            session.Wallet.Credit(1234);
            session.Save();
            var again = new GameSession(machine, store);
            Assert.AreEqual(session.Wallet.Balance, again.Wallet.Balance);
            Assert.AreEqual(4, again.Bet.LevelIndex);
        }

        [Test]
        public void NewFeature_PlugsInWithoutChangingTheFlow()
        {
            var (session, _, engine) = Create();
            var features = StandardFeatures.Create();
            features.Add(new TripleEveryWin());
            var flow = new SpinFlow(session, engine, features);
            engine.ForcedStops = new[] { 33, 13, 48, 1, 5 };
            SpinContext spin = Play(flow);
            Assert.AreEqual(3, spin.Outcome.Multiplier);
        }

        /// <summary>An example feature: every win pays three times.</summary>
        private sealed class TripleEveryWin : SlotFeature
        {
            public override int Order { get { return 20; } }
            public override void OnOutcome(SpinContext context) { context.Outcome.Multiplier *= 3; }
        }
    }
}
