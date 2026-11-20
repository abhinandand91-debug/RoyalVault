using System.Collections.Generic;

namespace RoyalVault.Core
{
    /// <summary>One spin as it moves through the flow; features read and change it at each hook.</summary>
    public sealed class SpinContext
    {
        public readonly GameSession Session;
        /// <summary>Credits this spin costs; features may change it (a free spin costs nothing).</summary>
        public long Cost;
        public bool IsFreeSpin;
        /// <summary>Set by a feature to stop the spin from happening, with a reason the player sees.</summary>
        public string Rejection;
        public SpinOutcome Outcome;
        /// <summary>Set by a feature when another spin should follow on its own (free spins, auto play).</summary>
        public bool ContinueAutomatically;

        public SpinContext(GameSession session, long cost)
        {
            Session = session;
            Cost = cost;
        }

        public bool Rejected { get { return Rejection != null; } }
    }

    /// <summary>
    /// A plug-in that joins the spin flow. Features run in <see cref="Order"/> at three points: before the spin is
    /// paid for, once the outcome is decided, and after it has been paid. New gameplay is added as a new feature;
    /// the engine and flow stay as they are.
    /// </summary>
    public interface ISlotFeature
    {
        int Order { get; }

        /// <summary>Before the bet is taken: may set the cost, mark a free spin or reject the spin.</summary>
        void OnSpinRequested(SpinContext context);

        /// <summary>The outcome is decided but not shown: may change multipliers, awards or the outcome itself.</summary>
        void OnOutcome(SpinContext context);

        /// <summary>The win has been credited: may ask for another spin.</summary>
        void OnPaid(SpinContext context);
    }

    /// <summary>Convenience base: override only the hooks a feature needs.</summary>
    public abstract class SlotFeature : ISlotFeature
    {
        public virtual int Order { get { return 0; } }
        public virtual void OnSpinRequested(SpinContext context) { }
        public virtual void OnOutcome(SpinContext context) { }
        public virtual void OnPaid(SpinContext context) { }
    }

    /// <summary>Runs the registered features in order at each hook.</summary>
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
