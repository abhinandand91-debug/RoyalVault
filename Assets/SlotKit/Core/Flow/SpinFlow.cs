using System;

namespace SlotKit
{
    public enum SpinState
    {
        // Waiting for the player
        Idle,
        // The outcome is decided and the reels are moving
        Spinning,
        // The win is paid and being shown
        Presenting,
    }

    // Runs one spin from request to payout and tells the presentation what happens through events
    public sealed class SpinFlow
    {
        private readonly GameSession session;
        private readonly SpinEngine engine;
        private readonly FeaturePipeline features;
        private SpinContext current;

        public SpinState State { get; private set; } = SpinState.Idle;
        public SpinContext Current
        {
            get
            {
                return current;
            }
        }
        public FeaturePipeline Features
        {
            get
            {
                return features;
            }
        }

        // The outcome is decided and the reels should start
        public event Action<SpinContext> SpinStarted;
        // The spin did not happen; the context's Rejection says why
        public event Action<SpinContext> SpinRejected;
        // The win has been credited and should be shown
        public event Action<SpinContext> SpinPaid;
        // The spin is over; ContinueAutomatically says whether another follows
        public event Action<SpinContext> SpinFinished;

        public SpinFlow(GameSession session, SpinEngine engine, FeaturePipeline features)
        {
            this.session = session;
            this.engine = engine;
            this.features = features;
        }

        // Starts a spin if the machine is idle and the features and wallet allow it
        public bool RequestSpin()
        {
            if (State != SpinState.Idle)
            {
                return false;
            }

            var context = new SpinContext(session, session.Bet.TotalBet);
            features.SpinRequested(context);
            if (!context.Rejected && !context.IsFreeSpin && !session.Wallet.TryDebit(context.Cost))
            {
                context.Rejection = "Not enough credits for this bet.";
            }

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

        // Called by the presentation when every reel has landed: pays the win
        public void ReelsStopped()
        {
            if (State != SpinState.Spinning)
            {
                return;
            }

            session.Wallet.Credit(current.Outcome.TotalWin);
            features.Paid(current);
            State = SpinState.Presenting;
            session.Save();
            SpinPaid?.Invoke(current);
        }

        // Called by the presentation when the win has been shown: the machine is idle again
        public void PresentationDone()
        {
            if (State != SpinState.Presenting)
            {
                return;
            }

            State = SpinState.Idle;
            SpinContext finished = current;
            current = null;
            SpinFinished?.Invoke(finished);
        }
    }
}
