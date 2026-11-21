using System.Collections;
using System.Collections.Generic;
using RoyalVault.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalVault.Unity
{
    /// <summary>
    /// The celebration overlay: BIG / MEGA / EPIC WIN and jackpot wins with a counting amount and a shower of gold
    /// coins, plus short title cards (free spins awarded, free spins total). A tap skips to the end. When the theme has
    /// heroes, they stride in at either side of the banner and play their action (the prospector shoulders his
    /// pickaxe, the sharpshooter spins her revolver) for as long as the celebration lasts.
    /// </summary>
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

        public bool IsShowing { get; private set; }

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

        /// <summary>Heroes beside the banner: their images, baked animations and where each stands (feet, canvas pixels).</summary>
        public void SetupHeroes(Image[] heroImages, CharacterAnimation[] motion, Vector2[] home)
        {
            heroes = heroImages;
            heroMotion = motion;
            heroHome = home;
        }

        private bool HasHero(int i) => i < heroMotion.Length && heroMotion[i] != null && heroMotion[i].IsValid;

        /// <summary>Shows the heroes' frames at <paramref name="t"/> seconds and slides them in by <paramref name="enter"/> (0..1).</summary>
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

        /// <summary>Celebrates a big win or a jackpot; finishes when the count is done and the player has seen it.</summary>
        public IEnumerator ShowWin(WinTier tier, long value, string jackpotName)
        {
            string heading = jackpotName != null ? jackpotName + " JACKPOT" : tier == WinTier.Epic ? "EPIC WIN" : tier == WinTier.Mega ? "MEGA WIN" : null;
            float seconds = jackpotName != null ? 5f : tier == WinTier.Epic ? 5f : tier == WinTier.Mega ? 4f : 3f;
            yield return Show(heading, null, value, seconds, true);
        }

        /// <summary>A short title card without an amount (or with one when <paramref name="value"/> is above 0).</summary>
        public IEnumerator ShowTitle(string heading, string line, long value, float seconds)
        {
            yield return Show(heading, line, value, seconds, value > 0);
        }

        private IEnumerator Show(string heading, string line, long value, float countSeconds, bool coinsOn)
        {
            IsShowing = true;
            skipped = false;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            group.blocksRaycasts = true;

            // the BIG WIN artwork when there is no other heading
            banner.gameObject.SetActive(heading == null);
            title.gameObject.SetActive(heading != null);
            title.text = heading ?? "";
            subtitle.gameObject.SetActive(line != null);
            subtitle.text = line ?? "";
            amount.gameObject.SetActive(value > 0);
            if (value > 0) amount.Roll(0, value, countSeconds);

            RectTransform hero = heading == null ? banner.rectTransform : title.rectTransform;
            float clock = 0f;
            yield return Tween.Run(0.35f, k =>
            {
                group.alpha = k;
                hero.localScale = Vector3.one * Mathf.LerpUnclamped(0.6f, 1f, Ease.OutBack(k));
            }, Ease.Linear);

            float t = 0f;
            const float stride = 0.55f;
            float spawn = 0f;
            float hold = value > 0 ? countSeconds + 1.4f : countSeconds;
            while (t < hold && !skipped)
            {
                t += Time.deltaTime;
                clock += Time.deltaTime;
                PoseHeroes(clock, Mathf.Clamp01(clock / stride));
                if (rays != null) rays.localRotation = Quaternion.Euler(0f, 0f, -t * 12f);
                if (coinsOn && t < countSeconds)
                {
                    spawn += Time.deltaTime * 22f;
                    while (spawn >= 1f) { spawn -= 1f; StartCoroutine(Coin()); }
                }
                yield return null;
            }
            if (skipped && value > 0)
            {
                amount.Set(value);
                yield return new WaitForSeconds(0.6f);
            }

            yield return Tween.Run(0.3f, k => { group.alpha = 1f - k; clock += Time.deltaTime; PoseHeroes(clock, 1f); }, Ease.Linear);
            foreach (RectTransform coin in coins) coin.gameObject.SetActive(false);
            Hide();
            IsShowing = false;
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
