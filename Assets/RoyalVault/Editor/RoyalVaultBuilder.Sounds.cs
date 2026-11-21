using System;
using System.Collections.Generic;
using System.IO;
using RoyalVault.Unity;
using UnityEditor;
using UnityEngine;

namespace RoyalVault.EditorTools
{
    public static partial class RoyalVaultBuilder
    {
        /// <summary>Where licensed audio goes (git-ignored with the rest of Art.Private).</summary>
        private const string AudioFolder = Root + "Art.Private/Audio";

        /// <summary>
        /// The sound bank, with an entry for every cue. Clips dropped into <c>Art.Private/Audio</c> (any sub-folder) are
        /// filled in by name, so a sound pack only needs renaming, not wiring:
        /// <list type="bullet">
        /// <item><c>Music_Loading…</c>, <c>Music_Base…</c>, <c>Music_FreeSpins…</c> are the music loops;</item>
        /// <item>a file starting with a cue's name is one of that cue's variations, e.g. <c>ReelStop_01.wav</c>,
        /// <c>ReelStop_02.wav</c>, <c>BigWin.wav</c>, <c>SealCrack_a.ogg</c>.</item>
        /// </list>
        /// Clips already assigned by hand in the Inspector are kept; only empty cues are filled (and slots whose file
        /// was deleted are cleared first).
        /// </summary>
        private static SoundBank CreateSounds()
        {
            string path = DataPath + "RoyalVault_Sounds.asset";
            var bank = AssetDatabase.LoadAssetAtPath<SoundBank>(path);
            bool created = bank == null;
            if (created) bank = ScriptableObject.CreateInstance<SoundBank>();
            bank.EnsureAllCues();
            // drop slots whose files were deleted, so those cues can be filled again
            foreach (SoundBank.Sound sound in bank.sounds)
                if (sound.clips != null) sound.clips = System.Array.FindAll(sound.clips, c => c != null);
            // a music slot whose file is gone compares equal to null; assigning null clears the stale reference
            if (bank.loadingMusic == null) bank.loadingMusic = null;
            if (bank.baseGameMusic == null) bank.baseGameMusic = null;
            if (bank.freeSpinsMusic == null) bank.freeSpinsMusic = null;

            if (AssetDatabase.IsValidFolder(AudioFolder))
            {
                var found = new Dictionary<SoundCue, List<AudioClip>>();
                // longest names first, so "ReelSpinLoop" is never taken for "ReelStop" or the like
                var cues = new List<SoundCue>((SoundCue[])Enum.GetValues(typeof(SoundCue)));
                cues.Sort((a, b) => b.ToString().Length.CompareTo(a.ToString().Length));

                foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { AudioFolder }))
                {
                    string file = AssetDatabase.GUIDToAssetPath(guid);
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(file);
                    string name = Path.GetFileNameWithoutExtension(file);
                    if (name.StartsWith("Music_Loading", StringComparison.OrdinalIgnoreCase)) { if (bank.loadingMusic == null) bank.loadingMusic = clip; continue; }
                    if (name.StartsWith("Music_FreeSpins", StringComparison.OrdinalIgnoreCase)) { if (bank.freeSpinsMusic == null) bank.freeSpinsMusic = clip; continue; }
                    if (name.StartsWith("Music_Base", StringComparison.OrdinalIgnoreCase)) { if (bank.baseGameMusic == null) bank.baseGameMusic = clip; continue; }
                    foreach (SoundCue cue in cues)
                    {
                        if (!name.StartsWith(cue.ToString(), StringComparison.OrdinalIgnoreCase)) continue;
                        if (!found.TryGetValue(cue, out List<AudioClip> list)) found[cue] = list = new List<AudioClip>();
                        list.Add(clip);
                        break;
                    }
                }
                foreach (SoundBank.Sound sound in bank.sounds)
                {
                    if (sound.HasClips || !found.TryGetValue(sound.cue, out List<AudioClip> clips)) continue;
                    sound.clips = clips.ToArray();
                    // a starting mix for a newly filled cue; later Inspector changes are kept
                    (sound.volume, sound.pitchVariance) = StartingMix(sound.cue);
                }
            }

            if (created) AssetDatabase.CreateAsset(bank, path);
            EditorUtility.SetDirty(bank);
            return bank;
        }

        /// <summary>
        /// Where a cue starts in the mix: interface and loops sit under the action, wins and the seal on top. Jingles and
        /// transitions keep their pitch (shifting them would put them out of tune); clicks and stops vary a little.
        /// </summary>
        private static (float volume, float pitchVariance) StartingMix(SoundCue cue)
        {
            switch (cue)
            {
                case SoundCue.ButtonClick:
                case SoundCue.BetChange: return (0.55f, 0.05f);
                case SoundCue.SpinPress: return (0.7f, 0.03f);
                case SoundCue.ReelSpinLoop: return (0.45f, 0f);
                case SoundCue.ReelStop: return (0.8f, 0.04f);
                case SoundCue.WildLand:
                case SoundCue.ScatterLand:
                case SoundCue.JackpotLand: return (0.85f, 0f);
                case SoundCue.SealCrack: return (0.9f, 0f);
                case SoundCue.SealBreak: return (1f, 0f);
                case SoundCue.SmallWin: return (0.7f, 0f);
                case SoundCue.CoinShowerLoop: return (0.5f, 0f);
                case SoundCue.StoryPage: return (0.6f, 0f);
                case SoundCue.GuardianFlame: return (0.7f, 0f);
                case SoundCue.AnticipationLoop: return (0.8f, 0f);
                case SoundCue.AnticipationEnd: return (0.85f, 0f);
                default: return (1f, 0f); // big wins, jackpots, free spins
            }
        }
    }
}
