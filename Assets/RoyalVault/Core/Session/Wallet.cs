using System;

namespace RoyalVault.Core
{
    /// <summary>The player's credits.</summary>
    public sealed class Wallet
    {
        public long Balance { get; private set; }

        /// <summary>Raised with the new balance after every change.</summary>
        public event Action<long> Changed;

        public Wallet(long balance) { Balance = Math.Max(0, balance); }

        public bool CanAfford(long amount) { return amount <= Balance; }

        /// <summary>Takes <paramref name="amount"/> if the balance covers it.</summary>
        public bool TryDebit(long amount)
        {
            if (amount < 0 || amount > Balance) return false;
            Balance -= amount;
            Changed?.Invoke(Balance);
            return true;
        }

        public void Credit(long amount)
        {
            if (amount <= 0) return;
            Balance += amount;
            Changed?.Invoke(Balance);
        }
    }
}
