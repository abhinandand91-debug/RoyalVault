using System.Collections.Generic;
using PrimeTween;

namespace GameKit
{
    // The tweens one component started, stopped together (when it hides or is disabled) so none outlives its target
    public sealed class TweenGroup
    {
        private readonly List<Tween> tweens;
        private readonly List<Sequence> sequences;

        public TweenGroup(int capacity = 8)
        {
            tweens = new List<Tween>(capacity);
            sequences = new List<Sequence>(capacity);
        }

        // Whether any tween in the group is still running
        public bool IsPlaying
        {
            get
            {
                foreach (Tween tween in tweens)
                {
                    if (tween.isAlive)
                    {
                        return true;
                    }
                }

                foreach (Sequence sequence in sequences)
                {
                    if (sequence.isAlive)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public Tween Add(Tween tween)
        {
            Prune();
            tweens.Add(tween);
            return tween;
        }

        public Sequence Add(Sequence sequence)
        {
            Prune();
            sequences.Add(sequence);
            return sequence;
        }

        public void StopAll()
        {
            foreach (Tween tween in tweens)
            {
                tween.Stop();
            }

            foreach (Sequence sequence in sequences)
            {
                sequence.Stop();
            }

            tweens.Clear();
            sequences.Clear();
        }

        // Finished handles are dropped as new ones arrive, so the lists never grow
        private void Prune()
        {
            for (int i = tweens.Count - 1; i >= 0; i--)
            {
                if (!tweens[i].isAlive)
                {
                    tweens.RemoveAt(i);
                }
            }

            for (int i = sequences.Count - 1; i >= 0; i--)
            {
                if (!sequences[i].isAlive)
                {
                    sequences.RemoveAt(i);
                }
            }
        }
    }
}
