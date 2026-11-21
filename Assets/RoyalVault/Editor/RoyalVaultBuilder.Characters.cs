using System.IO;
using RoyalVault.Unity;
using UnityEditor;
using UnityEngine;

namespace RoyalVault.EditorTools
{
    public static partial class RoyalVaultBuilder
    {
        [System.Serializable]
        private sealed class TrackClip
        {
            public float[] hand;
            public float[] flame;
        }

        [System.Serializable]
        private sealed class Track
        {
            public float fps;
            public float figureShare;
            public TrackClip idle;
            public TrackClip action;
        }

        /// <summary>Height the still character art is shown at; the baked frames are sized so the figure matches it.</summary>
        private const float FigureHeight = 705f;

        /// <summary>
        /// Fills a <see cref="CharacterAnimation"/> next to baked character frames (idle_000.png..., action_000.png...
        /// and a track file with the hand path). Returns null when the folder is absent, as in a public checkout.
        /// </summary>
        private static CharacterAnimation CreateCharacterAnimation(string folder)
        {
            string name = Path.GetFileName(folder).ToLowerInvariant();
            string trackPath = folder + "/" + name + "_track.json";
            if (!File.Exists(trackPath)) return null;

            var track = JsonUtility.FromJson<Track>(File.ReadAllText(trackPath));
            string assetPath = folder + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<CharacterAnimation>(assetPath);
            bool created = asset == null;
            if (created) asset = ScriptableObject.CreateInstance<CharacterAnimation>();

            asset.framesPerSecond = track.fps;
            asset.displayHeight = Mathf.Round(FigureHeight / Mathf.Max(0.1f, track.figureShare));
            asset.idle = Clip(folder, "idle", track.idle);
            asset.action = Clip(folder, "action", track.action);

            if (created) AssetDatabase.CreateAsset(asset, assetPath);
            else EditorUtility.SetDirty(asset);
            return asset;
        }

        private static CharacterAnimation.Clip Clip(string folder, string prefix, TrackClip data)
        {
            var clip = new CharacterAnimation.Clip();
            if (data == null) return clip;
            int count = data.flame.Length;
            clip.frames = new Sprite[count];
            clip.hand = new Vector2[count];
            clip.flame = new float[count];
            for (int i = 0; i < count; i++)
            {
                clip.frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/{prefix}_{i:000}.png");
                clip.hand[i] = new Vector2(data.hand[i * 2], data.hand[i * 2 + 1]);
                clip.flame[i] = data.flame[i];
            }
            return clip;
        }
    }
}
