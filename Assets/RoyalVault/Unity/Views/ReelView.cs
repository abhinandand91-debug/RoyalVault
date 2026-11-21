using System;
using System.Collections;
using RoyalVault.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalVault.Unity
{
    /// <summary>
    /// One reel: a column of symbols scrolling down through a masked window. It only animates; the engine has already
    /// chosen where it stops. The reel's position is one number, the strip index showing in the top row, so a spin is
    /// that number travelling from its start to the target: a short wind-up, an accelerating run with motion blur, and
    /// a small overshoot that settles.
    /// </summary>
    public sealed class ReelView : MonoBehaviour
    {
        private const float Speed = 24f;          // rows per second at full speed
        private const float WindUp = 0.28f;       // rows the reel lifts before it starts
        private const float Overshoot = 0.22f;    // rows it runs past the target before settling
        private const float Acceleration = 0.14f; // share of the run spent speeding up

        private int[] strip;
        private Func<int, ThemeDefinition.SymbolArt> artFor;
        private SymbolView[] views;
        private int rows;
        private float pitch;
        private float topY;
        private float position;
        private bool hurry;
        private Coroutine spin;

        public bool IsSpinning { get { return spin != null; } }

        public void Build(int[] reelStrip, int visibleRows, Vector2 cell, float gap, float symbolScale, Sprite glow,
                          Func<int, ThemeDefinition.SymbolArt> symbolArt, int startTop, Material symbolMaterial = null)
        {
            strip = reelStrip;
            rows = visibleRows;
            artFor = symbolArt;
            pitch = cell.y + gap;
            topY = (rows - 1) * pitch * 0.5f;

            var rect = (RectTransform)transform;
            rect.sizeDelta = new Vector2(cell.x, rows * cell.y + (rows - 1) * gap);
            gameObject.AddComponent<RectMask2D>();

            // one view above and one below the window, so symbols scroll in and out smoothly
            views = new SymbolView[rows + 2];
            Vector2 symbolSize = Vector2.one * cell.y * symbolScale;
            for (int k = 0; k < views.Length; k++) views[k] = SymbolView.Create(transform, symbolSize, glow, symbolMaterial);
            position = startTop;
            Layout(false);
        }

        /// <summary>The symbol view showing <paramref name="row"/> (0 = top) while the reel is at rest.</summary>
        public SymbolView ViewAt(int row) { return views[row + 1]; }

        public void Spin(int targetTop, float delay, float runSeconds, Action onStopped)
        {
            if (spin != null) StopCoroutine(spin);
            hurry = false;
            spin = StartCoroutine(SpinRoutine(targetTop, delay, runSeconds, onStopped));
        }

        /// <summary>Lands the reel as soon as possible (the player pressed spin again).</summary>
        public void Hurry() { hurry = true; }

        private IEnumerator SpinRoutine(int target, float delay, float runSeconds, Action onStopped)
        {
            float t = 0f;
            while (t < delay && !hurry) { t += Time.deltaTime; yield return null; }

            int length = strip.Length;
            float start = position;

            // wind-up: the reel lifts a little before it falls
            if (!hurry) yield return Tween.Run(0.12f, k => { position = start + WindUp * k; Layout(false); }, Ease.OutCubic);

            // the run lasts exactly the requested time, so reels land in the order and at the moment the machine asks.
            // At full speed that covers a fixed distance, but the target can sit anywhere on the strip: the difference
            // (under half a lap either way) is made up by one jump along the strip while the reel is a motion blur,
            // where it cannot be seen.
            float from = position;
            float planned = Speed * runSeconds;
            float landing = target + length * Mathf.Round((from - planned - target) / length);
            if (from - (landing - Overshoot) < Mathf.Max(3f, planned * 0.25f)) landing -= length;
            float end = landing - Overshoot;
            float distance = from - end;
            float jump = distance - planned;
            bool jumped = false;
            float duration = Mathf.Max(0.05f, runSeconds);
            float norm = 1f - Acceleration * 0.5f;
            t = 0f;
            while (t < duration)
            {
                if (hurry && duration - t > 0.15f)
                {
                    // skip ahead to a symbol and a half before the end and finish quickly
                    float remaining = Mathf.Min(1.5f, position - end);
                    yield return Tween.Run(0.1f, k => { position = end + remaining * (1f - k); Layout(true); }, Ease.Linear);
                    break;
                }
                float u = t / duration;
                float travelled = (u < Acceleration ? u * u / (2f * Acceleration) : u - Acceleration * 0.5f) / norm;
                bool blurred = u > Acceleration * 0.4f && u < 0.98f;
                jumped |= blurred;
                position = from - planned * travelled - (jumped ? jump : 0f);
                Layout(blurred);
                t += Time.deltaTime;
                yield return null;
            }

            // settle back onto the target
            yield return Tween.Run(0.2f, k => { position = end + Overshoot * k; Layout(false); }, Ease.OutCubic);
            position = target;
            Layout(false);
            spin = null;
            onStopped?.Invoke();
        }

        /// <summary>Places the symbol views for the current position and gives them the right art.</summary>
        private void Layout(bool blur)
        {
            int top = Mathf.FloorToInt(position);
            for (int k = 0; k < views.Length; k++)
            {
                int index = top + k - 1;
                float rowFromTop = index - position;
                views[k].Rect.anchoredPosition = new Vector2(0f, topY - rowFromTop * pitch);
                int symbol = strip[Mod(index, strip.Length)];
                views[k].SetSymbol(symbol, artFor(symbol));
                views[k].SetBlur(blur);
            }
        }

        private static int Mod(int value, int length)
        {
            int m = value % length;
            return m < 0 ? m + length : m;
        }
    }
}
