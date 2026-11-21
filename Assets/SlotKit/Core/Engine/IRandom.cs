namespace SlotKit
{
    // The source of every random decision, so tests and replays can use a fixed seed
    public interface IRandom
    {
        // A whole number from 0 up to, but not including, maxExclusive
        int Next(int maxExclusive);
    }

    // IRandom on System.Random; the same seed always gives the same spins
    public sealed class SeededRandom : IRandom
    {
        private readonly System.Random random;

        public SeededRandom(int seed) { random = new System.Random(seed); }

        public SeededRandom() { random = new System.Random(); }

        public int Next(int maxExclusive) { return random.Next(maxExclusive); }
    }
}
