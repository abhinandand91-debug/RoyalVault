using System.Collections;
using TMPro;
using UnityEngine;

namespace GameKit
{
    // A short message above the console (not enough credits, and so on) that shows for a while and fades away; a newer
    // message replaces it
    public sealed class MessageToast : DialogView
    {
        [SerializeField]
        private TextMeshProUGUI label;

        private float seconds = 2f;

        public void Setup(TextMeshProUGUI text)
        {
            label = text;
        }

        public void Show(string message, float showSeconds = 2f)
        {
            label.text = message;
            seconds = showSeconds;
            Present();
        }

        // Stays up for its time unless a newer message takes its place
        protected override IEnumerator Content()
        {
            for (float t = 0f; t < seconds && !CloseRequested; t += Time.deltaTime)
            {
                yield return null;
            }
        }
    }
}
