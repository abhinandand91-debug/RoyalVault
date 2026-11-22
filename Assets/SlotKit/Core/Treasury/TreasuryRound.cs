using System.Collections.Generic;

namespace SlotKit
{
    public enum UrnPrize
    {
        // Credits worth a number of total bets
        Credits,
        // Adds one to the round's multiplier
        Multiplier,
        // Blocks the next eye
        Shield,
        // Ends the round unless a shield blocks it
        Eye,
    }

    // What one urn holds
    public readonly struct Urn
    {
        public readonly UrnPrize Prize;
        // For credit urns: how many total bets
        public readonly int TotalBets;

        public Urn(UrnPrize prize, int totalBets = 0)
        {
            Prize = prize;
            TotalBets = totalBets;
        }
    }

    // One Treasury round, decided in full when it opens: whichever urn the player taps reveals the next prize in Order, so
    // the player's choices change what they see but never the odds
    public sealed class TreasuryRound
    {
        // Every urn's prize, in the order they are revealed
        public readonly Urn[] Order;
        // How many urns are opened before the round ends
        public readonly int Picks;
        public readonly long TotalBet;
        // The keys that opened it
        public readonly List<GridPosition> Keys;

        private TreasuryRound(Urn[] order, long totalBet, List<GridPosition> keys)
        {
            Order = order;
            TotalBet = totalBet;
            Keys = keys;
            Picks = CountPicks(order);
        }

        // Credits won by the end of the round
        public long Amount
        {
            get
            {
                return AmountAfter(Picks);
            }
        }

        // The credits collected after this many picks, before the multiplier
        public long CreditsAfter(int picks)
        {
            long credits = 0;
            for (int i = 0; i < picks && i < Order.Length; i++)
            {
                if (Order[i].Prize == UrnPrize.Credits)
                {
                    credits += Order[i].TotalBets * TotalBet;
                }
            }

            return credits;
        }

        // The round's multiplier after this many picks
        public int MultiplierAfter(int picks)
        {
            int multiplier = 1;
            for (int i = 0; i < picks && i < Order.Length; i++)
            {
                if (Order[i].Prize == UrnPrize.Multiplier)
                {
                    multiplier++;
                }
            }

            return multiplier;
        }

        // Shields held after this many picks, after any eyes they blocked
        public int ShieldsAfter(int picks)
        {
            int shields = 0;
            for (int i = 0; i < picks && i < Order.Length; i++)
            {
                if (Order[i].Prize == UrnPrize.Shield)
                {
                    shields++;
                }
                else if (Order[i].Prize == UrnPrize.Eye && shields > 0)
                {
                    shields--;
                }
            }

            return shields;
        }

        public long AmountAfter(int picks)
        {
            return CreditsAfter(picks) * MultiplierAfter(picks);
        }

        // Fills the urns from the settings and shuffles them; the first pick always holds credits, so no round pays nothing
        public static TreasuryRound Draw(TreasurySettings settings, IRandom random, long totalBet, List<GridPosition> keys)
        {
            var order = new Urn[settings.Urns];
            int next = 0;
            next = Fill(order, next, settings.Eyes, new Urn(UrnPrize.Eye));
            next = Fill(order, next, settings.Shields, new Urn(UrnPrize.Shield));
            next = Fill(order, next, settings.Multipliers, new Urn(UrnPrize.Multiplier));
            while (next < order.Length)
            {
                order[next] = new Urn(UrnPrize.Credits, Weighted(settings.CreditValues, settings.CreditWeights, random));
                next++;
            }

            Shuffle(order, random);
            if (order[0].Prize != UrnPrize.Credits)
            {
                SwapFirstWithCredits(order, random);
            }

            return new TreasuryRound(order, totalBet, keys);
        }

        private static int Fill(Urn[] order, int start, int count, Urn urn)
        {
            int end = System.Math.Min(order.Length, start + count);
            for (int i = start; i < end; i++)
            {
                order[i] = urn;
            }

            return end;
        }

        private static int Weighted(int[] values, int[] weights, IRandom random)
        {
            int total = 0;
            foreach (int weight in weights)
            {
                total += weight;
            }

            int roll = random.Next(total);
            for (int i = 0; i < values.Length; i++)
            {
                roll -= weights[i];
                if (roll < 0)
                {
                    return values[i];
                }
            }

            return values[values.Length - 1];
        }

        private static void Shuffle(Urn[] order, IRandom random)
        {
            for (int i = order.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
        }

        private static void SwapFirstWithCredits(Urn[] order, IRandom random)
        {
            int credits = 0;
            foreach (Urn urn in order)
            {
                if (urn.Prize == UrnPrize.Credits)
                {
                    credits++;
                }
            }

            int pick = random.Next(credits);
            for (int i = 1; i < order.Length; i++)
            {
                if (order[i].Prize != UrnPrize.Credits)
                {
                    continue;
                }

                if (pick == 0)
                {
                    (order[0], order[i]) = (order[i], order[0]);
                    return;
                }

                pick--;
            }
        }

        // Opens urns until an eye gets through, or every urn is open
        private static int CountPicks(Urn[] order)
        {
            int shields = 0;
            for (int i = 0; i < order.Length; i++)
            {
                if (order[i].Prize == UrnPrize.Shield)
                {
                    shields++;
                }
                else if (order[i].Prize == UrnPrize.Eye)
                {
                    if (shields == 0)
                    {
                        return i + 1;
                    }

                    shields--;
                }
            }

            return order.Length;
        }
    }
}
