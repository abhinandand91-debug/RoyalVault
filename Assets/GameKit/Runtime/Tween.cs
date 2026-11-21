using System;
using System.Collections;
using System.Globalization;
using UnityEngine;

namespace GameKit
{
    public static class Ease
    {
        public static float Linear(float t)
        {
            return t;
        }
        public static float OutCubic(float t)
        {
            t = 1f - t;
            return 1f - t * t * t;
        }
        public static float InOutCubic(float t)
        {
            return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
        }
        public static float OutBack(float t)
        {
            const float c = 1.70158f;
            t -= 1f;
            return 1f + (c + 1f) * t * t * t + c * t * t;
        }
        public static float InOutSine(float t)
        {
            return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;
        }
    }

    // Coroutine tweens: drive any value from 0 to 1 over time with an easing curve
    public static class Tween
    {
        public static IEnumerator Run(float duration, Action<float> step, Func<float, float> ease = null, bool unscaled = false)
        {
            ease = ease ?? Ease.OutCubic;
            float t = 0f;
            while (t < duration)
            {
                step(ease(Mathf.Clamp01(t / duration)));
                yield return null;
                t += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            }
            step(ease(1f));
        }
    }

    // How credits are written everywhere in the game: 48,250 (the same in every locale)
    public static class NumberFormat
    {
        public static string Credits(long value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture);
        }
    }
}
