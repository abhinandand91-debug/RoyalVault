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

        /// <summary>
        /// The story "Gold Rush to the Pharaoh's Vault": six scenes from a frontier saloon to the Pharaoh's crown. The
        /// painted backdrops are public; the prospector and the sharpshooter are licensed art and stay out of the
        /// repository, so a public checkout tells the story over the paintings alone.
        /// </summary>
        private static StoryDefinition CreateStory()
        {
            string path = DataPath + "RoyalVault_Story.asset";
            var story = AssetDatabase.LoadAssetAtPath<StoryDefinition>(path);
            bool created = story == null;
            if (created) story = ScriptableObject.CreateInstance<StoryDefinition>();

            Sprite cowboy = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Art.Private/Characters/Heroes/cowboy.png");
            Sprite cowgirl = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Art.Private/Characters/Heroes/cowgirl.png");
            StoryDefinition.Figure Hero(Sprite sprite, float x, float y, float height, bool mirrored = false) =>
                new StoryDefinition.Figure { sprite = sprite, position = new Vector2(x, y), height = height, mirrored = mirrored };
            StoryDefinition.Scene Scene(int n, string title, string caption, StoryMoment moment, params StoryDefinition.Figure[] figures) =>
                new StoryDefinition.Scene { title = title, caption = caption, art = Sprite("Story/scene_" + n, ".jpg"), moment = moment,
                                            figures = System.Array.FindAll(figures, f => f.sprite != null) };

            story.chapterLabel = "Chapter";
            story.secondsPerScene = 5f;
            story.scenes = new[]
            {
                Scene(1, "The Card Game",
                      "Midnight in a frontier saloon. Ruby Vance wins the last hand, and a torn map drawn in her grandfather's hand.",
                      StoryMoment.Intro, Hero(cowgirl, -640f, -610f, 900f), Hero(cowboy, 650f, -600f, 830f, true)),
                Scene(2, "Across the Sea of Sand",
                      "Weeks later she and Cole Harlan stand on a dune at dusk. The tip of a buried pyramid catches the last light.",
                      StoryMoment.Intro, Hero(cowboy, -780f, -590f, 720f), Hero(cowgirl, -560f, -600f, 770f)),
                Scene(3, "The Buried Door",
                      "Cole digs; Ruby keeps watch. A golden seal bars the door, and every Vault Seal cracks it a little more.",
                      StoryMoment.BaseGame),
                Scene(4, "Hall of the Guardians",
                      "The seal breaks. Two Anubis statues open their eyes, blue fire in their hands. Prove you are not thieves.",
                      StoryMoment.FreeSpins),
                Scene(5, "The Treasury",
                      "Behind the guardians lie three chambers. Crowns on the reels open them one by one.",
                      StoryMoment.MajorJackpot),
                Scene(6, "The Pharaoh's Crown",
                      "Beside the crown lies her grandfather's hat. The guardians bow: the worthy may leave with the gold.",
                      StoryMoment.GrandJackpot),
            };

            if (created) AssetDatabase.CreateAsset(story, path);
            EditorUtility.SetDirty(story);
            return story;
        }
    }
}
