using System.Collections;
using GameKit;
using PrimeTween;
using UnityEngine.UI;
using UnityEngine;

namespace RoyalVault.Unity
{
    // A guardian beside the reels
    public sealed class CharacterView : MonoBehaviour
    {
        [SerializeField]
        private Image image;
        [SerializeField]
        private float phase;
        [Header("Effects")]
        [SerializeField]
        private Image aura;
        [SerializeField]
        private RectTransform hand;
        [SerializeField]
        private Image handGlow;
        [SerializeField]
        private UiParticleEmitter flame;
        [SerializeField]
        private UiParticleEmitter embers;
        [SerializeField]
        private UiParticleEmitter sparks;
        [SerializeField]
        private UiParticleEmitter dust;
        [Tooltip("Shows the last pose fading out over the new one")]
        [SerializeField]
        private Image poseFade;

        private const float CalmDelay = 14f;
        private const float FloatHeight = 5f;
        private const float FloatSpeed = Mathf.PI * 2f / 2.4f;

        private Sprite[] poses = new Sprite[0];
        private GuardianPose pose;
        private Tween fading;
        private float calmAt;
        private Vector2 handHome;
        private CharacterAnimation motion;  // the baked frames, or null for the still art
        private Vector2 staticAnchor;
        private CharacterAnimation.Clip clip;
        private float clipTime;
        private float nextAction;
        private float flameWeight = 1f;

        private Coroutine celebrate;
        private Coroutine throwing;
        private Sequence recoil;
        private Tween lifting;
        private readonly WaitForSeconds celebrateHold = new WaitForSeconds(0.7f);
        private float lift;
        private float lunge;
        private Vector2 figureHome;
        private bool homeKnown;
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

        public void SetupPoses(Image fadeImage)
        {
            poseFade = fadeImage;
        }

        // Gives the guardian its stills, in GuardianPose order
        public void BindPoses(Sprite[] stills)
        {
            poses = stills ?? new Sprite[0];
            calmAt = Time.time + CalmDelay;
        }

        // Crossfades to a pose; a pose the theme lacks is ignored
        public void ShowPose(GuardianPose next)
        {
            int index = (int)next;
            if (next == pose || index >= poses.Length || poses[index] == null)
            {
                return;
            }

            pose = next;
            poseFade.sprite = image.sprite;
            poseFade.gameObject.SetActive(true);
            image.sprite = poses[index];
            fading.Stop();
            fading = Tween.Alpha(poseFade, 1f, 0f, 0.25f, Ease.OutCubic)
                .OnComplete(poseFade, faded => faded.gameObject.SetActive(false));
        }

        // Hides the figure while a full-screen moment shows the guardian in its own pose
        public void SetShown(bool shown)
        {
            image.enabled = shown;
            aura.enabled = shown;
            hand.gameObject.SetActive(shown);
            if (!shown)
            {
                poseFade.gameObject.SetActive(false);
            }
        }

        // A new spin: the guardian raises its flame again
        public void Wake()
        {
            calmAt = Time.time + CalmDelay;
            if (celebrate == null)
            {
                ShowPose(GuardianPose.Stance);
            }
        }

        // effectAnchor is where the magic burns, as a share of the art
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
            if (sprite == null || hand == null)
            {
                return;
            }

            PlaceHand(effectAnchor);
            handGlow.color = new Color(magic.r, magic.g, magic.b, 0.6f);
            flame.StartColor = new Color(Mathf.Lerp(magic.r, 1f, 0.55f), Mathf.Lerp(magic.g, 1f, 0.55f), Mathf.Lerp(magic.b, 1f, 0.55f), 0.95f);
            flame.EndColor = new Color(magic.r * 0.5f, magic.g * 0.6f, magic.b, 0f);
            embers.StartColor = new Color(0.85f, 0.97f, 1f, 1f);
            embers.EndColor = new Color(magic.r, magic.g, magic.b, 0f);
            nextSurge = Time.time + 3f + phase;
        }

        // True from the moment a throw's flame is up in the hand until it has been thrown
        public bool ReadyToThrow { get; private set; }

        // Where the magic burns, in world space (the fireball leaves from here)
        public Vector3 HandPosition => hand != null ? hand.position : transform.position;

        // Leans towards the reels and calls up the flame
        public void WindUp()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            StopThrow();
            calmAt = Time.time + CalmDelay;
            ShowPose(GuardianPose.Stance);
            throwing = StartCoroutine(WindUpRoutine());
        }

        // The flame leaves the hand: a recoil, a flash in the hand, then back to rest
        public void Release()
        {
            ReadyToThrow = false;
            if (!isActiveAndEnabled)
            {
                return;
            }

            StopThrow();
            if (flame != null)
            {
                flame.Burst(30, 3f);
                sparks.Burst(20, 1.2f);
            }

            recoil = Sequence.Create()
                .Chain(Tween.Custom(this, 1f, 1.35f, 0.12f, (view, value) => view.lunge = value, Ease.OutCubic))
                .Chain(Tween.Custom(this, 1.35f, 0f, 0.6f, (view, value) => view.lunge = value, Ease.InOutSine));
        }

        private void StopThrow()
        {
            if (throwing != null)
            {
                StopCoroutine(throwing);
                throwing = null;
            }

            recoil.Stop();
        }

        private IEnumerator WindUpRoutine()
        {
            ReadyToThrow = false;
            if (motion != null && !IsActing)
            {
                Act();
            }

            float t = 0f;
            // lean in, then hold until the flame is up
            while (true)
            {
                t += Time.deltaTime;
                lunge = Easing.Evaluate(Mathf.Clamp01(t / 0.35f), Ease.OutCubic);
                surge = Mathf.Max(surge, 0.6f);
                bool flameUp = motion == null ? t >= 0.4f : flameWeight > 0.6f && t >= 0.35f;
                if (flameUp || t > 1.6f)
                {
                    break;
                }

                yield return null;
            }
            ReadyToThrow = true;
            throwing = null;
        }

        // The hand's flame flares up and throws embers (the guardians watch the reels being anticipated)
        public void Surge()
        {
            if (!isActiveAndEnabled || hand == null)
            {
                return;
            }

            surge = 1f;
            embers.Burst(10, 1.6f);
        }

        // True while the one-shot action plays
        public bool IsActing => motion != null && clip == motion.action;

        // Plays the action once (the guardian calls up its flame), then returns to the idle loop
        public void Act()
        {
            if (motion == null || !motion.action.IsValid)
            {
                return;
            }

            clip = motion.action;
            clipTime = 0f;
            nextAction = Time.time + Random.Range(12f, 20f);
        }

        private void PlaceHand(Vector2 share)
        {
            Vector2 size = image.rectTransform.sizeDelta;
            float mirror = Mathf.Sign(image.rectTransform.localScale.x);
            handHome = new Vector2((share.x - 0.5f) * size.x * mirror, share.y * size.y);
            hand.anchoredPosition = handHome;
        }

        // Shows the current frame of the baked animation and moves the hand effects with it
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
            if (clip == motion.idle && Time.time >= nextAction)
            {
                Act();
                frame = 0;
            }
            frame %= clip.frames.Length;

            image.sprite = clip.frames[frame];
            if (hand == null)
            {
                return;
            }

            if (frame < clip.hand.Length)
            {
                PlaceHand(clip.hand[frame]);
            }

            float target = frame < clip.flame.Length ? clip.flame[frame] : 1f;
            flameWeight = Mathf.MoveTowards(flameWeight, target, Time.deltaTime * 6f);
        }

        private void Update()
        {
            Animate(Time.time + phase);
        }

        // The idle breath and pose, and the magic in the guardian's hand
        private void Animate(float time)
        {
            float breath = Mathf.Sin(time * 1.4f) * 0.5f + 0.5f;
            Pose(time, breath);
            if (hand == null)
            {
                return;
            }

            FlareNow();
            Burn(time, breath);
        }

        // Leans towards the reels on a throw; the still art also breathes and sways
        private void Pose(float time, float breath)
        {
            RectTransform r = image.rectTransform;
            float sign = Mathf.Sign(r.localScale.x);
            if (!homeKnown)
            {
                figureHome = r.anchoredPosition;
                homeKnown = true;
            }
            r.anchoredPosition = figureHome + new Vector2(sign * 34f * lunge, 6f * lunge);
            float lean = -sign * 6f * lunge;
            if (motion != null)
            {
                float grow = 1f + lift * 0.03f + lunge * 0.04f;
                r.localScale = new Vector3(sign * grow, grow, 1f);
                r.localRotation = Quaternion.Euler(0f, 0f, lean);
                Animate();
                return;
            }
            if (poses.Length > 0)
            {
                Float(time, sign, lean);
                return;
            }

            r.localScale = new Vector3(sign * (1f + lift * 0.04f), 1f + 0.012f * breath + lift * 0.05f, 1f);
            r.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 0.6f) * 0.35f + lean);
        }

        // A posed guardian hovers a few pixels and settles into its calm stance when nothing has happened for a while
        private void Float(float time, float sign, float lean)
        {
            RectTransform r = image.rectTransform;
            Vector2 drift = new Vector2(0f, Mathf.Sin(time * FloatSpeed) * FloatHeight);
            r.anchoredPosition += drift;
            float grow = 1f + lift * 0.04f + lunge * 0.03f;
            r.localScale = new Vector3(sign * grow, grow, 1f);
            r.localRotation = Quaternion.Euler(0f, 0f, lean);
            if (hand != null)
            {
                hand.anchoredPosition = handHome + drift;
            }

            if (pose == GuardianPose.Stance && celebrate == null && throwing == null && Time.time >= calmAt)
            {
                ShowPose(GuardianPose.Calm);
            }

            float target = pose == GuardianPose.Stance ? 1f : 0f;
            flameWeight = Mathf.MoveTowards(flameWeight, target, Time.deltaTime * 6f);
        }

        // The flame flares up every few seconds
        private void FlareNow()
        {
            if (Time.time >= nextSurge)
            {
                nextSurge = Time.time + Random.Range(5f, 9f);
                surge = 1f;
                embers.Burst(8, 1.4f);
            }
            surge = Mathf.MoveTowards(surge, 0f, Time.deltaTime * 1.3f);
        }

        private void Burn(float time, float breath)
        {
            bool posed = poses.Length > 0;
            float baseFlame = posed ? 0.3f : motion != null ? 0.45f : 1f;
            flame.Intensity = flameWeight * (baseFlame + surge * 2f + lift * 3f);
            embers.Intensity = flameWeight;
            float flicker = (0.55f + 0.2f * Mathf.PerlinNoise(time * 7f, phase) + surge * 0.35f + lift * 0.4f) * flameWeight;
            Color g = handGlow.color;
            handGlow.color = new Color(g.r, g.g, g.b, Mathf.Clamp01(flicker));
            handGlow.rectTransform.localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(time * 9f) + surge * 0.5f + lift * 0.8f);
            Color a = aura.color;
            float auraRest = posed ? 0.2f + 0.12f * breath : 0.12f + 0.06f * breath;
            aura.color = new Color(a.r, a.g, a.b, auraRest + lift * 0.45f);
        }

        public void Celebrate(GuardianPose victory = GuardianPose.Win)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (celebrate != null)
            {
                StopCoroutine(celebrate);
            }

            lifting.Stop();
            ShowPose(victory);
            celebrate = StartCoroutine(CelebrateRoutine());
        }

        private IEnumerator CelebrateRoutine()
        {
            if (motion != null && !IsActing)
            {
                // the guardian calls up its flame; the eruption comes as the flame appears in its hand
                Act();
                while (IsActing && flameWeight < 0.5f)
                {
                    yield return null;
                }
            }
            if (flame != null)
            {
                flame.Burst(50, 2.8f);
                sparks.Burst(46, 1f);
                embers.Burst(20, 2f);
            }
            lifting = Tween.Custom(this, 0f, 1f, 0.25f, (view, value) => view.Lift(value), Ease.OutCubic);
            yield return lifting.ToYieldInstruction();
            if (sparks != null)
            {
                sparks.Burst(24, 0.8f);
            }

            yield return celebrateHold;
            lifting = Tween.Custom(this, 1f, 0f, 0.6f, (view, value) => view.Lift(value), Ease.InOutSine);
            yield return lifting.ToYieldInstruction();
            celebrate = null;
            calmAt = Time.time + CalmDelay;
            ShowPose(GuardianPose.Stance);
        }

        // Raises the figure in a warm light; 0 is at rest
        private void Lift(float amount)
        {
            lift = amount;
            image.color = Color.Lerp(Color.white, new Color(1.4f, 1.25f, 0.9f), amount);
        }
    }
}
