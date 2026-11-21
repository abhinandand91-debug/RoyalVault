using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using RoyalVault.Core;
using RoyalVault.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RoyalVault.Tests
{
    /// <summary>The win showcase lands every result it promises, and the reels honour every start and stop order.</summary>
    public sealed class ShowcaseTests
    {
        private static IEnumerator WaitFor(System.Func<bool> condition, float timeout = 60f)
        {
            float t = 0f;
            while (!condition() && t < timeout) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(condition(), "timed out");
        }

        private static IEnumerator LoadGame()
        {
            PlayerPrefs.DeleteAll();
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
        }

        [UnityTest, Timeout(900000)]
        public IEnumerator EveryShowcaseSpinLandsItsResult()
        {
            yield return LoadGame();
            var game = Object.FindFirstObjectByType<GameBootstrap>();
            SpinOutcome paid = null;
            game.Flow.SpinPaid += context => { if (!context.IsFreeSpin) paid = context.Outcome; };

            ShowcaseWin[] order = game.ShowcaseOrder;
            game.ShowcaseWins = true;
            var seen = new List<string>();
            foreach (ShowcaseWin expected in order)
            {
                yield return WaitFor(() => game.Flow.State == SpinState.Idle && game.Session.FreeSpins.Remaining == 0
                                           && !Object.FindFirstObjectByType<BigWinView>(FindObjectsInactive.Include).IsShowing);
                Assert.AreEqual(expected, game.Showcase.Armed, "armed for " + expected);
                paid = null;
                Assert.IsTrue(game.Flow.RequestSpin());
                yield return WaitFor(() => paid != null);
                Assert.IsTrue(WinShowcase.Matches(expected, paid, game.Machine), expected + " landed " + paid.Tier);
                seen.Add(expected + "=" + paid.Tier + (paid.JackpotTier >= 0 ? " jackpot " + paid.JackpotTier : "") +
                         (paid.FreeSpinsAwarded > 0 ? " free spins " + paid.FreeSpinsAwarded : ""));
            }
            Debug.Log("[showcase] " + string.Join(", ", seen));
            game.ShowcaseWins = false;
            Assert.IsNull(game.Showcase.Armed);
            PlayerPrefs.DeleteAll();
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator ReelsHonourEveryStartAndStopOrder()
        {
            yield return LoadGame();
            var game = Object.FindFirstObjectByType<GameBootstrap>();
            var machine = Object.FindFirstObjectByType<MachineView>();
            var landed = new List<int>();
            int finished = 0;
            SpinOutcome decided = null;
            game.Flow.SpinStarted += context => decided = context.Outcome;
            machine.ReelStopped += reel => landed.Add(reel);
            machine.AllReelsStopped += () => finished++;

            foreach (ReelOrder start in new[] { ReelOrder.Sequential, ReelOrder.Random, ReelOrder.AllAtOnce })
                foreach (ReelOrder stop in new[] { ReelOrder.Sequential, ReelOrder.Random, ReelOrder.AllAtOnce })
                {
                    machine.StartOrder = start;
                    machine.StopOrder = stop;
                    yield return WaitFor(() => game.Flow.State == SpinState.Idle);
                    landed.Clear();
                    finished = 0;
                    game.ForceNextStops(19, 18, 40, 46, 23);
                    Assert.IsTrue(game.Flow.RequestSpin());
                    yield return WaitFor(() => finished == 1, 20f);
                    Assert.AreEqual(game.Machine.Reels, landed.Count, start + "/" + stop + " landings");
                    CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 3, 4 }, landed, "every reel lands once");
                    if (stop == ReelOrder.Sequential) CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4 }, landed, "left to right");
                    // the reels show exactly what the engine decided, whatever the order
                    for (int r = 0; r < game.Machine.Reels; r++)
                        for (int row = 0; row < game.Machine.Rows; row++)
                            Assert.AreEqual(decided.Grid[r, row], machine.SymbolAt(new GridPosition(r, row)).SymbolIndex,
                                            start + "/" + stop + " cell " + r + "," + row);
                }
            machine.StartOrder = ReelOrder.Sequential;
            machine.StopOrder = ReelOrder.Sequential;
            PlayerPrefs.DeleteAll();
        }
        [UnityTest, Timeout(300000)]
        public IEnumerator AnticipationWaitsOnTheReelsAfterTheSecondSeal()
        {
            yield return LoadGame();
            var game = Object.FindFirstObjectByType<GameBootstrap>();
            var machine = Object.FindFirstObjectByType<MachineView>();
            SoundPlayer sound = game.Sound.Player;
            var landedAt = new Dictionary<int, float>();
            machine.ReelStopped += reel => landedAt[reel] = Time.time;

            // three seals, left to right: the reels after the second seal are anticipated, each held back
            yield return WaitFor(() => game.Flow.State == SpinState.Idle);
            sound.ClearHistory();
            landedAt.Clear();
            game.ForceNextStops(33, 48, 5, 22, 6);
            Assert.IsTrue(game.Flow.RequestSpin());
            yield return WaitFor(() => landedAt.Count == game.Machine.Reels, 30f);
            int scatter = game.Machine.FirstIndexOfKind(SymbolKind.Scatter);
            SpinOutcome outcome = game.Engine.Evaluate(new[] { 33, 48, 5, 22, 6 }, 1);
            int seals = 0, secondSealReel = -1;
            for (int r = 0; r < game.Machine.Reels && secondSealReel < 0; r++)
                for (int row = 0; row < game.Machine.Rows; row++)
                    if (outcome.Grid[r, row] == scatter && ++seals == 2) secondSealReel = r;
            var expected = new List<int>();
            for (int r = secondSealReel + 1; r < game.Machine.Reels; r++) expected.Add(r);
            CollectionAssert.AreEqual(expected, machine.AnticipatedReels, "anticipated after reel " + secondSealReel);
            foreach (int r in expected)
                Assert.Greater(landedAt[r] - landedAt[r - 1], 1.0f, "reel " + r + " is held back");
            Assert.Contains(SoundCue.AnticipationLoop, (System.Collections.ICollection)new List<SoundCue>(sound.History));
            Assert.Contains(SoundCue.AnticipationEnd, (System.Collections.ICollection)new List<SoundCue>(sound.History));
            yield return WaitFor(() => game.Session.FreeSpins.Remaining == 0 && game.Flow.State == SpinState.Idle, 120f);

            // a plain spin is not anticipated, and nothing is anticipated when every reel lands at once
            game.ForceNextStops(19, 18, 40, 46, 23);
            Assert.IsTrue(game.Flow.RequestSpin());
            yield return WaitFor(() => game.Flow.State != SpinState.Spinning);
            Assert.AreEqual(0, machine.AnticipatedReels.Count, "plain spin");
            yield return WaitFor(() => game.Flow.State == SpinState.Idle);
            machine.StopOrder = ReelOrder.AllAtOnce;
            game.ForceNextStops(33, 48, 5, 22, 6);
            Assert.IsTrue(game.Flow.RequestSpin());
            yield return WaitFor(() => game.Flow.State != SpinState.Spinning);
            Assert.AreEqual(0, machine.AnticipatedReels.Count, "all at once");
            machine.StopOrder = ReelOrder.Sequential;
            PlayerPrefs.DeleteAll();
        }
    }
}
