using System;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalVault.Unity
{
    /// <summary>The spin button: a slow golden glow while it waits, a press bounce, dimmed while the reels run.</summary>
    public sealed class SpinButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image glow;

        private bool spinning;

        public event Action Pressed;

        public void Setup(Button spinButton, Image glowImage)
        {
            button = spinButton;
            glow = glowImage;
        }

        private void Awake()
        {
            button.onClick.AddListener(() =>
            {
                StopAllCoroutines();
                StartCoroutine(Tween.Run(0.18f, k => button.transform.localScale = Vector3.one * (1f - 0.06f * Mathf.Sin(k * Mathf.PI)), Ease.Linear));
                Pressed?.Invoke();
            });
        }

        /// <summary>While spinning the button still works (it lands the reels quickly) but stops glowing.</summary>
        public void SetSpinning(bool value) { spinning = value; }

        public void SetInteractable(bool value) { button.interactable = value; }

        private void Update()
        {
            if (glow == null) return;
            float pulse = spinning ? 0f : 0.35f + 0.3f * (Mathf.Sin(Time.time * 2.2f) * 0.5f + 0.5f);
            Color c = glow.color;
            glow.color = new Color(c.r, c.g, c.b, Mathf.MoveTowards(c.a, pulse, Time.deltaTime * 2f));
        }
    }
}
