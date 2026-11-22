using System.Collections;
using GameKit;
using UnityEngine.UI;
using UnityEngine;

namespace RoyalVault.Unity
{
    // A framed story chapter over the game, told beat by beat; a tap moves it on. Its definition grows it in and fades
    // it out
    public sealed class ChapterCardView : DialogView
    {
        [SerializeField]
        private StoryCardView card;
        [SerializeField]
        private Button tapArea;
        [SerializeField]
        private CanvasGroup continuePrompt;

        private readonly TweenGroup tweens = new TweenGroup(2);
        private bool advance;
        private bool closeNow;
        private bool waiting;
        private StoryDefinition.Scene scene;
        private int number;
        private string chapterLabel;

        // A chapter is on screen or waiting its turn
        public bool IsShowing => IsOpen || waiting;

        public void Setup(StoryCardView storyCard, Button tap, CanvasGroup prompt)
        {
            card = storyCard;
            tapArea = tap;
            continuePrompt = prompt;
        }

        protected override void Awake()
        {
            base.Awake();
            tapArea.onClick.AddListener(Advance);
        }

        public void Advance() => advance = true;

        // Closes the card at once (testing, or when the story is turned off)
        public void CloseNow() => closeNow = true;

        // Waits its turn with the dialog manager, then tells the scene until the last beat is read
        public IEnumerator Tell(StoryDefinition.Scene storyScene, int chapterNumber, string label)
        {
            scene = storyScene;
            number = chapterNumber;
            chapterLabel = label;
            closeNow = false;
            waiting = true;
            yield return Present();
        }

        protected override void OnOpening()
        {
            waiting = false;
            advance = false;
            card.Group.alpha = 1f;
            card.Tell(scene, number, chapterLabel);
            tweens.Add(UiMotion.Pulse(continuePrompt, 0.05f, 0.85f, 2.1f));
        }

        // Each tap shows the next beat; after the last one the card closes
        protected override IEnumerator Content()
        {
            while (!closeNow && (!TakeAdvance() || card.NextBeat()))
            {
                yield return null;
            }
        }

        protected override void OnClosed()
        {
            tweens.StopAll();
        }

        private bool TakeAdvance()
        {
            bool taken = advance || (Input.anyKeyDown && !Input.GetMouseButtonDown(0));
            advance = false;
            return taken;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            tweens.StopAll();
        }
    }
}
