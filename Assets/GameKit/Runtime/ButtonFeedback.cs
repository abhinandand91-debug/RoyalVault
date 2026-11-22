using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameKit
{
    // A button presses in a little under the finger and springs back when let go, so every button feels the same
    public sealed class ButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField]
        private RectTransform target;
        [SerializeField]
        private Selectable selectable;
        [SerializeField, Range(0.8f, 1f)]
        private float pressedScale = 0.93f;

        private readonly TweenGroup tweens = new TweenGroup(1);

        // Adds press feedback to a button and points it at the button
        public static ButtonFeedback Attach(Selectable button)
        {
            var feedback = button.gameObject.AddComponent<ButtonFeedback>();
            feedback.target = (RectTransform)button.transform;
            feedback.selectable = button;
            return feedback;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (selectable.IsInteractable())
            {
                ScaleTo(pressedScale, 0.08f, Ease.OutCubic);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ScaleTo(1f, 0.22f, Ease.OutBack);
        }

        private void ScaleTo(float scale, float seconds, Ease ease)
        {
            tweens.StopAll();
            tweens.Add(Tween.Scale(target, target.localScale.x, scale, seconds, ease));
        }

        private void OnDisable()
        {
            tweens.StopAll();
            target.localScale = Vector3.one;
        }
    }
}
