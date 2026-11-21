using System.Collections.Generic;
using GameKit;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

namespace RoyalVault.Unity
{
    // A winning payline drawn as a glowing gold line through its cells, with the amount on a badge
    public sealed class PaylineView : MonoBehaviour
    {
        private readonly List<Image> segments = new List<Image>();
        private Sprite lineSprite;
        private Image badge;
        private TextMeshProUGUI amount;
        private CanvasGroup group;

        public static PaylineView Create(Transform parent, ThemeDefinition theme)
        {
            RectTransform root = UiFactory.Stretch("Payline", parent);
            var view = root.gameObject.AddComponent<PaylineView>();
            view.lineSprite = theme.line;
            view.group = root.gameObject.AddComponent<CanvasGroup>();
            view.group.blocksRaycasts = false;
            view.badge = UiFactory.Image("Badge", root, theme.pillButton, Vector2.zero, new Vector2(150f, 52f));
            view.badge.type = Image.Type.Sliced;
            view.amount = UiFactory.Text("Amount", view.badge.transform, theme.headingFont, "", 26f, theme.paleGold, Vector2.zero, new Vector2(150f, 52f));
            root.gameObject.SetActive(false);
            return view;
        }

        // Shows the line through points (local positions) with an amount, or none when it is 0
        public void Show(IList<Vector2> points, long win, Color color)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            group.alpha = 1f;
            for (int i = 0; i < points.Count - 1; i++)
            {
                Image segment = Segment(i);
                Vector2 a = points[i], b = points[i + 1];
                Vector2 d = b - a;
                RectTransform rect = segment.rectTransform;
                rect.anchoredPosition = (a + b) * 0.5f;
                rect.sizeDelta = new Vector2(d.magnitude + 14f, 28f);
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                segment.color = color;
                segment.gameObject.SetActive(true);
            }
            for (int i = points.Count - 1; i < segments.Count; i++)
            {
                segments[i].gameObject.SetActive(false);
            }

            badge.gameObject.SetActive(win > 0);
            if (win > 0)
            {
                amount.text = NumberFormat.Credits(win);
                badge.rectTransform.sizeDelta = new Vector2(Mathf.Max(120f, amount.preferredWidth + 56f), 52f);
                badge.rectTransform.anchoredPosition = points[points.Count / 2];
                badge.transform.SetAsLastSibling();
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private Image Segment(int index)
        {
            while (segments.Count <= index)
            {
                Image image = UiFactory.Image("Segment", transform, lineSprite, Vector2.zero, new Vector2(10f, 16f));
                segments.Add(image);
            }
            return segments[index];
        }
    }
}
