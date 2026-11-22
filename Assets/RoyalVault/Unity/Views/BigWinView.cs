using System.Collections;
using System.Collections.Generic;
using GameKit;
using PrimeTween;
using SlotKit;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

namespace RoyalVault.Unity
{
    // Big win, jackpot and free spins celebrations
    public enum CelebrationKind
    {
        BigWin, MegaWin, EpicWin, Jackpot, FreeSpinsAwarded, FreeSpinsTotal, Title
    }

    // One celebration waiting its turn: what it says, what it counts and for how long
    public readonly struct Celebration
    {
        public readonly string Heading;
        public readonly string Line;
        public readonly long Value;
        public readonly float Seconds;
        public readonly bool Coins;
        public readonly CelebrationKind Kind;

        public Celebration(string heading, string line, long value, float seconds, bool coins, CelebrationKind kind)
        {
            Heading = heading;
            Line = line;
            Value = value;
            Seconds = seconds;
            Coins = coins;
            Kind = kind;
        }
    }

    // A dialog on the overlay layer: it pops its heading in, counts, showers coins and poses the heroes, then fades away
    public sealed class BigWinView : DialogView
    {
        [SerializeField]
        private Image banner;
        [SerializeField]
        private TextMeshProUGUI title;
        [SerializeField]
        private TextMeshProUGUI subtitle;
        [SerializeField]
        private CountingText amount;
        [SerializeField]
        private RectTransform rays;
        [Tooltip("Optional vault ring behind every celebration, turning the other way to the rays")]
        [SerializeField]
        private RectTransform ring;
        [SerializeField]
        private UiParticleEmitter coins;
        [SerializeField]
        private Button skip;
        [Header("Heroes")]
        [SerializeField]
        private Image[] heroes = new Image[0];
        [SerializeField]
        private CharacterAnimation[] heroMotion = new CharacterAnimation[0];
        [SerializeField]
        private Vector2[] heroHome = new Vector2[0];

        private readonly Queue<Celebration> requests = new Queue<Celebration>(4);
        private Celebration current;
        private bool skipped;
        private float heroClock;
        private RectTransform popping;
        private readonly WaitForSeconds settle = new WaitForSeconds(0.6f);

        // A celebration is on screen or waiting to be
        public bool IsShowing => IsOpen || requests.Count > 0;

        // A celebration opens (what it announces, and whether an amount counts up)
        public event System.Action<CelebrationKind, bool> Opened;
        // The celebration has faded away
        public event System.Action Closed;

        public void Setup(Image bannerImage, TextMeshProUGUI titleText, TextMeshProUGUI subtitleText, CountingText amountText,
                          RectTransform raysRect, UiParticleEmitter coinShower, Button skipButton)
        {
            banner = bannerImage;
            title = titleText;
            subtitle = subtitleText;
            amount = amountText;
            rays = raysRect;
            coins = coinShower;
            skip = skipButton;
        }

        // Heroes beside the banner: their images, baked animations and where each stands (feet, canvas pixels)
        public void SetupHeroes(Image[] heroImages, CharacterAnimation[] motion, Vector2[] home)
        {
            heroes = heroImages;
            heroMotion = motion;
            heroHome = home;
        }

        private bool HasHero(int i) => i < heroMotion.Length && heroMotion[i] != null && heroMotion[i].IsValid;

        // Shows the heroes' frames at t seconds and slides them in by enter (0..1)
        private void PoseHeroes(float t, float enter)
        {
            for (int i = 0; i < heroes.Length; i++)
            {
                if (!HasHero(i))
                {
                    heroes[i].gameObject.SetActive(false);
                    continue;
                }
                CharacterAnimation motion = heroMotion[i];
                CharacterAnimation.Clip clip = motion.action.IsValid ? motion.action : motion.idle;
                heroes[i].gameObject.SetActive(true);
                heroes[i].sprite = clip.frames[Mathf.FloorToInt(t * motion.framesPerSecond) % clip.frames.Length];
                Vector2 home = heroHome[i];
                float side = Mathf.Sign(home.x);
                heroes[i].rectTransform.anchoredPosition = home + new Vector2(side * 520f * (1f - Easing.Evaluate(enter, Ease.OutCubic)), 0f);
                heroes[i].color = new Color(1f, 1f, 1f, enter);
            }
        }

        protected override void Awake()
        {
            base.Awake();
            skip.onClick.AddListener(() => skipped = true);
            coins.Intensity = 0f;
        }

        // Celebrates a big win or a jackpot; finishes when the count is done and the player has seen it
        public IEnumerator ShowWin(WinTier tier, long value, string jackpotName)
        {
            string heading = jackpotName != null ? jackpotName + " JACKPOT" : tier == WinTier.Epic ? "EPIC WIN" : tier == WinTier.Mega ? "MEGA WIN" : null;
            float seconds = jackpotName != null ? 5f : tier == WinTier.Epic ? 5f : tier == WinTier.Mega ? 4f : 3f;
            CelebrationKind kind = jackpotName != null ? CelebrationKind.Jackpot : tier == WinTier.Epic ? CelebrationKind.EpicWin
                                 : tier == WinTier.Mega ? CelebrationKind.MegaWin : CelebrationKind.BigWin;
            yield return Show(new Celebration(heading, null, value, seconds, true, kind));
        }

        // A short title card without an amount (or with one when value is above 0)
        public IEnumerator ShowTitle(string heading, string line, long value, float seconds, CelebrationKind kind = CelebrationKind.Title)
        {
            yield return Show(new Celebration(heading, line, value, seconds, value > 0, kind));
        }

        public void SetupRing(RectTransform vaultRing)
        {
            ring = vaultRing;
        }

        // Waits its turn with the dialog manager, then runs until it has faded away
        private IEnumerator Show(Celebration celebration)
        {
            requests.Enqueue(celebration);
            yield return Present();
        }

        protected override void OnOpening()
        {
            current = requests.Dequeue();
            // without banner art a plain big win is spelled out
            string heading = current.Heading == null && banner.sprite == null ? "BIG WIN" : current.Heading;
            Open(heading, current.Line, current.Value, current.Seconds);
            Opened?.Invoke(current.Kind, current.Value > 0);
            heroClock = 0f;
            popping = heading == null ? banner.rectTransform : title.rectTransform;
        }

        // Pops in, holds while the amount counts, settles if skipped, then fades out with the heroes still moving
        protected override IEnumerator Content()
        {
            long value = current.Value;
            yield return Tween.Custom(this, 0f, 1f, 0.35f, (view, k) => view.PopIn(k), Ease.Linear).ToYieldInstruction();
            yield return Hold(value > 0 ? current.Seconds + 1.4f : current.Seconds, current.Coins ? current.Seconds : 0f);
            if (skipped && value > 0)
            {
                amount.Set(value);
                yield return settle;
            }

            yield return Tween.Custom(this, 0f, 1f, 0.3f, (view, k) => view.FadeOut(k), Ease.Linear).ToYieldInstruction();
        }

        protected override void OnClosed()
        {
            coins.Intensity = 0f;
            coins.Clear();
            Closed?.Invoke();
        }

        // Shows the BIG WIN artwork when there is no other heading
        private void Open(string heading, string line, long value, float countSeconds)
        {
            skipped = false;
            banner.gameObject.SetActive(heading == null);
            title.gameObject.SetActive(heading != null);
            title.text = heading ?? "";
            subtitle.gameObject.SetActive(line != null);
            subtitle.text = line ?? "";
            amount.gameObject.SetActive(value > 0);
            if (value > 0)
            {
                amount.Roll(0, value, countSeconds);
            }
        }

        private void PopIn(float k)
        {
            Group.alpha = k;
            popping.localScale = Vector3.one * Mathf.LerpUnclamped(0.6f, 1f, Easing.Evaluate(k, Ease.OutBack));
        }

        // Turns the rays, poses the heroes and showers coins for coinSeconds
        private IEnumerator Hold(float seconds, float coinSeconds)
        {
            const float stride = 0.55f;
            for (float t = 0f; t < seconds && !skipped;)
            {
                t += Time.deltaTime;
                heroClock += Time.deltaTime;
                PoseHeroes(heroClock, Mathf.Clamp01(heroClock / stride));
                if (rays != null)
                {
                    rays.localRotation = Quaternion.Euler(0f, 0f, -t * 12f);
                }

                if (ring != null)
                {
                    ring.localRotation = Quaternion.Euler(0f, 0f, t * 6f);
                }

                coins.Intensity = t < coinSeconds ? 1f : 0f;
                yield return null;
            }

            coins.Intensity = 0f;
        }

        private void FadeOut(float k)
        {
            Group.alpha = 1f - k;
            heroClock += Time.deltaTime;
            PoseHeroes(heroClock, 1f);
        }

    }
}
