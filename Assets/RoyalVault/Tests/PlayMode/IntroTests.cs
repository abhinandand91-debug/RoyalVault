using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RoyalVault.Unity;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace RoyalVault.Tests
{
    // The first-launch intro, clicked through the way a player would
    public sealed class IntroTests
    {
        private static IEnumerator WaitFor(System.Func<bool> condition, float timeout = 30f)
        {
            float t = 0f;
            while (!condition() && t < timeout) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(condition(), "timed out");
        }

        // Clicks whatever is on top at the button's centre, as the mouse would
        private static void Click(Button button)
        {
            Assert.IsNotNull(EventSystem.current, "no EventSystem in the scene");
            var data = new PointerEventData(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(null, button.transform.position)
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, hits);
            Assert.IsTrue(hits.Count > 0, "nothing under the button takes clicks");
            GameObject handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
            Assert.AreEqual(button.gameObject, handler, "something covers the button");
            ExecuteEvents.Execute(handler, data, ExecuteEvents.pointerClickHandler);
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator EnterTheVaultTakesARealClick()
        {
            PlayerPrefs.DeleteKey("royalvault.story.intro.seen");
            yield return SceneManager.LoadSceneAsync("Boot");
            var intro = Object.FindFirstObjectByType<StoryIntroView>();
            yield return WaitFor(() => intro.IsPlaying);
            for (int i = 0; i < 40 && !intro.ShowsEnter; i++) { intro.Advance(); yield return new WaitForSeconds(0.3f); }
            Assert.IsTrue(intro.ShowsEnter, "the story never reached its last page");

            yield return new WaitForSeconds(0.5f);
            Button enter = intro.GetComponentsInChildren<Button>().First(b => b.name == "Enter");
            Click(enter);
            yield return WaitFor(() => SceneManager.GetActiveScene().name == "Game", 40f);
        }
    }
}
