using UnityEngine;

namespace GameKit
{
    // Stops drawing a canvas while its CanvasGroup is fully faded out; an invisible full-screen layer still costs fill rate
    [RequireComponent(typeof(Canvas))]
    public sealed class CanvasAutoHide : MonoBehaviour
    {
        [SerializeField]
        private Canvas canvas;
        [SerializeField]
        private CanvasGroup group;

        public void Setup(Canvas ownCanvas, CanvasGroup fadingGroup)
        {
            canvas = ownCanvas;
            group = fadingGroup;
        }

        private void LateUpdate()
        {
            SyncCanvasToAlpha();
        }

        // The canvas draws only while its group can be seen
        private void SyncCanvasToAlpha()
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
