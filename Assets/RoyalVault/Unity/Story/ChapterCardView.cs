using System.Collections;
using GameKit;
using PrimeTween;
using UnityEngine.UI;
using UnityEngine;

namespace RoyalVault.Unity
{
    // A framed story chapter over the game, told beat by beat; a tap moves it on
    public sealed class ChapterCardView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform panel;
        [SerializeField] private StoryCardView card;
        [SerializeField] private Button tapArea;
        [SerializeField] private CanvasGroup continuePrompt;

        private bool advance;
        private bool closeNow;

        public bool IsShowing { get; private set; }

        private readonly TweenGroup tweens = new TweenGroup(2);

        public void Setup(CanvasGroup canvasGroup, RectTransform panelRect, StoryCardView storyCard, Button tap, CanvasGroup prompt)
        {
            group = canvasGroup;
            panel = panelRect;
            card = storyCard;
            tapArea = tap;
            continuePrompt = prompt;
        }

        private void Awake()
        {
            tapArea.onClick.AddListener(Advance);
            SetVisible(false);
        }

        public void Advance() => advance = true;

        // Closes the card at once (testing, or when the story is turned off)
        public void CloseNow() => closeNow = true;

        public IEnumerator Tell(StoryDefinition.Scene scene, int number, string chapterLabel)
        {
            closeNow = false;
            SetVisible(true);
            card.Group.alpha = 1f;
            card.Tell(scene, number, chapterLabel);
            yield return Tween.Custom(this, 0f, 1f, 0.45f, (view, k) => view.Appear(k), Ease.OutCubic).ToYieldInstruction();
            while (!closeNow && (!TakeAdvance() || card.NextBeat()))
            {
                yield return null;
            }

            yield return Tween.Alpha(group, 1f, 0f, 0.35f, Ease.InOutSine).ToYieldInstruction();
            SetVisible(false);
        }

        private void Appear(float k)
        {
            group.alpha = k;
            panel.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, k);
        }

        private bool TakeAdvance()
        {
            bool taken = advance || (Input.anyKeyDown && !Input.GetMouseButtonDown(0));
            advance = false;
            return taken;
        }

        private void SetVisible(bool visible)
        {
            tweens.StopAll();
            if (visible)
            {
                tweens.Add(UiMotion.Pulse(continuePrompt, 0.05f, 0.85f, 2.1f));
            }

            IsShowing = visible;
            gameObject.SetActive(true);
            group.alpha = visible ? group.alpha : 0f;
            group.blocksRaycasts = visible;
            transform.SetAsLastSibling();
        }

        private void OnDisable()
        {
            tweens.StopAll();
        }
    }
}
