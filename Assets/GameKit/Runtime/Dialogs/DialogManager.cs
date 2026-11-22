using System;
using System.Collections;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace GameKit
{
    // Opens dialogs in order: one at a time per layer, highest priority first, with a stack for dialogs opened on top of
    // others and one shared backdrop that sits just beneath the topmost dialog that wants it
    public sealed class DialogManager : MonoBehaviour
    {
        [Tooltip("Full-screen image sharing the dialogs' parent; it darkens behind them and catches taps")]
        [SerializeField] private Image backdrop;
        [SerializeField] private Button backdropButton;
        [SerializeField, Range(0.05f, 1f)] private float backdropSeconds = 0.25f;

        private readonly Dictionary<DialogLayer, List<DialogHandle>> waiting = new Dictionary<DialogLayer, List<DialogHandle>>(3);
        private readonly Dictionary<DialogLayer, Stack<DialogHandle>> beneath = new Dictionary<DialogLayer, Stack<DialogHandle>>(3);
        private readonly Dictionary<DialogLayer, DialogHandle> showing = new Dictionary<DialogLayer, DialogHandle>(3);
        // Every open dialog, oldest first, so the topmost is last
        private readonly List<DialogHandle> open = new List<DialogHandle>(6);
        private readonly TweenGroup tweens = new TweenGroup(2);

        // A dialog starts to open or has finished closing; sound and analytics listen here, the dialogs stay silent
        public event Action<DialogDefinition> Opened;
        public event Action<DialogDefinition> Closed;

        public bool AnyOpen => open.Count > 0;

        public void Setup(Image backdropImage, Button backdropTap)
        {
            backdrop = backdropImage;
            backdropButton = backdropTap;
            // made while playing (tests, tools), Awake has already run
            if (Application.isPlaying)
            {
                WireBackdrop();
            }
        }

        private void Awake()
        {
            WireBackdrop();
        }

        private void WireBackdrop()
        {
            if (backdrop == null)
            {
                return;
            }

            if (backdropButton != null)
            {
                backdropButton.onClick.AddListener(BackdropTapped);
            }

            SetBackdrop(0f, false);
        }

        // Shows the dialog now, or when its layer is free, by its definition's policy
        public DialogHandle Show(DialogView view)
        {
            var handle = new DialogHandle(view);
            DialogDefinition definition = view.Definition;
            if (!showing.TryGetValue(definition.layer, out DialogHandle current))
            {
                StartCoroutine(Present(handle));
            }
            else if (definition.policy == DialogPolicy.Stack)
            {
                Beneath(definition.layer).Push(current);
                StartCoroutine(Present(handle));
            }
            else if (definition.policy == DialogPolicy.Replace)
            {
                current.View.RequestClose();
                Enqueue(handle, true);
            }
            else
            {
                Enqueue(handle, false);
            }

            return handle;
        }

        // Closes an open dialog, or drops one still waiting
        public void Close(DialogHandle handle)
        {
            if (Waiting(handle.View.Definition.layer).Remove(handle))
            {
                handle.IsClosed = true;
                return;
            }

            handle.View.RequestClose();
        }

        // Runs one dialog from its entry to its exit, then hands its layer on
        private IEnumerator Present(DialogHandle handle)
        {
            DialogView view = handle.View;
            DialogDefinition definition = view.Definition;
            showing[definition.layer] = handle;
            open.Add(handle);
            view.transform.SetAsLastSibling();
            FitBackdrop();
            Opened?.Invoke(definition);
            yield return view.Run();
            open.Remove(handle);
            handle.IsClosed = true;
            Closed?.Invoke(definition);
            HandOn(definition.layer, handle);
            FitBackdrop();
        }

        // The dialog beneath comes back to the top, or the next one waiting opens
        private void HandOn(DialogLayer layer, DialogHandle closed)
        {
            if (!showing.TryGetValue(layer, out DialogHandle current) || current != closed)
            {
                return;
            }

            showing.Remove(layer);
            Stack<DialogHandle> stack = Beneath(layer);
            while (stack.Count > 0)
            {
                DialogHandle under = stack.Pop();
                if (!under.IsClosed)
                {
                    showing[layer] = under;
                    return;
                }
            }

            List<DialogHandle> queue = Waiting(layer);
            if (queue.Count > 0)
            {
                DialogHandle next = queue[0];
                queue.RemoveAt(0);
                StartCoroutine(Present(next));
            }
        }

        // Higher priority first; first come, first served within a priority (or ahead of its equals when asked)
        private void Enqueue(DialogHandle handle, bool ahead)
        {
            List<DialogHandle> queue = Waiting(handle.View.Definition.layer);
            int priority = handle.View.Definition.priority;
            int index = 0;
            while (index < queue.Count && Ranks(queue[index], priority, ahead))
            {
                index++;
            }

            queue.Insert(index, handle);
        }

        private static bool Ranks(DialogHandle queued, int priority, bool ahead)
        {
            int other = queued.View.Definition.priority;
            return ahead ? other > priority : other >= priority;
        }

        // The backdrop sits just beneath the topmost dialog that wants one, at that dialog's darkness
        private void FitBackdrop()
        {
            if (backdrop == null)
            {
                return;
            }

            DialogHandle top = TopWithBackdrop();
            if (top == null)
            {
                SetBackdrop(0f, false);
                return;
            }

            Transform dialog = top.View.transform;
            if (backdrop.transform.parent == dialog.parent)
            {
                // moving down past the dialog shifts it up by one, so aim one place lower
                int index = dialog.GetSiblingIndex();
                bool above = backdrop.transform.GetSiblingIndex() < index;
                backdrop.transform.SetSiblingIndex(above ? index - 1 : index);
            }

            SetBackdrop(top.View.Definition.backdropAlpha, top.View.Definition.blocksInput);
        }

        private DialogHandle TopWithBackdrop()
        {
            for (int i = open.Count - 1; i >= 0; i--)
            {
                if (open[i].View.Definition.backdropAlpha > 0f)
                {
                    return open[i];
                }
            }

            return null;
        }

        private void SetBackdrop(float alpha, bool blocks)
        {
            tweens.StopAll();
            backdrop.raycastTarget = blocks;
            if (Application.isPlaying)
            {
                tweens.Add(Tween.Alpha(backdrop, backdrop.color.a, alpha, backdropSeconds, Ease.OutCubic));
                return;
            }

            Color colour = backdrop.color;
            colour.a = alpha;
            backdrop.color = colour;
        }

        // A tap on the backdrop closes the topmost dialog that allows it
        private void BackdropTapped()
        {
            DialogHandle top = TopWithBackdrop();
            if (top != null && top.View.Definition.closeOnBackdropTap)
            {
                top.View.RequestClose();
            }
        }

        private List<DialogHandle> Waiting(DialogLayer layer)
        {
            if (!waiting.TryGetValue(layer, out List<DialogHandle> queue))
            {
                queue = new List<DialogHandle>(4);
                waiting[layer] = queue;
            }

            return queue;
        }

        private Stack<DialogHandle> Beneath(DialogLayer layer)
        {
            if (!beneath.TryGetValue(layer, out Stack<DialogHandle> stack))
            {
                stack = new Stack<DialogHandle>(4);
                beneath[layer] = stack;
            }

            return stack;
        }

        private void OnDisable()
        {
            tweens.StopAll();
        }
    }
}
