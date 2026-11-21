using System.Collections.Generic;

namespace SlotKit
{
    // Now and then turns a losing base-game spin's regular symbols into wilds, then evaluates the grid again
    public sealed class RandomWildsFeature : SlotFeature
    {
        private readonly IRandom random;
        private readonly WinEvaluator evaluator;
        private readonly List<GridPosition> candidates = new List<GridPosition>();

        // Testing aid: when set, the next losing base-game spin gets wilds whatever the chance, then this is cleared
        public bool ForceNext;

        public RandomWildsFeature(IRandom random, WinEvaluator evaluator = null)
        {
            this.random = random;
            this.evaluator = evaluator ?? WinEvaluator.Standard();
        }

        // Before every other feature, so multipliers, jackpots and win tiers see the final grid
        public override int Order
        {
            get
            {
                return -100;
            }
        }

        public override void OnOutcome(SpinContext context)
        {
            MachineDefinition machine = context.Session.Machine;
            RandomWildSettings settings = machine.RandomWilds;
            SpinOutcome outcome = context.Outcome;
            int wild = machine.FirstIndexOfKind(SymbolKind.Wild);
            if (context.IsFreeSpin || outcome.IsWin || wild < 0 || settings == null || (settings.ChancePerMille <= 0 && !ForceNext))
            {
                return;
            }

            bool forced = ForceNext;
            ForceNext = false;
            if (!forced && random.Next(1000) >= settings.ChancePerMille)
            {
                return;
            }

            AddWilds(machine, outcome, wild, settings);
            outcome.ClearWins();
            evaluator.Evaluate(machine, outcome);
        }

        private void AddWilds(MachineDefinition machine, SpinOutcome outcome, int wild, RandomWildSettings settings)
        {
            FindCandidates(machine, outcome.Grid);
            int count = settings.MinWilds + random.Next(settings.MaxWilds - settings.MinWilds + 1);
            for (int i = 0; i < count && candidates.Count > 0; i++)
            {
                int pick = random.Next(candidates.Count);
                GridPosition position = candidates[pick];
                candidates.RemoveAt(pick);
                outcome.Grid[position.Reel, position.Row] = wild;
                outcome.AddedWilds.Add(position);
            }
        }

        // Only regular symbols burn: wilds, scatters and jackpot symbols stay as they are
        private void FindCandidates(MachineDefinition machine, SymbolGrid grid)
        {
            candidates.Clear();
            for (int reel = 0; reel < grid.Reels; reel++)
            {
                for (int row = 0; row < grid.Rows; row++)
                {
                    if (machine.Symbols[grid[reel, row]].Kind == SymbolKind.Regular)
                    {
                        candidates.Add(new GridPosition(reel, row));
                    }
                }
            }
        }
    }
}
