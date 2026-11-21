using System.Collections;
using PrimeTween;
using TMPro;
using UnityEngine;

namespace GameKit
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

        private void Awake()
        {
            group.alpha = 0f;
        }

        public void Show(string message, float seconds = 2f)
        {
            label.text = message;
            StopAllCoroutines();
            Tween.StopAll(group);
            StartCoroutine(Run(seconds));
        }

        private IEnumerator Run(float seconds)
        {
            yield return Tween.Alpha(group, 0f, 1f, 0.2f, Ease.OutCubic).ToYieldInstruction();
            yield return new WaitForSeconds(seconds);
            yield return Tween.Alpha(group, 1f, 0f, 0.4f, Ease.Linear).ToYieldInstruction();
        }
    }
}
