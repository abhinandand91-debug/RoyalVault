using System.Collections;
using GameKit;
using PrimeTween;
using UnityEngine.UI;
using UnityEngine;

namespace RoyalVault.Unity
{
    // One symbol on a reel: its art, the motion-blurred art while spinning, and its moments of life
    public sealed class SymbolView : MonoBehaviour
    {
        private static readonly int ShinePos = Shader.PropertyToID("_ShinePos");
        private static readonly int ShineColor = Shader.PropertyToID("_ShineColor");
        private static readonly int Flash = Shader.PropertyToID("_Flash");

        private Image image;
        private Image glow;
        private Material shared;
        private Material own;
        private ThemeDefinition.SymbolArt art;
        private Coroutine pulse;
        private Tween glint;
        private Sequence landing;
        private bool blurred;
        private float flash;

        public int SymbolIndex { get; private set; } = -1;
        public RectTransform Rect
        {
            get
            {
                return (RectTransform)transform;
            }
        }
        public ThemeDefinition.SymbolArt Art => art;
        public SymbolStyle Style => art != null ? art.style : SymbolStyle.Card;
        public bool IsCelebrating => pulse != null;

        public static SymbolView Create(Transform parent, Vector2 size, Sprite glowSprite, Material symbolMaterial = null)
        {
            RectTransform root = UiFactory.Rect("Symbol", parent, Vector2.zero, size);
            var view = root.gameObject.AddComponent<SymbolView>();
            view.glow = UiFactory.Image("Glow", root, glowSprite, Vector2.zero, size * 1.35f);
            view.glow.color = new Color(1f, 0.85f, 0.45f, 0f);
            view.image = UiFactory.Image("Art", root, null, Vector2.zero, size);
            view.image.preserveAspect = true;
            view.image.useSpriteMesh = true;
            // resting symbols share one material so the reels batch together
            view.shared = symbolMaterial;
            view.image.material = symbolMaterial;
            return view;
        }

        private void OnDestroy()
        {
            if (own != null)
            {
                Destroy(own);
            }
        }

        public void SetSymbol(int index, ThemeDefinition.SymbolArt symbolArt)
        {
            if (index == SymbolIndex && symbolArt == art)
            {
                return;
            }

            SymbolIndex = index;
            art = symbolArt;
            Apply();
        }

        public void SetBlur(bool value)
        {
            if (value == blurred)
            {
                return;
            }

            blurred = value;
            Apply();
        }

        private void Apply()
        {
            Sprite sprite = art == null ? null : blurred && art.blur != null ? art.blur : art.sprite;
            image.sprite = sprite;
            image.color = sprite == null ? Color.clear : Color.white;
            if (blurred)
            {
                Shine(-1f);
            }
        }

        // Dims the symbol while other symbols are winning
        public void SetDimmed(bool dimmed)
        {
            if (art == null)
            {
                return;
            }

            image.color = dimmed ? new Color(0.42f, 0.42f, 0.46f, 1f) : Color.white;
        }

        // Pops, pulses and shines while its win is shown
        public void Celebrate(float seconds)
        {
            StopCelebrating();
            bool baked = art != null && art.winAnimation != null && art.winAnimation.IsValid;
            pulse = StartCoroutine(baked ? PlayFrames(seconds) : Pulse(seconds));
        }

        public void StopCelebrating()
        {
            if (pulse != null)
            {
                StopCoroutine(pulse);
            }

            landing.Stop();
            glint.Stop();
            pulse = null;
            image.rectTransform.localScale = Vector3.one;
            image.rectTransform.localRotation = Quaternion.identity;
            glow.color = new Color(glow.color.r, glow.color.g, glow.color.b, 0f);
            Shine(-1f);
            if (art != null && !blurred)
            {
                image.sprite = art.sprite;
            }
        }

        // A special symbol lands: a squash, a settle and a flash of light
        public void Land()
        {
            if (art == null || !isActiveAndEnabled)
            {
                return;
            }

            Lit();
            flash = 0.75f;
            landing.Stop();
            var squashed = new Vector3(1.08f, 0.9f, 1f);
            landing = Sequence.Create()
                .Chain(Tween.Custom(this, Vector3.one, squashed, 0.08f, (view, scale) => view.Squash(scale), Ease.OutCubic))
                .Chain(Tween.Custom(this, squashed, Vector3.one, 0.24f, (view, scale) => view.Squash(scale), Ease.OutBack));
        }

        // A single band of light passes over the symbol (idle reels)
        public void Glint()
        {
            if (art == null || pulse != null || glint.isAlive || !isActiveAndEnabled)
            {
                return;
            }

            Material lit = Lit();
            if (lit != null)
            {
                lit.SetColor(ShineColor, new Color(1f, 0.96f, 0.85f, 1f));
            }

            glint = Tween.Custom(this, -0.4f, 1.4f, 0.75f, (view, position) => view.Shine(position), Ease.InOutSine)
                .OnComplete(this, view => view.Shine(-1f));
        }

        private void Shine(float position)
        {
            if (own != null)
            {
                own.SetFloat(ShinePos, position);
            }
        }

        private void Update()
        {
            if (own == null || image.material != own)
            {
                return;
            }

            flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime * 2.2f);
            own.SetFloat(Flash, flash);
            if (flash <= 0f && pulse == null && !glint.isAlive)
            {
                own.SetFloat(ShinePos, -1f);
                image.material = shared;
            }
        }

        private Color GlowColour()
        {
            Color tint = art != null ? art.tint : new Color(1f, 0.85f, 0.45f, 1f);
            return new Color(tint.r, tint.g, tint.b, 0f);
        }

        private IEnumerator Pulse(float seconds)
        {
            SymbolStyle style = Style;
            Color g = GlowColour();
            Material lit = Lit();
            if (lit != null)
            {
                lit.SetColor(ShineColor, Color.Lerp(Color.white, new Color(g.r, g.g, g.b, 1f), 0.35f));
            }

            bool grand = style == SymbolStyle.Wild || style == SymbolStyle.Jackpot || style == SymbolStyle.Scatter;
            float pop = grand ? 1.2f : 1.13f;
            if (grand)
            {
                flash = 0.9f;
            }

            // the pop: in fast with a little overshoot, settling to the pulse
            float t = 0f;
            const float popTime = 0.3f;
            while (t < popTime && t < seconds)
            {
                float k = t / popTime;
                float up = Easing.Evaluate(Mathf.Min(1f, k * 1.7f), Ease.OutBack);
                image.rectTransform.localScale = Vector3.one * Mathf.LerpUnclamped(1f, pop, up) * (1f - 0.06f * k);
                glow.color = new Color(g.r, g.g, g.b, 0.9f * k);
                t += Time.deltaTime;
                yield return null;
            }

            // then a gentle pulse while a shine sweeps across every 1.3 s
            float sweep = 0f;
            while (t < seconds)
            {
                float k = Mathf.Sin((t - popTime) * Mathf.PI * 2f * 0.9f) * 0.5f + 0.5f;
                image.rectTransform.localScale = Vector3.one * (1.04f + 0.06f * k + (grand ? 0.03f : 0f));
                if (style == SymbolStyle.Gem)
                {
                    image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 3.1f) * 2.5f);
                }

                glow.color = new Color(g.r, g.g, g.b, 0.4f + 0.45f * k);
                sweep += Time.deltaTime / 1.3f;
                Shine(Mathf.Lerp(-0.4f, 1.4f, Mathf.Clamp01(Mathf.Repeat(sweep, 1f) / 0.75f)));
                t += Time.deltaTime;
                yield return null;
            }
            pulse = null;
            StopCelebrating();
        }

        // Baked win frames (from an animation tool): played on a loop for the win, then back to the still art
        private IEnumerator PlayFrames(float seconds)
        {
            CharacterAnimation motion = art.winAnimation;
            CharacterAnimation.Clip clip = motion.action.IsValid ? motion.action : motion.idle;
            image.rectTransform.localScale = Vector3.one / Mathf.Clamp(motion.figureShare, 0.1f, 1f);
            Color g = GlowColour();
            float t = 0f;
            while (t < seconds)
            {
                image.sprite = clip.frames[Mathf.FloorToInt(t * motion.framesPerSecond) % clip.frames.Length];
                glow.color = new Color(g.r, g.g, g.b, 0.35f + 0.25f * Mathf.Sin(t * 5f));
                t += Time.deltaTime;
                yield return null;
            }
            pulse = null;
            StopCelebrating();
        }

        // While it shines or flashes, a symbol draws with its own copy of the material
        private Material Lit()
        {
            if (shared == null)
            {
                return null;
            }

            if (own == null)
            {
                own = new Material(shared) { name = shared.name + " (lit)" };
            }

            image.material = own;
            return own;
        }

        // Landing squash, unless a celebration has taken over the scale
        private void Squash(Vector3 scale)
        {
            if (pulse == null)
            {
                image.rectTransform.localScale = scale;
            }
        }
    }
}
