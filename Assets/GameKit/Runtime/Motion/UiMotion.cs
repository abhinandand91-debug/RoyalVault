using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace GameKit
{
    // What a motion moves: a rect, the CanvasGroup that fades it (optional) and where it rests
    public readonly struct MotionTarget
    {
        public readonly RectTransform Rect;
        public readonly CanvasGroup Group;
        // The rect's resting position, taken when the target is made; moves are measured from it
        public readonly Vector2 Rest;

        public MotionTarget(RectTransform rect, CanvasGroup group = null)
        {
            Rect = rect;
            Group = group;
            Rest = rect.anchoredPosition;
        }
    }

    // The game's common animations in one place, on PrimeTween; every method returns its handle for a TweenGroup
    public static class UiMotion
    {
        // Plays every step of a preset together
        public static Sequence Play(MotionPreset preset, MotionTarget target)
        {
            Sequence sequence = Sequence.Create();
            foreach (MotionPreset.Step step in preset.steps)
            {
                Tween tween = StepTween(step, target);
                if (tween.isAlive)
                {
                    sequence.Group(tween);
                }
            }

            return sequence;
        }

        private static Tween StepTween(MotionPreset.Step step, MotionTarget target)
        {
            switch (step.channel)
            {
                case MotionChannel.Fade:
                    return target.Group != null
                        ? Tween.Alpha(target.Group, step.from, step.to, step.duration, step.ease, startDelay: step.delay)
                        : default;
                case MotionChannel.Scale:
                    return Tween.Scale(target.Rect, step.from, step.to, step.duration, step.ease, startDelay: step.delay);
                case MotionChannel.MoveX:
                    return Tween.UIAnchoredPositionX(target.Rect, target.Rest.x + step.from, target.Rest.x + step.to, step.duration, step.ease,
                                                     startDelay: step.delay);
                case MotionChannel.MoveY:
                    return Tween.UIAnchoredPositionY(target.Rect, target.Rest.y + step.from, target.Rest.y + step.to, step.duration, step.ease,
                                                     startDelay: step.delay);
                default:
                    return Tween.LocalEulerAngles(target.Rect, new Vector3(0f, 0f, step.from), new Vector3(0f, 0f, step.to), step.duration,
                                                  step.ease, startDelay: step.delay);
            }
        }

        public static Tween FadeIn(CanvasGroup group, float seconds = 0.3f)
        {
            return Tween.Alpha(group, 0f, 1f, seconds, Ease.OutCubic);
        }

        public static Tween FadeOut(CanvasGroup group, float seconds = 0.3f)
        {
            return Tween.Alpha(group, 1f, 0f, seconds, Ease.InOutSine);
        }

        // Grows from small to full size with a little overshoot
        public static Tween PopIn(Transform target, float from = 0.6f, float seconds = 0.35f, float delay = 0f)
        {
            return Tween.Scale(target, from, 1f, seconds, Ease.OutBack, startDelay: delay);
        }

        // Swells to peak and settles back, once
        public static Tween Punch(Transform target, float peak = 1.15f, float seconds = 0.3f)
        {
            return Tween.Scale(target, 1f, peak, seconds * 0.5f, Ease.OutCubic, cycles: 2, cycleMode: CycleMode.Yoyo);
        }

        // Swells and settles forever, like breathing
        public static Tween Breathe(Transform target, float peak = 1.06f, float period = 1.4f)
        {
            return Tween.Scale(target, 1f, peak, period * 0.5f, Ease.InOutSine, cycles: -1, cycleMode: CycleMode.Yoyo);
        }

        // A graphic's alpha swinging between high and low forever
        public static Tween Pulse(Graphic graphic, float low, float high, float period = 1.4f, float delay = 0f)
        {
            return Tween.Alpha(graphic, high, low, period * 0.5f, Ease.InOutSine, cycles: -1, cycleMode: CycleMode.Yoyo, startDelay: delay);
        }

        // Turns forever at a steady speed; negative degrees turn clockwise
        public static Tween Spin(Transform target, float degreesPerSecond)
        {
            float turn = degreesPerSecond < 0f ? -360f : 360f;
            float seconds = 360f / Mathf.Max(0.01f, Mathf.Abs(degreesPerSecond));
            return Tween.LocalEulerAngles(target, Vector3.zero, new Vector3(0f, 0f, turn), seconds, Ease.Linear, cycles: -1);
        }
    }
}
