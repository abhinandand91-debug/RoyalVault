using System;
using System.Collections;
using System.Collections.Generic;
using GameKit;
using PrimeTween;
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
        private StoryDirector story;
        private readonly WaitForSeconds autoSpinPause = new WaitForSeconds(0.35f);
        private readonly List<ILandingStep> landingSteps = new List<ILandingStep>();
        private SpinContext landing;
        private GambleSession gamble;
        private GambleView gambleView;
        private readonly List<IPresentationStep> presentationSteps = new List<IPresentationStep>();
        private long gambleWin;
        private long gambleBet;
        private Action onReelsLanded;

        // Spins played automatically after the player starts auto play
        public int AutoPlaySpins = 25;

        public void Bind(GameSession gameSession, SpinFlow spinFlow, MachineView machineView, ConsoleView consoleView, WinPresenter winPresenter,
                         BigWinView bigWinView, PaytableView paytableView, MessageToast messageToast, StoryDirector storyDirector = null)
        {
            story = storyDirector;
            onReelsLanded = OnReelsLanded;
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
            console.BetUpPressed += () =>
            {
                if (flow.State == SpinState.Idle)
                {
                    session.Bet.Increase();
                }
            };
            console.BetDownPressed += () =>
            {
                if (flow.State == SpinState.Idle)
                {
                    session.Bet.Decrease();
                }
            };
            console.AutoPressed += OnAutoPressed;
            console.MenuPressed += () =>
            {
                if (flow.State == SpinState.Idle)
                {
                    paytable.Open();
                }
            };

            session.Wallet.Changed += value => console.SetBalance(value, 0.6f);
            session.Bet.Changed += bet =>
            {
                console.SetBet(bet);
                session.Save();
            };
            session.FreeSpins.Changed += state => console.SetFreeSpins(state, session.Machine.FreeSpinMultiplier);
            autoPlay.Changed += a => console.SetAuto(a.Active, a.Remaining);

            flow.SpinStarted += OnSpinStarted;
            flow.SpinRejected += context =>
            {
                autoPlay.Stop();
                toast.Show(context.Rejection);
            };
            flow.SpinPaid += OnSpinPaid;
            flow.SpinFinished += OnSpinFinished;

            console.SetBalance(session.Wallet.Balance);
            console.SetBet(session.Bet);
            console.SetFreeSpins(session.FreeSpins, session.Machine.FreeSpinMultiplier);
            console.SetAuto(false, 0);
            console.ClearWin();
            // a saved free spin round carries on
            if (session.FreeSpins.Remaining > 0)
            {
                Tween.Delay(this, 1.2f, controller => controller.flow.RequestSpin());
            }
            else if (story != null)
            {
                StartCoroutine(story.Welcome());
            }
        }

        private void OnSpinPressed()
        {
            bool gambling = gambleView != null && gambleView.IsOpen;
            if (paytable.IsOpen || bigWin.IsShowing || gambling || (story != null && story.IsTelling))
            {
                return;
            }

            if (flow.State == SpinState.Spinning)
            {
                machine.Hurry();
                return;
            }
            if (flow.State == SpinState.Presenting)
            {
                return;
            }

            flow.RequestSpin();
        }

        private void OnAutoPressed()
        {
            if (autoPlay.Active)
            {
                autoPlay.Stop();
                return;
            }
            autoPlay.Start(AutoPlaySpins);
            if (flow.State == SpinState.Idle)
            {
                flow.RequestSpin();
            }
        }

        private void OnSpinStarted(SpinContext context)
        {
            presenter.Cancel();
            console.OfferGamble(false);
            console.SetControls(false, true, session.Bet);
            if (!context.IsFreeSpin)
            {
                console.ClearWin();
            }

            bool fast = autoPlay.Active || context.IsFreeSpin;
            landing = context;
            machine.Spin(context.Outcome, fast, onReelsLanded);
        }

        // Offers Ruby's Last Hand after a base-game win
        public void SetGamble(GambleSession gambleSession, GambleView view)
        {
            gamble = gambleSession;
            gambleView = view;
            console.GamblePressed += OnGamblePressed;
            session.Bet.Changed += bet => console.OfferGamble(false);
        }

        // What the current free spin round has won, shared with the steps that announce and total it
        public FreeSpinTally Tally { get; } = new FreeSpinTally();

        // Played after each spin's wins, in the order added, when they apply
        public void AddPresentationStep(IPresentationStep step)
        {
            presentationSteps.Add(step);
        }

        // Shown between the reels landing and the wins, in the order added
        public void AddLandingStep(ILandingStep step)
        {
            landingSteps.Add(step);
        }

        private void OnReelsLanded()
        {
            StartCoroutine(PlayLandingSteps(landing));
        }

        private IEnumerator PlayLandingSteps(SpinContext context)
        {
            foreach (ILandingStep step in landingSteps)
            {
                if (step.Wants(context))
                {
                    yield return step.Play(context);
                }
            }

            flow.ReelsStopped();
        }

        private void OnSpinPaid(SpinContext context)
        {
            console.SetControls(false, false, session.Bet);
            if (context.IsFreeSpin)
            {
                Tally.Win += context.Outcome.TotalWin;
            }

            presenter.Present(context, autoPlay.Active || context.IsFreeSpin, flow.PresentationDone);
        }

        private void OnSpinFinished(SpinContext context)
        {
            StartCoroutine(Finish(context));
        }

        // A plain base-game win played by hand may be gambled; free spins, jackpots and auto play are left alone
        private void OfferGamble(SpinContext context)
        {
            SpinOutcome outcome = context.Outcome;
            bool plain = !context.IsFreeSpin && outcome.FreeSpinsAwarded == 0 && outcome.JackpotTier < 0 && outcome.Treasury == null
                         && !autoPlay.Active;
            if (gamble == null || !plain || !gamble.Offers(outcome.TotalWin, outcome.TotalBet))
            {
                return;
            }

            gambleWin = outcome.TotalWin;
            gambleBet = outcome.TotalBet;
            console.OfferGamble(true);
        }

        private void OnGamblePressed()
        {
            if (flow.State != SpinState.Idle || gambleView.IsOpen)
            {
                return;
            }

            console.OfferGamble(false);
            presenter.Cancel();
            StartCoroutine(RunGamble());
        }

        private IEnumerator RunGamble()
        {
            gamble.Start(gambleWin, gambleBet);
            yield return gambleView.Play(gamble);
            console.SetWin(gamble.Stake);
            session.Save();
        }

        // Every presentation step that applies plays in turn; then the next free or auto spin starts, or the player
        // gets the controls back (and the gamble, after a plain win)
        private IEnumerator Finish(SpinContext context)
        {
            foreach (IPresentationStep step in presentationSteps)
            {
                if (step.Applies(context))
                {
                    yield return step.Run(context);
                }
            }

            if (context.ContinueAutomatically)
            {
                yield return autoSpinPause;
                flow.RequestSpin();
                yield break;
            }
            console.SetControls(true, false, session.Bet);
            OfferGamble(context);
        }
    }
}
