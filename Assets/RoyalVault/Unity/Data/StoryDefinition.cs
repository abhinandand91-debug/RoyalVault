using System;
using UnityEngine;

namespace RoyalVault.Unity
{
    // When in the game a story scene is told
    public enum StoryMoment
    {
        // On the loading screen, before the reels appear
        Intro,
        BaseGame,
        FreeSpins,
        MinorJackpot,
        MajorJackpot,
        GrandJackpot,
    }

    // The story's scenes and the game moment each one belongs to
    [CreateAssetMenu(menuName = "Royal Vault/Story", fileName = "Story")]
    public sealed class StoryDefinition : ScriptableObject
    {
        [Serializable]
        public sealed class Figure
        {
            [Tooltip("Cut-out character art, pivoted on its feet (licensed art may be missing in a public checkout)")]
            public Sprite sprite;
            [Tooltip("Where the feet stand, in canvas pixels from the centre of a 1920 x 1080 screen")]
            public Vector2 position;
            [Tooltip("Height on screen in canvas pixels")]
            public float height = 760f;
            [Tooltip("Face the other way")]
            public bool mirrored;
        }

        [Serializable]
        public sealed class Scene
        {
            public string title;
            [TextArea(2, 5)]
            public string caption;
            [Tooltip("The scene told beat by beat, one line per click")]
            [TextArea(1, 3)]
            public string[] lines = new string[0];
            public Sprite art;
            public Figure[] figures = new Figure[0];
            public StoryMoment moment;

            // Beats to tell, falling back to the one-line caption
            public string[] Beats => lines != null && lines.Length > 0 ? lines : new[] { caption };
        }

        [Tooltip("Shown above each scene's title, e.g. CHAPTER")]
        public string chapterLabel = "Chapter";
        [Tooltip("Seconds each intro scene stays on screen the first time the story is told")]
        public float secondsPerScene = 5f;
        public Scene[] scenes = new Scene[0];
        [Tooltip("First-visit hints: the spin button, the seal meter, the jackpots")]
        [TextArea(1, 3)]
        public string[] controlHints = new string[0];

        // "CHAPTER III" style heading
        public static string ChapterName(string label, int number)
        {
            string[] numerals = { "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X", "XI", "XII" };
            string roman = number >= 0 && number < numerals.Length ? numerals[number] : number.ToString();
            return (label + " " + roman).ToUpperInvariant();
        }

        // The scenes told at moment, in story order, with their chapter numbers (1-based)
        public (Scene scene, int number)[] At(StoryMoment moment)
        {
            int count = 0;
            foreach (Scene s in scenes)
            {
                if (s.moment == moment)
                {
                    count++;
                }
            }

            var result = new (Scene, int)[count];
            int k = 0;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].moment == moment)
                {
                    result[k++] = (scenes[i], i + 1);
                }
            }

            return result;
        }
    }
}
