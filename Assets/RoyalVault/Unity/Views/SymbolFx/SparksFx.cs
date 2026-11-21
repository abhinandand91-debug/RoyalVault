using GameKit;
using UnityEngine;

namespace RoyalVault.Unity
{
    // Rising gold dust or blue sparks in front of a symbol; the last particles die out after the win
    public sealed class SparksFx : SymbolFx
    {
        private const float Linger = 1.2f;

        private UiParticleEmitter emitter;
        private float emitSeconds;

        public static SparksFx Create(Transform parent, Material additive)
        {
            RectTransform rect = UiFactory.Rect("Sparks", parent, Vector2.zero, Vector2.one * 10f);
            SparksFx fx = rect.gameObject.AddComponent<SparksFx>();
            fx.emitter = rect.gameObject.AddComponent<UiParticleEmitter>();
            fx.emitter.Material = additive;
            fx.emitter.LifeRange = new Vector2(0.7f, 1.3f);
            fx.emitter.EndSize = 0.3f;
            fx.emitter.VelocityMin = new Vector2(-20f, 60f);
            fx.emitter.VelocityMax = new Vector2(20f, 140f);
            fx.emitter.Swirl = 18f;
            fx.emitter.SwirlFrequency = 3f;
            fx.emitter.MaxParticles = 40;
            return fx;
        }

        public void Play(Vector2 centre, Vector2 cell, Color colour, bool dust, Sprite sprite, float seconds)
        {
            emitSeconds = seconds * 0.85f;
            emitter.Clear();
            emitter.Sprite = sprite;
            emitter.Rate = dust ? 16f : 12f;
            emitter.SizeRange = dust ? new Vector2(8f, 16f) : new Vector2(12f, 24f);
            emitter.Area = new Vector2(cell.x * 0.6f, 20f);
            emitter.StartColor = WithAlpha(colour, 0.9f);
            emitter.EndColor = WithAlpha(colour, 0f);
            Begin(centre - new Vector2(0f, cell.y * 0.3f), seconds + Linger);
        }

        protected override void Animate(float time, float seconds)
        {
            emitter.Intensity = time < emitSeconds ? 1f : 0f;
        }
    }
}
