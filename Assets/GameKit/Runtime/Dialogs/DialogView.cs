using System.Collections;
using UnityEngine;

namespace GameKit
{
    // A dialog on screen: plays its definition's motions, runs its content, and closes when asked or when its content
    // ends; each dialog inherits this and keeps only what makes it different
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class DialogView : MonoBehaviour
    {
        [SerializeField] private DialogDefinition definition;
        [Tooltip("The part that moves; the backdrop stays put")]
        [SerializeField] private RectTransform panel;
        [SerializeField] private DialogManager manager;
        [SerializeField] private CanvasGroup group;

        private readonly TweenGroup tweens = new TweenGroup(4);
        private bool closeRequested;

        public DialogDefinition Definition => definition;
        public DialogManager Manager => manager;
        public bool IsOpen { get; private set; }
        // Closing has been asked for
        protected bool CloseRequested => closeRequested;

        public void SetupDialog(DialogDefinition dialogDefinition, RectTransform movingPanel, DialogManager dialogManager, CanvasGroup canvasGroup)
        {
            definition = dialogDefinition;
            panel = movingPanel;
            manager = dialogManager;
            group = canvasGroup;
            // set up while playing (tests, tools), Awake has already run without a group
            if (Application.isPlaying)
            {
                Hide();
            }
        }

        protected virtual void Awake()
        {
            if (group != null)
            {
                Hide();
            }
        }

        // Asks the manager to show this dialog, in its turn
        public DialogHandle Present()
        {
            return manager.Show(this);
        }

        public void RequestClose()
        {
            closeRequested = true;
        }

        // Run by the manager: the entry motion, the content, then the exit motion
        public IEnumerator Run()
        {
            closeRequested = false;
            IsOpen = true;
            group.blocksRaycasts = definition.blocksInput;
            OnOpening();
            yield return PlayMotion(definition.entry, 1f);
            OnOpened();
            yield return Content();
            OnClosing();
            group.blocksRaycasts = false;
            yield return PlayMotion(definition.exit, 0f);
            Hide();
            OnClosed();
        }

        // A dialog's own content; by default it waits until something asks it to close
        protected virtual IEnumerator Content()
        {
            while (!closeRequested)
            {
                yield return null;
            }
        }

        protected virtual void OnOpening()
        {
        }

        protected virtual void OnOpened()
        {
        }

        protected virtual void OnClosing()
        {
        }

        protected virtual void OnClosed()
        {
        }

        // Without a preset the dialog simply appears or disappears
        private IEnumerator PlayMotion(MotionPreset preset, float alphaWithout)
        {
            tweens.StopAll();
            if (preset == null || preset.steps.Length == 0)
            {
                group.alpha = alphaWithout;
                yield break;
            }

            RectTransform moving = panel != null ? panel : (RectTransform)transform;
            yield return tweens.Add(UiMotion.Play(preset, new MotionTarget(moving, group))).ToYieldInstruction();
        }

        private void Hide()
        {
            tweens.StopAll();
            IsOpen = false;
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }

        protected virtual void OnDisable()
        {
            tweens.StopAll();
        }
    }
}
