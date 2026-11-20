using System;

namespace RoyalVault.Core
{
    /// <summary>The progressive jackpots: each tier grows by a share of every paid bet and resets to its seed when won.</summary>
    public sealed class JackpotPool
    {
        private readonly JackpotTierDefinition[] tiers;
        // values are kept in hundredths of a credit so small contributions are not lost to rounding
        private readonly long[] centi;

        /// <summary>Raised with the tier index after it changes.</summary>
        public event Action<int> Changed;

        public JackpotPool(JackpotTierDefinition[] tiers, long[] saved = null)
        {
            this.tiers = tiers;
            centi = new long[tiers.Length];
            for (int i = 0; i < tiers.Length; i++)
                centi[i] = 100 * (saved != null && i < saved.Length && saved[i] >= tiers[i].Seed ? saved[i] : tiers[i].Seed);
        }

        public int Count { get { return tiers.Length; } }

        public JackpotTierDefinition Tier(int index) { return tiers[index]; }

        public long Value(int index) { return centi[index] / 100; }

        public long[] Values()
        {
            var values = new long[tiers.Length];
            for (int i = 0; i < values.Length; i++) values[i] = Value(i);
            return values;
        }

        /// <summary>Adds each tier's share of a paid bet.</summary>
        public void Contribute(long bet)
        {
            for (int i = 0; i < tiers.Length; i++)
            {
                long before = Value(i);
                centi[i] += (long)Math.Round(bet * tiers[i].ContributionPercent);
                if (Value(i) != before) Changed?.Invoke(i);
            }
        }

        /// <summary>Pays out a tier: returns its value and resets it to the seed.</summary>
        public long Award(int index)
        {
            long won = Value(index);
            centi[index] = 100 * tiers[index].Seed;
            Changed?.Invoke(index);
            return won;
        }
    }
}
