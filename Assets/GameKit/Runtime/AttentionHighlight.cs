using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace GameKit
{
    // Draws the eye to a control that has just become available: it pops in, then breathes, its halo pulses and a band of
    // light sweeps across it until it is hidden or used
    public sealed class AttentionHighlight : MonoBehaviour
    {
        [SerializeField]
        private RectTransform target;
        [Tooltip("A soft light behind the control")]
        [SerializeField]
        private Graphic halo;
        [Tooltip("A band of light that sweeps across the control, inside a mask")]
        [SerializeField]
        private RectTransform shine;
        [SerializeField, Range(0.5f, 4f)]
        private float sweepEvery = 1.6f;
        [SerializeField, Range(1f, 1.2f)]
        private float breath = 1.06f;

        private readonly TweenGroup tweens = new TweenGroup(5);

        public void Setup(RectTransform targetRect, Graphic haloGraphic, RectTransform shineRect)
        {
            target = targetRect;
            halo = haloGraphic;
            shine = shineRect;
        }

        public void Begin()
        {
            End();
            gameObject.SetActive(true);
            tweens.Add(Sequence.Create()
                           .Chain(Tween.Scale(target, 0.4f, 1.18f, 0.22f, Ease.OutCubic))
                           .Chain(Tween.Scale(target, 1.18f, 1f, 0.28f, Ease.OutBack))
                           .ChainCallback(this, highlight => highlight.Loop()));
            if (halo != null)
            {
                tweens.Add(Tween.Alpha(halo, 0f, 1f, 0.25f, Ease.OutCubic));
            }
        }

        public void End()
        {
            tweens.StopAll();
            target.localScale = Vector3.one;
            if (halo != null)
            {
                SetAlpha(halo, 0f);
            }

            if (shine != null)
            {
                shine.gameObject.SetActive(false);
            }
        }

        // Breathing, a pulsing halo and a sweep of light, repeated until End
        private void Loop()
        {
            tweens.Add(UiMotion.Breathe(target, breath));
            if (halo != null)
            {
                tweens.Add(UiMotion.Pulse(halo, 0.4f, 1f));
            }

            if (shine != null)
            {
                tweens.Add(Sweep());
            }
        }

        // The band of light crosses the control, then waits before crossing again
        private Tween Sweep()
        {
            shine.gameObject.SetActive(true);
            float half = ((RectTransform)shine.parent).rect.width * 0.5f + shine.rect.width;
            return Tween.UIAnchoredPositionX(shine, -half, half, 0.55f, Ease.InOutSine, cycles: -1, cycleMode: CycleMode.Restart,
                                             endDelay: sweepEvery);
        }

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            Color colour = graphic.color;
            colour.a = alpha;
            graphic.color = colour;
        }

        private void OnDisable()
        {
            tweens.StopAll();
        }
    }
}
