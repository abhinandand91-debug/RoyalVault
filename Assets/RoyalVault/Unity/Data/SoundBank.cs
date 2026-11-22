using System;
using UnityEngine;

namespace RoyalVault.Unity
{
    // Every moment in the game that can make a sound
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
        // anticipation (added last: the bank stores cues by number)
        AnticipationLoop,
        AnticipationEnd,
        // the guardians' flame clash
        FlameThrow,
        FlameClash,
    }

    // Clips, volume and pitch spread for each sound cue, plus the music loops
    [CreateAssetMenu(menuName = "Royal Vault/Sound Bank", fileName = "SoundBank")]
    public sealed class SoundBank : ScriptableObject
    {
        [Serializable]
        public sealed class Sound
        {
            public SoundCue cue;
            [Tooltip("One of these plays at random")]
            public AudioClip[] clips = new AudioClip[0];
            [Range(0f, 1f)]
            public float volume = 1f;
            [Tooltip("Random pitch change either way, so repeated sounds don't feel mechanical")]
            [Range(0f, 0.3f)]
            public float pitchVariance = 0.03f;

            // True when at least one clip is really there (a deleted file leaves an empty slot behind)
            public bool HasClips
            {
                get
                {
                    if (clips == null)
                    {
                        return false;
                    }

                    foreach (AudioClip clip in clips)
                    {
                        if (clip != null)
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }

            // A random clip of the ones that are there, or null
            public AudioClip Pick()
            {
                if (!HasClips)
                {
                    return null;
                }

                AudioClip clip;
                do
                {
                    clip = clips[UnityEngine.Random.Range(0, clips.Length)];
                }
                while (clip == null);
                return clip;
            }
        }

        [Header("Music (loops)")]
        public AudioClip loadingMusic;
        public AudioClip baseGameMusic;
        public AudioClip freeSpinsMusic;
        [Range(0f, 1f)]
        public float musicVolume = 0.55f;
        [Tooltip("Seconds music takes to cross-fade into the next track")]
        public float musicFade = 1.2f;

        [Header("Effects")]
        [Range(0f, 1f)]
        public float effectsVolume = 1f;
        public Sound[] sounds = new Sound[0];

        // The entry for cue, or null when the bank has none
        public Sound Find(SoundCue cue)
        {
            if (sounds == null)
            {
                return null;
            }

            foreach (Sound s in sounds)
            {
                if (s != null && s.cue == cue)
                {
                    return s;
                }
            }

            return null;
        }

        // Makes sure every cue has an entry (empty ones stay silent), in the order of the enum
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
