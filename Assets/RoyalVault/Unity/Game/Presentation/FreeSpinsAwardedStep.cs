using System.Collections;
using SlotKit;

namespace RoyalVault.Unity
{
    // A paid spin won free spins: the story's chapter (once), the round's title, then its Sealed Relic
    public sealed class FreeSpinsAwardedStep : IPresentationStep
    {
        private readonly GameSession session;
        private readonly StoryDirector story;
        private readonly BigWinView titles;
        private readonly RelicRevealView relicReveal;
        private readonly FreeSpinTally tally;
        private readonly BonusActivationView activation;

        public FreeSpinsAwardedStep(GameSession gameSession, StoryDirector storyDirector, BigWinView titleView, RelicRevealView relicView,
                                    FreeSpinTally roundTally, BonusActivationView bonusActivation = null)
        {
            activation = bonusActivation;
            session = gameSession;
            story = storyDirector;
            titles = titleView;
            relicReveal = relicView;
            tally = roundTally;
        }

        public bool Applies(SpinContext context)
        {
            return !context.IsFreeSpin && context.Outcome.FreeSpinsAwarded > 0;
        }

        public IEnumerator Run(SpinContext context)
        {
            if (activation != null && context.Outcome.Scatter != null)
            {
                yield return activation.Play(context.Outcome.Scatter.Positions);
            }

            if (story != null)
            {
                yield return story.TellOnce(StoryMoment.FreeSpins);
            }

            tally.Win = 0;
            yield return titles.ShowTitle(context.Outcome.FreeSpinsAwarded + " FREE SPINS", Line(), 0, 2.2f, CelebrationKind.FreeSpinsAwarded);
            if (relicReveal != null && session.FreeSpins.Relic >= 0)
            {
                yield return relicReveal.Play(session.FreeSpins.Relic);
            }
        }

        // The line under the title: the multiplier when there is one, otherwise the relic
        private string Line()
        {
            if (session.Machine.FreeSpinMultiplier > 1)
            {
                return "EVERY WIN x" + session.Machine.FreeSpinMultiplier;
            }

            // a fixed gap: the heading font's kerning swallows a space after the D
            return session.FreeSpins.Relic >= 0 ? "A SEALED<space=0.4em>RELIC AWAITS" : null;
        }
    }
}
