using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalVault.Unity
{
    /// <summary>One symbol on a reel: its art, the motion-blurred art while spinning, and the win glow and pulse.</summary>
    public sealed class SymbolView : MonoBehaviour
    {
        private Image image;
        private Image glow;
        private ThemeDefinition.SymbolArt art;
        private Coroutine pulse;
        private bool blurred;

        public int SymbolIndex { get; private set; } = -1;
        public RectTransform Rect { get { return (RectTransform)transform; } }

        public static SymbolView Create(Transform parent, Vector2 size, Sprite glowSprite)
        {
            RectTransform root = UiFactory.Rect("Symbol", parent, Vector2.zero, size);
            var view = root.gameObject.AddComponent<SymbolView>();
            view.glow = UiFactory.Image("Glow", root, glowSprite, Vector2.zero, size * 1.35f);
            view.glow.color = new Color(1f, 0.85f, 0.45f, 0f);
            view.image = UiFactory.Image("Art", root, null, Vector2.zero, size);
            view.image.preserveAspect = true;
            return view;
        }

        public void SetSymbol(int index, ThemeDefinition.SymbolArt symbolArt)
        {
            if (index == SymbolIndex && symbolArt == art) return;
            SymbolIndex = index;
            art = symbolArt;
            Apply();
        }

        public void SetBlur(bool value)
        {
            if (value == blurred) return;
            blurred = value;
            Apply();
        }

        private void Apply()
        {
            Sprite sprite = art == null ? null : blurred && art.blur != null ? art.blur : art.sprite;
            image.sprite = sprite;
            image.color = sprite == null ? Color.clear : Color.white;
        }

        /// <summary>Dims the symbol while other symbols are winning.</summary>
        public void SetDimmed(bool dimmed)
        {
            if (art == null) return;
            image.color = dimmed ? new Color(0.42f, 0.42f, 0.46f, 1f) : Color.white;
        }

        /// <summary>Glows and pulses while its win is shown.</summary>
        public void Celebrate(float seconds)
        {
            StopCelebrating();
            pulse = StartCoroutine(Pulse(seconds));
        }

        public void StopCelebrating()
        {
            if (pulse != null) StopCoroutine(pulse);
            pulse = null;
            image.rectTransform.localScale = Vector3.one;
            glow.color = new Color(glow.color.r, glow.color.g, glow.color.b, 0f);
        }

        private IEnumerator Pulse(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                float k = Mathf.Sin(t * Mathf.PI * 2f * 1.1f) * 0.5f + 0.5f;
                image.rectTransform.localScale = Vector3.one * (1f + 0.09f * k);
                glow.color = new Color(glow.color.r, glow.color.g, glow.color.b, 0.35f + 0.45f * k);
                t += Time.deltaTime;
                yield return null;
            }
            StopCelebrating();
        }
    }
}
