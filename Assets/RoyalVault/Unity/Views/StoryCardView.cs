using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalVault.Unity
{
    // A full-screen story scene with figures stepping in and typed captions
    public sealed class StoryCardView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image art;
        [SerializeField] private Image[] figures = new Image[0];
        [SerializeField] private TextMeshProUGUI chapter;
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private TextMeshProUGUI caption;
        [SerializeField] private TypewriterText typewriter;
        [SerializeField] private float push = 0.07f;
        [SerializeField] private float figureEntrance = 280f;

        private float shownAt;
        private float duration = 5f;
        private Vector2[] figureHome = new Vector2[0];
        private string[] beats = new string[0];
        private int beat;
        private bool telling;

        public CanvasGroup Group => group;

        public void Setup(CanvasGroup canvasGroup, Image artImage, Image[] figureImages, TextMeshProUGUI chapterText, TextMeshProUGUI titleText,
                          TextMeshProUGUI captionText, TypewriterText captionTypewriter = null)
        {
            group = canvasGroup;
            art = artImage;
            figures = figureImages;
            chapter = chapterText;
            title = titleText;
            caption = captionText;
            typewriter = captionTypewriter;
        }

        // Shows the scene with its one-line caption, for about the given seconds
        public void Show(StoryDefinition.Scene scene, int number, string chapterLabel, float seconds)
        {
            Present(scene, number, chapterLabel, seconds);
            telling = false;
            caption.text = scene.caption;
            caption.maxVisibleCharacters = int.MaxValue;
        }

        // Shows the scene and types its beats one at a time (see NextBeat)
        public void Tell(StoryDefinition.Scene scene, int number, string chapterLabel)
        {
            Present(scene, number, chapterLabel, 8f);
            telling = typewriter != null;
            beats = scene.Beats;
            beat = 0;
            if (telling) typewriter.Play(beats[0]);
            else caption.text = string.Join(" ", beats);
        }

        // Finishes the line being typed, or starts the next; false when the scene has been told
        public bool NextBeat()
        {
            if (!telling) return false;
            if (typewriter.IsTyping) { typewriter.Complete(); return true; }
            if (beat + 1 >= beats.Length) return false;
            typewriter.Play(beats[++beat]);
            return true;
        }

        public bool IsLastBeat => !telling || (beat >= beats.Length - 1 && !typewriter.IsTyping);

        private void Present(StoryDefinition.Scene scene, int number, string chapterLabel, float seconds)
        {
            art.sprite = scene.art;
            art.color = Color.white;
            art.enabled = scene.art != null;
            chapter.text = StoryDefinition.ChapterName(chapterLabel, number);
            title.text = scene.title;
            PlaceFigures(scene.figures);
            duration = Mathf.Max(1f, seconds);
            shownAt = Time.time;
            Update();
        }

        private void PlaceFigures(StoryDefinition.Figure[] cast)
        {
            figureHome = new Vector2[figures.Length];
            for (int i = 0; i < figures.Length; i++)
            {
                StoryDefinition.Figure figure = i < cast.Length ? cast[i] : null;
                bool show = figure != null && figure.sprite != null;
                figures[i].gameObject.SetActive(show);
                if (show) PlaceFigure(figures[i], figure);
                figureHome[i] = show ? figure.position : Vector2.zero;
            }
        }

        private static void PlaceFigure(Image image, StoryDefinition.Figure figure)
        {
            image.sprite = figure.sprite;
            Rect r = figure.sprite.rect;
            image.rectTransform.sizeDelta = new Vector2(figure.height * r.width / r.height, figure.height);
            image.rectTransform.localScale = new Vector3(figure.mirrored ? -1f : 1f, 1f, 1f);
        }

        private void Update()
        {
            float t = Time.time - shownAt;
            PushCamera(t);
            MoveFigures(t);
            Reveal(chapter, t - 0.25f);
            Reveal(title, t - 0.45f);
            Reveal(caption, t - 0.8f);
        }

        private void PushCamera(float t)
        {
            float k = Mathf.Clamp01(t / (duration + 1.5f));
            art.rectTransform.localScale = Vector3.one * (1f + push * (Ease.OutCubic(k) * 0.6f + k * 0.4f));
        }

        // Figures step in from their side of the screen, then sway gently
        private void MoveFigures(float t)
        {
            float enter = Ease.OutCubic(Mathf.Clamp01((t - 0.2f) / 0.9f));
            for (int i = 0; i < figures.Length && i < figureHome.Length; i++)
            {
                if (!figures[i].gameObject.activeSelf) continue;
                Vector2 home = figureHome[i];
                float side = Mathf.Sign(home.x);
                figures[i].rectTransform.anchoredPosition = home + new Vector2(side * figureEntrance * (1f - enter), 6f * Mathf.Sin(t * 0.8f + i));
                figures[i].color = new Color(1f, 1f, 1f, enter);
            }
        }

        // Text fades in and rises into place
        private static void Reveal(TextMeshProUGUI text, float t)
        {
            float k = Ease.OutCubic(Mathf.Clamp01(t / 0.7f));
            text.alpha = k;
            text.margin = new Vector4(0f, (1f - k) * 36f, 0f, 0f);
        }
    }
}
