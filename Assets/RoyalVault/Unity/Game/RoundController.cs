using System.Collections;
using GameKit;
using SlotKit;
using UnityEngine;

namespace RoyalVault.Unity
{
    // Turns console presses into spin requests and flow events into reel spins and win shows
    public sealed class RoundController : MonoBehaviour
    {
        private GameSession session;
        private SpinFlow flow;
        private AutoPlayFeature autoPlay;
        private MachineView machine;
        private ConsoleView console;
        private WinPresenter presenter;
        private BigWinView bigWin;
        private PaytableView paytable;
        private MessageToast toast;
        private long freeSpinRoundWin;
        private StoryDirector story;

        // Spins played automatically after the player starts auto play
        public int AutoPlaySpins = 25;

        public void Bind(GameSession gameSession, SpinFlow spinFlow, MachineView machineView, ConsoleView consoleView, WinPresenter winPresenter,
                         BigWinView bigWinView, PaytableView paytableView, MessageToast messageToast, StoryDirector storyDirector = null)
        {
            story = storyDirector;
            session = gameSession;
            flow = spinFlow;
            autoPlay = flow.Features.Get<AutoPlayFeature>();
            machine = machineView;
            console = consoleView;
            presenter = winPresenter;
            bigWin = bigWinView;
            paytable = paytableView;
            toast = messageToast;

            console.SpinPressed += OnSpinPressed;
            console.BetUpPressed += () => { if (flow.State == SpinState.Idle) session.Bet.Increase(); };
            console.BetDownPressed += () => { if (flow.State == SpinState.Idle) session.Bet.Decrease(); };
            console.AutoPressed += OnAutoPressed;
            console.MenuPressed += () => { if (flow.State == SpinState.Idle) paytable.Open(); };

            session.Wallet.Changed += value => console.SetBalance(value, 0.6f);
            session.Bet.Changed += bet => { console.SetBet(bet); session.Save(); };
            session.FreeSpins.Changed += state => console.SetFreeSpins(state, session.Machine.FreeSpinMultiplier);
            autoPlay.Changed += a => console.SetAuto(a.Active, a.Remaining);

            flow.SpinStarted += OnSpinStarted;
            flow.SpinRejected += context => { autoPlay.Stop(); toast.Show(context.Rejection); };
            flow.SpinPaid += OnSpinPaid;
            flow.SpinFinished += OnSpinFinished;

            console.SetBalance(session.Wallet.Balance);
            console.SetBet(session.Bet);
            console.SetFreeSpins(session.FreeSpins, session.Machine.FreeSpinMultiplier);
            console.SetAuto(false, 0);
            console.ClearWin();
            // a saved free spin round carries on
            if (session.FreeSpins.Remaining > 0) StartCoroutine(After(1.2f, () => flow.RequestSpin()));
            else if (story != null) StartCoroutine(story.Welcome());
        }

        private void OnSpinPressed()
        {
            if (paytable.IsOpen || bigWin.IsShowing || (story != null && story.IsTelling)) return;
            if (flow.State == SpinState.Spinning) { machine.Hurry(); return; }
            if (flow.State == SpinState.Presenting) return;
            flow.RequestSpin();
        }

        private void OnAutoPressed()
        {
            if (autoPlay.Active) { autoPlay.Stop(); return; }
            autoPlay.Start(AutoPlaySpins);
            if (flow.State == SpinState.Idle) flow.RequestSpin();
        }

        private void OnSpinStarted(SpinContext context)
        {
            presenter.Cancel();
            console.SetControls(false, true, session.Bet);
            if (!context.IsFreeSpin) console.ClearWin();
            bool fast = autoPlay.Active || context.IsFreeSpin;
            machine.Spin(context.Outcome, fast, flow.ReelsStopped);
        }

        private void OnSpinPaid(SpinContext context)
        {
            console.SetControls(false, false, session.Bet);
            if (context.IsFreeSpin) freeSpinRoundWin += context.Outcome.TotalWin;
            presenter.Present(context, autoPlay.Active || context.IsFreeSpin, flow.PresentationDone);
        }

        private void OnSpinFinished(SpinContext context)
        {
            StartCoroutine(Finish(context));
        }

        private IEnumerator Finish(SpinContext context)
        {
            SpinOutcome outcome = context.Outcome;
            // free spins awarded from a paid spin: announce the round
            if (!context.IsFreeSpin && outcome.FreeSpinsAwarded > 0)
            {
                if (story != null) yield return story.TellOnce(StoryMoment.FreeSpins);
                freeSpinRoundWin = 0;
                yield return bigWin.ShowTitle(outcome.FreeSpinsAwarded + " FREE SPINS", "EVERY WIN x" + session.Machine.FreeSpinMultiplier, 0, 2.2f,
                                             CelebrationKind.FreeSpinsAwarded);
            }
            else if (context.IsFreeSpin && outcome.FreeSpinsAwarded > 0)
            {
                yield return bigWin.ShowTitle("+" + outcome.FreeSpinsAwarded + " FREE SPINS", null, 0, 1.6f, CelebrationKind.FreeSpinsAwarded);
            }
            // the last free spin: the round's total
            if (context.IsFreeSpin && session.FreeSpins.Remaining == 0)
            {
                yield return bigWin.ShowTitle("FREE SPINS WIN", null, freeSpinRoundWin, 2.5f, CelebrationKind.FreeSpinsTotal);
                freeSpinRoundWin = 0;
            }

            if (context.ContinueAutomatically)
            {
                yield return new WaitForSeconds(0.35f);
                flow.RequestSpin();
                yield break;
            }
            console.SetControls(true, false, session.Bet);
        }

        private static IEnumerator After(float seconds, System.Action action)
        {
            yield return new WaitForSeconds(seconds);
            action();
        }
    }
}
