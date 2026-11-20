using TMPro;
using UnityEngine;

namespace RoyalVault.Unity
{
    /// <summary>A number that rolls smoothly towards its target value instead of jumping.</summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class CountingText : MonoBehaviour
    {
        private TextMeshProUGUI label;
        private double shown;
        private long target;
        private double speed;

        public string Prefix = "";

        private void Awake() { label = GetComponent<TextMeshProUGUI>(); }

        /// <summary>Rolls to <paramref name="value"/> over <paramref name="seconds"/> (0 = at once).</summary>
        public void Set(long value, float seconds = 0f)
        {
            if (label == null) label = GetComponent<TextMeshProUGUI>();
            target = value;
            if (seconds <= 0f)
            {
                shown = value;
                Write();
                return;
            }
            speed = System.Math.Abs(value - shown) / seconds;
        }

        /// <summary>Starts from <paramref name="from"/> and rolls to <paramref name="to"/>.</summary>
        public void Roll(long from, long to, float seconds)
        {
            shown = from;
            Write();
            Set(to, seconds);
        }

        public bool Done { get { return System.Math.Abs(shown - target) < 0.5; } }

        private void Update()
        {
            if (Done) return;
            double step = speed * Time.deltaTime;
            shown = shown < target ? System.Math.Min(target, shown + step) : System.Math.Max(target, shown - step);
            Write();
        }

        private void Write()
        {
            label.text = Prefix + NumberFormat.Credits((long)System.Math.Round(shown));
        }
    }
}
