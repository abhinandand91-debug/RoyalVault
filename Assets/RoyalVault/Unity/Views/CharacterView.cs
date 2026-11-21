using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalVault.Unity
{
    /// <summary>
    /// A guardian beside the reels. While idle it breathes and sways a little, its hand burns with a living blue flame
    /// that throws embers, a warm aura glows behind it and gold dust drifts past; every few seconds the flame surges.
    /// On big wins it rises in a flash of light, the flame erupts and gold sparks spray from its hand. When the theme
    /// has no character art (the licensed figure is not in a public checkout) the whole guardian hides itself.
    /// </summary>
    public sealed class CharacterView : MonoBehaviour
    {
        [SerializeField] private Image image;
        [SerializeField] private float phase;
        [Header("Effects")]
        [SerializeField] private Image aura;
        [SerializeField] private RectTransform hand;
        [SerializeField] private Image handGlow;
        [SerializeField] private UiParticleEmitter flame;
        [SerializeField] private UiParticleEmitter embers;
        [SerializeField] private UiParticleEmitter sparks;
        [SerializeField] private UiParticleEmitter dust;

        private Coroutine celebrate;
        private float lift;
        private float surge;
        private float nextSurge;

        public void Setup(Image characterImage, float idlePhase)
        {
            image = characterImage;
            phase = idlePhase;
        }

        public void SetupEffects(Image auraImage, RectTransform handAnchor, Image handGlowImage, UiParticleEmitter flameEmitter,
                                 UiParticleEmitter emberEmitter, UiParticleEmitter sparkEmitter, UiParticleEmitter dustEmitter)
        {
            aura = auraImage;
            hand = handAnchor;
            handGlow = handGlowImage;
            flame = flameEmitter;
            embers = emberEmitter;
            sparks = sparkEmitter;
            dust = dustEmitter;
        }

        /// <summary>
        /// Shows <paramref name="sprite"/>; <paramref name="effectAnchor"/> is where the magic burns, as a share of the
        /// art (0,0 = bottom left of the unmirrored figure).
        /// </summary>
        public void Bind(Sprite sprite, Vector2 effectAnchor, Color magic)
        {
            image.sprite = sprite;
            gameObject.SetActive(sprite != null);
            if (sprite == null || hand == null) return;

            Vector2 size = image.rectTransform.sizeDelta;
            float mirror = Mathf.Sign(image.rectTransform.localScale.x);
            hand.anchoredPosition = new Vector2((effectAnchor.x - 0.5f) * size.x * mirror, effectAnchor.y * size.y);
            handGlow.color = new Color(magic.r, magic.g, magic.b, 0.6f);
            flame.StartColor = new Color(Mathf.Lerp(magic.r, 1f, 0.55f), Mathf.Lerp(magic.g, 1f, 0.55f), Mathf.Lerp(magic.b, 1f, 0.55f), 0.95f);
            flame.EndColor = new Color(magic.r * 0.5f, magic.g * 0.6f, magic.b, 0f);
            embers.StartColor = new Color(0.85f, 0.97f, 1f, 1f);
            embers.EndColor = new Color(magic.r, magic.g, magic.b, 0f);
            nextSurge = Time.time + 3f + phase;
        }

        private void Update()
        {
            float time = Time.time + phase;

            // a slow breath from the feet and the faintest sway
            float breath = Mathf.Sin(time * 1.4f) * 0.5f + 0.5f;
            float sign = Mathf.Sign(image.rectTransform.localScale.x);
            image.rectTransform.localScale = new Vector3(sign * (1f + lift * 0.04f), 1f + 0.012f * breath + lift * 0.05f, 1f);
            image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 0.6f) * 0.35f);

            if (hand == null) return;

            // the flame surges every few seconds
            if (Time.time >= nextSurge)
            {
                nextSurge = Time.time + Random.Range(5f, 9f);
                surge = 1f;
                embers.Burst(8, 1.4f);
            }
            surge = Mathf.MoveTowards(surge, 0f, Time.deltaTime * 1.3f);
            flame.Intensity = 1f + surge * 2.5f + lift * 3f;

            float flicker = 0.55f + 0.2f * Mathf.PerlinNoise(time * 7f, phase) + surge * 0.35f + lift * 0.4f;
            Color g = handGlow.color;
            handGlow.color = new Color(g.r, g.g, g.b, Mathf.Clamp01(flicker));
            handGlow.rectTransform.localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(time * 9f) + surge * 0.5f + lift * 0.8f);

            float glow = 0.12f + 0.06f * breath + lift * 0.45f;
            Color a = aura.color;
            aura.color = new Color(a.r, a.g, a.b, glow);
        }

        public void Celebrate()
        {
            if (!isActiveAndEnabled) return;
            if (celebrate != null) StopCoroutine(celebrate);
            celebrate = StartCoroutine(CelebrateRoutine());
        }

        private IEnumerator CelebrateRoutine()
        {
            if (flame != null)
            {
                flame.Burst(50, 2.8f);
                sparks.Burst(46, 1f);
                embers.Burst(20, 2f);
            }
            yield return Tween.Run(0.25f, k => { lift = k; image.color = Color.Lerp(Color.white, new Color(1.4f, 1.25f, 0.9f), k); }, Ease.OutCubic);
            if (sparks != null) sparks.Burst(24, 0.8f);
            yield return new WaitForSeconds(0.7f);
            yield return Tween.Run(0.6f, k => { lift = 1f - k; image.color = Color.Lerp(new Color(1.4f, 1.25f, 0.9f), Color.white, k); }, Ease.InOutSine);
            celebrate = null;
        }
    }
}
