using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalVault.Unity
{
    // Dims everything except one control, rings it with light and explains it in a bubble
    public sealed class CoachMarkView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform[] shades = new RectTransform[4];
        [SerializeField] private RectTransform ring;
        [SerializeField] private RectTransform bubble;
        [SerializeField] private TextMeshProUGUI message;
        [SerializeField] private Button tapArea;
        [SerializeField] private float padding = 18f;

        private bool advance;
        private bool closeNow;

        public bool IsShowing { get; private set; }

        public void Setup(CanvasGroup canvasGroup, RectTransform[] shadeRects, RectTransform ringRect, RectTransform bubbleRect,
                          TextMeshProUGUI messageText, Button tap)
        {
            group = canvasGroup;
            shades = shadeRects;
            ring = ringRect;
            bubble = bubbleRect;
            message = messageText;
            tapArea = tap;
        }

        private void Awake()
        {
            tapArea.onClick.AddListener(() => advance = true);
            Hide();
        }

        public void Advance() => advance = true;
        public void CloseNow() => closeNow = true;

        // Spotlights the target and waits for a tap; above = bubble over the target, otherwise below it
        public IEnumerator Show(RectTransform target, string text, bool above)
        {
            closeNow = false;
            advance = false;
            Rect hole = Spotlight(target);
            message.text = text;
            bubble.anchoredPosition = new Vector2(Mathf.Clamp(hole.center.x, -640f, 640f), above ? hole.yMax + 150f : hole.yMin - 150f);
            IsShowing = true;
            group.blocksRaycasts = true;
            transform.SetAsLastSibling();
            yield return Tween.Run(0.3f, k => group.alpha = k, Ease.OutCubic);
            while (!advance && !closeNow) yield return null;
            yield return Tween.Run(0.25f, k => group.alpha = 1f - k, Ease.Linear);
            Hide();
        }

        // Four shades around the target leave a clear window over it
        private Rect Spotlight(RectTransform target)
        {
            Rect hole = LocalRect(target);
            hole.xMin -= padding; hole.xMax += padding; hole.yMin -= padding; hole.yMax += padding;
            Vector2 size = ((RectTransform)transform).rect.size;
            float l = -size.x / 2f, r = size.x / 2f, b = -size.y / 2f, t = size.y / 2f;
            Place(shades[0], l, hole.xMin, b, t);
            Place(shades[1], hole.xMax, r, b, t);
            Place(shades[2], hole.xMin, hole.xMax, hole.yMax, t);
            Place(shades[3], hole.xMin, hole.xMax, b, hole.yMin);
            ring.anchoredPosition = hole.center;
            ring.sizeDelta = Vector2.one * Mathf.Max(hole.width, hole.height) * 1.25f;
            return hole;
        }

        private Rect LocalRect(RectTransform target)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Vector2 min = transform.InverseTransformPoint(corners[0]);
            Vector2 max = transform.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static void Place(RectTransform shade, float xMin, float xMax, float yMin, float yMax)
        {
            shade.anchoredPosition = new Vector2((xMin + xMax) / 2f, (yMin + yMax) / 2f);
            shade.sizeDelta = new Vector2(Mathf.Max(0f, xMax - xMin), Mathf.Max(0f, yMax - yMin));
        }

        private void Hide()
        {
            IsShowing = false;
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }

        private void Update()
        {
            if (IsShowing) ring.localScale = Vector3.one * (1f + 0.05f * Mathf.Sin(Time.time * 4f));
        }
    }
}
