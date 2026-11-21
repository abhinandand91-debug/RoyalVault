using System;
using System.Collections;
using System.Collections.Generic;
using RoyalVault.Core;
using UnityEngine;

namespace RoyalVault.Unity
{
    /// <summary>
    /// Shows a paid spin: all winning lines and symbols at once while the win counts up, the big win celebration when
    /// the win is large, and then each winning line in turn until the next spin starts.
    /// </summary>
    public sealed class WinPresenter : MonoBehaviour
    {
        private MachineView machine;
        private ConsoleView console;
        private BigWinView bigWin;
        private JackpotPanelView jackpots;
        private CharacterView[] characters;
        private FlameClashView clash;
        private StoryDirector story;
        private Coroutine routine;

        public void Bind(MachineView machineView, ConsoleView consoleView, BigWinView bigWinView, JackpotPanelView jackpotView,
                         CharacterView[] characterViews, FlameClashView flameClash = null, StoryDirector storyDirector = null)
        {
            story = storyDirector;
            clash = flameClash;
            machine = machineView;
            console = consoleView;
            bigWin = bigWinView;
            jackpots = jackpotView;
            characters = characterViews;
        }

        /// <summary>Presents a paid spin; <paramref name="onShown"/> runs once the player has seen it and may spin again.</summary>
        public void Present(SpinContext context, bool quick, Action onShown)
        {
            Cancel();
            routine = StartCoroutine(PresentRoutine(context, quick, onShown));
        }

        /// <summary>Stops showing the previous win (a new spin is starting).</summary>
        // The chapter told the first time the Major or Grand jackpot is won.
        private static StoryMoment JackpotMoment(int tier) => tier == 1 ? StoryMoment.MajorJackpot : StoryMoment.GrandJackpot;

        public void Cancel()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
            machine.ClearWins();
        }

        private IEnumerator PresentRoutine(SpinContext context, bool quick, Action onShown)
        {
            SpinOutcome outcome = context.Outcome;
            if (!outcome.IsWin)
            {
                console.ClearWin();
                onShown();
                yield break;
            }

            // everything that won, together
            var cells = new List<GridPosition>();
            foreach (LineWin line in outcome.LineWins) cells.AddRange(line.Positions);
            if (outcome.Scatter != null) cells.AddRange(outcome.Scatter.Positions);
            cells.AddRange(outcome.JackpotPositions);
            float together = quick ? 0.9f : 1.6f;
            machine.HighlightCells(cells, together);
            for (int i = 0; i < outcome.LineWins.Count; i++) machine.ShowLine(outcome.LineWins[i].Line, 0, i);
            console.ShowWin(outcome.TotalWin, Mathf.Clamp(outcome.TotalWin / (float)Math.Max(1, outcome.TotalBet) * 0.25f, 0.4f, 2.5f));
            yield return new WaitForSeconds(together);
            machine.HideLines();

            // the celebration for jackpots and big wins
            if (outcome.JackpotTier >= 0 || outcome.Tier >= WinTier.Big)
            {
                // the guardians hurl their flames together and the clash opens the celebration
                if (clash != null && clash.PlayOnBigWins && !quick) yield return clash.Play();
                foreach (CharacterView character in characters) character.Celebrate();
                if (outcome.JackpotTier >= 0) jackpots.Celebrate(outcome.JackpotTier);
                string jackpotName = outcome.JackpotTier >= 0 ? context.Session.Machine.JackpotTiers[outcome.JackpotTier].Name : null;
                yield return bigWin.ShowWin(outcome.Tier, outcome.TotalWin, jackpotName);
                if (story != null && outcome.JackpotTier >= 1) yield return story.TellOnce(JackpotMoment(outcome.JackpotTier));
            }

            onShown();

            // then each line in turn, until the next spin
            if (outcome.LineWins.Count == 0) yield break;
            while (true)
            {
                foreach (LineWin line in outcome.LineWins)
                {
                    machine.HideLines();
                    machine.HighlightCells(line.Positions, 1.5f);
                    machine.ShowLine(line.Line, line.Amount * outcome.Multiplier, 0);
                    yield return new WaitForSeconds(1.5f);
                }
            }
        }
    }
}
