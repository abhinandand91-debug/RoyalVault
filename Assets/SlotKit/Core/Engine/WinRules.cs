using System.Collections.Generic;

namespace SlotKit
{
    // One way of winning
    public interface IWinRule
    {
        void Evaluate(MachineDefinition machine, SpinOutcome outcome);
    }

    // Left-to-right payline wins, wilds substituting for regular symbols
    public sealed class PaylineRule : IWinRule
    {
        public void Evaluate(MachineDefinition machine, SpinOutcome outcome)
        {
            var ids = new int[machine.Reels];
            for (int line = 0; line < machine.Paylines.Length; line++)
            {
                int[] rows = machine.Paylines[line];
                for (int reel = 0; reel < machine.Reels; reel++)
                {
                    ids[reel] = outcome.Grid[reel, rows[reel]];
                }

                if (!BestRun(machine, ids, out int symbol, out int count, out int pay))
                {
                    continue;
                }

                var positions = new List<GridPosition>(count);
                for (int reel = 0; reel < count; reel++)
                {
                    positions.Add(new GridPosition(reel, rows[reel]));
                }

                outcome.LineWins.Add(new LineWin(line, symbol, count, pay * outcome.LineBet, positions));
            }
        }

        // The best paying run on one line of symbol ids; false when the line pays nothing
        public static bool BestRun(MachineDefinition machine, int[] ids, out int symbol, out int count, out int pay)
        {
            symbol = -1;
            count = 0;
            pay = 0;

            // wilds on their own
            int wilds = 0;
            while (wilds < ids.Length && machine.Symbols[ids[wilds]].Kind == SymbolKind.Wild)
            {
                wilds++;
            }

            if (wilds > 0)
            {
                int wildPay = machine.Symbols[ids[0]].PayFor(wilds);
                if (wildPay > 0)
                {
                    symbol = ids[0];
                    count = wilds;
                    pay = wildPay;
                }
            }

            // the first regular symbol, extended by wilds
            int first = -1;
            foreach (int id in ids)
            {
                if (machine.Symbols[id].Kind != SymbolKind.Wild)
                {
                    first = id;
                    break;
                }
            }

            if (first < 0 || machine.Symbols[first].Kind != SymbolKind.Regular)
            {
                return pay > 0;
            }

            int run = 0;
            while (run < ids.Length && (ids[run] == first || machine.Symbols[ids[run]].Kind == SymbolKind.Wild))
            {
                run++;
            }

            int runPay = machine.Symbols[first].PayFor(run);
            if (runPay > pay)
            {
                symbol = first;
                count = run;
                pay = runPay;
            }
            return pay > 0;
        }
    }

    // Scatter wins: enough scatters anywhere pay the total bet times a multiplier and award free spins
    public sealed class ScatterRule : IWinRule
    {
        public void Evaluate(MachineDefinition machine, SpinOutcome outcome)
        {
            int scatter = machine.FirstIndexOfKind(SymbolKind.Scatter);
            if (scatter < 0)
            {
                return;
            }

            List<GridPosition> positions = outcome.Grid.PositionsOf(scatter);

            ScatterAward best = null;
            foreach (ScatterAward award in machine.ScatterAwards)
            {
                if (positions.Count >= award.Count && (best == null || award.Count > best.Count))
                {
                    best = award;
                }
            }

            if (best != null)
            {
                outcome.Scatter = new ScatterWin(positions.Count, best.TotalBetMultiplier * outcome.TotalBet, best.FreeSpins, positions);
            }
        }
    }

    // Jackpot wins: the highest tier whose symbol count is reached by jackpot symbols anywhere
    public sealed class JackpotRule : IWinRule
    {
        public void Evaluate(MachineDefinition machine, SpinOutcome outcome)
        {
            int jackpot = machine.FirstIndexOfKind(SymbolKind.Jackpot);
            if (jackpot < 0)
            {
                return;
            }

            List<GridPosition> positions = outcome.Grid.PositionsOf(jackpot);

            for (int tier = machine.JackpotTiers.Length - 1; tier >= 0; tier--)
            {
                if (positions.Count < machine.JackpotTiers[tier].SymbolCount)
                {
                    continue;
                }

                outcome.JackpotTier = tier;
                outcome.JackpotPositions = positions;
                return;
            }
        }
    }

    // Runs the win rules in order; add a rule here to add a new way of winning
    public sealed class WinEvaluator
    {
        private readonly List<IWinRule> rules;

        public WinEvaluator(IEnumerable<IWinRule> rules)
        {
            this.rules = new List<IWinRule>(rules);
        }

        // Paylines, scatter and jackpot: the rules every machine in this game uses
        public static WinEvaluator Standard()
        {
            return new WinEvaluator(new IWinRule[] { new PaylineRule(), new ScatterRule(), new JackpotRule() });
        }

        public void Evaluate(MachineDefinition machine, SpinOutcome outcome)
        {
            foreach (IWinRule rule in rules)
            {
                rule.Evaluate(machine, outcome);
            }
        }
    }
}
