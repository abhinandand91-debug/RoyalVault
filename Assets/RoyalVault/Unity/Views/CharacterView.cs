using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalVault.Unity
{
    /// <summary>
    /// A guardian beside the reels. With a baked <see cref="CharacterAnimation"/> it plays its idle loop and, on big
    /// wins and now and then on its own, its action: it lowers its hand and calls up a blue flame. The particle flame,
    /// embers and glow follow the hand frame by frame. Without the animation the still art breathes and sways.
    /// A warm aura glows behind it and gold dust drifts past; on big wins it rises in a flash of light and gold sparks
    /// spray from its hand. When the theme has no character art (the licensed figure is not in a public checkout) the
    /// whole guardian hides itself.
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

        private CharacterAnimation motion;  // the baked frames, or null for the still art
        private Vector2 staticAnchor;
        private CharacterAnimation.Clip clip;
        private float clipTime;
        private float nextAction;
        private float flameWeight = 1f;

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
        public void Bind(Sprite sprite, Vector2 effectAnchor, Color magic, CharacterAnimation baked = null)
        {
            motion = baked != null && baked.IsValid ? baked : null;
            if (motion != null)
            {
                sprite = motion.idle.frames[0];
                image.rectTransform.sizeDelta = new Vector2(motion.displayHeight * motion.Aspect, motion.displayHeight);
                clip = motion.idle;
                clipTime = phase;
                nextAction = Time.time + 6f + phase * 4f;
            }
            image.sprite = sprite;
            gameObject.SetActive(sprite != null);
            staticAnchor = effectAnchor;
            if (sprite == null || hand == null) return;

            PlaceHand(effectAnchor);
            handGlow.color = new Color(magic.r, magic.g, magic.b, 0.6f);
            flame.StartColor = new Color(Mathf.Lerp(magic.r, 1f, 0.55f), Mathf.Lerp(magic.g, 1f, 0.55f), Mathf.Lerp(magic.b, 1f, 0.55f), 0.95f);
            flame.EndColor = new Color(magic.r * 0.5f, magic.g * 0.6f, magic.b, 0f);
            embers.StartColor = new Color(0.85f, 0.97f, 1f, 1f);
            embers.EndColor = new Color(magic.r, magic.g, magic.b, 0f);
            nextSurge = Time.time + 3f + phase;
        }

        /// <summary>True while the one-shot action plays.</summary>
        public bool IsActing => motion != null && clip == motion.action;

        /// <summary>Plays the action once (the guardian calls up its flame), then returns to the idle loop.</summary>
        public void Act()
        {
            if (motion == null || !motion.action.IsValid) return;
            clip = motion.action;
            clipTime = 0f;
            nextAction = Time.time + Random.Range(12f, 20f);
        }

        private void PlaceHand(Vector2 share)
        {
            Vector2 size = image.rectTransform.sizeDelta;
            float mirror = Mathf.Sign(image.rectTransform.localScale.x);
            hand.anchoredPosition = new Vector2((share.x - 0.5f) * size.x * mirror, share.y * size.y);
        }

        /// <summary>Shows the current frame of the baked animation and moves the hand effects with it.</summary>
        private void Animate()
        {
            clipTime += Time.deltaTime;
            float fps = motion.framesPerSecond;
            int frame = Mathf.FloorToInt(clipTime * fps);
            if (clip == motion.action && frame >= clip.frames.Length)
            {
                clip = motion.idle;
                clipTime = 0f;
                frame = 0;
            }
            if (clip == motion.idle && Time.time >= nextAction) { Act(); frame = 0; }
            frame %= clip.frames.Length;

            image.sprite = clip.frames[frame];
            if (hand == null) return;
            if (frame < clip.hand.Length) PlaceHand(clip.hand[frame]);
            float target = frame < clip.flame.Length ? clip.flame[frame] : 1f;
            flameWeight = Mathf.MoveTowards(flameWeight, target, Time.deltaTime * 6f);
        }

        private void Update()
        {
            float time = Time.time + phase;
            float breath = Mathf.Sin(time * 1.4f) * 0.5f + 0.5f;
            float sign = Mathf.Sign(image.rectTransform.localScale.x);

            if (motion != null)
            {
                // the frames carry the breathing; only the celebration lift is added
                image.rectTransform.localScale = new Vector3(sign * (1f + lift * 0.03f), 1f + lift * 0.03f, 1f);
                Animate();
            }
            else
            {
                // a slow breath from the feet and the faintest sway
                image.rectTransform.localScale = new Vector3(sign * (1f + lift * 0.04f), 1f + 0.012f * breath + lift * 0.05f, 1f);
                image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 0.6f) * 0.35f);
            }

            if (hand == null) return;

            // the flame surges every few seconds
            if (Time.time >= nextSurge)
            {
                nextSurge = Time.time + Random.Range(5f, 9f);
                surge = 1f;
                embers.Burst(8, 1.4f);
            }
            surge = Mathf.MoveTowards(surge, 0f, Time.deltaTime * 1.3f);
            float baseFlame = motion != null ? 0.45f : 1f;
            flame.Intensity = flameWeight * (baseFlame + surge * 2f + lift * 3f);
            embers.Intensity = flameWeight;

            float flicker = (0.55f + 0.2f * Mathf.PerlinNoise(time * 7f, phase) + surge * 0.35f + lift * 0.4f) * flameWeight;
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
            if (motion != null && !IsActing)
            {
                // the guardian calls up its flame; the eruption comes as the flame appears in its hand
                Act();
                while (IsActing && flameWeight < 0.5f) yield return null;
            }
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
