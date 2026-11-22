using System.Collections;
using SlotKit;
using UnityEngine;

namespace RoyalVault.Unity
{
    // The keys glow, the Treasury is announced, the urns are picked, and the round's credits are paid
    public sealed class TreasuryStep : IPresentationStep
    {
        private readonly GameSession session;
        private readonly MachineView machine;
        private readonly BigWinView titles;
        private readonly TreasuryView treasury;
        private readonly ConsoleView console;
        private readonly AutoPlayFeature autoPlay;
        private readonly WaitForSeconds keysGlow = new WaitForSeconds(1.3f);

        public TreasuryStep(GameSession gameSession, MachineView machineView, BigWinView titleView, TreasuryView treasuryView,
                            ConsoleView consoleView, AutoPlayFeature autoPlayFeature)
        {
            session = gameSession;
            machine = machineView;
            titles = titleView;
            treasury = treasuryView;
            console = consoleView;
            autoPlay = autoPlayFeature;
        }

        public bool Applies(SpinContext context)
        {
            return session.Treasury.Round != null;
        }

        public IEnumerator Run(SpinContext context)
        {
            TreasuryRound round = session.Treasury.Round;
            machine.HighlightCells(round.Keys, 1.4f);
            yield return keysGlow;
            yield return titles.ShowTitle("THE TREASURY", "THREE KEYS OPEN THE DOOR", 0, 1.8f, CelebrationKind.FreeSpinsAwarded);
            if (treasury != null)
            {
                yield return treasury.Play(round, autoPlay.Active);
            }

            long paid = session.Treasury.Collect(session.Wallet);
            console.SetWin(context.Outcome.TotalWin + paid);
            session.Save();
        }
    }
}
