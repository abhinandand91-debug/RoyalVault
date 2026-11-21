using System.Collections;
using SlotKit;

namespace RoyalVault.Unity
{
    // Something shown after the reels land and before the wins, such as wilds burning in
    public interface ILandingStep
    {
        bool Wants(SpinContext context);

        IEnumerator Play(SpinContext context);
    }
}
