using System;
using System.Collections.Generic;

namespace SlotKit
{
    // What the player calls before a card is turned: a colour pays double, a suit four times
    public enum GambleChoice
    {
        Red,
        Black,
        Hearts,
        Diamonds,
        Clubs,
        Spades,
    }

    // A playing card: suit 0 hearts, 1 diamonds, 2 clubs, 3 spades; rank 1 (ace) to 13 (king)
    public readonly struct Card
    {
        public readonly int Suit;
        public readonly int Rank;

        public Card(int suit, int rank)
        {
            Suit = suit;
            Rank = rank;
        }

        public bool IsRed
        {
            get
            {
                return Suit < 2;
            }
        }
    }

    // A double-or-nothing gamble on a win that has already been credited: a right call adds to it, a wrong one takes it
    // back; every call has fair odds, so the gamble does not change the machine's return
    public sealed class GambleSession
    {
        private readonly Wallet wallet;
        private readonly IRandom random;
        private readonly GambleSettings settings;
        private readonly List<Card> history = new List<Card>();
        private long limit;

        public GambleSession(Wallet wallet, IRandom random, GambleSettings settings)
        {
            this.wallet = wallet;
            this.random = random;
            this.settings = settings;
        }

        public bool Active { get; private set; }
        public long Stake { get; private set; }
        public int Rounds { get; private set; }
        public IReadOnlyList<Card> History
        {
            get
            {
                return history;
            }
        }

        // Raised after every call with the card turned and whether the call was right
        public event Action<Card, bool> Resolved;
        // Raised when the gamble ends, with what the player keeps
        public event Action<long> Ended;

        // A win this size may be gambled at this bet
        public bool Offers(long win, long totalBet)
        {
            return settings.Enabled && win > 0 && win * 2 <= totalBet * settings.MaxWinTotalBets;
        }

        public void Start(long win, long totalBet)
        {
            Stake = win;
            Rounds = 0;
            limit = totalBet * settings.MaxWinTotalBets;
            history.Clear();
            Active = true;
        }

        public static int Multiplier(GambleChoice choice)
        {
            return choice == GambleChoice.Red || choice == GambleChoice.Black ? 2 : 4;
        }

        // A call may be made while the gamble runs and its win stays within the limit
        public bool CanCall(GambleChoice choice)
        {
            return Active && Stake * Multiplier(choice) <= limit;
        }

        public Card Call(GambleChoice choice)
        {
            if (!CanCall(choice))
            {
                throw new InvalidOperationException("That call is not available.");
            }

            var card = new Card(random.Next(4), 1 + random.Next(13));
            history.Add(card);
            Rounds++;
            bool right = Matches(choice, card);
            Settle(choice, right);
            Resolved?.Invoke(card, right);
            if (!right || Rounds >= settings.MaxRounds || !CanCall(GambleChoice.Red))
            {
                Finish();
            }

            return card;
        }

        // Keeps the stake and ends the gamble
        public void Collect()
        {
            if (Active)
            {
                Finish();
            }
        }

        public static bool Matches(GambleChoice choice, Card card)
        {
            switch (choice)
            {
                case GambleChoice.Red:
                    return card.IsRed;
                case GambleChoice.Black:
                    return !card.IsRed;
                default:
                    return card.Suit == (int)choice - (int)GambleChoice.Hearts;
            }
        }

        private void Settle(GambleChoice choice, bool right)
        {
            if (right)
            {
                long won = Stake * (Multiplier(choice) - 1);
                wallet.Credit(won);
                Stake += won;
                return;
            }

            wallet.TryDebit(Stake);
            Stake = 0;
        }

        private void Finish()
        {
            Active = false;
            Ended?.Invoke(Stake);
        }
    }
}
