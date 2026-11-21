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

        // Height the still character art is shown at; the baked frames are sized so the figure matches it
        private const float FigureHeight = 705f;

        // Fills a CharacterAnimation next to baked character frames (idle_000.png..., action_000.png..
        private static CharacterAnimation CreateCharacterAnimation(string folder, string assetPath = null)
        {
            string name = Path.GetFileName(folder).ToLowerInvariant();
            string trackPath = folder + "/" + name + "_track.json";
            if (!File.Exists(trackPath))
            {
                return null;
            }

            var track = JsonUtility.FromJson<Track>(File.ReadAllText(trackPath));
            assetPath = assetPath ?? folder + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<CharacterAnimation>(assetPath);
            bool created = asset == null;
            if (created)
            {
                asset = ScriptableObject.CreateInstance<CharacterAnimation>();
            }

            asset.framesPerSecond = track.fps;
            asset.figureShare = Mathf.Clamp(track.figureShare, 0.1f, 1f);
            asset.displayHeight = Mathf.Round(FigureHeight / Mathf.Max(0.1f, track.figureShare));
            asset.idle = Clip(folder, "idle", track.idle);
            asset.action = Clip(folder, "action", track.action);

            if (created)
            {
                AssetDatabase.CreateAsset(asset, assetPath);
            }
            else
            {
                EditorUtility.SetDirty(asset);
            }

            return asset;
        }

        // A symbol's baked win frames from Art.Private/Symbols/<id>
        private static CharacterAnimation SymbolWinAnimation(string id)
        {
            string symbols = Root + "Art.Private/Symbols";
            string assetPath = symbols + "/" + id + ".asset";
            if (!Directory.Exists(symbols))
            {
                return null;
            }

            foreach (string dir in Directory.GetDirectories(symbols))
            {
                if (string.Equals(Path.GetFileName(dir), id, System.StringComparison.OrdinalIgnoreCase))
                {
                    return CreateCharacterAnimation(symbols + "/" + Path.GetFileName(dir), assetPath);
                }
            }

            return AssetDatabase.LoadAssetAtPath<CharacterAnimation>(assetPath);
        }

        private static CharacterAnimation.Clip Clip(string folder, string prefix, TrackClip data)
        {
            var clip = new CharacterAnimation.Clip();
            if (data == null)
            {
                return clip;
            }

            int count = data.flame.Length;
            clip.frames = new Sprite[count];
            clip.hand = new Vector2[count];
            clip.flame = new float[count];
            for (int i = 0; i < count; i++)
            {
                clip.frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/{prefix}_{i:000}.png");
                // a character without magic in its hand has no hand path
                clip.hand[i] = data.hand != null && data.hand.Length > i * 2 + 1 ? new Vector2(data.hand[i * 2], data.hand[i * 2 + 1]) : Vector2.zero;
                clip.flame[i] = data.flame[i];
            }
            return clip;
        }

        // Each chapter's beats and the first-visit hints
        private static void SetBeats(StoryDefinition story)
        {
            string[][] beats =
            {
                new[] { "Midnight in a frontier saloon. One last hand, one last pot.",
                        "Ruby Vance wins it, and with it a torn map drawn in her grandfather's hand.",
                        "He vanished in the desert forty years ago. The map says where." },
                new[] { "Cole Harlan, a prospector with nothing left to dig, offers his pickaxe.",
                        "Weeks of sea, rail and sand later, they stand on a dune at dusk.",
                        "Below them, the tip of a buried pyramid catches the last light." },
                new[] { "At the pyramid's foot, a golden seal bars the way in.",
                        "Cole digs. Ruby keeps watch. The seal will not break by hand.",
                        "But every Vault Seal they find cracks it a little more." },
                new[] { "The seal shatters. Blue fire runs along the walls.",
                        "Two Anubis statues open their eyes and raise their hands.",
                        "\"Prove you are not thieves.\" The trial begins." },
                new[] { "Behind the guardians lie three chambers, each richer than the last.",
                        "Crowns are the keys. The deeper the chamber, the more crowns it takes." },
                new[] { "In the sealed tomb, beside the Pharaoh's crown, lies a dusty cowboy hat.",
                        "Ruby's grandfather made it this far.",
                        "The guardians bow. The worthy may leave with the gold." },
            };
            for (int i = 0; i < story.scenes.Length && i < beats.Length; i++)
            {
                story.scenes[i].lines = beats[i];
            }

            story.controlHints = new[]
            {
                "Spin to dig. Every spin brings you closer to the buried door.",
                "Each Vault Seal cracks the seal. Land three in one spin to break it and wake the guardians.",
                "Crowns open the vault's chambers. Land enough of them to win these jackpots.",
            };
        }

        private static StoryDefinition CreateStory()
        {
            string path = DataPath + "RoyalVault_Story.asset";
            var story = AssetDatabase.LoadAssetAtPath<StoryDefinition>(path);
            bool created = story == null;
            if (created)
            {
                story = ScriptableObject.CreateInstance<StoryDefinition>();
            }

            story.chapterLabel = "Chapter";
            story.secondsPerScene = 5f;
            story.scenes = StoryScenes();
            SetBeats(story);
            if (created)
            {
                AssetDatabase.CreateAsset(story, path);
            }

            EditorUtility.SetDirty(story);
            return story;
        }

        private static StoryDefinition.Scene[] StoryScenes()
        {
            Sprite cowboy = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Art.Private/Characters/Heroes/cowboy.png");
            Sprite cowgirl = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Art.Private/Characters/Heroes/cowgirl.png");
            return new[]
            {
                StoryScene(1, "The Card Game",
                      "Midnight in a frontier saloon. Ruby Vance wins the last hand, and a torn map drawn in her grandfather's hand.",
                      StoryMoment.Intro, StoryFigure(cowgirl, -640f, -610f, 900f), StoryFigure(cowboy, 650f, -600f, 830f, true)),
                StoryScene(2, "Across the Sea of Sand",
                      "Weeks later she and Cole Harlan stand on a dune at dusk. The tip of a buried pyramid catches the last light.",
                      StoryMoment.Intro, StoryFigure(cowboy, -780f, -590f, 720f), StoryFigure(cowgirl, -560f, -600f, 770f)),
                StoryScene(3, "The Buried Door",
                      "Cole digs; Ruby keeps watch. A golden seal bars the door, and every Vault Seal cracks it a little more.",
                      StoryMoment.BaseGame),
                StoryScene(4, "Hall of the Guardians",
                      "The seal breaks. Two Anubis statues open their eyes, blue fire in their hands. Prove you are not thieves.",
                      StoryMoment.FreeSpins),
                StoryScene(5, "The Treasury",
                      "Behind the guardians lie three chambers. Crowns on the reels open them one by one.",
                      StoryMoment.MajorJackpot),
                StoryScene(6, "The Pharaoh's Crown",
                      "Beside the crown lies her grandfather's hat. The guardians bow: the worthy may leave with the gold.",
                      StoryMoment.GrandJackpot),
            };
        }

        private static StoryDefinition.Figure StoryFigure(Sprite sprite, float x, float y, float height, bool mirrored = false) =>
            new StoryDefinition.Figure { sprite = sprite, position = new Vector2(x, y), height = height, mirrored = mirrored };

        // A figure without its (licensed) sprite is left out
        private static StoryDefinition.Scene StoryScene(int n, string title, string caption, StoryMoment moment, params StoryDefinition.Figure[] figures) =>
            new StoryDefinition.Scene
            {
                title = title,
                caption = caption,
                art = Sprite("Story/scene_" + n, ".jpg"),
                moment = moment,
                figures = System.Array.FindAll(figures, f => f.sprite != null)
            };
    }
}
