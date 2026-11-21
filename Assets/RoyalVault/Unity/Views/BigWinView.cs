using System.Collections.Generic;
using System.Collections;
using GameKit;
using SlotKit;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

namespace RoyalVault.Unity
{
    // Big win, jackpot and free spins celebrations
    public enum CelebrationKind { BigWin, MegaWin, EpicWin, Jackpot, FreeSpinsAwarded, FreeSpinsTotal, Title }

    public sealed class BigWinView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image banner;
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private TextMeshProUGUI subtitle;
        [SerializeField] private CountingText amount;
        [SerializeField] private RectTransform rays;
        [SerializeField] private RectTransform coinLayer;
        [SerializeField] private Sprite coinSprite;
        [SerializeField] private Button skip;
        [Header("Heroes")]
        [SerializeField] private Image[] heroes = new Image[0];
        [SerializeField] private CharacterAnimation[] heroMotion = new CharacterAnimation[0];
        [SerializeField] private Vector2[] heroHome = new Vector2[0];

        private readonly List<RectTransform> coins = new List<RectTransform>();
        private bool skipped;
        private float heroClock;

        public bool IsShowing { get; private set; }

        // A celebration opens (what it announces, and whether an amount counts up)
        public event System.Action<CelebrationKind, bool> Opened;
        // The celebration has faded away
        public event System.Action Closed;

        public void Setup(CanvasGroup canvasGroup, Image bannerImage, TextMeshProUGUI titleText, TextMeshProUGUI subtitleText,
                          CountingText amountText, RectTransform raysRect, RectTransform coins, Sprite coin, Button skipButton)
        {
            group = canvasGroup;
            banner = bannerImage;
            title = titleText;
            subtitle = subtitleText;
            amount = amountText;
            rays = raysRect;
            coinLayer = coins;
            coinSprite = coin;
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
                if (!HasHero(i)) { heroes[i].gameObject.SetActive(false); continue; }
                CharacterAnimation motion = heroMotion[i];
                CharacterAnimation.Clip clip = motion.action.IsValid ? motion.action : motion.idle;
                heroes[i].gameObject.SetActive(true);
                heroes[i].sprite = clip.frames[Mathf.FloorToInt(t * motion.framesPerSecond) % clip.frames.Length];
                Vector2 home = heroHome[i];
                float side = Mathf.Sign(home.x);
                heroes[i].rectTransform.anchoredPosition = home + new Vector2(side * 520f * (1f - Ease.OutCubic(enter)), 0f);
                heroes[i].color = new Color(1f, 1f, 1f, enter);
            }
        }

        private void Awake()
        {
            skip.onClick.AddListener(() => skipped = true);
            // starts hidden, unless the first activation is a celebration
            if (!IsShowing) Hide();
        }

        private void Hide()
        {
            group.alpha = 0f;
            group.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        // Celebrates a big win or a jackpot; finishes when the count is done and the player has seen it
        public IEnumerator ShowWin(WinTier tier, long value, string jackpotName)
        {
            string heading = jackpotName != null ? jackpotName + " JACKPOT" : tier == WinTier.Epic ? "EPIC WIN" : tier == WinTier.Mega ? "MEGA WIN" : null;
            float seconds = jackpotName != null ? 5f : tier == WinTier.Epic ? 5f : tier == WinTier.Mega ? 4f : 3f;
            CelebrationKind kind = jackpotName != null ? CelebrationKind.Jackpot : tier == WinTier.Epic ? CelebrationKind.EpicWin
                                 : tier == WinTier.Mega ? CelebrationKind.MegaWin : CelebrationKind.BigWin;
            yield return Show(heading, null, value, seconds, true, kind);
        }

        // A short title card without an amount (or with one when value is above 0)
        public IEnumerator ShowTitle(string heading, string line, long value, float seconds, CelebrationKind kind = CelebrationKind.Title)
        {
            yield return Show(heading, line, value, seconds, value > 0, kind);
        }

        private IEnumerator Show(string heading, string line, long value, float countSeconds, bool coinsOn, CelebrationKind kind)
        {
            Open(heading, line, value, countSeconds);
            Opened?.Invoke(kind, value > 0);
            RectTransform hero = heading == null ? banner.rectTransform : title.rectTransform;
            heroClock = 0f;
            yield return Tween.Run(0.35f, k => PopIn(hero, k), Ease.Linear);
            yield return Hold(value > 0 ? countSeconds + 1.4f : countSeconds, coinsOn ? countSeconds : 0f);
            if (skipped && value > 0)
            {
                amount.Set(value);
                yield return new WaitForSeconds(0.6f);
            }
            yield return Tween.Run(0.3f, FadeOut, Ease.Linear);
            Close();
        }

        // Shows the BIG WIN artwork when there is no other heading
        private void Open(string heading, string line, long value, float countSeconds)
        {
            IsShowing = true;
            skipped = false;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            group.blocksRaycasts = true;
            banner.gameObject.SetActive(heading == null);
            title.gameObject.SetActive(heading != null);
            title.text = heading ?? "";
            subtitle.gameObject.SetActive(line != null);
            subtitle.text = line ?? "";
            amount.gameObject.SetActive(value > 0);
            if (value > 0) amount.Roll(0, value, countSeconds);
        }

        private void PopIn(RectTransform hero, float k)
        {
            group.alpha = k;
            hero.localScale = Vector3.one * Mathf.LerpUnclamped(0.6f, 1f, Ease.OutBack(k));
        }

        // Turns the rays, poses the heroes and showers coins for coinSeconds
        private IEnumerator Hold(float seconds, float coinSeconds)
        {
            const float stride = 0.55f;
            float spawn = 0f;
            for (float t = 0f; t < seconds && !skipped;)
            {
                t += Time.deltaTime;
                heroClock += Time.deltaTime;
                PoseHeroes(heroClock, Mathf.Clamp01(heroClock / stride));
                if (rays != null) rays.localRotation = Quaternion.Euler(0f, 0f, -t * 12f);
                if (t < coinSeconds) spawn += Time.deltaTime * 22f;
                for (; spawn >= 1f; spawn -= 1f) StartCoroutine(Coin());
                yield return null;
            }
        }

        private void FadeOut(float k)
        {
            group.alpha = 1f - k;
            heroClock += Time.deltaTime;
            PoseHeroes(heroClock, 1f);
        }

        private void Close()
        {
            foreach (RectTransform coin in coins) coin.gameObject.SetActive(false);
            Hide();
            IsShowing = false;
            Closed?.Invoke();
        }

        private IEnumerator Coin()
        {
            RectTransform coin = null;
            foreach (RectTransform c in coins)
                if (!c.gameObject.activeSelf) { coin = c; break; }
            if (coin == null)
            {
                coin = UiFactory.Image("Coin", coinLayer, coinSprite, Vector2.zero, Vector2.one * 70f).rectTransform;
                coins.Add(coin);
            }
            coin.gameObject.SetActive(true);
            float size = Random.Range(46f, 96f);
            coin.sizeDelta = Vector2.one * size;
            var start = new Vector2(Random.Range(-260f, 260f), -40f);
            var velocity = new Vector2(Random.Range(-520f, 520f), Random.Range(900f, 1500f));
            float spinSpeed = Random.Range(-360f, 360f);
            float flip = Random.Range(4f, 9f);
            float t = 0f;
            while (t < 2.6f && coin.gameObject.activeSelf)
            {
                t += Time.deltaTime;
                Vector2 p = start + velocity * t + new Vector2(0f, -1500f) * t * t * 0.5f;
                coin.anchoredPosition = p;
                coin.localRotation = Quaternion.Euler(0f, 0f, spinSpeed * t);
                coin.localScale = new Vector3(Mathf.Abs(Mathf.Cos(t * flip)) * 0.8f + 0.2f, 1f, 1f);
                if (p.y < -700f) break;
                yield return null;
            }
            coin.gameObject.SetActive(false);
        }
    }
}
