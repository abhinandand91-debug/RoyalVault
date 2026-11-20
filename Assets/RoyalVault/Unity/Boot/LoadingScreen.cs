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

        private void Start()
        {
            SetProgress(0f);
            for (int i = 0; i < 26; i++) dust.Add(UiFactory.Image("Dust", dustLayer, dustSprite, RandomDustPosition(true), Vector2.one * Random.Range(10f, 30f)).rectTransform);
            if (tips.Length > 0) StartCoroutine(RotateTips());
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
