using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalVault.Unity
{
    /// <summary>
    /// The guardians' flame clash, played before a big win is announced: both Anubis lean in and call up their flame,
    /// hurl it at the reels, and the two fireballs arc in and meet at the centre of the screen in a burst of light (a
    /// flash, shockwave rings, a spray of blue and gold sparks and a jolt through the machine). Without guardians (the
    /// licensed art is not in a public checkout) the fireballs leave from the niches.
    /// </summary>
    public sealed class FlameClashView : MonoBehaviour
    {
        [SerializeField] private RectTransform layer;
        [SerializeField] private Image[] fireballs = new Image[0];
        [SerializeField] private Image[] cores = new Image[0];
        [SerializeField] private UiParticleEmitter[] trails = new UiParticleEmitter[0];
        [SerializeField] private Image flash;
        [SerializeField] private Image[] rings = new Image[0];
        [SerializeField] private UiParticleEmitter burst;
        [SerializeField] private UiParticleEmitter goldBurst;
        [SerializeField] private RectTransform shakeTarget;
        [Tooltip("Play the clash before big wins and jackpots")]
        [SerializeField] private bool playOnBigWins = true;
        [Tooltip("Where the flames meet, in canvas pixels from the centre of the screen")]
        [SerializeField] private Vector2 meetingPoint = new Vector2(0f, 40f);
        [Tooltip("Seconds a fireball takes to reach the centre")]
        [SerializeField, Range(0.2f, 1.5f)] private float flightSeconds = 0.55f;
        [SerializeField] private Color magic = new Color(0.35f, 0.75f, 1f, 1f);

        private CharacterView[] guardians = new CharacterView[0];
        private Vector2[] fallbackStarts = { new Vector2(-700f, 250f), new Vector2(700f, 250f) };

        /// <summary>The fireballs leave the guardians' hands.</summary>
        public event Action Thrown;
        /// <summary>The fireballs meet in the centre.</summary>
        public event Action Clashed;

        public bool PlayOnBigWins { get => playOnBigWins; set => playOnBigWins = value; }
        public bool IsPlaying { get; private set; }

        public void Setup(RectTransform effectLayer, Image[] fireballImages, Image[] coreImages, UiParticleEmitter[] trailEmitters, Image flashImage,
                          Image[] ringImages, UiParticleEmitter burstEmitter, UiParticleEmitter goldEmitter, RectTransform shake)
        {
            layer = effectLayer;
            fireballs = fireballImages;
            cores = coreImages;
            trails = trailEmitters;
            flash = flashImage;
            rings = ringImages;
            burst = burstEmitter;
            goldBurst = goldEmitter;
            shakeTarget = shake;
        }

        public void Bind(CharacterView[] characters, Color magicColour)
        {
            guardians = characters ?? new CharacterView[0];
            magic = magicColour;
            HideAll();
        }

        private void HideAll()
        {
            foreach (Image f in fireballs) f.gameObject.SetActive(false);
            if (flash != null) flash.color = new Color(1f, 1f, 1f, 0f);
            foreach (Image r in rings) r.gameObject.SetActive(false);
            foreach (UiParticleEmitter t in trails) t.Intensity = 0f;
        }

        /// <summary>The whole clash: wind-up, throw, flight and the burst where the flames meet.</summary>
        public IEnumerator Play()
        {
            IsPlaying = true;
            var active = new System.Collections.Generic.List<CharacterView>();
            foreach (CharacterView g in guardians) if (g != null && g.isActiveAndEnabled) active.Add(g);

            // wind-up: both lean in and call up their flame; throw when both are ready
            foreach (CharacterView g in active) g.WindUp();
            float wait = 0f;
            while (wait < 1.8f && active.Exists(g => !g.ReadyToThrow)) { wait += Time.deltaTime; yield return null; }

            int count = Mathf.Min(2, fireballs.Length);
            var starts = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                starts[i] = i < active.Count ? (Vector2)layer.InverseTransformPoint(active[i].HandPosition) : fallbackStarts[i];
                if (i < active.Count) active[i].Release();
            }
            // the left guardian's ball comes from the left: order the starts by x
            if (count == 2 && starts[0].x > starts[1].x) (starts[0], starts[1]) = (starts[1], starts[0]);
            Thrown?.Invoke();

            // flight: an arc that rises a little, accelerating into the centre, with a trail of sparks behind
            for (int i = 0; i < count; i++)
            {
                fireballs[i].gameObject.SetActive(true);
                fireballs[i].color = new Color(magic.r, magic.g, magic.b, 0.95f);
                trails[i].StartColor = new Color(Mathf.Lerp(magic.r, 1f, 0.6f), Mathf.Lerp(magic.g, 1f, 0.6f), 1f, 0.95f);
                trails[i].EndColor = new Color(magic.r, magic.g, magic.b, 0f);
                trails[i].Intensity = 1f;
            }
            float t = 0f;
            while (t < flightSeconds)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / flightSeconds);
                float e = k * k * (1.6f - 0.6f * k); // eases in: slow off the hand, fast into the clash
                for (int i = 0; i < count; i++)
                {
                    Vector2 a = starts[i];
                    Vector2 control = Vector2.Lerp(a, meetingPoint, 0.5f) + new Vector2(0f, 170f);
                    Vector2 p = Mathf.Pow(1f - e, 2f) * a + 2f * (1f - e) * e * control + e * e * meetingPoint;
                    fireballs[i].rectTransform.anchoredPosition = p;
                    float pulse = 1f + 0.12f * Mathf.Sin(t * 40f + i);
                    fireballs[i].rectTransform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.25f, e) * pulse;
                    cores[i].rectTransform.localScale = Vector3.one * (0.9f + 0.2f * Mathf.Sin(t * 55f));
                    trails[i].Origin = p - ((RectTransform)trails[i].transform).anchoredPosition;
                }
                yield return null;
            }

            // the clash: the balls vanish into a flash, rings blast out, sparks spray, the machine jolts
            for (int i = 0; i < count; i++) { fireballs[i].gameObject.SetActive(false); trails[i].Intensity = 0f; }
            Clashed?.Invoke();
            burst.Origin = meetingPoint - ((RectTransform)burst.transform).anchoredPosition;
            goldBurst.Origin = burst.Origin;
            burst.StartColor = new Color(Mathf.Lerp(magic.r, 1f, 0.5f), Mathf.Lerp(magic.g, 1f, 0.5f), 1f, 1f);
            burst.EndColor = new Color(magic.r, magic.g, magic.b, 0f);
            burst.Burst(110, 3.4f);
            goldBurst.Burst(60, 2.4f);
            StartCoroutine(Rings());
            StartCoroutine(Shake(0.35f, 14f));
            yield return Tween.Run(0.08f, k => flash.color = new Color(Mathf.Lerp(magic.r, 1f, 0.7f), Mathf.Lerp(magic.g, 1f, 0.7f), 1f, 0.55f * k), Ease.Linear);
            yield return Tween.Run(0.5f, k => flash.color = new Color(Mathf.Lerp(magic.r, 1f, 0.7f), Mathf.Lerp(magic.g, 1f, 0.7f), 1f, 0.55f * (1f - k)), Ease.OutCubic);
            IsPlaying = false;
        }

        private IEnumerator Rings()
        {
            for (int i = 0; i < rings.Length; i++)
            {
                Image ring = rings[i];
                int index = i; // each ring keeps its own number (the loop variable is shared by the closures)
                ring.gameObject.SetActive(true);
                ring.rectTransform.anchoredPosition = meetingPoint;
                Color c = index == 0 ? new Color(Mathf.Lerp(magic.r, 1f, 0.5f), Mathf.Lerp(magic.g, 1f, 0.5f), 1f, 1f) : new Color(1f, 0.85f, 0.5f, 1f);
                StartCoroutine(Tween.Run(0.7f + 0.2f * index, k =>
                {
                    ring.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.2f, 3.2f + index, Ease.OutCubic(k));
                    ring.color = new Color(c.r, c.g, c.b, 1f - k);
                }, Ease.Linear));
                yield return new WaitForSeconds(0.1f);
            }
        }

        private IEnumerator Shake(float seconds, float strength)
        {
            if (shakeTarget == null) yield break;
            Vector2 home = shakeTarget.anchoredPosition;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float fade = 1f - t / seconds;
                shakeTarget.anchoredPosition = home + UnityEngine.Random.insideUnitCircle * strength * fade;
                yield return null;
            }
            shakeTarget.anchoredPosition = home;
        }
    }
}
