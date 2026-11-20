using System;

namespace RoyalVault.Core
{
    /// <summary>What is saved between sessions.</summary>
    [Serializable]
    public sealed class SaveData
    {
        public long balance;
        public int lineBetIndex;
        public long[] jackpots;
        public int freeSpins;
    }

    /// <summary>Where the session is saved; the game uses PlayerPrefs, tests use memory.</summary>
    public interface ISaveStore
    {
        /// <summary>The saved data, or null when nothing has been saved yet.</summary>
        SaveData Load();

        void Save(SaveData data);
    }

    /// <summary>An <see cref="ISaveStore"/> that keeps the data in memory (tests, demos).</summary>
    public sealed class MemorySaveStore : ISaveStore
    {
        public SaveData Data;

        public SaveData Load() { return Data; }

        public void Save(SaveData data) { Data = data; }
    }

    /// <summary>One player's state on one machine: credits, bet, jackpots and free spins.</summary>
    public sealed class GameSession
    {
        private readonly ISaveStore store;

        public MachineDefinition Machine { get; }
        public Wallet Wallet { get; }
        public BetModel Bet { get; }
        public JackpotPool Jackpots { get; }
        public FreeSpinState FreeSpins { get; }

        public GameSession(MachineDefinition machine, ISaveStore store)
        {
            Machine = machine;
            this.store = store;
            SaveData saved = store?.Load();
            Wallet = new Wallet(saved != null ? saved.balance : machine.StartBalance);
            Bet = new BetModel(machine, saved != null ? saved.lineBetIndex : machine.DefaultLineBetIndex);
            Jackpots = new JackpotPool(machine.JackpotTiers, saved?.jackpots);
            FreeSpins = new FreeSpinState(saved != null ? saved.freeSpins : 0);
        }

        public void Save()
        {
            store?.Save(new SaveData
            {
                balance = Wallet.Balance,
                lineBetIndex = Bet.LevelIndex,
                jackpots = Jackpots.Values(),
                freeSpins = FreeSpins.Remaining,
            });
        }
    }
}
