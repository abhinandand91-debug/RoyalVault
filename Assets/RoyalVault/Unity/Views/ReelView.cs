using System.Collections;
using System;
using GameKit;
using SlotKit;
using UnityEngine.UI;
using UnityEngine;

namespace RoyalVault.Unity
{
    // One reel: a column of symbols scrolling down through a masked window
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

        // The symbol view showing row (0 = top) while the reel is at rest
        public SymbolView ViewAt(int row) { return views[row + 1]; }

        public void Spin(int targetTop, float delay, float runSeconds, Action onStopped)
        {
            if (spin != null) StopCoroutine(spin);
            hurry = false;
            spin = StartCoroutine(SpinRoutine(targetTop, delay, runSeconds, onStopped));
        }

        // Lands the reel as soon as possible (the player pressed spin again)
        public void Hurry() { hurry = true; }

        private IEnumerator SpinRoutine(int target, float delay, float runSeconds, Action onStopped)
        {
            for (float t = 0f; t < delay && !hurry; t += Time.deltaTime) yield return null;
            float start = position;
            if (!hurry) yield return Tween.Run(0.12f, k => { position = start + WindUp * k; Layout(false); }, Ease.OutCubic);

            float end = StopBefore(target, Speed * runSeconds);
            yield return Run(end, runSeconds);
            yield return Tween.Run(0.2f, k => { position = end + Overshoot * k; Layout(false); }, Ease.OutCubic);
            position = target;
            Layout(false);
            spin = null;
            onStopped?.Invoke();
        }

        // Where the run ends: just past the target, at least a few symbols away
        private float StopBefore(int target, float planned)
        {
            int length = strip.Length;
            float landing = target + length * Mathf.Round((position - planned - target) / length);
            if (position - (landing - Overshoot) < Mathf.Max(3f, planned * 0.25f)) landing -= length;
            return landing - Overshoot;
        }

        // Exact run time; the distance left over is jumped while the reel is blurred
        private IEnumerator Run(float end, float runSeconds)
        {
            float from = position;
            float planned = Speed * runSeconds;
            float jump = from - end - planned;
            float duration = Mathf.Max(0.05f, runSeconds);
            bool jumped = false;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                if (hurry && duration - t > 0.15f) { yield return FinishQuickly(end); yield break; }
                float u = t / duration;
                bool blurred = u > Acceleration * 0.4f && u < 0.98f;
                jumped |= blurred;
                position = from - planned * Travelled(u) - (jumped ? jump : 0f);
                Layout(blurred);
                yield return null;
            }
        }

        // Share of the planned distance covered at u: accelerates, then full speed
        private static float Travelled(float u) =>
            (u < Acceleration ? u * u / (2f * Acceleration) : u - Acceleration * 0.5f) / (1f - Acceleration * 0.5f);

        private IEnumerator FinishQuickly(float end)
        {
            float remaining = Mathf.Min(1.5f, position - end);
            yield return Tween.Run(0.1f, k => { position = end + remaining * (1f - k); Layout(true); }, Ease.Linear);
        }

        // Places the symbol views for the current position and gives them the right art
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
