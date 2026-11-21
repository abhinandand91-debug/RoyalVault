using System;
using UnityEngine;

namespace RoyalVault.Unity
{
    /// <summary>
    /// A character baked to frames: an idle loop and a one-shot action, each with the point in every frame where the
    /// character's magic burns (so particles can follow the hand) and how strongly it burns there. The frames are
    /// rendered from the character's skeletal animation by an offline tool; the builder fills this asset from them.
    /// </summary>
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
        public Clip idle = new Clip();
        public Clip action = new Clip();

        public bool IsValid => idle.IsValid && framesPerSecond > 0f;
        public float Aspect => IsValid ? idle.frames[0].rect.width / idle.frames[0].rect.height : 1f;
    }
}
