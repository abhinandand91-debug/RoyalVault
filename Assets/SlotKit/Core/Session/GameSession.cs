using System;

namespace SlotKit
{
    // What is saved between sessions
    [Serializable]
    public sealed class SaveData
    {
        public long balance;
        public int lineBetIndex;
        public long[] jackpots;
        public int freeSpins;
        // The free spin round's relic symbol plus one, so saves made before relics existed read as none
        public int freeSpinRelicPlusOne;
        // Credits from a Treasury round that had not been shown when the game closed
        public long treasuryPending;
    }

    // Where the session is saved; the game uses PlayerPrefs, tests use memory
    public interface ISaveStore
    {
        // The saved data, or null when nothing has been saved yet
        SaveData Load();

        void Save(SaveData data);
    }

    // An ISaveStore that keeps the data in memory (tests, demos)
    public sealed class MemorySaveStore : ISaveStore
    {
        public SaveData Data;

        public SaveData Load()
        {
            return Data;
        }

        public void Save(SaveData data)
        {
            Data = data;
        }
    }

    // One player's state on one machine: credits, bet, jackpots and free spins
    public sealed class GameSession
    {
        private readonly ISaveStore store;

        public MachineDefinition Machine { get; }
        public Wallet Wallet { get; }
        public BetModel Bet { get; }
        public JackpotPool Jackpots { get; }
        public FreeSpinState FreeSpins { get; }
        public TreasuryState Treasury { get; } = new TreasuryState();

        public GameSession(MachineDefinition machine, ISaveStore store)
        {
            Machine = machine;
            this.store = store;
            SaveData saved = store?.Load();
            Wallet = new Wallet(saved != null ? saved.balance : machine.StartBalance);
            Bet = new BetModel(machine, saved != null ? saved.lineBetIndex : machine.DefaultLineBetIndex);
            Jackpots = new JackpotPool(machine.JackpotTiers, saved?.jackpots);
            FreeSpins = new FreeSpinState(saved != null ? saved.freeSpins : 0, saved != null ? saved.freeSpinRelicPlusOne - 1 : -1);
            // a round won before the game closed is paid now rather than lost
            if (saved != null && saved.treasuryPending > 0)
            {
                Wallet.Credit(saved.treasuryPending);
                Save();
            }
        }

        public void Save()
        {
            store?.Save(new SaveData
            {
                balance = Wallet.Balance,
                lineBetIndex = Bet.LevelIndex,
                jackpots = Jackpots.Values(),
                freeSpins = FreeSpins.Remaining,
                freeSpinRelicPlusOne = FreeSpins.Relic + 1,
                treasuryPending = Treasury.Pending,
            });
        }
    }
}
