using System.Collections.Generic;
using System.Collections;
using System;
using PrimeTween;
using SlotKit;
using UnityEngine;

namespace RoyalVault.Unity
{
    // Shows a paid spin's wins, the big win when it is large, then each line in turn
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
        private readonly List<GridPosition> wonCells = new List<GridPosition>();
        private readonly WaitForSeconds linePause = new WaitForSeconds(1.5f);

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

        // Presents a paid spin; onShown runs once the player has seen it and may spin again
        public void Present(SpinContext context, bool quick, Action onShown)
        {
            Cancel();
            routine = StartCoroutine(PresentRoutine(context, quick, onShown));
        }

        private static StoryMoment JackpotMoment(int tier) => tier == 1 ? StoryMoment.MajorJackpot : StoryMoment.GrandJackpot;

        // Stops showing the previous win
        public void Cancel()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
            }

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
            yield return ShowAllWins(outcome, quick ? 0.9f : 1.6f);
            if (outcome.JackpotTier >= 0 || outcome.Tier >= WinTier.Big)
            {
                yield return Celebrate(context, quick);
            }

            onShown();
            if (outcome.LineWins.Count > 0)
            {
                yield return CycleLines(outcome);
            }
        }

        // Everything that won, together
        private IEnumerator ShowAllWins(SpinOutcome outcome, float seconds)
        {
            List<GridPosition> cells = wonCells;
            cells.Clear();
            foreach (LineWin line in outcome.LineWins)
            {
                cells.AddRange(line.Positions);
            }

            if (outcome.Scatter != null)
            {
                cells.AddRange(outcome.Scatter.Positions);
            }

            cells.AddRange(outcome.JackpotPositions);
            AddRelicCells(outcome, cells);
            machine.HighlightCells(cells, seconds);
            for (int i = 0; i < outcome.LineWins.Count; i++)
            {
                machine.ShowLine(outcome.LineWins[i].Line, 0, i);
            }

            console.ShowWin(outcome.TotalWin, Mathf.Clamp(outcome.TotalWin / (float)Math.Max(1, outcome.TotalBet) * 0.25f, 0.4f, 2.5f));
            yield return Tween.Delay(seconds).ToYieldInstruction();
            machine.HideLines();
        }

        // Every cell of the reels the relic covered
        private void AddRelicCells(SpinOutcome outcome, List<GridPosition> cells)
        {
            if (outcome.Expanded == null)
            {
                return;
            }

            foreach (int reel in outcome.Expanded.Reels)
            {
                for (int row = 0; row < outcome.Grid.Rows; row++)
                {
                    cells.Add(new GridPosition(reel, row));
                }
            }
        }

        // The flame clash, then the big win or jackpot celebration
        private IEnumerator Celebrate(SpinContext context, bool quick)
        {
            SpinOutcome outcome = context.Outcome;
            if (clash != null && clash.PlayOnBigWins && !quick)
            {
                yield return clash.Play();
            }

            foreach (CharacterView character in characters)
            {
                character.Celebrate();
            }

            if (outcome.JackpotTier >= 0)
            {
                jackpots.Celebrate(outcome.JackpotTier);
            }

            string jackpotName = outcome.JackpotTier >= 0 ? context.Session.Machine.JackpotTiers[outcome.JackpotTier].Name : null;
            yield return bigWin.ShowWin(outcome.Tier, outcome.TotalWin, jackpotName);
            if (story != null && outcome.JackpotTier >= 1)
            {
                yield return story.TellOnce(JackpotMoment(outcome.JackpotTier));
            }
        }

        // Each line in turn until the next spin
        private IEnumerator CycleLines(SpinOutcome outcome)
        {
            while (true)
            {
                foreach (LineWin line in outcome.LineWins)
                {
                    machine.HideLines();
                    machine.HighlightCells(line.Positions, 1.5f);
                    machine.ShowLine(line.Line, line.Amount * outcome.Multiplier, 0);
                    yield return linePause;
                }
            }
        }
    }
}
