using System.Collections.Generic;
using System.Collections;
using System;
using GameKit;
using PrimeTween;
using UnityEngine.UI;
using UnityEngine;

namespace RoyalVault.Unity
{
    // Both guardians hurl flames that meet over the reels before a big win
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
        private GameObject impactVfx;
        private GameObject sparksVfx;
        private float vfxScale = 90f;
        private List<(Transform guardian, int index)> lifted;
        private readonly List<CharacterView> activeGuardians = new List<CharacterView>();
        private Vector2[] fallbackStarts = { new Vector2(-700f, 250f), new Vector2(700f, 250f) };

        // The fireballs leave the guardians' hands
        public event Action Thrown;
        // The fireballs meet in the centre
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

        // Optional store particle effects played where the flames meet (see UiVfx)
        public void SetStoreEffects(GameObject impact, GameObject sparks, float scale)
        {
            impactVfx = impact;
            sparksVfx = sparks;
            vfxScale = scale;
            UiVfx.Prewarm(impact, layer);
            UiVfx.Prewarm(sparks, layer);
        }

        public void Bind(CharacterView[] characters, Color magicColour)
        {
            guardians = characters ?? new CharacterView[0];
            magic = magicColour;
            HideAll();
        }

        private void HideAll()
        {
            foreach (Image f in fireballs)
            {
                f.gameObject.SetActive(false);
            }

            if (flash != null)
            {
                flash.color = new Color(1f, 1f, 1f, 0f);
            }

            foreach (Image r in rings)
            {
                r.gameObject.SetActive(false);
            }

            foreach (UiParticleEmitter t in trails)
            {
                t.Intensity = 0f;
            }
        }

        // The whole clash: wind-up, throw, flight and the burst where the flames meet
        public IEnumerator Play()
        {
            IsPlaying = true;
            List<CharacterView> active = ActiveGuardians();
            if (lifted != null)
            {
                Restore(lifted);
            }

            lifted = RaiseAboveMachine(active);

            yield return WindUp(active);
            Vector2[] starts = Throw(active);
            yield return Fly(starts);
            yield return Clash(starts.Length);

            Restore(lifted);
            lifted = null;
            IsPlaying = false;
        }

        private List<CharacterView> ActiveGuardians()
        {
            activeGuardians.Clear();
            foreach (CharacterView guardian in guardians)
            {
                if (guardian != null && guardian.isActiveAndEnabled)
                {
                    activeGuardians.Add(guardian);
                }
            }

            return activeGuardians;
        }

        private static IEnumerator WindUp(List<CharacterView> active)
        {
            foreach (CharacterView g in active)
            {
                g.WindUp();
            }

            for (float t = 0f; t < 1.8f && active.Exists(g => !g.ReadyToThrow); t += Time.deltaTime)
            {
                yield return null;
            }
        }

        // Releases the flames and returns where each ball starts, left one first
        private Vector2[] Throw(List<CharacterView> active)
        {
            int count = Mathf.Min(2, fireballs.Length);
            var starts = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                starts[i] = i < active.Count ? (Vector2)layer.InverseTransformPoint(active[i].HandPosition) : fallbackStarts[i];
                if (i < active.Count)
                {
                    active[i].Release();
                }
            }
            if (count == 2 && starts[0].x > starts[1].x)
            {
                (starts[0], starts[1]) = (starts[1], starts[0]);
            }

            Thrown?.Invoke();
            return starts;
        }

        private IEnumerator Fly(Vector2[] starts)
        {
            for (int i = 0; i < starts.Length; i++)
            {
                Light(i);
            }

            for (float t = 0f; t < flightSeconds;)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / flightSeconds);
                float e = k * k * (1.6f - 0.6f * k); // slow off the hand, fast into the clash
                for (int i = 0; i < starts.Length; i++)
                {
                    PlaceBall(i, starts[i], e, t);
                }

                yield return null;
            }
        }

        private void Light(int i)
        {
            fireballs[i].gameObject.SetActive(true);
            fireballs[i].color = new Color(magic.r, magic.g, magic.b, 0.95f);
            trails[i].StartColor = Whiter(0.6f, 0.95f);
            trails[i].EndColor = new Color(magic.r, magic.g, magic.b, 0f);
            trails[i].Intensity = 1f;
        }

        // Along a rising arc into the meeting point
        private void PlaceBall(int i, Vector2 start, float e, float t)
        {
            Vector2 control = Vector2.Lerp(start, meetingPoint, 0.5f) + new Vector2(0f, 170f);
            Vector2 p = Mathf.Pow(1f - e, 2f) * start + 2f * (1f - e) * e * control + e * e * meetingPoint;
            fireballs[i].rectTransform.anchoredPosition = p;
            float pulse = 1f + 0.12f * Mathf.Sin(t * 40f + i);
            fireballs[i].rectTransform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.25f, e) * pulse;
            cores[i].rectTransform.localScale = Vector3.one * (0.9f + 0.2f * Mathf.Sin(t * 55f));
            trails[i].Origin = p - ((RectTransform)trails[i].transform).anchoredPosition;
        }

        private IEnumerator Clash(int count)
        {
            for (int i = 0; i < count; i++)
            {
                fireballs[i].gameObject.SetActive(false);
                trails[i].Intensity = 0f;
            }
            Clashed?.Invoke();
            UiVfx.Play(impactVfx, layer, meetingPoint, vfxScale, 3f);
            UiVfx.Play(sparksVfx, layer, meetingPoint, vfxScale, 3f);
            Burst();
            Rings();
            if (shakeTarget != null)
            {
                Tween.ShakeLocalPosition(shakeTarget, new Vector3(14f, 14f, 0f), 0.35f, frequency: 30f);
            }

            flash.color = Whiter(0.7f, 0f);
            yield return Tween.Alpha(flash, 0f, 0.55f, 0.08f, Ease.Linear).ToYieldInstruction();
            yield return Tween.Alpha(flash, 0.55f, 0f, 0.5f, Ease.OutCubic).ToYieldInstruction();
        }

        private void Burst()
        {
            burst.Origin = meetingPoint - ((RectTransform)burst.transform).anchoredPosition;
            goldBurst.Origin = burst.Origin;
            burst.StartColor = Whiter(0.5f, 1f);
            burst.EndColor = new Color(magic.r, magic.g, magic.b, 0f);
            burst.Burst(110, 3.4f);
            goldBurst.Burst(60, 2.4f);
        }

        private Color Whiter(float amount, float alpha) =>
            new Color(Mathf.Lerp(magic.r, 1f, amount), Mathf.Lerp(magic.g, 1f, amount), 1f, alpha);

        // Draws the guardians above the machine and returns their old order
        private List<(Transform guardian, int index)> RaiseAboveMachine(List<CharacterView> active)
        {
            var lifted = new List<(Transform, int)>();
            if (shakeTarget == null)
            {
                return lifted;
            }

            Transform machine = shakeTarget;
            foreach (CharacterView g in active)
            {
                Transform t = g.transform;
                if (t.parent != machine.parent || t.GetSiblingIndex() > machine.GetSiblingIndex())
                {
                    continue;
                }

                lifted.Add((t, t.GetSiblingIndex()));
            }
            // move the later ones first so the remembered indices stay right
            for (int i = lifted.Count - 1; i >= 0; i--)
            {
                lifted[i].Item1.SetSiblingIndex(machine.GetSiblingIndex());
            }

            return lifted;
        }

        private static void Restore(List<(Transform guardian, int index)> lifted)
        {
            foreach ((Transform guardian, int index) in lifted)
            {
                guardian.SetSiblingIndex(index);
            }
        }

        // Shockwaves blasting out one after another
        private void Rings()
        {
            for (int i = 0; i < rings.Length; i++)
            {
                Image ring = rings[i];
                Color c = i == 0 ? Whiter(0.5f, 1f) : new Color(1f, 0.85f, 0.5f, 1f);
                float seconds = 0.7f + 0.2f * i;
                float delay = 0.1f * i;
                ring.gameObject.SetActive(true);
                ring.rectTransform.anchoredPosition = meetingPoint;
                ring.rectTransform.localScale = Vector3.one * 0.2f;
                ring.color = new Color(c.r, c.g, c.b, 0f);
                Tween.Scale(ring.rectTransform, 0.2f, 3.2f + i, seconds, Ease.OutCubic, startDelay: delay);
                Tween.Color(ring, c, new Color(c.r, c.g, c.b, 0f), seconds, Ease.Linear, startDelay: delay);
            }
        }

    }
}
