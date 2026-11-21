using System.Collections.Generic;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoyalVault.Unity;
using SlotKit;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine;

namespace RoyalVault.Tests
{
    // The win showcase lands every result it promises, and the reels honour every start and stop order
    public sealed class ShowcaseTests
    {
        private static IEnumerator WaitFor(System.Func<bool> condition, float timeout = 60f)
        {
            float t = 0f;
            while (!condition() && t < timeout)
            {
                t += Time.deltaTime;
                yield return null;
            }
            Assert.IsTrue(condition(), "timed out");
        }

        private static IEnumerator LoadGame()
        {
            PlayerPrefs.DeleteAll();
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            Object.FindFirstObjectByType<GameBootstrap>().Story.Mute();
        }

        [UnityTest, Timeout(900000)]
        public IEnumerator EveryShowcaseSpinLandsItsResult()
        {
            yield return LoadGame();
            var game = Object.FindFirstObjectByType<GameBootstrap>();
            SpinOutcome paid = null;
            game.Flow.SpinPaid += context =>
            {
                if (!context.IsFreeSpin)
                {
                    paid = context.Outcome;
                }
            };

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
            {
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
                    if (stop == ReelOrder.Sequential)
                    {
                        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4 }, landed, "left to right");
                    }
                    // the reels show exactly what the engine decided, whatever the order
                    for (int r = 0; r < game.Machine.Reels; r++)
                    {
                        for (int row = 0; row < game.Machine.Rows; row++)
                        {
                            Assert.AreEqual(decided.Grid[r, row], machine.SymbolAt(new GridPosition(r, row)).SymbolIndex,
                                            start + "/" + stop + " cell " + r + "," + row);
                        }
                    }
                }
            }

            machine.StartOrder = ReelOrder.Sequential;
            machine.StopOrder = ReelOrder.Sequential;
            PlayerPrefs.DeleteAll();
        }

        private static readonly int[] ThreeSeals = { 33, 48, 5, 22, 6 };

        [UnityTest, Timeout(300000)]
        public IEnumerator AnticipationWaitsOnTheReelsAfterTheSecondSeal()
        {
            yield return LoadGame();
            var game = Object.FindFirstObjectByType<GameBootstrap>();
            var machine = Object.FindFirstObjectByType<MachineView>();
            SoundPlayer sound = game.Sound.Player;
            var landedAt = new Dictionary<int, float>();
            machine.ReelStopped += reel => landedAt[reel] = Time.time;

            yield return WaitFor(() => game.Flow.State == SpinState.Idle);
            sound.ClearHistory();
            game.ForceNextStops(ThreeSeals);
            Assert.IsTrue(game.Flow.RequestSpin());
            yield return WaitFor(() => landedAt.Count == game.Machine.Reels, 30f);

            // the reels after the second seal are anticipated, each held back
            int second = SecondSealReel(game);
            List<int> expected = Enumerable.Range(second + 1, game.Machine.Reels - second - 1).ToList();
            CollectionAssert.AreEqual(expected, machine.AnticipatedReels, "anticipated after reel " + second);
            foreach (int r in expected)
            {
                Assert.Greater(landedAt[r] - landedAt[r - 1], 1.0f, "reel " + r + " is held back");
            }

            CollectionAssert.Contains(sound.History, SoundCue.AnticipationLoop);
            CollectionAssert.Contains(sound.History, SoundCue.AnticipationEnd);
            yield return WaitFor(() => game.Session.FreeSpins.Remaining == 0 && game.Flow.State == SpinState.Idle, 120f);

            yield return NothingAnticipated(game, machine, new[] { 19, 18, 40, 46, 23 }, "plain spin");
            machine.StopOrder = ReelOrder.AllAtOnce;
            yield return NothingAnticipated(game, machine, ThreeSeals, "all at once");
            machine.StopOrder = ReelOrder.Sequential;
            PlayerPrefs.DeleteAll();
        }

        private static int SecondSealReel(GameBootstrap game)
        {
            int scatter = game.Machine.FirstIndexOfKind(SymbolKind.Scatter);
            SpinOutcome outcome = game.Engine.Evaluate(ThreeSeals, 1);
            int seals = 0;
            for (int r = 0; r < game.Machine.Reels; r++)
            {
                for (int row = 0; row < game.Machine.Rows; row++)
                {
                    if (outcome.Grid[r, row] == scatter && ++seals == 2)
                    {
                        return r;
                    }
                }
            }

            return -1;
        }

        private static IEnumerator NothingAnticipated(GameBootstrap game, MachineView machine, int[] stops, string label)
        {
            yield return WaitFor(() => game.Flow.State == SpinState.Idle);
            game.ForceNextStops(stops);
            Assert.IsTrue(game.Flow.RequestSpin());
            yield return WaitFor(() => game.Flow.State != SpinState.Spinning);
            Assert.AreEqual(0, machine.AnticipatedReels.Count, label);
        }

        // The first stops (searched in order) whose grid wins nothing
        public static int[] LosingStops(GameBootstrap game)
        {
            for (int seed = 0; seed < 5000; seed++)
            {
                var stops = new int[game.Machine.Reels];
                for (int reel = 0; reel < stops.Length; reel++)
                {
                    stops[reel] = (seed * (7 + reel * 3)) % game.Machine.Strips[reel].Length;
                }

                if (!game.Engine.Evaluate(stops, 1).IsWin)
                {
                    return stops;
                }
            }

            Assert.Fail("no losing stops found");
            return null;
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator GuardiansFireBurnsWildsIntoTheLandedReels()
        {
            yield return LoadGame();
            var game = Object.FindFirstObjectByType<GameBootstrap>();
            var machine = Object.FindFirstObjectByType<MachineView>();
            SpinOutcome paid = null;
            game.Flow.SpinPaid += context =>
            {
                paid = context.Outcome;
            };

            yield return WaitFor(() => game.Flow.State == SpinState.Idle);
            game.ForceNextStops(LosingStops(game));
            game.ForceGuardianFire();
            Assert.IsTrue(game.Flow.RequestSpin());
            yield return WaitFor(() => paid != null, 30f);

            int wild = game.Machine.FirstIndexOfKind(SymbolKind.Wild);
            Assert.That(paid.AddedWilds.Count, Is.InRange(game.Machine.RandomWilds.MinWilds, game.Machine.RandomWilds.MaxWilds));
            foreach (GridPosition cell in paid.AddedWilds)
            {
                Assert.AreEqual(wild, machine.SymbolAt(cell).SymbolIndex, "cell " + cell + " shows the wild");
            }

            PlayerPrefs.DeleteAll();
        }

        // The first stops (searched in order) where symbol shows on at least three reels
        public static int[] RelicStops(GameBootstrap game, int symbol)
        {
            for (int seed = 0; seed < 20000; seed++)
            {
                var stops = new int[game.Machine.Reels];
                for (int reel = 0; reel < stops.Length; reel++)
                {
                    stops[reel] = (seed * (11 + reel * 5) + reel) % game.Machine.Strips[reel].Length;
                }

                if (ReelsHolding(game.Engine.Evaluate(stops, 1).Grid, symbol) >= 3)
                {
                    return stops;
                }
            }

            Assert.Fail("no stops with the relic on three reels");
            return null;
        }

        private static int ReelsHolding(SymbolGrid grid, int symbol)
        {
            int reels = 0;
            for (int reel = 0; reel < grid.Reels; reel++)
            {
                for (int row = 0; row < grid.Rows; row++)
                {
                    if (grid[reel, row] == symbol)
                    {
                        reels++;
                        break;
                    }
                }
            }

            return reels;
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator TheSealedRelicCoversItsReelsDuringFreeSpins()
        {
            yield return LoadGame();
            var game = Object.FindFirstObjectByType<GameBootstrap>();
            var machine = Object.FindFirstObjectByType<MachineView>();
            int relic = game.Machine.SymbolIndex("ruby");
            SpinOutcome expanded = null;
            game.Flow.SpinPaid += context =>
            {
                if (context.IsFreeSpin && expanded == null)
                {
                    expanded = context.Outcome;
                }
                else if (!context.IsFreeSpin)
                {
                    game.ForceNextStops(RelicStops(game, relic));
                }
            };

            yield return WaitFor(() => game.Flow.State == SpinState.Idle);
            game.ForceRelic("ruby");
            game.ForceNextStops(ThreeSeals);
            Assert.IsTrue(game.Flow.RequestSpin());
            yield return WaitFor(() => expanded != null, 60f);

            Assert.AreEqual(relic, game.Session.FreeSpins.Relic);
            Assert.IsNotNull(expanded.Expanded, "the relic pays");
            yield return WaitFor(() => game.Flow.State != SpinState.Spinning);
            foreach (int reel in expanded.Expanded.Reels)
            {
                for (int row = 0; row < game.Machine.Rows; row++)
                {
                    Assert.AreEqual(relic, machine.SymbolAt(new GridPosition(reel, row)).SymbolIndex, "reel " + reel + " row " + row);
                }
            }

            yield return WaitFor(() => game.Session.FreeSpins.Remaining == 0 && game.Flow.State == SpinState.Idle, 180f);
            PlayerPrefs.DeleteAll();
        }

        private static Button Named(string name)
        {
            foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (button.name == name)
                {
                    return button;
                }
            }

            Assert.Fail("no button " + name);
            return null;
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator RubysLastHandGamblesAPlainWin()
        {
            yield return LoadGame();
            var game = Object.FindFirstObjectByType<GameBootstrap>();
            var view = Object.FindFirstObjectByType<GambleView>(FindObjectsInactive.Include);
            long win = 0;
            game.Flow.SpinPaid += context =>
            {
                win = context.Outcome.TotalWin;
            };

            yield return WaitFor(() => game.Flow.State == SpinState.Idle);
            game.ForceNextStops(19, 18, 40, 46, 23);
            Assert.IsTrue(game.Flow.RequestSpin());
            Button offer = Named("Gamble");
            yield return WaitFor(() => offer.gameObject.activeInHierarchy, 30f);
            Assert.Greater(win, 0);
            long before = game.Session.Wallet.Balance;

            offer.onClick.Invoke();
            yield return WaitFor(() => view.IsOpen && Named("RED  x2").interactable, 10f);
            Named("RED  x2").onClick.Invoke();
            yield return WaitFor(() => !game.Gamble.Active || game.Gamble.Rounds > 0, 10f);
            yield return new WaitForSeconds(0.6f);
            Card card = game.Gamble.History[0];
            long expected = card.IsRed ? before + win : before - win;
            Assert.AreEqual(expected, game.Session.Wallet.Balance, "the stake is settled on the wallet");
            if (game.Gamble.Active)
            {
                Named("Collect").onClick.Invoke();
            }

            yield return WaitFor(() => !view.IsOpen, 10f);
            Assert.IsTrue(game.Flow.RequestSpin(), "spins resume after the gamble");
            PlayerPrefs.DeleteAll();
        }
    }
}
