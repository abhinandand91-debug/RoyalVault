using GameKit;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalVault.Unity
{
    // A soft coloured glow that breathes behind a gem or treasure
    public sealed class GlowFx : SymbolFx
    {
        private Image image;
        private Color colour;
        private float strength;

        public static GlowFx Create(Transform parent, Sprite glow, Material additive)
        {
            Image image = UiFactory.Image("Glow", parent, glow, Vector2.zero, Vector2.one);
            image.material = additive;
            GlowFx fx = image.gameObject.AddComponent<GlowFx>();
            fx.image = image;
            return fx;
        }

        public void Play(Vector2 centre, Vector2 size, Color glowColour, float glowStrength, float seconds)
        {
            colour = glowColour;
            strength = glowStrength;
            image.rectTransform.sizeDelta = size;
            Begin(centre, seconds);
        }

        protected override void Animate(float time, float seconds)
        {
            float alpha = strength * Envelope(time, seconds) * (0.7f + 0.3f * Mathf.Sin(time * 5.6f));
            image.color = WithAlpha(colour, alpha);
        }
    }
}
