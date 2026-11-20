using System;

namespace RoyalVault.Core
{
    /// <summary>Free spins waiting to be played and what the current round has won so far.</summary>
    public sealed class FreeSpinState
    {
        public int Remaining { get; private set; }
        /// <summary>Spins played in the current round, including the one showing.</summary>
        public int Played { get; private set; }
        public int Total { get { return Remaining + Played; } }
        public long RoundWin { get; private set; }
        public bool Active { get { return Remaining > 0 || Played > 0; } }

        /// <summary>Raised after any change.</summary>
        public event Action<FreeSpinState> Changed;

        public FreeSpinState(int remaining = 0) { Remaining = Math.Max(0, remaining); }

        public void Add(int spins)
        {
            if (spins <= 0) return;
            Remaining += spins;
            Changed?.Invoke(this);
        }

        /// <summary>Uses one spin; false when none are left.</summary>
        public bool TryConsume()
        {
            if (Remaining <= 0) return false;
            Remaining--;
            Played++;
            Changed?.Invoke(this);
            return true;
        }

        public void AddWin(long amount)
        {
            if (amount <= 0) return;
            RoundWin += amount;
            Changed?.Invoke(this);
        }

        /// <summary>Clears the round once its last spin has been paid.</summary>
        public void EndRound()
        {
            Played = 0;
            RoundWin = 0;
            Changed?.Invoke(this);
        }
    }
}
