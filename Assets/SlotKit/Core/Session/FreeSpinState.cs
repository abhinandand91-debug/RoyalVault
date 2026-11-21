using System;

namespace SlotKit
{
    // Free spins waiting to be played and what the current round has won so far
    public sealed class FreeSpinState
    {
        public int Remaining { get; private set; }
        // Spins played in the current round, including the one showing
        public int Played { get; private set; }
        public int Total
        {
            get
            {
                return Remaining + Played;
            }
        }
        public long RoundWin { get; private set; }
        // The symbol that expands during this round (an index into the machine's symbols), or -1
        public int Relic { get; private set; } = -1;
        public bool Active
        {
            get
            {
                return Remaining > 0 || Played > 0;
            }
        }

        // Raised after any change
        public event Action<FreeSpinState> Changed;

        public FreeSpinState(int remaining = 0, int relic = -1)
        {
            Remaining = Math.Max(0, remaining);
            Relic = Remaining > 0 ? relic : -1;
        }

        public void SetRelic(int symbol)
        {
            Relic = symbol;
            Changed?.Invoke(this);
        }

        public void Add(int spins)
        {
            if (spins <= 0)
            {
                return;
            }

            Remaining += spins;
            Changed?.Invoke(this);
        }

        // Uses one spin; false when none are left
        public bool TryConsume()
        {
            if (Remaining <= 0)
            {
                return false;
            }

            Remaining--;
            Played++;
            Changed?.Invoke(this);
            return true;
        }

        public void AddWin(long amount)
        {
            if (amount <= 0)
            {
                return;
            }

            RoundWin += amount;
            Changed?.Invoke(this);
        }

        // Clears the round once its last spin has been paid
        public void EndRound()
        {
            Played = 0;
            RoundWin = 0;
            Relic = -1;
            Changed?.Invoke(this);
        }
    }
}
