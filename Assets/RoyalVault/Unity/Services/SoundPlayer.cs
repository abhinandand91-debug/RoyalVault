using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

namespace RoyalVault.Unity
{
    // Plays one-shots on a channel pool, cue loops and cross-faded music
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
        private Sequence musicFade;
        private int next;

        public SoundBank Bank => bank;

        // The cues played most recently, oldest first (for tests and debugging; silent cues count too)
        public IReadOnlyList<SoundCue> History => history;

        // True when the player has turned the sound off (kept between sessions)
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

        // Forgets the cues played so far (tests count one moment at a time)
        public void ClearHistory() => history.Clear();

        public void Setup(SoundBank soundBank)
        {
            bank = soundBank;
        }

        private void Awake()
        {
            AudioListener.volume = Muted ? 0f : 1f;
            for (int i = 0; i < Channels; i++)
            {
                effects.Add(NewSource("Effect " + (i + 1), false));
            }

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

        // Plays cue once
        public void Play(SoundCue cue, float volume = 1f, float pitch = 1f)
        {
            Remember(cue);
            SoundBank.Sound sound = bank != null ? bank.Find(cue) : null;
            if (sound == null || !sound.HasClips || effects.Count == 0)
            {
                return;
            }

            AudioSource source = effects[next];
            next = (next + 1) % effects.Count;
            source.clip = sound.Pick();
            source.volume = sound.volume * bank.effectsVolume * volume;
            source.pitch = pitch * (1f + Random.Range(-sound.pitchVariance, sound.pitchVariance));
            source.Play();
        }

        // Starts a looping cue (reels spinning, coins pouring); calling it again while it plays does nothing
        public void StartLoop(SoundCue cue)
        {
            if (loops.TryGetValue(cue, out AudioSource playing) && playing != null && playing.isPlaying)
            {
                return;
            }

            Remember(cue);
            SoundBank.Sound sound = bank != null ? bank.Find(cue) : null;
            if (sound == null || !sound.HasClips)
            {
                return;
            }

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

        // Stops a looping cue, fading it out over seconds
        public void StopLoop(SoundCue cue, float seconds = 0.15f)
        {
            if (!loops.TryGetValue(cue, out AudioSource source) || source == null || !source.isPlaying)
            {
                return;
            }

            Tween.AudioVolume(source, source.volume, 0f, seconds, useUnscaledTime: true)
                .OnComplete(source, loop => loop.Stop());
        }

        // Cross-fades to clip (null fades the music out)
        public void PlayMusic(AudioClip clip, float seconds = -1f)
        {
            if (bank == null)
            {
                return;
            }

            if (seconds < 0f)
            {
                seconds = bank.musicFade;
            }

            if (clip == currentMusic)
            {
                return;
            }

            currentMusic = clip;
            musicFade.Stop();
            musicFade = CrossFade(clip, Mathf.Max(0.01f, seconds));
        }

        // The playing music fades out while the new clip fades in; null only fades out
        private Sequence CrossFade(AudioClip clip, float seconds)
        {
            AudioSource outgoing = music[musicIndex];
            musicIndex = 1 - musicIndex;
            AudioSource incoming = music[musicIndex];
            Sequence crossFade = Sequence.Create(useUnscaledTime: true)
                .Chain(Tween.AudioVolume(outgoing, outgoing.volume, 0f, seconds));
            if (clip != null)
            {
                incoming.clip = clip;
                incoming.volume = 0f;
                incoming.Play();
                crossFade.Group(Tween.AudioVolume(incoming, 0f, bank.musicVolume, seconds));
            }

            return crossFade.ChainCallback(outgoing, source => source.Stop());
        }

        private void Remember(SoundCue cue)
        {
            history.Add(cue);
            if (history.Count > HistoryLength)
            {
                history.RemoveAt(0);
            }
        }
    }
}
