using System.Collections;
using GameKit;
using NUnit.Framework;
using PrimeTween;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace RoyalVault.Tests
{
    // GameKit's motion module: presets play to their end values, tween groups stop what they started, and the attention
    // highlight leaves its control as it found it
    public sealed class MotionTests
    {
        private GameObject root;

        [SetUp]
        public void Create()
        {
            root = new GameObject("Motion Test", typeof(RectTransform), typeof(Canvas));
        }

        [TearDown]
        public void Destroy()
        {
            Object.Destroy(root);
        }

        private (RectTransform rect, CanvasGroup group) Panel(Vector2 rest)
        {
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasGroup));
            panel.transform.SetParent(root.transform, false);
            var rect = (RectTransform)panel.transform;
            rect.anchoredPosition = rest;
            return (rect, panel.GetComponent<CanvasGroup>());
        }

        [Test]
        public void APresetLastsUntilItsLatestStepEnds()
        {
            MotionPreset preset = MotionPreset.Create(MotionPreset.StepOf(MotionChannel.Fade, 0f, 1f, 0.3f, Ease.OutCubic),
                                                      MotionPreset.StepOf(MotionChannel.Scale, 0.5f, 1f, 0.2f, Ease.OutBack, 0.25f));
            Assert.AreEqual(0.45f, preset.Duration, 0.0001f);
            Object.Destroy(preset);
        }

        [UnityTest]
        public IEnumerator APresetStartsFromItsFirstValuesAndEndsOnItsLast()
        {
            (RectTransform rect, CanvasGroup group) = Panel(new Vector2(100f, 50f));
            MotionPreset entry = MotionPreset.Create(MotionPreset.StepOf(MotionChannel.Fade, 0f, 1f, 0.2f, Ease.OutCubic),
                                                     MotionPreset.StepOf(MotionChannel.Scale, 0.6f, 1f, 0.2f, Ease.OutBack),
                                                     MotionPreset.StepOf(MotionChannel.MoveY, -40f, 0f, 0.2f, Ease.OutCubic),
                                                     MotionPreset.StepOf(MotionChannel.Rotate, 10f, 0f, 0.2f, Ease.OutCubic));
            Sequence play = UiMotion.Play(entry, new MotionTarget(rect, group));
            yield return null;
            Assert.Less(group.alpha, 0.9f, "fading in");
            yield return play.ToYieldInstruction();

            Assert.AreEqual(1f, group.alpha, 0.001f);
            Assert.AreEqual(1f, rect.localScale.x, 0.001f);
            Assert.Less(Vector2.Distance(new Vector2(100f, 50f), rect.anchoredPosition), 0.01f, "moves are measured from where it rests");
            Assert.AreEqual(0f, Mathf.DeltaAngle(rect.localEulerAngles.z, 0f), 0.01f);
            Object.Destroy(entry);
        }

        [UnityTest]
        public IEnumerator AGroupStopsEveryTweenItStartedEvenEndlessOnes()
        {
            (RectTransform rect, CanvasGroup group) = Panel(Vector2.zero);
            var tweens = new TweenGroup();
            tweens.Add(UiMotion.Spin(rect, 90f));
            tweens.Add(UiMotion.Breathe(rect));
            tweens.Add(UiMotion.FadeIn(group, 5f));
            yield return null;
            Assert.IsTrue(tweens.IsPlaying);

            tweens.StopAll();
            Assert.IsFalse(tweens.IsPlaying);
            float alpha = group.alpha;
            Quaternion turn = rect.localRotation;
            yield return null;
            yield return null;
            Assert.AreEqual(alpha, group.alpha, "the fade stopped where it was");
            Assert.AreEqual(turn, rect.localRotation, "the spin stopped where it was");
        }

        [UnityTest]
        public IEnumerator FinishedTweensAreDroppedSoAGroupNeverGrows()
        {
            (RectTransform rect, _) = Panel(Vector2.zero);
            var tweens = new TweenGroup(2);
            for (int i = 0; i < 20; i++)
            {
                yield return tweens.Add(UiMotion.Punch(rect, 1.1f, 0.02f)).ToYieldInstruction();
            }

            Assert.IsFalse(tweens.IsPlaying);
        }

        [UnityTest]
        public IEnumerator AnAttentionHighlightLeavesItsControlAsItFoundIt()
        {
            (RectTransform rect, _) = Panel(Vector2.zero);
            var haloObject = new GameObject("Halo", typeof(RectTransform), typeof(Image));
            haloObject.transform.SetParent(root.transform, false);
            Image halo = haloObject.GetComponent<Image>();
            var highlight = root.AddComponent<AttentionHighlight>();
            highlight.Setup(rect, halo, null);

            highlight.Begin();
            yield return new WaitForSeconds(0.7f);
            Assert.AreNotEqual(Vector3.one, rect.localScale, "breathing");
            Assert.Greater(halo.color.a, 0f, "the halo glows");

            highlight.End();
            yield return null;
            Assert.AreEqual(Vector3.one, rect.localScale);
            Assert.AreEqual(0f, halo.color.a);
        }
    }
}
