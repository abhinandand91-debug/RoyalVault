using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalVault.Unity
{
    // Spin medallion: idle glow and slow rays, fast rays and STOP while the reels run
    public sealed class SpinButtonView : MonoBehaviour
    {
        private static readonly int ShinePos = Shader.PropertyToID("_ShinePos");

        [SerializeField] private Button button;
        [SerializeField] private Image glow;
        [SerializeField] private Image rays;
        [SerializeField] private Image medallion;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private string idleText = "SPIN";
        [SerializeField] private string spinningText = "STOP";
        [SerializeField] private float idleRaySpeed = 8f;
        [SerializeField] private float spinningRaySpeed = 140f;
        [SerializeField] private float shineEvery = 4.5f;

        private bool spinning;
        private float raySpeed;
        private float press;
        private Material shine;

        public event Action Pressed;

        public void Setup(Button spinButton, Image glowImage, Image raysImage, Image medallionImage, TextMeshProUGUI labelText)
        {
            button = spinButton;
            glow = glowImage;
            rays = raysImage;
            medallion = medallionImage;
            label = labelText;
        }

        private void Awake()
        {
            button.onClick.AddListener(OnClick);
            CopyShineMaterial();
            SetSpinning(false);
        }

        private void OnDestroy()
        {
            if (shine != null)
            {
                Destroy(shine);
            }
        }

        private void CopyShineMaterial()
        {
            if (medallion == null || medallion.material == null || !medallion.material.HasProperty(ShinePos))
            {
                return;
            }

            shine = new Material(medallion.material);
            medallion.material = shine;
        }

        private void OnClick()
        {
            press = 1f;
            Pressed?.Invoke();
        }

        // Pressing while spinning lands the reels early, so the label says STOP
        public void SetSpinning(bool value)
        {
            spinning = value;
            if (label != null)
            {
                label.text = value ? spinningText : idleText;
            }
        }

        public void SetInteractable(bool value)
        {
            button.interactable = value;
            if (medallion != null)
            {
                medallion.color = value ? Color.white : new Color(0.6f, 0.6f, 0.62f, 1f);
            }
        }

        private void Update()
        {
            UpdatePress();
            UpdateRays();
            UpdateGlow();
            UpdateShine();
        }

        private void UpdatePress()
        {
            press = Mathf.MoveTowards(press, 0f, Time.deltaTime * 6f);
            transform.localScale = Vector3.one * (1f - 0.08f * Mathf.Sin(press * Mathf.PI));
        }

        private void UpdateRays()
        {
            if (rays == null)
            {
                return;
            }

            float target = spinning ? spinningRaySpeed : idleRaySpeed;
            raySpeed = Mathf.MoveTowards(raySpeed, target, Time.deltaTime * 260f);
            rays.rectTransform.Rotate(0f, 0f, -raySpeed * Time.deltaTime);
            rays.color = WithAlpha(rays.color, spinning ? 0.75f : 0.35f);
        }

        private void UpdateGlow()
        {
            if (glow == null)
            {
                return;
            }

            float target = spinning ? 0.15f : 0.35f + 0.3f * (Mathf.Sin(Time.time * 2.2f) * 0.5f + 0.5f);
            glow.color = WithAlpha(glow.color, Mathf.MoveTowards(glow.color.a, target, Time.deltaTime * 2f));
        }

        // A band of light crosses the gold every few seconds
        private void UpdateShine()
        {
            if (shine == null)
            {
                return;
            }

            float t = Mathf.Repeat(Time.time, shineEvery) / 0.9f;
            shine.SetFloat(ShinePos, t <= 1f ? Mathf.Lerp(-0.4f, 1.4f, t) : -1f);
        }

        private static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);
    }
}
