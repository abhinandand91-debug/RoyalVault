using System.Collections;
using System.IO;
using GameKit;
using NUnit.Framework;
using RoyalVault.Unity;
using SlotKit;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine;

namespace RoyalVault.Tests
{
    // Captures every screen when ROYALVAULT_SHOTS names a folder
    public sealed class ScreenshotTests
    {
        private static string folder;
        private static RenderTexture target;

        // Batch mode has no game view: draw the overlay canvases through the camera into a texture
        private static void UseTarget(int width, int height)
        {
            target = new RenderTexture(width, height, 24);
            Camera camera = Camera.main;
            camera.targetTexture = target;
            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!canvas.isRootCanvas)
                {
                    continue;
                }

                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 5f;
            }
            Canvas.ForceUpdateCanvases();
        }

        private static IEnumerator Shot(string name)
        {
            yield return null;
            Camera.main.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), texture.EncodeToPNG());
            Object.Destroy(texture);
            Debug.Log("[shots] saved " + name);
        }

        private static IEnumerator WaitFor(System.Func<bool> condition, float timeout = 20f)
        {
            float t = 0f;
            while (!condition() && t < timeout)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        // Taps through the story until the condition holds
        private static IEnumerator AdvanceUntil(StoryIntroView intro, System.Func<bool> condition)
        {
            for (int i = 0; i < 40 && !condition(); i++)
            {
                intro.Advance();
                yield return new WaitForSeconds(0.35f);
            }
            Assert.IsTrue(condition(), "story did not advance");
        }

        private static IEnumerator SpinTo(GameBootstrap game, int[] stops)
        {
            yield return WaitFor(() => game.Flow.State == SpinState.Idle);
            game.ForceNextStops(stops);
            Assert.IsTrue(game.Flow.RequestSpin());
        }

        private static GameBootstrap game;

        private static bool Celebrating() => Object.FindFirstObjectByType<BigWinView>(FindObjectsInactive.Include).IsShowing;

        [UnityTest, Timeout(600000)]
        public IEnumerator CaptureScreens()
        {
            folder = System.Environment.GetEnvironmentVariable("ROYALVAULT_SHOTS");
            Assume.That(folder, Is.Not.Null.And.Not.Empty);
            Directory.CreateDirectory(folder);
            PlayerPrefs.DeleteAll();
            yield return Intro();
            yield return FirstVisit();
            yield return LineWin();
            yield return BigWins();
            yield return Jackpot();
            yield return FreeSpins();
            yield return Paytable();
            yield return GuardiansAndSymbols();
            yield return Showcase();
            yield return OtherLayouts();
            PlayerPrefs.DeleteAll();
        }

        // First launch: the story told beat by beat, then Enter the Vault
        private static IEnumerator Intro()
        {
            yield return SceneManager.LoadSceneAsync("Boot");
            UseTarget(1920, 1080);
            var intro = Object.FindFirstObjectByType<StoryIntroView>();
            yield return WaitFor(() => intro.IsPlaying);
            yield return new WaitForSeconds(2.4f);
            yield return Shot("01_loading");
            yield return AdvanceUntil(intro, () => intro.Page == 1);
            yield return new WaitForSeconds(2.4f);
            yield return Shot("00_story_scene_2");
            yield return AdvanceUntil(intro, () => intro.ShowsEnter);
            yield return new WaitForSeconds(0.6f);
            yield return Shot("01b_story_enter");
            intro.Enter();
            yield return WaitFor(() => SceneManager.GetActiveScene().name == "Game");
            yield return null;
            UseTarget(1920, 1080);
            game = Object.FindFirstObjectByType<GameBootstrap>();
        }

        // Chapter III over the game, then the hints
        private static IEnumerator FirstVisit()
        {
            var chapter = Object.FindFirstObjectByType<ChapterCardView>();
            yield return WaitFor(() => chapter.IsShowing);
            yield return new WaitForSeconds(2.6f);
            yield return Shot("25_chapter_card");
            var coach = Object.FindFirstObjectByType<CoachMarkView>();
            for (int i = 0; i < 12 && !coach.IsShowing; i++)
            {
                chapter.Advance();
                yield return new WaitForSeconds(0.3f);
            }
            yield return new WaitForSeconds(0.5f);
            yield return Shot("26_coach_mark");
            game.Story.Mute();
            yield return new WaitForSeconds(0.6f);
            yield return Shot("02_idle");
        }

        private static IEnumerator LineWin()
        {
            yield return SpinTo(game, new[] { 19, 18, 40, 46, 23 });
            yield return new WaitForSeconds(0.7f);
            yield return Shot("03_spinning");
            yield return WaitFor(() => game.Flow.State != SpinState.Spinning);
            yield return new WaitForSeconds(0.5f);
            yield return Shot("04_win_lines");
            yield return new WaitForSeconds(2.2f);
            yield return Shot("05_win_cycle");
        }

        // The flame clash opens a big win; then a mega win
        private static IEnumerator BigWins()
        {
            var clash = Object.FindFirstObjectByType<FlameClashView>();
            bool thrown = false, clashed = false;
            clash.Thrown += () => thrown = true;
            clash.Clashed += () => clashed = true;
            yield return SpinTo(game, new[] { 33, 13, 48, 1, 5 });
            yield return WaitFor(() => thrown);
            yield return new WaitForSeconds(0.3f);
            yield return Shot("22_flame_throw");
            yield return WaitFor(() => clashed);
            yield return new WaitForSeconds(0.1f);
            yield return Shot("23_flame_clash");
            yield return Celebration(null, "06_big_win", 2.2f);
            yield return Celebration(new[] { 43, 9, 48, 5, 25 }, "07_mega_win", 2.5f);
        }

        private static IEnumerator Jackpot() => Celebration(new[] { 12, 27, 3, 7, 46 }, "08_jackpot", 2.5f);

        // Spins to stops (unless already spun), shoots the celebration and waits for the machine to settle
        private static IEnumerator Celebration(int[] stops, string shot, float delay)
        {
            if (stops != null)
            {
                yield return SpinTo(game, stops);
            }

            yield return WaitFor(Celebrating);
            yield return new WaitForSeconds(delay);
            yield return Shot(shot);
            yield return WaitFor(() => game.Flow.State == SpinState.Idle, 30f);
        }

        // Anticipation, the seal cracking and breaking, the award and a free spin
        private static IEnumerator FreeSpins()
        {
            bool anticipating = false;
            Object.FindFirstObjectByType<MachineView>().AnticipationStarted += reel => anticipating = true;
            yield return SpinTo(game, new[] { 33, 48, 5, 22, 6 });
            yield return ShotWhen(() => anticipating, 0.7f, "21_anticipation");
            var seal = Object.FindFirstObjectByType<SealMeterView>();
            yield return ShotWhen(() => seal.Count >= 2, 0.25f, "16_seal_cracking");
            yield return ShotWhen(() => seal.IsOpen, 0.25f, "24_seal_break");
            yield return ShotWhen(Celebrating, 0.9f, "09_free_spins_awarded");
            yield return WaitFor(() => game.Session.FreeSpins.Played > 0, 30f);
            yield return ShotWhen(() => game.Flow.State != SpinState.Spinning, 0.4f, "10_free_spin");
            while (game.Session.FreeSpins.Remaining > 0 || game.Flow.State != SpinState.Idle || Celebrating())
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.5f);
        }

        private static IEnumerator ShotWhen(System.Func<bool> condition, float delay, string shot)
        {
            yield return WaitFor(condition, 30f);
            yield return new WaitForSeconds(delay);
            yield return Shot(shot);
        }

        private static IEnumerator Paytable()
        {
            var paytable = Object.FindFirstObjectByType<PaytableView>(FindObjectsInactive.Include);
            paytable.Open();
            yield return new WaitForSeconds(0.5f);
            yield return Shot("11_paytable");
            paytable.Close();
            yield return new WaitForSeconds(0.4f);
        }

        // The guardians' celebration on its own, then every symbol celebrating at once
        private static IEnumerator GuardiansAndSymbols()
        {
            foreach (CharacterView guardian in Object.FindObjectsByType<CharacterView>(FindObjectsSortMode.None))
            {
                guardian.Celebrate();
            }

            yield return new WaitForSeconds(0.45f);
            yield return Shot("15_guardians_act");
            yield return new WaitForSeconds(0.6f);
            yield return Shot("14_guardians_celebrate");
            yield return new WaitForSeconds(2f);

            yield return SpinTo(game, new[] { 33, 13, 48, 1, 5 });
            yield return WaitFor(() => game.Flow.State == SpinState.Idle, 40f);
            var machineView = Object.FindFirstObjectByType<MachineView>();
            var all = new System.Collections.Generic.List<GridPosition>();
            for (int r = 0; r < game.Machine.Reels; r++)
            {
                for (int row = 0; row < game.Machine.Rows; row++)
                {
                    all.Add(new GridPosition(r, row));
                }
            }

            machineView.HighlightCells(all, 6f);
            yield return new WaitForSeconds(0.5f);
            yield return Shot("17_symbols_celebrate");
            yield return new WaitForSeconds(0.55f);
            yield return Shot("18_symbols_shine");
            machineView.ClearWins();
        }

        // The rarest celebrations, landed by the showcase
        private static IEnumerator Showcase()
        {
            game.ShowcaseOrder = new[] { ShowcaseWin.EpicWin, ShowcaseWin.GrandJackpot };
            game.ShowcaseWins = true;
            foreach (string name in new[] { "19_epic_win", "20_grand_jackpot" })
            {
                yield return WaitFor(() => game.Flow.State == SpinState.Idle, 40f);
                Assert.IsTrue(game.Flow.RequestSpin());
                yield return WaitFor(Celebrating, 30f);
                yield return new WaitForSeconds(2.5f);
                yield return Shot(name);
                yield return WaitFor(() => game.Flow.State == SpinState.Idle, 40f);
            }
            game.ShowcaseWins = false;
        }

        // An ultra-wide screen, and the same prefab with the 3 x 3 machine
        private static IEnumerator OtherLayouts()
        {
            UseTarget(2560, 1080);
            yield return new WaitForSeconds(0.3f);
            yield return Shot("12_ultrawide");
            yield return SceneManager.LoadSceneAsync("Game_Classic3x3");
            yield return null;
            UseTarget(1920, 1080);
            yield return new WaitForSeconds(0.6f);
            yield return Shot("13_classic_3x3");
        }
    }
}
