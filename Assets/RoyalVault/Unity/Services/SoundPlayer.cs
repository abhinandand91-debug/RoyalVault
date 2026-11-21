using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RoyalVault.Unity
{
    /// <summary>
    /// Plays a <see cref="SoundBank"/>: one-shot effects on a small pool of channels, looping effects that start and
    /// stop by cue, and music on two channels that cross-fade into each other. Mute is the player's choice, kept
    /// between sessions and shared by every scene. Each scene has its own player; there is no global sound object.
    /// </summary>
    public sealed class SoundPlayer : MonoBehaviour
    {
        private const string MutedKey = "royalvault.sound.muted";
        private const int Channels = 10;
        private const int HistoryLength = 64;

        [SerializeField] private SoundBank bank;

        private readonly List<AudioSource> effects = new List<AudioSource>();
        private readonly Dictionary<SoundCue, AudioSource> loops = new Dictionary<SoundCue, AudioSource>();
        private readonly List<SoundCue> history = new List<SoundCue>();
        private AudioSource[] music = new AudioSource[2];
        private int musicIndex;
        private AudioClip currentMusic;
        private Coroutine fade;
        private int next;

        public SoundBank Bank => bank;

        /// <summary>The cues played most recently, oldest first (for tests and debugging; silent cues count too).</summary>
        public IReadOnlyList<SoundCue> History => history;

        /// <summary>True when the player has turned the sound off (kept between sessions).</summary>
        public static bool Muted
        {
            get => PlayerPrefs.GetInt(MutedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(MutedKey, value ? 1 : 0);
                PlayerPrefs.Save();
                AudioListener.volume = value ? 0f : 1f;
            }
        }

        /// <summary>Forgets the cues played so far (tests count one moment at a time).</summary>
        public void ClearHistory() => history.Clear();

        public void Setup(SoundBank soundBank)
        {
            bank = soundBank;
        }

        private void Awake()
        {
            AudioListener.volume = Muted ? 0f : 1f;
            for (int i = 0; i < Channels; i++) effects.Add(NewSource("Effect " + (i + 1), false));
            music[0] = NewSource("Music A", true);
            music[1] = NewSource("Music B", true);
        }

        private AudioSource NewSource(string name, bool loop)
        {
            var child = new GameObject(name);
            child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            return source;
        }

        /// <summary>Plays <paramref name="cue"/> once. <paramref name="pitch"/> scales the pitch (rising reel stops).</summary>
        public void Play(SoundCue cue, float volume = 1f, float pitch = 1f)
        {
            Remember(cue);
            SoundBank.Sound sound = bank != null ? bank.Find(cue) : null;
            if (sound == null || !sound.HasClips || effects.Count == 0) return;
            AudioSource source = effects[next];
            next = (next + 1) % effects.Count;
            source.clip = sound.Pick();
            source.volume = sound.volume * bank.effectsVolume * volume;
            source.pitch = pitch * (1f + Random.Range(-sound.pitchVariance, sound.pitchVariance));
            source.Play();
        }

        /// <summary>Starts a looping cue (reels spinning, coins pouring); calling it again while it plays does nothing.</summary>
        public void StartLoop(SoundCue cue)
        {
            if (loops.TryGetValue(cue, out AudioSource playing) && playing != null && playing.isPlaying) return;
            Remember(cue);
            SoundBank.Sound sound = bank != null ? bank.Find(cue) : null;
            if (sound == null || !sound.HasClips) return;
            if (!loops.TryGetValue(cue, out AudioSource source) || source == null)
            {
                source = NewSource(cue + " Loop", true);
                loops[cue] = source;
            }
            source.clip = sound.Pick();
            source.volume = sound.volume * bank.effectsVolume;
            source.pitch = 1f;
            source.Play();
        }

        /// <summary>Stops a looping cue, fading it out over <paramref name="seconds"/>.</summary>
        public void StopLoop(SoundCue cue, float seconds = 0.15f)
        {
            if (!loops.TryGetValue(cue, out AudioSource source) || source == null || !source.isPlaying) return;
            StartCoroutine(FadeOut(source, seconds));
        }

        private static IEnumerator FadeOut(AudioSource source, float seconds)
        {
            float from = source.volume;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                source.volume = from * (1f - t / seconds);
                yield return null;
            }
            source.Stop();
            source.volume = from;
        }

        /// <summary>Cross-fades to <paramref name="clip"/> (null fades the music out). The same clip keeps playing.</summary>
        public void PlayMusic(AudioClip clip, float seconds = -1f)
        {
            if (bank == null) return;
            if (seconds < 0f) seconds = bank.musicFade;
            if (clip == currentMusic) return;
            currentMusic = clip;
            if (fade != null) StopCoroutine(fade);
            fade = StartCoroutine(CrossFade(clip, seconds));
        }

        private IEnumerator CrossFade(AudioClip clip, float seconds)
        {
            AudioSource outgoing = music[musicIndex];
            musicIndex = 1 - musicIndex;
            AudioSource incoming = music[musicIndex];
            float target = bank.musicVolume;
            float outFrom = outgoing.volume;
            if (clip != null)
            {
                incoming.clip = clip;
                incoming.volume = 0f;
                incoming.Play();
            }
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float k = t / Mathf.Max(0.01f, seconds);
                outgoing.volume = outFrom * (1f - k);
                if (clip != null) incoming.volume = target * k;
                yield return null;
            }
            outgoing.Stop();
            if (clip != null) incoming.volume = target;
            fade = null;
        }

        private void Remember(SoundCue cue)
        {
            history.Add(cue);
            if (history.Count > HistoryLength) history.RemoveAt(0);
        }
    }
}
