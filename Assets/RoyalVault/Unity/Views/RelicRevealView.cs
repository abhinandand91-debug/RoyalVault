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
    public sealed class RelicRevealView : MonoBehaviour
    {
        private const int Flickers = 18;

        [SerializeField] private CanvasGroup group;
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

        // Each face that flickers past
        public event Action Ticked;
        // The relic lands on the card
        public event Action Settled;
        // The reveal has closed; the relic's art goes on to the seal
        public event Action<Sprite> Revealed;

        public void Setup(CanvasGroup canvasGroup, RectTransform cardRect, Image symbolImage, Image glowImage, TextMeshProUGUI captionText,
                          UiParticleEmitter burstEmitter, Button tap)
        {
            group = canvasGroup;
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
            Hide();
        }

        public Sprite SpriteOf(int relic)
        {
            return theme.Symbol(machine.Symbols[relic].Id)?.sprite;
        }

        public IEnumerator Play(int relic)
        {
            Sprite relicSprite = SpriteOf(relic);
            Open();
            yield return Tween.Alpha(group, 0f, 1f, 0.3f, Ease.OutCubic).ToYieldInstruction();
            yield return Flicker();
            Settle(relicSprite);
            yield return Hold();
            yield return Tween.Alpha(group, 1f, 0f, 0.3f, Ease.InOutSine).ToYieldInstruction();
            Hide();
            Revealed?.Invoke(relicSprite);
        }

        private void Open()
        {
            tapped = false;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            group.blocksRaycasts = true;
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
                yield return Tween.Delay(Mathf.Lerp(0.05f, 0.2f, k * k)).ToYieldInstruction();
            }
        }

        private void Settle(Sprite relicSprite)
        {
            symbol.sprite = relicSprite;
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

        private void Hide()
        {
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }
    }
}
