using System.Collections;
using SlotKit;

namespace RoyalVault.Unity
{
    // On a free spin where the Sealed Relic pays, it sweeps over its reels before the wins are shown
    public sealed class RelicLandingStep : ILandingStep
    {
        private readonly MachineView machine;

        public RelicLandingStep(MachineView machineView)
        {
            machine = machineView;
        }

        public bool Wants(SpinContext context)
        {
            return context.Outcome.Expanded != null;
        }

        public IEnumerator Play(SpinContext context)
        {
            return machine.ExpandRelic(context.Outcome.Expanded);
        }
    }
}
