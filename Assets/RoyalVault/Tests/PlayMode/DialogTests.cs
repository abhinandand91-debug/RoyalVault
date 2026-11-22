using System.Collections;
using System.Collections.Generic;
using GameKit;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace RoyalVault.Tests
{
    // GameKit's dialog manager: one dialog per layer, waiting dialogs open by priority, Replace and Stack behave as named,
    // handles can be waited on, and the shared backdrop follows the topmost dialog that wants it
    public sealed class DialogTests
    {
        // A dialog that closes when asked, or by itself after a while
        private sealed class TestDialog : DialogView
        {
            public float CloseAfter = -1f;

            protected override IEnumerator Content()
            {
                for (float t = 0f; !CloseRequested && (CloseAfter < 0f || t < CloseAfter); t += Time.deltaTime)
                {
                    yield return null;
                }
            }
        }

        private GameObject root;
        private DialogManager manager;
        private Image backdrop;
        private readonly List<string> log = new List<string>();

        [SetUp]
        public void Create()
        {
            log.Clear();
            root = new GameObject("Dialog Test", typeof(RectTransform), typeof(Canvas));
            var backdropObject = new GameObject("Backdrop", typeof(RectTransform), typeof(Image), typeof(Button));
            backdropObject.transform.SetParent(root.transform, false);
            backdrop = backdropObject.GetComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, 0f);
            manager = root.AddComponent<DialogManager>();
            manager.Setup(backdrop, backdropObject.GetComponent<Button>());
            manager.Opened += definition => log.Add("open " + definition.id);
            manager.Closed += definition => log.Add("close " + definition.id);
        }

        [TearDown]
        public void Destroy()
        {
            Object.Destroy(root);
        }

        private TestDialog Dialog(string id, DialogLayer layer, DialogPolicy policy, int priority = 0, float backdropAlpha = 0.6f)
        {
            var holder = new GameObject(id, typeof(RectTransform), typeof(CanvasGroup));
            holder.transform.SetParent(root.transform, false);
            var view = holder.AddComponent<TestDialog>();
            DialogDefinition definition = DialogDefinition.Create(id, layer, policy);
            definition.priority = priority;
            definition.backdropAlpha = backdropAlpha;
            view.SetupDialog(definition, (RectTransform)holder.transform, manager, holder.GetComponent<CanvasGroup>());
            return view;
        }

        private static IEnumerator Frames(int count = 3)
        {
            for (int i = 0; i < count; i++)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator AHandleWaitsUntilItsDialogHasClosed()
        {
            TestDialog popup = Dialog("A", DialogLayer.Popup, DialogPolicy.Queue);
            DialogHandle handle = popup.Present();
            yield return Frames();
            Assert.IsTrue(popup.IsOpen);
            Assert.IsFalse(handle.IsClosed);

            handle.Close();
            yield return handle;
            Assert.IsTrue(handle.IsClosed);
            Assert.IsFalse(popup.IsOpen);
            CollectionAssert.AreEqual(new[] { "open A", "close A" }, log);
        }

        [UnityTest]
        public IEnumerator WaitingDialogsOpenInPriorityOrderOncePerLayer()
        {
            TestDialog first = Dialog("First", DialogLayer.Popup, DialogPolicy.Queue);
            TestDialog low = Dialog("Low", DialogLayer.Popup, DialogPolicy.Queue, 0);
            TestDialog high = Dialog("High", DialogLayer.Popup, DialogPolicy.Queue, 5);
            DialogHandle a = first.Present();
            DialogHandle b = low.Present();
            DialogHandle c = high.Present();
            yield return Frames();
            Assert.IsTrue(first.IsOpen);
            Assert.IsFalse(low.IsOpen || high.IsOpen, "one dialog at a time on a layer");

            a.Close();
            yield return a;
            yield return Frames();
            Assert.IsTrue(high.IsOpen, "the higher priority goes next");
            c.Close();
            yield return c;
            yield return Frames();
            Assert.IsTrue(low.IsOpen);
            b.Close();
            yield return b;
            CollectionAssert.AreEqual(new[] { "open First", "close First", "open High", "close High", "open Low", "close Low" }, log);
        }

        [UnityTest]
        public IEnumerator LayersAreIndependentAndADroppedDialogNeverOpens()
        {
            TestDialog popup = Dialog("Popup", DialogLayer.Popup, DialogPolicy.Queue);
            TestDialog toast = Dialog("Toast", DialogLayer.Toast, DialogPolicy.Queue, 0, 0f);
            TestDialog later = Dialog("Later", DialogLayer.Popup, DialogPolicy.Queue);
            DialogHandle open = popup.Present();
            toast.Present();
            DialogHandle dropped = later.Present();
            yield return Frames();
            Assert.IsTrue(popup.IsOpen && toast.IsOpen, "different layers show together");

            dropped.Close();
            Assert.IsTrue(dropped.IsClosed, "a waiting dialog closes at once");
            open.Close();
            yield return open;
            yield return Frames();
            Assert.IsFalse(later.IsOpen);
        }

        [UnityTest]
        public IEnumerator ReplaceClosesTheOpenDialogAndStackKeepsItBeneath()
        {
            TestDialog message = Dialog("Message", DialogLayer.Toast, DialogPolicy.Replace, 0, 0f);
            TestDialog newer = Dialog("Newer", DialogLayer.Toast, DialogPolicy.Replace, 0, 0f);
            message.Present();
            yield return Frames();
            newer.Present();
            yield return Frames();
            Assert.IsFalse(message.IsOpen, "replaced");
            Assert.IsTrue(newer.IsOpen);

            TestDialog under = Dialog("Under", DialogLayer.Popup, DialogPolicy.Queue);
            TestDialog over = Dialog("Over", DialogLayer.Popup, DialogPolicy.Stack);
            DialogHandle beneath = under.Present();
            yield return Frames();
            DialogHandle top = over.Present();
            yield return Frames();
            Assert.IsTrue(under.IsOpen && over.IsOpen, "stacked on top");

            top.Close();
            yield return top;
            Assert.IsTrue(under.IsOpen, "the one beneath stays");
            beneath.Close();
            yield return beneath;
        }

        [UnityTest]
        public IEnumerator TheBackdropSitsBeneathTheTopDialogAndATapClosesIt()
        {
            TestDialog popup = Dialog("Popup", DialogLayer.Popup, DialogPolicy.Queue, 0, 0.6f);
            DialogHandle handle = popup.Present();
            yield return new WaitForSeconds(0.4f);
            Assert.AreEqual(0.6f, backdrop.color.a, 0.01f);
            Assert.AreEqual(popup.transform.GetSiblingIndex() - 1, backdrop.transform.GetSiblingIndex(), "just beneath the dialog");
            Assert.IsTrue(backdrop.raycastTarget);

            backdrop.GetComponent<Button>().onClick.Invoke();
            yield return handle;
            yield return new WaitForSeconds(0.4f);
            Assert.AreEqual(0f, backdrop.color.a, 0.01f);
            Assert.IsFalse(backdrop.raycastTarget);
        }

        [UnityTest]
        public IEnumerator ADialogCanCloseItselfWhenItsContentEnds()
        {
            TestDialog timed = Dialog("Timed", DialogLayer.Overlay, DialogPolicy.Queue);
            timed.CloseAfter = 0.2f;
            DialogHandle handle = timed.Present();
            float started = Time.time;
            yield return handle;
            Assert.Less(Time.time - started, 1f);
            Assert.IsFalse(timed.IsOpen);
        }
    }
}
