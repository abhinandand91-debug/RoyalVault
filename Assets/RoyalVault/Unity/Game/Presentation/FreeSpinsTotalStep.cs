using System.Collections;
using SlotKit;

namespace RoyalVault.Unity
{
    // The last free spin of a round: what the whole round won
    public sealed class FreeSpinsTotalStep : IPresentationStep
    {
        private readonly GameSession session;
        private readonly BigWinView titles;
        private readonly FreeSpinTally tally;

        public FreeSpinsTotalStep(GameSession gameSession, BigWinView titleView, FreeSpinTally roundTally)
        {
            session = gameSession;
            titles = titleView;
            tally = roundTally;
        }

        public bool Applies(SpinContext context)
        {
            return context.IsFreeSpin && session.FreeSpins.Remaining == 0;
        }

        public IEnumerator Run(SpinContext context)
        {
            yield return titles.ShowTitle("FREE SPINS WIN", null, tally.Win, 2.5f, CelebrationKind.FreeSpinsTotal);
            tally.Win = 0;
        }
    }
}
