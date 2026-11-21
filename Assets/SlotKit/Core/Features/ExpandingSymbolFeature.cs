using System.Collections.Generic;

namespace SlotKit
{
    // Free spins with a relic: a regular symbol picked when the round starts covers every reel it appears on and pays on
    // all lines, as long as it shows on enough reels to pay
    public sealed class ExpandingSymbolFeature : SlotFeature
    {
        private readonly IRandom random;
        private readonly List<int> regulars = new List<int>();

        // Testing aid: when 0 or more, the next round's relic is this symbol, then this is cleared
        public int ForcedRelic = -1;

        public ExpandingSymbolFeature(IRandom random)
        {
            this.random = random;
        }

        // After free spins set their multiplier and before win tiers are judged
        public override int Order
        {
            get
            {
                return 5;
            }
        }

        public override void OnOutcome(SpinContext context)
        {
            FreeSpinState state = context.Session.FreeSpins;
            MachineDefinition machine = context.Session.Machine;
            if (!machine.ExpandingFreeSpinSymbol || !context.IsFreeSpin || state.Relic < 0)
            {
                return;
            }

            context.Outcome.Expanded = Expand(machine, context.Outcome, state.Relic);
        }

        // A round starts on a paid spin: its relic is chosen now and kept until the round ends
        public override void OnPaid(SpinContext context)
        {
            FreeSpinState state = context.Session.FreeSpins;
            MachineDefinition machine = context.Session.Machine;
            bool starts = !context.IsFreeSpin && context.Outcome.FreeSpinsAwarded > 0 && state.Relic < 0;
            if (machine.ExpandingFreeSpinSymbol && starts)
            {
                state.SetRelic(PickRelic(machine));
            }
        }

        private int PickRelic(MachineDefinition machine)
        {
            int forced = ForcedRelic;
            ForcedRelic = -1;
            if (forced >= 0)
            {
                return forced;
            }

            regulars.Clear();
            for (int i = 0; i < machine.Symbols.Length; i++)
            {
                if (machine.Symbols[i].Kind == SymbolKind.Regular && InPool(machine, machine.Symbols[i].Id))
                {
                    regulars.Add(i);
                }
            }

            return regulars[random.Next(regulars.Count)];
        }

        private static bool InPool(MachineDefinition machine, string id)
        {
            return machine.ExpandingSymbolPool.Length == 0 || System.Array.IndexOf(machine.ExpandingSymbolPool, id) >= 0;
        }

        // The reels holding the relic; it pays like a line of that many on every payline
        private static ExpandingWin Expand(MachineDefinition machine, SpinOutcome outcome, int relic)
        {
            var reels = new List<int>();
            for (int reel = 0; reel < outcome.Grid.Reels; reel++)
            {
                if (ReelHolds(outcome.Grid, reel, relic))
                {
                    reels.Add(reel);
                }
            }

            int pay = machine.Symbols[relic].PayFor(reels.Count);
            if (pay <= 0)
            {
                return null;
            }

            return new ExpandingWin(relic, reels, (long)pay * outcome.LineBet * machine.Paylines.Length);
        }

        private static bool ReelHolds(SymbolGrid grid, int reel, int symbol)
        {
            for (int row = 0; row < grid.Rows; row++)
            {
                if (grid[reel, row] == symbol)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
