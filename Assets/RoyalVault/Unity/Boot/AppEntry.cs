using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RoyalVault.Unity
{
    /// <summary>
    /// Where the application starts (the Boot scene, first in the build). It sets up the app, shows the loading
    /// screen while the Game scene loads in the background, and switches to the game when it is ready. The bar moves
    /// smoothly and the screen stays up for a short minimum time, so loading never flickers. The first time, the
    /// screen stays up while it tells the story's intro (the player can click through it).
    /// </summary>
    public sealed class AppEntry : MonoBehaviour
    {
        [SerializeField] private LoadingScreen loadingScreen;
        [SerializeField] private string gameScene = "Game";
        [Tooltip("The loading screen stays up at least this long")]
        [SerializeField] private float minimumSeconds = 2.6f;

        public void Setup(LoadingScreen screen, string scene)
        {
            loadingScreen = screen;
            gameScene = scene;
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 1;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        private IEnumerator Start()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(gameScene);
            load.allowSceneActivation = false;

            float elapsed = 0f;
            float shown = 0f;
            // the first time, the story's intro sets the pace; skipping it lets the bar finish quickly
            float total = Mathf.Max(minimumSeconds, loadingScreen.StorySeconds);
            while (shown < 1f || !loadingScreen.StoryDone)
            {
                elapsed += Time.unscaledDeltaTime;
                if (loadingScreen.Skipped) total = Mathf.Min(total, Mathf.Max(minimumSeconds, elapsed + 0.8f));
                // Unity reports 0.9 when a scene is loaded and waiting to be activated
                float loaded = Mathf.Clamp01(load.progress / 0.9f);
                float target = Mathf.Min(loaded, elapsed / total);
                shown = Mathf.MoveTowards(shown, target, Time.unscaledDeltaTime * 0.9f);
                loadingScreen.SetProgress(shown);
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.25f);
            yield return loadingScreen.FadeOut();
            load.allowSceneActivation = true;
        }
    }
}
