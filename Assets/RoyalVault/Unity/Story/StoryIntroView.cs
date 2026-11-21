using System.Collections;
using System;
using GameKit;
using PrimeTween;
using UnityEngine.UI;
using UnityEngine;

namespace RoyalVault.Unity
{
    // Letterboxed story told beat by beat at the player's pace, ending on an "enter" button
    public sealed class StoryIntroView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private StoryCardView[] cards = new StoryCardView[0];
        [SerializeField] private RectTransform topBar;
        [SerializeField] private RectTransform bottomBar;
        [SerializeField] private Button tapArea;
        [SerializeField] private Button skipButton;
        [SerializeField] private CanvasGroup continuePrompt;
        [SerializeField] private Button enterButton;
        [SerializeField] private float barHeight = 96f;

        private bool advance;
        private bool skipped;
        private bool entered;

        public bool IsPlaying { get; private set; }
        public int Page { get; private set; } = -1;
        public event Action PageTurned;
        public bool ShowsEnter => enterButton != null && enterButton.gameObject.activeSelf;

        public void Setup(CanvasGroup canvasGroup, StoryCardView[] storyCards, RectTransform top, RectTransform bottom, Button tap, Button skip,
                          CanvasGroup prompt, Button enter)
        {
            group = canvasGroup;
            cards = storyCards;
            topBar = top;
            bottomBar = bottom;
            tapArea = tap;
            skipButton = skip;
            continuePrompt = prompt;
            enterButton = enter;
        }

        private void Awake()
        {
            tapArea.onClick.AddListener(Advance);
            skipButton.onClick.AddListener(Skip);
            enterButton.onClick.AddListener(Enter);
            Hide();
        }

        public void Advance() => advance = true;
        public void Skip() => skipped = true;
        public void Enter() => entered = true;

        public IEnumerator Tell(StoryDefinition story, (StoryDefinition.Scene scene, int number)[] scenes)
        {
            yield return Open();
            for (int i = 0; i < scenes.Length && !skipped; i++, Page = i)
            {
                yield return TellScene(story, scenes[i], cards[i % cards.Length], i == scenes.Length - 1);
            }

            yield return Close();
        }

        private IEnumerator TellScene(StoryDefinition story, (StoryDefinition.Scene scene, int number) entry, StoryCardView card, bool last)
        {
            CrossFadeTo(card);
            card.Tell(entry.scene, entry.number, story.chapterLabel);
            PageTurned?.Invoke();
            while (!skipped)
            {
                if (last && card.IsLastBeat)
                {
                    yield return WaitForEnter();
                    yield break;
                }
                if (TakeAdvance() && !card.NextBeat())
                {
                    yield break;
                }

                yield return null;
            }
        }

        private IEnumerator WaitForEnter()
        {
            enterButton.gameObject.SetActive(true);
            continuePrompt.alpha = 0f;
            Tween.Scale(enterButton.transform, 0f, 1f, 0.4f, Ease.OutBack);
            while (!entered && !skipped)
            {
                yield return null;
            }
        }

        private bool TakeAdvance()
        {
            bool key = Input.anyKeyDown && !Input.GetMouseButtonDown(0);
            bool taken = advance || key;
            advance = false;
            return taken;
        }

        private void CrossFadeTo(StoryCardView card)
        {
            card.transform.SetAsLastSibling();
            topBar.parent.SetAsLastSibling();
            foreach (StoryCardView other in cards)
            {
                if (other != card)
                {
                    FadeCard(other, 0f);
                }
            }

            FadeCard(card, 1f);
        }

        private static void FadeCard(StoryCardView card, float target)
        {
            Tween.StopAll(card.Group);
            Tween.Alpha(card.Group, card.Group.alpha, target, 0.8f, Ease.InOutSine);
        }

        private IEnumerator Open()
        {
            IsPlaying = true;
            Page = 0;
            gameObject.SetActive(true);
            group.alpha = 1f;
            group.blocksRaycasts = true;
            yield return Tween.Custom(this, 0f, 1f, 0.6f, (view, k) => view.SetBars(k), Ease.OutCubic).ToYieldInstruction();
        }

        private IEnumerator Close()
        {
            yield return Tween.Alpha(group, 1f, 0f, 0.6f, Ease.InOutSine).ToYieldInstruction();
            Hide();
        }

        private void Hide()
        {
            group.alpha = 0f;
            group.blocksRaycasts = false;
            enterButton.gameObject.SetActive(false);
            foreach (StoryCardView card in cards)
            {
                card.Group.alpha = 0f;
            }

            IsPlaying = false;
        }

        private void SetBars(float k)
        {
            topBar.sizeDelta = new Vector2(0f, barHeight * k);
            bottomBar.sizeDelta = new Vector2(0f, barHeight * k);
        }

        private void Update()
        {
            if (!IsPlaying || ShowsEnter)
            {
                return;
            }

            continuePrompt.alpha = 0.45f + 0.4f * Mathf.Sin(Time.time * 3f);
        }
    }
}
