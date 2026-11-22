using System.Collections;
using SlotKit;

namespace RoyalVault.Unity
{
    // A free spin won more free spins
    public sealed class FreeSpinsRetriggeredStep : IPresentationStep
    {
        private readonly BigWinView titles;

        public FreeSpinsRetriggeredStep(BigWinView titleView)
        {
            titles = titleView;
        }

        public bool Applies(SpinContext context)
        {
            return context.IsFreeSpin && context.Outcome.FreeSpinsAwarded > 0;
        }

        public IEnumerator Run(SpinContext context)
        {
            yield return titles.ShowTitle("+" + context.Outcome.FreeSpinsAwarded + " FREE SPINS", null, 0, 1.6f, CelebrationKind.FreeSpinsAwarded);
        }
    }
}
