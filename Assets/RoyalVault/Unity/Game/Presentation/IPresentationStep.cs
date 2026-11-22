using System.Collections;
using SlotKit;

namespace RoyalVault.Unity
{
    // One moment after a spin has been paid and its wins shown: it decides whether it applies to the spin, then plays
    // itself. The round runs every step that applies, in the order they were added
    public interface IPresentationStep
    {
        bool Applies(SpinContext context);

        IEnumerator Run(SpinContext context);
    }
}
