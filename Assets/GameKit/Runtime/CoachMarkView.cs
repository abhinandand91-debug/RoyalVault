using System.Collections;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

namespace GameKit
{
    // Dims everything except one control, rings it with light and explains it in a bubble; its definition fades it in
    // and out
    public sealed class CoachMarkView : DialogView
    {
        [SerializeField]
        private RectTransform[] shades = new RectTransform[4];
        [SerializeField]
        private RectTransform ring;
        [SerializeField]
        private RectTransform bubble;
        [SerializeField]
        private TextMeshProUGUI message;
        [SerializeField]
        private Button tapArea;
        [SerializeField]
        private float padding = 18f;

        private readonly TweenGroup tweens = new TweenGroup(2);
        private readonly Vector3[] corners = new Vector3[4];
        private bool advance;
        private bool closeNow;
        private bool waiting;
        private RectTransform target;
        private string text;
        private bool above;

        // A hint is on screen or waiting its turn
        public bool IsShowing => IsOpen || waiting;

        public void Setup(RectTransform[] shadeRects, RectTransform ringRect, RectTransform bubbleRect, TextMeshProUGUI messageText, Button tap)
        {
            shades = shadeRects;
            ring = ringRect;
            bubble = bubbleRect;
            message = messageText;
            tapArea = tap;
        }

        protected override void Awake()
        {
            base.Awake();
            tapArea.onClick.AddListener(() => advance = true);
        }

        public void Advance() => advance = true;
        public void CloseNow() => closeNow = true;

        // Waits its turn with the dialog manager, then spotlights the target until a tap; above = bubble over the target
        public IEnumerator Show(RectTransform hintTarget, string hint, bool bubbleAbove)
        {
            target = hintTarget;
            text = hint;
            above = bubbleAbove;
            closeNow = false;
            waiting = true;
            yield return Present();
        }

        protected override void OnOpening()
        {
            waiting = false;
            advance = false;
            Rect hole = Spotlight(target);
            message.text = text;
            // kept on screen: half the canvas less half the bubble and a margin
            float reach = Mathf.Max(0f, ((RectTransform)bubble.parent).rect.width * 0.5f - bubble.rect.width * 0.5f - 24f);
            bubble.anchoredPosition = new Vector2(Mathf.Clamp(hole.center.x, -reach, reach), above ? hole.yMax + 150f : hole.yMin - 150f);
            tweens.Add(UiMotion.Breathe(ring, 1.05f, 1.6f));
        }

        // Waits for a tap, or for the hint to be closed from outside
        protected override IEnumerator Content()
        {
            while (!advance && !closeNow)
            {
                yield return null;
            }
        }

        protected override void OnClosed()
        {
            tweens.StopAll();
            ring.localScale = Vector3.one;
        }

        // Four shades around the target leave a clear window over it
        private Rect Spotlight(RectTransform spotlit)
        {
            Rect hole = LocalRect(spotlit);
            hole.xMin -= padding;
            hole.xMax += padding;
            hole.yMin -= padding;
            hole.yMax += padding;
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

        private Rect LocalRect(RectTransform rect)
        {
            rect.GetWorldCorners(corners);
            Vector2 min = transform.InverseTransformPoint(corners[0]);
            Vector2 max = transform.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static void Place(RectTransform shade, float xMin, float xMax, float yMin, float yMax)
        {
            shade.anchoredPosition = new Vector2((xMin + xMax) / 2f, (yMin + yMax) / 2f);
            shade.sizeDelta = new Vector2(Mathf.Max(0f, xMax - xMin), Mathf.Max(0f, yMax - yMin));
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            tweens.StopAll();
        }
    }
}
