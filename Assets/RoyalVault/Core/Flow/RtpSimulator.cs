namespace RoyalVault.Core
{
    /// <summary>What a long simulated session paid back.</summary>
    public sealed class RtpReport
    {
        public long Spins;
        public long Wagered;
        public long Returned;
        public long Hits;
        public long FreeSpinRounds;
        public long Jackpots;

        /// <summary>Return to player, in percent of everything wagered.</summary>
        public double Rtp { get { return Wagered == 0 ? 0 : 100.0 * Returned / Wagered; } }
        public double HitRate { get { return Spins == 0 ? 0 : 100.0 * Hits / Spins; } }

        public override string ToString()
        {
            return $"{Spins:N0} paid spins: RTP {Rtp:F2}%, hit rate {HitRate:F1}%, free spin rounds {FreeSpinRounds:N0}, jackpots {Jackpots:N0}";
        }
    }

    /// <summary>
    /// Plays a machine through the real spin flow and features for many spins with a fixed seed and reports the
    /// return to player. Used by the tests and the machine asset's inspector to check the maths.
    /// </summary>
    public static class RtpSimulator
    {
        public static RtpReport Run(MachineDefinition machine, long paidSpins, int seed = 1)
        {
            var session = new GameSession(machine, null);
            // a deep wallet so the simulation never runs dry
            session.Wallet.Credit(long.MaxValue / 4);
            var flow = new SpinFlow(session, new SpinEngine(machine, new SeededRandom(seed)), StandardFeatures.Create());
            var report = new RtpReport();

            bool free = false;
            flow.SpinStarted += context =>
            {
                if (context.IsFreeSpin) return;
                report.Spins++;
                report.Wagered += context.Cost;
            };
            flow.SpinPaid += context =>
            {
                SpinOutcome outcome = context.Outcome;
                report.Returned += outcome.TotalWin;
                if (!context.IsFreeSpin && outcome.TotalWin > 0) report.Hits++;
                if (outcome.FreeSpinsAwarded > 0 && !context.IsFreeSpin) report.FreeSpinRounds++;
                if (outcome.JackpotTier >= 0) report.Jackpots++;
            };
            flow.SpinFinished += context => free = context.ContinueAutomatically;

            while (report.Spins < paidSpins || free)
            {
                flow.RequestSpin();
                flow.ReelsStopped();
                flow.PresentationDone();
            }
            return report;
        }
    }
}
