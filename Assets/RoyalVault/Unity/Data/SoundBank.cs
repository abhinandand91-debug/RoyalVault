using System;
using UnityEngine;

namespace RoyalVault.Unity
{
    /// <summary>Every moment in the game that can make a sound.</summary>
    public enum SoundCue
    {
        // interface
        ButtonClick,
        SpinPress,
        BetChange,
        // reels
        ReelSpinLoop,
        ReelStop,
        WildLand,
        ScatterLand,
        JackpotLand,
        // the seal meter
        SealCrack,
        SealBreak,
        // wins
        SmallWin,
        BigWin,
        MegaWin,
        EpicWin,
        JackpotWin,
        CoinShowerLoop,
        FreeSpinsAwarded,
        FreeSpinsTotal,
        // story and characters
        StoryPage,
        GuardianFlame,
    }

    /// <summary>
    /// The game's sounds as data: for each <see cref="SoundCue"/> one or more clips (a random one plays, for variety),
    /// its volume and how much its pitch may vary, plus the music loops. An empty cue is simply silent, so the game
    /// runs with any part of the bank filled. Clips dropped into <c>Art.Private/Audio</c> are filled in by the builder
    /// (see the file-name rules in RoyalVaultBuilder.Sounds.cs).
    /// </summary>
    [CreateAssetMenu(menuName = "Royal Vault/Sound Bank", fileName = "SoundBank")]
    public sealed class SoundBank : ScriptableObject
    {
        [Serializable]
        public sealed class Sound
        {
            public SoundCue cue;
            [Tooltip("One of these plays at random")] public AudioClip[] clips = new AudioClip[0];
            [Range(0f, 1f)] public float volume = 1f;
            [Tooltip("Random pitch change either way, so repeated sounds don't feel mechanical")]
            [Range(0f, 0.3f)] public float pitchVariance = 0.03f;

            /// <summary>True when at least one clip is really there (a deleted file leaves an empty slot behind).</summary>
            public bool HasClips
            {
                get
                {
                    if (clips == null) return false;
                    foreach (AudioClip clip in clips) if (clip != null) return true;
                    return false;
                }
            }

            /// <summary>A random clip of the ones that are there, or null.</summary>
            public AudioClip Pick()
            {
                if (!HasClips) return null;
                AudioClip clip;
                do clip = clips[UnityEngine.Random.Range(0, clips.Length)]; while (clip == null);
                return clip;
            }
        }

        [Header("Music (loops)")]
        public AudioClip loadingMusic;
        public AudioClip baseGameMusic;
        public AudioClip freeSpinsMusic;
        [Range(0f, 1f)] public float musicVolume = 0.55f;
        [Tooltip("Seconds music takes to cross-fade into the next track")] public float musicFade = 1.2f;

        [Header("Effects")]
        [Range(0f, 1f)] public float effectsVolume = 1f;
        public Sound[] sounds = new Sound[0];

        /// <summary>The entry for <paramref name="cue"/>, or null when the bank has none.</summary>
        public Sound Find(SoundCue cue)
        {
            if (sounds == null) return null;
            foreach (Sound s in sounds)
                if (s != null && s.cue == cue) return s;
            return null;
        }

        /// <summary>Makes sure every cue has an entry (empty ones stay silent), in the order of the enum.</summary>
        public void EnsureAllCues()
        {
            Array cues = Enum.GetValues(typeof(SoundCue));
            var all = new Sound[cues.Length];
            for (int i = 0; i < cues.Length; i++)
            {
                var cue = (SoundCue)cues.GetValue(i);
                all[i] = Find(cue) ?? new Sound { cue = cue };
            }
            sounds = all;
        }
    }
}
