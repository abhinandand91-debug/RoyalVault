using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RoyalVault.Unity;
using UnityEditor;
using UnityEngine;

namespace RoyalVault.EditorTools
{
    public static partial class RoyalVaultBuilder
    {
        // Where licensed audio goes (git-ignored with the rest of Art.Private)
        private const string AudioFolder = Root + "Art.Private/Audio";

        // The sound bank, with an entry for every cue
        private static SoundBank CreateSounds()
        {
            string path = DataPath + "RoyalVault_Sounds.asset";
            var bank = AssetDatabase.LoadAssetAtPath<SoundBank>(path);
            bool created = bank == null;
            if (created)
            {
                bank = ScriptableObject.CreateInstance<SoundBank>();
            }

            bank.EnsureAllCues();
            DropMissing(bank);
            if (AssetDatabase.IsValidFolder(AudioFolder))
            {
                FillFromFolder(bank);
            }

            if (created)
            {
                AssetDatabase.CreateAsset(bank, path);
            }

            EditorUtility.SetDirty(bank);
            return bank;
        }

        // Clears clips whose files were deleted, so those slots can be filled again
        private static void DropMissing(SoundBank bank)
        {
            foreach (SoundBank.Sound sound in bank.sounds)
            {
                if (sound.clips != null)
                {
                    sound.clips = Array.FindAll(sound.clips, c => c != null);
                }
            }
            // a destroyed clip only compares equal to null; assigning null clears it for real
            if (bank.loadingMusic == null)
            {
                bank.loadingMusic = null;
            }

            if (bank.baseGameMusic == null)
            {
                bank.baseGameMusic = null;
            }

            if (bank.freeSpinsMusic == null)
            {
                bank.freeSpinsMusic = null;
            }
        }

        // Matches files to cues by name prefix and fills the empty slots
        private static void FillFromFolder(SoundBank bank)
        {
            var found = new Dictionary<SoundCue, List<AudioClip>>();
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { AudioFolder }))
            {
                string file = AssetDatabase.GUIDToAssetPath(guid);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(file);
                string name = Path.GetFileNameWithoutExtension(file);
                if (TakeMusic(bank, name, clip) || !CueFor(name, out SoundCue cue))
                {
                    continue;
                }

                if (!found.TryGetValue(cue, out List<AudioClip> list))
                {
                    found[cue] = list = new List<AudioClip>();
                }

                list.Add(clip);
            }
            foreach (SoundBank.Sound sound in bank.sounds)
            {
                if (sound.HasClips || !found.TryGetValue(sound.cue, out List<AudioClip> clips))
                {
                    continue;
                }

                sound.clips = clips.ToArray();
                (sound.volume, sound.pitchVariance) = StartingMix(sound.cue); // Inspector changes are kept later
            }
        }

        private static bool TakeMusic(SoundBank bank, string name, AudioClip clip)
        {
            bool Is(string prefix) => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
            if (Is("Music_Loading"))
            {
                if (bank.loadingMusic == null)
                {
                    bank.loadingMusic = clip;
                }

                return true;
            }
            if (Is("Music_FreeSpins"))
            {
                if (bank.freeSpinsMusic == null)
                {
                    bank.freeSpinsMusic = clip;
                }

                return true;
            }
            if (Is("Music_Base"))
            {
                if (bank.baseGameMusic == null)
                {
                    bank.baseGameMusic = clip;
                }

                return true;
            }
            return false;
        }

        // Longest names first, so "ReelSpinLoop" is never taken for "ReelStop"
        private static readonly SoundCue[] CuesByLength =
            ((SoundCue[])Enum.GetValues(typeof(SoundCue))).OrderByDescending(c => c.ToString().Length).ToArray();

        private static bool CueFor(string name, out SoundCue cue)
        {
            foreach (SoundCue c in CuesByLength)
            {
                if (name.StartsWith(c.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    cue = c;
                    return true;
                }
            }

            cue = default;
            return false;
        }

        // Where a cue starts in the mix: interface and loops sit under the action, wins and the seal on top
        private static (float volume, float pitchVariance) StartingMix(SoundCue cue)
        {
            switch (cue)
            {
                case SoundCue.ButtonClick:
                case SoundCue.BetChange:
                    return (0.55f, 0.05f);
                case SoundCue.SpinPress:
                    return (0.7f, 0.03f);
                case SoundCue.ReelSpinLoop:
                    return (0.45f, 0f);
                case SoundCue.ReelStop:
                    return (0.8f, 0.04f);
                case SoundCue.WildLand:
                case SoundCue.ScatterLand:
                case SoundCue.JackpotLand:
                    return (0.85f, 0f);
                case SoundCue.SealCrack:
                    return (0.9f, 0f);
                case SoundCue.SealBreak:
                    return (1f, 0f);
                case SoundCue.SmallWin:
                    return (0.7f, 0f);
                case SoundCue.CoinShowerLoop:
                    return (0.5f, 0f);
                case SoundCue.StoryPage:
                    return (0.6f, 0f);
                case SoundCue.GuardianFlame:
                    return (0.7f, 0f);
                case SoundCue.AnticipationLoop:
                    return (0.8f, 0f);
                case SoundCue.AnticipationEnd:
                    return (0.85f, 0f);
                case SoundCue.FlameThrow:
                    return (0.85f, 0.03f);
                case SoundCue.FlameClash:
                    return (1f, 0f);
                default:
                    return (1f, 0f); // big wins, jackpots, free spins
            }
        }
    }
}
