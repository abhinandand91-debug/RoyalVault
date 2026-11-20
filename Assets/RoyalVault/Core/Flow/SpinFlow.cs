using System;

namespace RoyalVault.Core
{
    public enum SpinState
    {
        /// <summary>Waiting for the player.</summary>
        Idle,
        /// <summary>The outcome is decided and the reels are moving.</summary>
        Spinning,
        /// <summary>The win is paid and being shown.</summary>
        Presenting,
    }

    /// <summary>
    /// Runs one spin from request to payout and tells the presentation what happens through events. It never waits
    /// on animations itself: the presentation calls <see cref="ReelsStopped"/> when the reels have landed and
    /// <see cref="PresentationDone"/> when the win has been shown.
    /// </summary>
    public sealed class SpinFlow
    {
        private readonly GameSession session;
        private readonly SpinEngine engine;
        private readonly FeaturePipeline features;
        private SpinContext current;

        public SpinState State { get; private set; } = SpinState.Idle;
        public SpinContext Current { get { return current; } }
        public FeaturePipeline Features { get { return features; } }

        /// <summary>The outcome is decided and the reels should start.</summary>
        public event Action<SpinContext> SpinStarted;
        /// <summary>The spin did not happen; the context's <see cref="SpinContext.Rejection"/> says why.</summary>
        public event Action<SpinContext> SpinRejected;
        /// <summary>The win has been credited and should be shown.</summary>
        public event Action<SpinContext> SpinPaid;
        /// <summary>The spin is over; <see cref="SpinContext.ContinueAutomatically"/> says whether another follows.</summary>
        public event Action<SpinContext> SpinFinished;

        public SpinFlow(GameSession session, SpinEngine engine, FeaturePipeline features)
        {
            this.session = session;
            this.engine = engine;
            this.features = features;
        }

        /// <summary>Starts a spin if the machine is idle and the features and wallet allow it.</summary>
        public bool RequestSpin()
        {
            if (State != SpinState.Idle) return false;

            var context = new SpinContext(session, session.Bet.TotalBet);
            features.SpinRequested(context);
            if (!context.Rejected && !context.IsFreeSpin && !session.Wallet.TryDebit(context.Cost))
                context.Rejection = "Not enough credits for this bet.";
            if (context.Rejected)
            {
                SpinRejected?.Invoke(context);
                return false;
            }

            context.Outcome = engine.Spin(session.Bet.LineBet);
            features.Outcome(context);
            current = context;
            State = SpinState.Spinning;
            session.Save();
            SpinStarted?.Invoke(context);
            return true;
        }

        /// <summary>Called by the presentation when every reel has landed: pays the win.</summary>
        public void ReelsStopped()
        {
            if (State != SpinState.Spinning) return;
            session.Wallet.Credit(current.Outcome.TotalWin);
            features.Paid(current);
            State = SpinState.Presenting;
            session.Save();
            SpinPaid?.Invoke(current);
        }

        /// <summary>Called by the presentation when the win has been shown: the machine is idle again.</summary>
        public void PresentationDone()
        {
            if (State != SpinState.Presenting) return;
            State = SpinState.Idle;
            SpinContext finished = current;
            current = null;
            SpinFinished?.Invoke(finished);
        }
    }
}
