using System.Collections.Generic;
using System.Collections;
using GameKit;
using PrimeTween;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

namespace RoyalVault.Unity
{
    // Boot screen: logo, gold progress bar, rotating tips and drifting dust
    public sealed class LoadingScreen : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform logo;
        [SerializeField] private RectTransform fillArea;
        [SerializeField] private Image fill;
        [SerializeField] private RectTransform tip;
        [SerializeField] private RectTransform shine;
        [SerializeField] private TextMeshProUGUI percent;
        [SerializeField] private TextMeshProUGUI tipText;
        [SerializeField] private RectTransform dustLayer;
        [SerializeField] private Sprite dustSprite;
        [SerializeField] private string[] tips = new string[0];
        [Header("Story")]
        [SerializeField] private StoryDefinition story;
        [SerializeField] private StoryCardView[] cards = new StoryCardView[0];
        [SerializeField] private CanvasGroup storyChrome;
        [Header("Sound")]
        [SerializeField] private SoundPlayer sound;

        [SerializeField] private StoryIntroView introView;

        private const string IntroSeenKey = "royalvault.story.intro.seen";

        private Sequence tipCycle;

        // True once the story has been told, or when there is none
        public bool StoryDone { get; private set; } = true;

        private readonly List<Image> dust = new List<Image>();
        private float shown;
        private int tipIndex;

        public void Setup(CanvasGroup canvasGroup, RectTransform logoRect, RectTransform fillAreaRect, Image fillImage, RectTransform tipRect,
                          RectTransform shineRect, TextMeshProUGUI percentText, TextMeshProUGUI tipLabel, RectTransform dustRect, Sprite dustImage,
                          string[] tipLines)
        {
            group = canvasGroup;
            logo = logoRect;
            fillArea = fillAreaRect;
            fill = fillImage;
            tip = tipRect;
            shine = shineRect;
            percent = percentText;
            tipText = tipLabel;
            dustLayer = dustRect;
            dustSprite = dustImage;
            tips = tipLines;
        }

        public void SetupSound(SoundPlayer player)
        {
            sound = player;
        }

        public void SetupStory(StoryDefinition storyAsset, StoryCardView[] storyCards, CanvasGroup chrome, StoryIntroView intro = null)
        {
            introView = intro;
            story = storyAsset;
            cards = storyCards;
            storyChrome = chrome;
        }

        private void Start()
        {
            if (sound != null && sound.Bank != null)
            {
                sound.PlayMusic(sound.Bank.loadingMusic, 1.5f);
            }

            SetProgress(0f);
            SpawnDust();
            foreach (StoryCardView card in cards)
            {
                card.Group.alpha = 0f;
            }

            var intro = story != null ? story.At(StoryMoment.Intro) : new (StoryDefinition.Scene, int)[0];
            if (storyChrome != null)
            {
                storyChrome.alpha = intro.Length > 0 ? 1f : 0f;
            }

            if (intro.Length == 0 || cards.Length == 0)
            {
                if (tips.Length > 0)
                {
                    RotateTips();
                }

                return;
            }
            StoryDone = false;
            bool firstTime = PlayerPrefs.GetInt(IntroSeenKey, 0) == 0 && introView != null;
            StartCoroutine(firstTime ? TellFirstTime(intro) : ShowLastScene(intro));
        }

        private void SpawnDust()
        {
            for (int i = 0; i < 26; i++)
            {
                dust.Add(UiFactory.Image("Dust", dustLayer, dustSprite, RandomDustPosition(true), Vector2.one * Random.Range(10f, 30f)));
            }
        }

        // First launch: the whole intro, paced by the player, while the game loads underneath
        private IEnumerator TellFirstTime((StoryDefinition.Scene scene, int number)[] intro)
        {
            introView.PageTurned += PlayPageSound;
            yield return introView.Tell(story, intro);
            PlayerPrefs.SetInt(IntroSeenKey, 1);
            PlayerPrefs.Save();
            yield return ShowLastScene(intro);
        }

        // Returning players see the last intro scene behind the bar
        private IEnumerator ShowLastScene((StoryDefinition.Scene scene, int number)[] intro)
        {
            var last = intro[intro.Length - 1];
            StoryCardView card = cards[0];
            card.Show(last.scene, last.number, story.chapterLabel, story.secondsPerScene);
            StoryDone = true;
            yield return Tween.Alpha(card.Group, 0f, 1f, 0.6f, Ease.InOutSine).ToYieldInstruction();
        }

        private void PlayPageSound()
        {
            if (sound != null)
            {
                sound.Play(SoundCue.StoryPage);
            }
        }

        // Shows 0..1 of the loading done
        public void SetProgress(float value)
        {
            shown = Mathf.Clamp01(value);
            fill.fillAmount = shown;
            float width = fillArea.rect.width;
            tip.anchoredPosition = new Vector2(-width * 0.5f + width * shown, 0f);
            tip.gameObject.SetActive(shown > 0.005f && shown < 0.999f);
            percent.SetText("{0:0}%", Mathf.Round(shown * 100f));
        }

        public IEnumerator FadeOut()
        {
            if (sound != null)
            {
                sound.PlayMusic(null, 0.45f);
            }

            yield return Tween.Alpha(group, 1f, 0f, 0.45f, Ease.InOutSine).ToYieldInstruction();
        }

        private void Update()
        {
            float time = Time.time;
            if (!StoryDone && tipText.text.Length > 0)
            {
                tipText.alpha = 0.45f + 0.35f * Mathf.Sin(time * 2.4f);
            }
            // the logo breathes a little light
            logo.localScale = Vector3.one * (1f + 0.012f * Mathf.Sin(time * 1.6f));
            // a soft light sweeps along the filled gold every two seconds
            float width = fillArea.rect.width;
            float sweep = Mathf.Repeat(time * 0.55f, 1.3f) - 0.15f;
            shine.anchoredPosition = new Vector2(-width * 0.5f + width * shown * sweep, 0f);
            shine.gameObject.SetActive(shown > 0.05f);
            // the tip twinkles
            tip.localScale = Vector3.one * (1f + 0.18f * Mathf.Sin(time * 9f));

            foreach (Image mote in dust)
            {
                RectTransform rect = mote.rectTransform;
                float phase = Phase(rect);
                Vector2 p = rect.anchoredPosition + new Vector2(Mathf.Sin(time + phase) * 6f, 38f) * Time.deltaTime;
                if (p.y > 560f)
                {
                    p = RandomDustPosition(false);
                }

                rect.anchoredPosition = p;
                mote.color = new Color(1f, 0.86f, 0.55f, 0.15f + 0.35f * (Mathf.Sin(time * 2f + phase * 3f) * 0.5f + 0.5f));
            }
        }

        private static float Phase(RectTransform t)
        {
            return t.GetInstanceID() * 0.37f;
        }

        private static Vector2 RandomDustPosition(bool anywhere)
        {
            return new Vector2(Random.Range(-940f, 940f), anywhere ? Random.Range(-540f, 540f) : -560f);
        }

        // One tip after another, fading in and out, until the screen goes away
        private void RotateTips()
        {
            tipCycle = Sequence.Create(cycles: -1)
                .ChainCallback(this, screen => screen.NextTip())
                .Chain(Tween.Alpha(tipText, 0f, 1f, 0.35f, Ease.Linear))
                .ChainDelay(1.3f)
                .Chain(Tween.Alpha(tipText, 1f, 0f, 0.35f, Ease.Linear));
        }

        private void OnDestroy()
        {
            tipCycle.Stop();
        }

        private void NextTip()
        {
            tipText.text = tips[tipIndex % tips.Length].ToUpperInvariant() + "…";
            tipIndex++;
        }
    }
}
