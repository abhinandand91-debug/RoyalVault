using GameKit;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalVault.Unity
{
    // Two sets of rays turning against each other behind a grand symbol, so the light shimmers
    public sealed class RaysFx : SymbolFx
    {
        private Image outer;
        private Image inner;
        private Color colour;
        private float strength;
        private float spin;

        public static RaysFx Create(Transform parent, Sprite rays, Material additive)
        {
            Image outer = UiFactory.Image("Rays", parent, rays, Vector2.zero, Vector2.one);
            Image inner = UiFactory.Image("Inner Rays", outer.transform, rays, Vector2.zero, Vector2.one);
            outer.material = additive;
            inner.material = additive;
            RaysFx fx = outer.gameObject.AddComponent<RaysFx>();
            fx.outer = outer;
            fx.inner = inner;
            return fx;
        }

        public void Play(Vector2 centre, Vector2 size, Color rayColour, float rayStrength, float degreesPerSecond, float seconds)
        {
            colour = rayColour;
            strength = rayStrength;
            spin = degreesPerSecond;
            outer.rectTransform.sizeDelta = size;
            inner.rectTransform.sizeDelta = size * 0.75f;
            Begin(centre, seconds);
        }

        protected override void Animate(float time, float seconds)
        {
            float alpha = strength * Envelope(time, seconds) * (0.75f + 0.25f * Mathf.Sin(time * 4f));
            outer.color = WithAlpha(colour, alpha);
            outer.rectTransform.localRotation = Quaternion.Euler(0f, 0f, spin * time);
            outer.rectTransform.localScale = Vector3.one * (0.9f + 0.1f * Mathf.Min(1f, time * 4f));
            inner.color = WithAlpha(colour, alpha * 0.8f);
            inner.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -spin * 0.6f * time + 15f);
        }
    }
}
