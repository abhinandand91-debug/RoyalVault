using System;
using PrimeTween;
using UnityEngine;

namespace GameKit
{
    // What one step of a motion changes
    public enum MotionChannel
    {
        // The target's CanvasGroup alpha, 0 to 1
        Fade,
        // A uniform scale multiplier
        Scale,
        // Sideways, in canvas pixels from where the target rests
        MoveX,
        // Up and down, in canvas pixels from where the target rests
        MoveY,
        // Turn about the z axis, in degrees
        Rotate,
    }

    // One reusable animation, tuned in the Inspector: its steps play together, each with its own timing
    [CreateAssetMenu(menuName = "GameKit/Motion Preset", fileName = "Motion")]
    public sealed class MotionPreset : ScriptableObject
    {
        [Serializable]
        public struct Step
        {
            public MotionChannel channel;
            [Tooltip("Start value: alpha, scale, pixels from rest or degrees, as the channel uses")]
            public float from;
            public float to;
            [Min(0f)] public float duration;
            [Tooltip("Seconds after the motion starts before this step begins")]
            [Min(0f)] public float delay;
            public Ease ease;
        }

        public Step[] steps = new Step[0];

        // Seconds until the last step ends
        public float Duration
        {
            get
            {
                float end = 0f;
                foreach (Step step in steps)
                {
                    end = Mathf.Max(end, step.delay + step.duration);
                }

                return end;
            }
        }

        // A preset made in code, for defaults and tests
        public static MotionPreset Create(params Step[] steps)
        {
            var preset = CreateInstance<MotionPreset>();
            preset.steps = steps;
            return preset;
        }

        public static Step StepOf(MotionChannel channel, float from, float to, float duration, Ease ease, float delay = 0f)
        {
            return new Step { channel = channel, from = from, to = to, duration = duration, ease = ease, delay = delay };
        }
    }
}
