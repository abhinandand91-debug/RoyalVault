using System.Collections;
using TMPro;
using UnityEngine;

namespace RoyalVault.Unity
{
    // A short message above the console (not enough credits, and so on) that fades away
    public sealed class MessageToast : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TextMeshProUGUI label;

        public void Setup(CanvasGroup canvasGroup, TextMeshProUGUI text)
        {
            group = canvasGroup;
            label = text;
        }

        private void Awake() { group.alpha = 0f; }

        public void Show(string message, float seconds = 2f)
        {
            label.text = message;
            StopAllCoroutines();
            StartCoroutine(Run(seconds));
        }

        private IEnumerator Run(float seconds)
        {
            yield return Tween.Run(0.2f, k => group.alpha = k, Ease.OutCubic);
            yield return new WaitForSeconds(seconds);
            yield return Tween.Run(0.4f, k => group.alpha = 1f - k, Ease.Linear);
        }
    }
}
