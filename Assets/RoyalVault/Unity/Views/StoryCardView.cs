using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalVault.Unity
{
    /// <summary>
    /// One full-screen story scene: the painted backdrop under a slow camera push, the characters standing in it, a
    /// dark fade at the foot of the screen and the chapter, title and caption rising in over it. Two of these
    /// cross-fade on the loading screen; the same card can tell any scene of a <see cref="StoryDefinition"/>.
    /// </summary>
    public sealed class StoryCardView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image art;
        [SerializeField] private Image[] figures = new Image[0];
        [SerializeField] private TextMeshProUGUI chapter;
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private TextMeshProUGUI caption;
        [Tooltip("How much the backdrop grows while the scene is shown")]
        [SerializeField] private float push = 0.07f;

        private float shownAt;
        private float duration = 5f;
        private Vector2[] figureHome = new Vector2[0];

        public CanvasGroup Group => group;

        public void Setup(CanvasGroup canvasGroup, Image artImage, Image[] figureImages, TextMeshProUGUI chapterText, TextMeshProUGUI titleText,
                          TextMeshProUGUI captionText)
        {
            group = canvasGroup;
            art = artImage;
            figures = figureImages;
            chapter = chapterText;
            title = titleText;
            caption = captionText;
        }

        /// <summary>Shows <paramref name="scene"/> as chapter <paramref name="number"/>; it plays for about <paramref name="seconds"/>.</summary>
        public void Show(StoryDefinition.Scene scene, int number, string chapterLabel, float seconds)
        {
            art.sprite = scene.art;
            art.color = Color.white;
            art.enabled = scene.art != null;
            chapter.text = (chapterLabel + " " + Roman(number)).ToUpperInvariant();
            title.text = scene.title;
            caption.text = scene.caption;

            figureHome = new Vector2[figures.Length];
            for (int i = 0; i < figures.Length; i++)
            {
                StoryDefinition.Figure figure = i < scene.figures.Length ? scene.figures[i] : null;
                Image image = figures[i];
                bool show = figure != null && figure.sprite != null;
                image.gameObject.SetActive(show);
                if (!show) continue;
                image.sprite = figure.sprite;
                image.color = Color.white;
                Rect r = figure.sprite.rect;
                image.rectTransform.sizeDelta = new Vector2(figure.height * r.width / r.height, figure.height);
                image.rectTransform.anchoredPosition = figure.position;
                image.rectTransform.localScale = new Vector3(figure.mirrored ? -1f : 1f, 1f, 1f);
                figureHome[i] = figure.position;
            }

            duration = Mathf.Max(1f, seconds);
            shownAt = Time.time;
            Update();
        }

        private void Update()
        {
            float t = Time.time - shownAt;
            float k = Mathf.Clamp01(t / (duration + 1.5f));

            // the camera pushes in slowly; the figures drift a touch less, so they stand in front of the scene
            art.rectTransform.localScale = Vector3.one * (1f + push * Ease.OutCubic(k) * 0.6f + push * k * 0.4f);
            for (int i = 0; i < figures.Length && i < figureHome.Length; i++)
            {
                RectTransform f = figures[i].rectTransform;
                f.anchoredPosition = figureHome[i] + new Vector2(figureHome[i].x * 0.025f * k, 6f * Mathf.Sin(t * 0.8f + i));
            }

            // the words rise in after the picture
            Reveal(chapter, t - 0.25f);
            Reveal(title, t - 0.45f);
            Reveal(caption, t - 0.8f);
        }

        private static void Reveal(TextMeshProUGUI text, float t)
        {
            float k = Ease.OutCubic(Mathf.Clamp01(t / 0.7f));
            text.alpha = k;
            // a top margin pushes the text down; it shrinks to nothing as the line rises into place
            text.margin = new Vector4(0f, (1f - k) * 36f, 0f, 0f);
        }

        private static string Roman(int n)
        {
            string[] numerals = { "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X", "XI", "XII" };
            return n >= 0 && n < numerals.Length ? numerals[n] : n.ToString();
        }
    }
}
