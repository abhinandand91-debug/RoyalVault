using System;
using UnityEngine;

namespace RoyalVault.Unity
{
    // A character baked to frames: idle and action clips with the hand position per frame
    [CreateAssetMenu(menuName = "Royal Vault/Character Animation", fileName = "CharacterAnimation")]
    public sealed class CharacterAnimation : ScriptableObject
    {
        [Serializable]
        public sealed class Clip
        {
            public Sprite[] frames = new Sprite[0];
            [Tooltip("Where the magic burns in each frame, as a share of the frame (0,0 = bottom left)")] public Vector2[] hand = new Vector2[0];
            [Tooltip("How strongly the magic burns in each frame, 0 to 1")] public float[] flame = new float[0];

            public bool IsValid => frames != null && frames.Length > 0;
            public float Duration(float fps) => frames.Length / fps;
        }

        public float framesPerSecond = 24f;
        [Tooltip("Height the frames are shown at, in canvas pixels")] public float displayHeight = 714f;
        [Tooltip("Share of a frame the still art covers: below 1 when the frames leave room for light and sparks around it")]
        public float figureShare = 1f;
        public Clip idle = new Clip();
        public Clip action = new Clip();

        public bool IsValid => idle.IsValid && framesPerSecond > 0f;
        public float Aspect => IsValid ? idle.frames[0].rect.width / idle.frames[0].rect.height : 1f;
    }
}
