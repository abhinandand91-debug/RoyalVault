using System.Collections;
using System;
using GameKit;
using UnityEngine;

namespace RoyalVault.Unity
{
    // Tells each story chapter once, at its moment in play, and walks a new player through the controls
    public sealed class StoryDirector : MonoBehaviour
    {
        [Serializable]
        public sealed class Hint
        {
            public RectTransform target;
            [TextArea(1, 3)] public string text;
            public bool above = true;
        }

        private const string KeyPrefix = "royalvault.story.";

        private StoryDefinition story;
        private ChapterCardView card;
        private CoachMarkView coach;
        private Hint[] hints = new Hint[0];
        private bool muted;

        public bool IsTelling { get; private set; }

        public void Bind(StoryDefinition storyAsset, ChapterCardView chapterCard, CoachMarkView coachMarks, Hint[] firstVisitHints)
        {
            story = storyAsset;
            card = chapterCard;
            coach = coachMarks;
            hints = firstVisitHints ?? new Hint[0];
        }

        // Turns the story off for this session and closes anything being told
        public void Mute()
        {
            muted = true;
            if (card != null)
            {
                card.CloseNow();
            }

            if (coach != null)
            {
                coach.CloseNow();
            }
        }

        // The first visit: the base game's chapter, then the controls
        public IEnumerator Welcome()
        {
            yield return TellOnce(StoryMoment.BaseGame);
            if (Seen("hints") || muted || coach == null)
            {
                yield break;
            }

            IsTelling = true;
            foreach (Hint hint in hints)
            {
                if (!muted && hint.target != null)
                {
                    yield return coach.Show(hint.target, hint.text, hint.above);
                }
            }

            MarkSeen("hints");
            IsTelling = false;
        }

        // Tells the chapters of a moment that this player has not seen yet
        public IEnumerator TellOnce(StoryMoment moment)
        {
            if (story == null || card == null || muted)
            {
                yield break;
            }

            foreach ((StoryDefinition.Scene scene, int number) in story.At(moment))
            {
                if (Seen("chapter." + number) || muted)
                {
                    continue;
                }

                IsTelling = true;
                yield return card.Tell(scene, number, story.chapterLabel);
                MarkSeen("chapter." + number);
            }
            IsTelling = false;
        }

        private static bool Seen(string key) => PlayerPrefs.GetInt(KeyPrefix + key, 0) == 1;

        private static void MarkSeen(string key)
        {
            PlayerPrefs.SetInt(KeyPrefix + key, 1);
            PlayerPrefs.Save();
        }
    }
}
