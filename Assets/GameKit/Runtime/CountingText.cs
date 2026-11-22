using TMPro;
using UnityEngine;

namespace GameKit
{
    // A number that rolls smoothly towards its target value instead of jumping
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class CountingText : MonoBehaviour
    {
        private readonly char[] buffer = new char[48];
        [SerializeField] private TextMeshProUGUI label;
        private long written = long.MinValue;
        private double shown;
        private long target;
        private double speed;

        public string Prefix = "";

        // Adds counting to a text and points it at that text
        public static CountingText Attach(TextMeshProUGUI text)
        {
            var counting = text.gameObject.AddComponent<CountingText>();
            counting.label = text;
            return counting;
        }

        // Rolls to value over seconds (0 = at once)
        public void Set(long value, float seconds = 0f)
        {
            target = value;
            if (seconds <= 0f)
            {
                shown = value;
                Write();
                return;
            }
            speed = System.Math.Abs(value - shown) / seconds;
        }

        // Starts from from and rolls to to
        public void Roll(long from, long to, float seconds)
        {
            shown = from;
            Write();
            Set(to, seconds);
        }

        public bool Done
        {
            get
            {
                return System.Math.Abs(shown - target) < 0.5;
            }
        }

        private void Update()
        {
            Advance(Time.deltaTime);
        }

        // Moves the shown number towards its target at the counting speed
        private void Advance(float deltaTime)
        {
            if (Done)
            {
                return;
            }

            double step = speed * deltaTime;
            shown = shown < target ? System.Math.Min(target, shown + step) : System.Math.Max(target, shown - step);
            Write();
        }

        // Only touches the text when the shown number changes, and builds it without allocating
        private void Write()
        {
            long value = (long)System.Math.Round(shown);
            if (value == written)
            {
                return;
            }

            written = value;
            Prefix.CopyTo(0, buffer, 0, Prefix.Length);
            int length = NumberFormat.Credits(value, buffer, Prefix.Length);
            label.SetCharArray(buffer, 0, length);
        }
    }
}
