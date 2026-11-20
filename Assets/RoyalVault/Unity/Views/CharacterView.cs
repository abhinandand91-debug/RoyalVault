using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalVault.Unity
{
    /// <summary>
    /// A guardian beside the reels. It breathes slowly while idle and rises with a flash of light on big wins. When the
    /// theme has no character art (the licensed figure is not in a public checkout) it hides itself.
    /// </summary>
    public sealed class CharacterView : MonoBehaviour
    {
        [SerializeField] private Image image;
        [SerializeField] private float phase;

        private Coroutine celebrate;
        private float lift;

        public void Setup(Image characterImage, float idlePhase)
        {
            image = characterImage;
            phase = idlePhase;
        }

        public void Bind(Sprite sprite)
        {
            image.sprite = sprite;
            gameObject.SetActive(sprite != null);
        }

        private void Update()
        {
            // a slow breath: the figure stretches a little upwards from its feet
            float breath = Mathf.Sin((Time.time + phase) * 1.4f) * 0.5f + 0.5f;
            float sign = Mathf.Sign(image.rectTransform.localScale.x);
            image.rectTransform.localScale = new Vector3(sign * (1f + lift * 0.04f), 1f + 0.012f * breath + lift * 0.05f, 1f);
        }

        public void Celebrate()
        {
            if (!isActiveAndEnabled) return;
            if (celebrate != null) StopCoroutine(celebrate);
            celebrate = StartCoroutine(CelebrateRoutine());
        }

        private IEnumerator CelebrateRoutine()
        {
            yield return Tween.Run(0.25f, k => { lift = k; image.color = Color.Lerp(Color.white, new Color(1.4f, 1.25f, 0.9f), k); }, Ease.OutCubic);
            yield return new WaitForSeconds(0.6f);
            yield return Tween.Run(0.5f, k => { lift = 1f - k; image.color = Color.Lerp(new Color(1.4f, 1.25f, 0.9f), Color.white, k); }, Ease.InOutSine);
            celebrate = null;
        }
    }
}
