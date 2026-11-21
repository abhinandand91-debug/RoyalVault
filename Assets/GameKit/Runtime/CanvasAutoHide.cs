using UnityEngine;

namespace GameKit
{
    // Stops drawing a canvas while its CanvasGroup is fully faded out; an invisible full-screen layer still costs fill rate
    [RequireComponent(typeof(Canvas))]
    public sealed class CanvasAutoHide : MonoBehaviour
    {
        private Canvas canvas;
        private CanvasGroup group;

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            group = GetComponent<CanvasGroup>();
        }

        private void LateUpdate()
        {
            if (group == null)
            {
                return;
            }

            bool visible = group.alpha > 0.001f;
            if (canvas.enabled != visible)
            {
                canvas.enabled = visible;
            }
        }
    }
}
