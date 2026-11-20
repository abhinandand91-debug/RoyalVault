using System.Collections;
using System.IO;
using NUnit.Framework;
using RoyalVault.Core;
using RoyalVault.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RoyalVault.Tests
{
    /// <summary>
    /// Opt-in visual check of every screen: set ROYALVAULT_SHOTS to a folder and run the PlayMode tests with graphics,
    /// e.g. Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter CaptureScreens.
    /// It boots the game for real, then forces known reel stops for each kind of win.
    /// </summary>
    public sealed class ScreenshotTests
    {
        private static string folder;
        private static RenderTexture target;

        /// <summary>Batch mode has no game view: draw the overlay canvases through the camera into a texture.</summary>
        private static void UseTarget(int width, int height)
        {
            target = new RenderTexture(width, height, 24);
            Camera camera = Camera.main;
            camera.targetTexture = target;
            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!canvas.isRootCanvas) continue;
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
            while (!condition() && t < timeout) { t += Time.deltaTime; yield return null; }
        }

        private static IEnumerator SpinTo(GameBootstrap game, int[] stops)
        {
            yield return WaitFor(() => game.Flow.State == SpinState.Idle);
            game.ForceNextStops(stops);
            Assert.IsTrue(game.Flow.RequestSpin());
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator CaptureScreens()
        {
            folder = System.Environment.GetEnvironmentVariable("ROYALVAULT_SHOTS");
            Assume.That(folder, Is.Not.Null.And.Not.Empty);
            Directory.CreateDirectory(folder);
            PlayerPrefs.DeleteAll();

            // boot: the loading screen part-way
            yield return SceneManager.LoadSceneAsync("Boot");
            UseTarget(1920, 1080);
            yield return new WaitForSeconds(1.3f);
            yield return Shot("01_loading");

            // the game scene arrives on its own
            yield return WaitFor(() => SceneManager.GetActiveScene().name == "Game");
            yield return null;
            UseTarget(1920, 1080);
            var game = Object.FindFirstObjectByType<GameBootstrap>();
            yield return new WaitForSeconds(0.5f);
            yield return Shot("02_idle");

            // a spin in motion, then a line win being shown
            yield return SpinTo(game, new[] { 19, 18, 40, 46, 23 });
            yield return new WaitForSeconds(0.7f);
            yield return Shot("03_spinning");
            yield return WaitFor(() => game.Flow.State != SpinState.Spinning);
            yield return new WaitForSeconds(0.5f);
            yield return Shot("04_win_lines");
            yield return new WaitForSeconds(2.2f);
            yield return Shot("05_win_cycle");

            // a big win and a mega win
            yield return SpinTo(game, new[] { 33, 13, 48, 1, 5 });
            yield return WaitFor(() => Object.FindFirstObjectByType<BigWinView>(FindObjectsInactive.Include).IsShowing);
            yield return new WaitForSeconds(2.2f);
            yield return Shot("06_big_win");
            yield return WaitFor(() => game.Flow.State == SpinState.Idle, 30f);
            yield return SpinTo(game, new[] { 43, 9, 48, 5, 25 });
            yield return WaitFor(() => Object.FindFirstObjectByType<BigWinView>(FindObjectsInactive.Include).IsShowing);
            yield return new WaitForSeconds(2.5f);
            yield return Shot("07_mega_win");
            yield return WaitFor(() => game.Flow.State == SpinState.Idle, 30f);

            // the jackpot
            yield return SpinTo(game, new[] { 12, 27, 3, 7, 46 });
            yield return WaitFor(() => Object.FindFirstObjectByType<BigWinView>(FindObjectsInactive.Include).IsShowing);
            yield return new WaitForSeconds(2.5f);
            yield return Shot("08_jackpot");
            yield return WaitFor(() => game.Flow.State == SpinState.Idle, 30f);

            // free spins: the award card, then a free spin with the counter in the console
            yield return SpinTo(game, new[] { 33, 48, 5, 22, 6 });
            yield return WaitFor(() => Object.FindFirstObjectByType<BigWinView>(FindObjectsInactive.Include).IsShowing);
            yield return new WaitForSeconds(0.9f);
            yield return Shot("09_free_spins_awarded");
            yield return WaitFor(() => game.Session.FreeSpins.Played > 0, 30f);
            yield return WaitFor(() => game.Flow.State != SpinState.Spinning, 30f);
            yield return new WaitForSeconds(0.4f);
            yield return Shot("10_free_spin");
            // let the round finish quickly
            var celebration = Object.FindFirstObjectByType<BigWinView>(FindObjectsInactive.Include);
            while (game.Session.FreeSpins.Remaining > 0 || game.Flow.State != SpinState.Idle || celebration.IsShowing) yield return null;
            yield return new WaitForSeconds(0.5f);

            // the pay table
            Object.FindFirstObjectByType<PaytableView>(FindObjectsInactive.Include).Open();
            yield return new WaitForSeconds(0.5f);
            yield return Shot("11_paytable");
            Object.FindFirstObjectByType<PaytableView>(FindObjectsInactive.Include).Close();
            yield return new WaitForSeconds(0.4f);

            // an ultra-wide screen
            UseTarget(2560, 1080);
            yield return new WaitForSeconds(0.3f);
            yield return Shot("12_ultrawide");

            // the same prefab with the 3 x 3 machine
            yield return SceneManager.LoadSceneAsync("Game_Classic3x3");
            yield return null;
            UseTarget(1920, 1080);
            yield return new WaitForSeconds(0.6f);
            yield return Shot("13_classic_3x3");
            PlayerPrefs.DeleteAll();
        }
    }
}
