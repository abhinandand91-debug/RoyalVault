using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalVault.Unity
{
    /// <summary>Small helpers to create UI objects in code (the reels are built at run time; the editor builder uses them too).</summary>
    public static class UiFactory
    {
        /// <summary>A RectTransform centred on its parent, at <paramref name="position"/> with <paramref name="size"/>.</summary>
        public static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent != null ? parent.gameObject.layer : 5;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        /// <summary>A RectTransform that fills its parent.</summary>
        public static RectTransform Stretch(string name, Transform parent)
        {
            RectTransform rect = Rect(name, parent, Vector2.zero, Vector2.zero);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static Image Image(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size, bool raycast = false)
        {
            RectTransform rect = Rect(name, parent, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = raycast;
            if (sprite == null) image.color = Color.clear;
            return image;
        }

        public static TextMeshProUGUI Text(string name, Transform parent, TMP_FontAsset font, string text, float size, Color color,
                                           Vector2 position, Vector2 boxSize, TextAlignmentOptions alignment = TextAlignmentOptions.Center,
                                           float characterSpacing = 0f)
        {
            RectTransform rect = Rect(name, parent, position, boxSize);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) label.font = font;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.characterSpacing = characterSpacing;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            return label;
        }

        public static Button Button(Image graphic)
        {
            graphic.raycastTarget = true;
            var button = graphic.gameObject.AddComponent<Button>();
            button.targetGraphic = graphic;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.75f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            return button;
        }
    }
}
