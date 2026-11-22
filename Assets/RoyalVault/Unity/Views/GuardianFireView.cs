using System;
using System.Collections;
using GameKit;
using PrimeTween;
using SlotKit;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalVault.Unity
{
    // Guardian's Fire: the guardians hurl flames at the cells SlotKit turned wild, and each burns into a wild as it lands
    public sealed class GuardianFireView : MonoBehaviour, ILandingStep
    {
        [SerializeField] private RectTransform layer;
        [SerializeField] private Image[] fireballs = new Image[0];
        [SerializeField] private UiParticleEmitter[] trails = new UiParticleEmitter[0];
        [SerializeField] private UiParticleEmitter burst;
        [Tooltip("Seconds a flame takes to reach its cell")]
        [SerializeField, Range(0.2f, 1f)] private float flightSeconds = 0.42f;
        [Tooltip("Seconds between one flame and the next")]
        [SerializeField, Range(0f, 0.5f)] private float stagger = 0.18f;

        private CharacterView[] guardians = new CharacterView[0];
        private MachineView machine;
        private Color magic = new Color(0.35f, 0.75f, 1f, 1f);
        private int flying;

        // A flame leaves a guardian's hand
        public event Action Thrown;
        // A flame hits its cell and the symbol becomes wild
        public event Action Burned;

        public void Setup(RectTransform effectLayer, Image[] fireballImages, UiParticleEmitter[] trailEmitters, UiParticleEmitter burstEmitter)
        {
            layer = effectLayer;
            fireballs = fireballImages;
            trails = trailEmitters;
            burst = burstEmitter;
        }

        public void Bind(CharacterView[] characters, MachineView machineView, Color magicColour)
        {
            guardians = characters ?? new CharacterView[0];
            machine = machineView;
            magic = magicColour;
            foreach (Image fireball in fireballs)
            {
                fireball.gameObject.SetActive(false);
            }

            // emitter intensity is not saved in the prefab, so the trails start lit
            foreach (UiParticleEmitter trail in trails)
            {
                trail.Intensity = 0f;
            }
        }

        public bool Wants(SpinContext context)
        {
            return context.Outcome.AddedWilds.Count > 0 && fireballs.Length > 0;
        }

        public IEnumerator Play(SpinContext context)
        {
            SpinOutcome outcome = context.Outcome;
            GridPosition first = outcome.AddedWilds[0];
            int wild = outcome.Grid[first.Reel, first.Row];
            flying = 0;
            for (int i = 0; i < outcome.AddedWilds.Count; i++)
            {
                StartCoroutine(Throw(i, outcome.AddedWilds[i], wild));
                yield return Waits.Seconds(stagger);
            }

            while (flying > 0)
            {
                yield return null;
            }

            yield return Waits.Seconds(0.3f);
        }

        // One flame from alternating guardians to its cell
        private IEnumerator Throw(int index, GridPosition cell, int wild)
        {
            flying++;
            Image ball = fireballs[index % fireballs.Length];
            UiParticleEmitter trail = trails[index % trails.Length];
            Vector2 start = StartFor(index);
            Vector2 target = layer.InverseTransformPoint(machine.CellWorldPosition(cell));
            Light(ball, trail);
            Thrown?.Invoke();

            for (float t = 0f; t < flightSeconds;)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / flightSeconds);
                Place(ball, trail, start, target, k * k * (1.6f - 0.6f * k));
                yield return null;
            }

            ball.gameObject.SetActive(false);
            trail.Intensity = 0f;
            Hit(target);
            machine.Burn(cell, wild);
            Burned?.Invoke();
            flying--;
        }

        private Vector2 StartFor(int index)
        {
            if (guardians.Length == 0)
            {
                return new Vector2(index % 2 == 0 ? -700f : 700f, 250f);
            }

            CharacterView guardian = guardians[index % guardians.Length];
            guardian.Release();
            return layer.InverseTransformPoint(guardian.HandPosition);
        }

        private void Light(Image ball, UiParticleEmitter trail)
        {
            ball.gameObject.SetActive(true);
            ball.color = new Color(magic.r, magic.g, magic.b, 0.95f);
            trail.StartColor = new Color(Mathf.Lerp(magic.r, 1f, 0.6f), Mathf.Lerp(magic.g, 1f, 0.6f), 1f, 0.95f);
            trail.EndColor = new Color(magic.r, magic.g, magic.b, 0f);
            trail.Intensity = 1f;
        }

        // Along an arc that rises a little, speeding up into the cell
        private static void Place(Image ball, UiParticleEmitter trail, Vector2 start, Vector2 target, float e)
        {
            Vector2 control = Vector2.Lerp(start, target, 0.5f) + new Vector2(0f, 160f);
            Vector2 p = Mathf.Pow(1f - e, 2f) * start + 2f * (1f - e) * e * control + e * e * target;
            ball.rectTransform.anchoredPosition = p;
            ball.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.55f, 0.95f, e);
            trail.Origin = p - ((RectTransform)trail.transform).anchoredPosition;
        }

        private void Hit(Vector2 target)
        {
            burst.Origin = target - ((RectTransform)burst.transform).anchoredPosition;
            burst.StartColor = new Color(Mathf.Lerp(magic.r, 1f, 0.5f), Mathf.Lerp(magic.g, 1f, 0.5f), 1f, 1f);
            burst.EndColor = new Color(magic.r, magic.g, magic.b, 0f);
            burst.Burst(40, 2.2f);
        }
    }
}
