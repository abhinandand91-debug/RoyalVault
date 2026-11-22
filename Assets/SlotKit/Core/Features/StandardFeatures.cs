using System;

namespace SlotKit
{
    // Free spins: plays them at no cost with the machine's multiplier, and adds any won during the round
    public sealed class FreeSpinsFeature : SlotFeature
    {
        public override int Order
        {
            get
            {
                return 0;
            }
        }

        public override void OnSpinRequested(SpinContext context)
        {
            if (!context.Session.FreeSpins.TryConsume())
            {
                return;
            }

            context.IsFreeSpin = true;
            context.Cost = 0;
        }

        public override void OnOutcome(SpinContext context)
        {
            if (context.IsFreeSpin)
            {
                context.Outcome.Multiplier = context.Session.Machine.FreeSpinMultiplier;
            }
        }

        public override void OnPaid(SpinContext context)
        {
            FreeSpinState state = context.Session.FreeSpins;
            if (context.IsFreeSpin)
            {
                state.AddWin(context.Outcome.TotalWin);
            }

            state.Add(context.Outcome.FreeSpinsAwarded);
            if (state.Remaining > 0)
            {
                context.ContinueAutomatically = true;
            }
            else if (context.IsFreeSpin)
            {
                state.EndRound();
            }
        }
    }

    // Progressive jackpots: every paid bet feeds the pool; a won tier is paid out and reset
    public sealed class JackpotFeature : SlotFeature
    {
        public override int Order
        {
            get
            {
                return 10;
            }
        }

        public override void OnSpinRequested(SpinContext context)
        {
            if (!context.IsFreeSpin)
            {
                context.Session.Jackpots.Contribute(context.Cost);
            }
        }

        public override void OnOutcome(SpinContext context)
        {
            SpinOutcome outcome = context.Outcome;
            if (outcome.JackpotTier >= 0)
            {
                outcome.JackpotAmount = context.Session.Jackpots.Award(outcome.JackpotTier);
            }
        }
    }

    // Labels the win against the total bet so the presentation knows how big to celebrate
    public sealed class WinTierFeature : SlotFeature
    {
        public override int Order
        {
            get
            {
                return 50;
            }
        }

        public override void OnOutcome(SpinContext context)
        {
            SpinOutcome outcome = context.Outcome;
            int[] tiers = context.Session.Machine.WinTierMultipliers;
            long win = outcome.TotalWin;
            if (win <= 0)
            {
                outcome.Tier = WinTier.None;
                return;
            }
            outcome.Tier = WinTier.Normal;
            if (outcome.JackpotTier >= 0)
            {
                outcome.Tier = WinTier.Epic;
                return;
            }
            for (int i = 0; i < tiers.Length && i < 3; i++)
            {
                if (win >= tiers[i] * outcome.TotalBet)
                {
                    outcome.Tier = (WinTier)((int)WinTier.Big + i);
                }
            }
        }
    }

    // Auto play: keeps spinning for a number of rounds, stopping early on a big win or when asked
    public sealed class AutoPlayFeature : SlotFeature
    {
        public override int Order
        {
            get
            {
                return 100;
            }
        }

        public int Remaining { get; private set; }
        public bool Active
        {
            get
            {
                return Remaining > 0;
            }
        }
        // Stop on a win of this tier or above
        public WinTier StopOnTier = WinTier.Big;

        // Raised after auto play starts, counts down or stops
        public event Action<AutoPlayFeature> Changed;

        public void Start(int spins)
        {
            Remaining = Math.Max(0, spins);
            Changed?.Invoke(this);
        }

        public void Stop()
        {
            if (Remaining == 0)
            {
                return;
            }

            Remaining = 0;
            Changed?.Invoke(this);
        }

        public override void OnSpinRequested(SpinContext context)
        {
            if (!Active || context.IsFreeSpin)
            {
                return;
            }

            Remaining--;
            Changed?.Invoke(this);
        }

        public override void OnPaid(SpinContext context)
        {
            if (!Active)
            {
                return;
            }

            if (context.Outcome.Tier >= StopOnTier && StopOnTier != WinTier.None)
            {
                Stop();
                return;
            }
            if (!context.Session.Wallet.CanAfford(context.Session.Bet.TotalBet))
            {
                Stop();
                return;
            }
            context.ContinueAutomatically = true;
        }
    }

    public static class StandardFeatures
    {
        // The features every machine in this game runs; random wilds draw from the same random source as the reels
        public static FeaturePipeline Create(IRandom random)
        {
            return new FeaturePipeline(new ISlotFeature[]
            {
                new RandomWildsFeature(random), new FreeSpinsFeature(), new ExpandingSymbolFeature(random), new TreasuryFeature(random),
                new JackpotFeature(),
                new WinTierFeature(), new AutoPlayFeature(),
            });
        }
    }
}
