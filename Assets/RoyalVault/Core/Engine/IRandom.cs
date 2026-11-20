namespace RoyalVault.Core
{
    /// <summary>The source of every random decision, so tests and replays can use a fixed seed.</summary>
    public interface IRandom
    {
        /// <summary>A whole number from 0 up to, but not including, <paramref name="maxExclusive"/>.</summary>
        int Next(int maxExclusive);
    }

    /// <summary><see cref="IRandom"/> on System.Random; the same seed always gives the same spins.</summary>
    public sealed class SeededRandom : IRandom
    {
        private readonly System.Random random;

        public SeededRandom(int seed) { random = new System.Random(seed); }

        public SeededRandom() { random = new System.Random(); }

        public int Next(int maxExclusive) { return random.Next(maxExclusive); }
    }
}
