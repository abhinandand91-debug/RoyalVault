using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalVault.Unity
{
    /// <summary>
    /// The boot screen: the logo over the vault, a slim gold progress bar whose bright tip leads the fill, a light that
    /// sweeps along the gold, the percentage, rotating lines of text and gold dust drifting upwards.
    ///
    /// With a <see cref="StoryDefinition"/> the screen also tells the story's intro: its scenes play full screen behind
    /// the bar, cross-fading from one to the next, and a click or key moves on. The whole intro is told the first time;
    /// after that only its last scene shows while the game loads.
    /// </summary>
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

        private const string IntroSeenKey = "royalvault.story.intro.seen";
        private bool skip;

        /// <summary>How long the story wants the screen kept up (0 when there is no story to tell).</summary>
        public float StorySeconds { get; private set; }
        /// <summary>True once the story has been told, or when there is none.</summary>
        public bool StoryDone { get; private set; } = true;
        /// <summary>True when the player chose to skip the rest of the story.</summary>
        public bool Skipped { get; private set; }

        private readonly List<RectTransform> dust = new List<RectTransform>();
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

        public void SetupStory(StoryDefinition storyAsset, StoryCardView[] storyCards, CanvasGroup chrome)
        {
            story = storyAsset;
            cards = storyCards;
            storyChrome = chrome;
        }

        private void Start()
        {
            SetProgress(0f);
            for (int i = 0; i < 26; i++) dust.Add(UiFactory.Image("Dust", dustLayer, dustSprite, RandomDustPosition(true), Vector2.one * Random.Range(10f, 30f)).rectTransform);
            var intro = story != null ? story.At(StoryMoment.Intro) : new (StoryDefinition.Scene, int)[0];
            foreach (StoryCardView card in cards) card.Group.alpha = 0f;
            if (intro.Length > 0 && cards.Length > 0)
            {
                bool seen = PlayerPrefs.GetInt(IntroSeenKey, 0) == 1;
                StoryDone = false;
                StorySeconds = seen ? 0f : intro.Length * story.secondsPerScene;
                tipText.text = seen ? "" : "CLICK TO CONTINUE";
                StartCoroutine(TellIntro(intro, seen));
            }
            else if (tips.Length > 0) StartCoroutine(RotateTips());
            if (storyChrome != null) storyChrome.alpha = intro.Length > 0 ? 1f : 0f;
        }

        private IEnumerator TellIntro((StoryDefinition.Scene scene, int number)[] intro, bool seen)
        {
            int first = seen ? intro.Length - 1 : 0;
            StoryCardView previous = null;
            for (int i = first; i < intro.Length; i++)
            {
                StoryCardView card = cards[i % cards.Length];
                // the incoming card goes just above the outgoing one (both stay under the logo and the bar)
                if (previous != null && previous != card && card.transform.GetSiblingIndex() < previous.transform.GetSiblingIndex())
                    card.transform.SetSiblingIndex(previous.transform.GetSiblingIndex());
                card.Show(intro[i].scene, intro[i].number, story.chapterLabel, story.secondsPerScene);
                StoryCardView fading = previous;
                StartCoroutine(Tween.Run(previous == null ? 0.6f : 0.9f, k =>
                {
                    card.Group.alpha = k;
                    if (fading != null && fading != card) fading.Group.alpha = 1f - k;
                }, Ease.InOutSine));
                previous = card;
                if (i == intro.Length - 1) PlayerPrefs.SetInt(IntroSeenKey, 1);
                if (seen) break;

                skip = false;
                float t = 0f;
                while (t < story.secondsPerScene && !skip) { t += Time.deltaTime; yield return null; }
                if (skip && i == intro.Length - 1) Skipped = true;
            }
            tipText.text = "";
            StoryDone = true;
        }

        /// <summary>Shows 0..1 of the loading done.</summary>
        public void SetProgress(float value)
        {
            shown = Mathf.Clamp01(value);
            fill.fillAmount = shown;
            float width = fillArea.rect.width;
            tip.anchoredPosition = new Vector2(-width * 0.5f + width * shown, 0f);
            tip.gameObject.SetActive(shown > 0.005f && shown < 0.999f);
            percent.text = Mathf.RoundToInt(shown * 100f) + "%";
        }

        public IEnumerator FadeOut()
        {
            yield return Tween.Run(0.45f, k => group.alpha = 1f - k, Ease.InOutSine);
        }

        private void Update()
        {
            float time = Time.time;
            if (!StoryDone && (Input.GetMouseButtonDown(0) || Input.anyKeyDown)) skip = true;
            if (!StoryDone && tipText.text.Length > 0) tipText.alpha = 0.45f + 0.35f * Mathf.Sin(time * 2.4f);
            // the logo breathes a little light
            logo.localScale = Vector3.one * (1f + 0.012f * Mathf.Sin(time * 1.6f));
            // a soft light sweeps along the filled gold every two seconds
            float width = fillArea.rect.width;
            float sweep = Mathf.Repeat(time * 0.55f, 1.3f) - 0.15f;
            shine.anchoredPosition = new Vector2(-width * 0.5f + width * shown * sweep, 0f);
            shine.gameObject.SetActive(shown > 0.05f);
            // the tip twinkles
            tip.localScale = Vector3.one * (1f + 0.18f * Mathf.Sin(time * 9f));

            foreach (RectTransform mote in dust)
            {
                Vector2 p = mote.anchoredPosition + new Vector2(Mathf.Sin(time + Phase(mote)) * 6f, 38f) * Time.deltaTime;
                if (p.y > 560f) p = RandomDustPosition(false);
                mote.anchoredPosition = p;
                var image = mote.GetComponent<Image>();
                image.color = new Color(1f, 0.86f, 0.55f, 0.15f + 0.35f * (Mathf.Sin(time * 2f + Phase(mote) * 3f) * 0.5f + 0.5f));
            }
        }

        private static float Phase(RectTransform t) { return t.GetInstanceID() * 0.37f; }

        private static Vector2 RandomDustPosition(bool anywhere)
        {
            return new Vector2(Random.Range(-940f, 940f), anywhere ? Random.Range(-540f, 540f) : -560f);
        }

        private IEnumerator RotateTips()
        {
            while (true)
            {
                tipText.text = tips[tipIndex % tips.Length].ToUpperInvariant() + "…";
                yield return Tween.Run(0.35f, k => tipText.alpha = k, Ease.Linear);
                yield return new WaitForSeconds(1.3f);
                yield return Tween.Run(0.35f, k => tipText.alpha = 1f - k, Ease.Linear);
                tipIndex++;
            }
        }
    }
}
