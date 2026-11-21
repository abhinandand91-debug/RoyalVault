using System;

namespace SlotKit
{
    // Progressive jackpots that grow with each bet and reset when won
    public sealed class JackpotPool
    {
        private readonly JackpotTierDefinition[] tiers;
        // values are kept in hundredths of a credit so small contributions are not lost to rounding
        private readonly long[] centi;

        // Raised with the tier index after it changes
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

        // Adds each tier's share of a paid bet
        public void Contribute(long bet)
        {
            for (int i = 0; i < tiers.Length; i++)
            {
                long before = Value(i);
                centi[i] += (long)Math.Round(bet * tiers[i].ContributionPercent);
                if (Value(i) != before) Changed?.Invoke(i);
            }
        }

        // Pays out a tier: returns its value and resets it to the seed
        public long Award(int index)
        {
            long won = Value(index);
            centi[index] = 100 * tiers[index].Seed;
            Changed?.Invoke(index);
            return won;
        }
    }
}
