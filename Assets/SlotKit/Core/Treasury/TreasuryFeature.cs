using System;
using System.Collections.Generic;

namespace SlotKit
{
    // A Treasury round that has been won but not yet paid: it is paid when the player has opened the urns, or straight
    // away on the next launch if the game closed in between
    public sealed class TreasuryState
    {
        public TreasuryRound Round { get; private set; }
        // Credits held for the round, saved so they are never lost
        public long Pending { get; private set; }

        // Raised after a round is held or paid
        public event Action<TreasuryState> Changed;

        public void Hold(TreasuryRound round)
        {
            Round = round;
            Pending = round.Amount;
            Changed?.Invoke(this);
        }

        // Pays the held round into the wallet; returns what was paid
        public long Collect(Wallet wallet)
        {
            long paid = Pending;
            Round = null;
            Pending = 0;
            if (paid > 0)
            {
                wallet.Credit(paid);
            }

            Changed?.Invoke(this);
            return paid;
        }
    }

    // The Treasury: a Vault Key on each key reel of a base-game spin opens a pick-and-reveal round
    public sealed class TreasuryFeature : SlotFeature
    {
        private readonly IRandom random;

        // Testing aid: the next base-game spin opens the Treasury whatever the reels show, then this is cleared
        public bool ForceNext;

        public TreasuryFeature(IRandom random)
        {
            this.random = random;
        }

        // After the relic and before win tiers are judged
        public override int Order
        {
            get
            {
                return 6;
            }
        }

        public override void OnOutcome(SpinContext context)
        {
            TreasurySettings settings = context.Session.Machine.Treasury;
            if (!settings.Enabled || context.IsFreeSpin)
            {
                return;
            }

            List<GridPosition> keys = Keys(context.Session.Machine, context.Outcome.Grid);
            bool forced = ForceNext;
            ForceNext = false;
            if (keys != null || forced)
            {
                context.Outcome.Treasury = TreasuryRound.Draw(settings, random, context.Outcome.TotalBet, keys ?? new List<GridPosition>());
            }
        }

        // Held until the presentation has shown the urns
        public override void OnPaid(SpinContext context)
        {
            if (context.Outcome.Treasury != null)
            {
                context.Session.Treasury.Hold(context.Outcome.Treasury);
            }
        }

        // Every key on the key reels, or null unless each key reel shows one
        private static List<GridPosition> Keys(MachineDefinition machine, SymbolGrid grid)
        {
            int key = machine.SymbolIndex(machine.Treasury.KeySymbol);
            if (key < 0)
            {
                return null;
            }

            List<GridPosition> keys = null;
            foreach (int reel in machine.Treasury.KeyReels)
            {
                int found = 0;
                for (int row = 0; row < grid.Rows; row++)
                {
                    if (grid[reel, row] != key)
                    {
                        continue;
                    }

                    keys ??= new List<GridPosition>();
                    keys.Add(new GridPosition(reel, row));
                    found++;
                }

                if (found == 0)
                {
                    return null;
                }
            }

            return keys;
        }
    }
}
