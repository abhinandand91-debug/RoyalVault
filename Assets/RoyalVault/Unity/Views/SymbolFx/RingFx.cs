using GameKit;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalVault.Unity
{
    // A ring of light that opens from a special symbol as it lands
    public sealed class RingFx : SymbolFx
    {
        private Image image;
        private Color colour;
        private Vector2 cell;

        public static RingFx Create(Transform parent, Sprite glow, Material additive)
        {
            Image image = UiFactory.Image("Land Ring", parent, glow, Vector2.zero, Vector2.one);
            image.material = additive;
            RingFx fx = image.gameObject.AddComponent<RingFx>();
            fx.image = image;
            return fx;
        }

        public void Play(Vector2 centre, Vector2 cellSize, Color ringColour)
        {
            cell = cellSize;
            colour = ringColour;
            Begin(centre, 0.45f);
        }

        protected override void Animate(float time, float seconds)
        {
            float k = time / seconds;
            image.rectTransform.sizeDelta = cell * Mathf.Lerp(0.6f, 1.7f, Easing.Evaluate(k, Ease.OutCubic));
            image.color = WithAlpha(colour, 0.85f * (1f - k));
        }
    }
}
