using TMPro;
using UnityEngine;

namespace GameKit
{
    // Reveals a TextMeshPro line letter by letter; Complete() shows the rest at once
    [RequireComponent(typeof(TMP_Text))]
    public sealed class TypewriterText : MonoBehaviour
    {
        [SerializeField] private float charactersPerSecond = 38f;
        [SerializeField] private float punctuationPause = 0.18f;

        private TMP_Text text;
        private float shown;
        private float pause;

        public bool IsTyping => text != null && text.maxVisibleCharacters < text.textInfo.characterCount;

        private void Awake()
        {
            text = GetComponent<TMP_Text>();
        }

        public void Play(string line)
        {
            if (text == null)
            {
                Awake();
            }

            text.text = line;
            text.maxVisibleCharacters = 0;
            text.ForceMeshUpdate();
            shown = 0f;
            pause = 0f;
        }

        public void Complete()
        {
            if (text == null)
            {
                return;
            }

            text.maxVisibleCharacters = text.textInfo.characterCount;
            shown = text.maxVisibleCharacters;
        }

        public void Clear()
        {
            if (text != null)
            {
                text.text = "";
            }
        }

        private void Update()
        {
            Type(Time.deltaTime);
        }

        // Reveals the characters due by now, after any pause at a sentence's end
        private void Type(float deltaTime)
        {
            if (!IsTyping)
            {
                return;
            }

            if (pause > 0f)
            {
                pause -= deltaTime;
                return;
            }
            shown += deltaTime * charactersPerSecond;
            int visible = Mathf.Min(Mathf.FloorToInt(shown), text.textInfo.characterCount);
            if (visible > text.maxVisibleCharacters)
            {
                Reveal(visible);
            }
        }

        private void Reveal(int visible)
        {
            text.maxVisibleCharacters = visible;
            char last = text.textInfo.characterInfo[visible - 1].character;
            if (last == '.' || last == ',' || last == ':' || last == '!' || last == '?')
            {
                pause = punctuationPause;
            }
        }
    }
}
