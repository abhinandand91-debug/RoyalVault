using UnityEngine;
using UnityEngine.Pool;

namespace RoyalVault.Unity
{
    // A pooled win effect that animates itself each frame until its time is up
    public abstract class SymbolFx : MonoBehaviour
    {
        private float elapsed;
        private float duration;

        public IObjectPool<SymbolFx> Pool { get; set; }

        // Advances the effect; false once it has finished
        public bool Tick(float deltaTime)
        {
            elapsed += deltaTime;
            if (elapsed >= duration)
            {
                return false;
            }

            Animate(elapsed, duration);
            return true;
        }

        public void Release()
        {
            Pool.Release(this);
        }

        protected void Begin(Vector2 centre, float seconds)
        {
            elapsed = 0f;
            duration = seconds;
            RectTransform rect = (RectTransform)transform;
            rect.anchoredPosition = centre;
            rect.SetAsLastSibling();
            gameObject.SetActive(true);
            Animate(0f, seconds);
        }

        protected abstract void Animate(float time, float seconds);

        // Fades in fast and out over the last fifth
        protected static float Envelope(float time, float seconds)
        {
            float fadeIn = Mathf.Clamp01(time * 6f);
            float fadeOut = Mathf.Clamp01((seconds - time) / Mathf.Max(0.05f, seconds * 0.2f));
            return fadeIn * fadeOut;
        }

        protected static Color WithAlpha(Color colour, float alpha)
        {
            return new Color(colour.r, colour.g, colour.b, alpha);
        }
    }
}
