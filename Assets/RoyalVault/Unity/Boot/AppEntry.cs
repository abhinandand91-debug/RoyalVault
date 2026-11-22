using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RoyalVault.Unity
{
    // Where the application starts (the Boot scene, first in the build)
    public sealed class AppEntry : MonoBehaviour
    {
        [SerializeField]
        private LoadingScreen loadingScreen;
        [SerializeField]
        private string gameScene = "Game";
        [Tooltip("The loading screen stays up at least this long")]
        [SerializeField]
        private float minimumSeconds = 2.6f;

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
            float total = minimumSeconds;
            while (shown < 1f || !loadingScreen.StoryDone)
            {
                elapsed += Time.unscaledDeltaTime;
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
