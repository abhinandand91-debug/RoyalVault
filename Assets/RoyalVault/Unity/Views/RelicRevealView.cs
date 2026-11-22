using System;
using System.Collections;
using System.Collections.Generic;
using GameKit;
using PrimeTween;
using SlotKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalVault.Unity
{
    // The Sealed Relic is revealed as free spins begin: symbols flicker past on a card, slow down and settle on the relic
    public sealed class RelicRevealView : DialogView
    {
        private const int Flickers = 18;

        [SerializeField] private RectTransform card;
        [SerializeField] private Image symbol;
        [SerializeField] private Image glow;
        [SerializeField] private TextMeshProUGUI caption;
        [SerializeField] private UiParticleEmitter burst;
        [SerializeField] private Button tapArea;
        [Tooltip("Seconds the revealed relic stays up before free spins begin (a tap moves on sooner)")]
        [SerializeField, Range(0.5f, 5f)] private float holdSeconds = 2.2f;

        private readonly List<Sprite> faces = new List<Sprite>();
        private ThemeDefinition theme;
        private MachineDefinition machine;
        private bool tapped;
        private Sprite relicSprite;

        // Each face that flickers past
        public event Action Ticked;
        // The relic lands on the card
        public event Action Settled;
        // The reveal has closed; the relic's art goes on to the seal
        public event Action<Sprite> Revealed;

        public void Setup(RectTransform cardRect, Image symbolImage, Image glowImage, TextMeshProUGUI captionText, UiParticleEmitter burstEmitter,
                          Button tap)
        {
            card = cardRect;
            symbol = symbolImage;
            glow = glowImage;
            caption = captionText;
            burst = burstEmitter;
            tapArea = tap;
        }

        public void Bind(MachineDefinition machineDefinition, ThemeDefinition themeDefinition)
        {
            machine = machineDefinition;
            theme = themeDefinition;
            faces.Clear();
            foreach (SymbolDefinition definition in machine.Symbols)
            {
                Sprite sprite = definition.Kind == SymbolKind.Regular ? theme.Symbol(definition.Id)?.sprite : null;
                if (sprite != null)
                {
                    faces.Add(sprite);
                }
            }

            tapArea.onClick.AddListener(() => tapped = true);
        }

        public Sprite SpriteOf(int relic)
        {
            return theme.Symbol(machine.Symbols[relic].Id)?.sprite;
        }

        // Waits its turn with the dialog manager; the definition fades it in and out
        public IEnumerator Play(int relic)
        {
            relicSprite = SpriteOf(relic);
            yield return Present();
        }

        // Faces flicker past, the relic settles, and the card holds until a tap or its time is up
        protected override IEnumerator Content()
        {
            yield return Flicker();
            Settle(relicSprite);
            yield return Hold();
        }

        // The relic's art goes on to the seal once the card has gone
        protected override void OnClosed()
        {
            Revealed?.Invoke(relicSprite);
        }

        protected override void OnOpening()
        {
            tapped = false;
            caption.alpha = 0f;
            glow.color = new Color(1f, 0.8f, 0.45f, 0.2f);
            card.localScale = Vector3.one;
        }

        // Faster at first, slowing as the relic draws near
        private IEnumerator Flicker()
        {
            for (int i = 0; i < Flickers && faces.Count > 0; i++)
            {
                symbol.sprite = faces[UnityEngine.Random.Range(0, faces.Count)];
                Ticked?.Invoke();
                float k = i / (float)Flickers;
                yield return Waits.Seconds(Mathf.Lerp(0.05f, 0.2f, k * k));
            }
        }

        private void Settle(Sprite art)
        {
            symbol.sprite = art;
            Tween.Scale(card, 1.18f, 1f, 0.45f, Ease.OutBack);
            Tween.Color(glow, new Color(1f, 0.9f, 0.6f, 0.9f), new Color(1f, 0.8f, 0.45f, 0.45f), 0.8f, Ease.OutCubic);
            Tween.Alpha(caption, 0f, 1f, 0.5f, Ease.OutCubic, startDelay: 0.25f);
            burst.Burst(60, 2.6f);
            Settled?.Invoke();
        }

        private IEnumerator Hold()
        {
            for (float t = 0f; t < holdSeconds && !tapped; t += Time.deltaTime)
            {
                yield return null;
            }
        }

    }
}
