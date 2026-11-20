using System.Collections;
using RoyalVault.Core;
using UnityEngine;

namespace RoyalVault.Unity
{
    /// <summary>
    /// The glue between the player, the spin flow and the views: it turns console presses into flow requests and
    /// flow events into reel spins, win shows and the next automatic spin. It holds no rules of its own.
    /// </summary>
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
        private bool soundOn = true;

        /// <summary>Spins played automatically after the player starts auto play.</summary>
        public int AutoPlaySpins = 25;

        public void Bind(GameSession gameSession, SpinFlow spinFlow, MachineView machineView, ConsoleView consoleView, WinPresenter winPresenter,
                         BigWinView bigWinView, PaytableView paytableView, MessageToast messageToast)
        {
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
            console.SoundPressed += () => { soundOn = !soundOn; AudioListener.volume = soundOn ? 1f : 0f; console.SetSoundOn(soundOn); };

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
            console.SetSoundOn(true);
            console.ClearWin();
            // a saved free spin round carries on
            if (session.FreeSpins.Remaining > 0) StartCoroutine(After(1.2f, () => flow.RequestSpin()));
        }

        private void OnSpinPressed()
        {
            if (paytable.IsOpen || bigWin.IsShowing) return;
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
                freeSpinRoundWin = 0;
                yield return bigWin.ShowTitle(outcome.FreeSpinsAwarded + " FREE SPINS", "EVERY WIN x" + session.Machine.FreeSpinMultiplier, 0, 2.2f);
            }
            else if (context.IsFreeSpin && outcome.FreeSpinsAwarded > 0)
            {
                yield return bigWin.ShowTitle("+" + outcome.FreeSpinsAwarded + " FREE SPINS", null, 0, 1.6f);
            }
            // the last free spin: the round's total
            if (context.IsFreeSpin && session.FreeSpins.Remaining == 0)
            {
                yield return bigWin.ShowTitle("FREE SPINS WIN", null, freeSpinRoundWin, 2.5f);
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
