using System.Collections.Generic;

namespace RoyalVault.Core
{
    // One spin as it moves through the flow; features read and change it at each hook
    public sealed class SpinContext
    {
        public readonly GameSession Session;
        // Credits this spin costs; features may change it (a free spin costs nothing)
        public long Cost;
        public bool IsFreeSpin;
        // Set by a feature to stop the spin from happening, with a reason the player sees
        public string Rejection;
        public SpinOutcome Outcome;
        // Set by a feature when another spin should follow on its own (free spins, auto play)
        public bool ContinueAutomatically;

        public SpinContext(GameSession session, long cost)
        {
            Session = session;
            Cost = cost;
        }

        public bool Rejected { get { return Rejection != null; } }
    }

    // A plug-in that joins the spin flow
    public interface ISlotFeature
    {
        int Order { get; }

        // Before the bet is taken: may set the cost, mark a free spin or reject the spin
        void OnSpinRequested(SpinContext context);

        // The outcome is decided but not shown: may change multipliers, awards or the outcome itself
        void OnOutcome(SpinContext context);

        // The win has been credited: may ask for another spin
        void OnPaid(SpinContext context);
    }

    // Convenience base: override only the hooks a feature needs
    public abstract class SlotFeature : ISlotFeature
    {
        public virtual int Order { get { return 0; } }
        public virtual void OnSpinRequested(SpinContext context) { }
        public virtual void OnOutcome(SpinContext context) { }
        public virtual void OnPaid(SpinContext context) { }
    }

    // Runs the registered features in order at each hook
    public sealed class FeaturePipeline
    {
        private readonly List<ISlotFeature> features = new List<ISlotFeature>();

        public IReadOnlyList<ISlotFeature> Features { get { return features; } }

        public FeaturePipeline(IEnumerable<ISlotFeature> features = null)
        {
            if (features != null) foreach (ISlotFeature feature in features) Add(feature);
        }

        public void Add(ISlotFeature feature)
        {
            features.Add(feature);
            features.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        public T Get<T>() where T : class, ISlotFeature
        {
            foreach (ISlotFeature feature in features)
                if (feature is T match) return match;
            return null;
        }

        public void SpinRequested(SpinContext context)
        {
            foreach (ISlotFeature feature in features)
            {
                feature.OnSpinRequested(context);
                if (context.Rejected) return;
            }
        }

        public void Outcome(SpinContext context)
        {
            foreach (ISlotFeature feature in features) feature.OnOutcome(context);
        }

        public void Paid(SpinContext context)
        {
            foreach (ISlotFeature feature in features) feature.OnPaid(context);
        }
    }
}
