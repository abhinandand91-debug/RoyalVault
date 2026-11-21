namespace RoyalVault.Core
{
    // Decides spins: a random stop on every reel, the grid it shows and everything that grid wins
    public sealed class SpinEngine
    {
        private readonly MachineDefinition machine;
        private readonly IRandom random;
        private readonly WinEvaluator evaluator;

        // Testing aid: when set, the next spin stops here instead of at random, then this is cleared
        public int[] ForcedStops;

        public SpinEngine(MachineDefinition machine, IRandom random, WinEvaluator evaluator = null)
        {
            this.machine = machine;
            this.random = random;
            this.evaluator = evaluator ?? WinEvaluator.Standard();
        }

        public MachineDefinition Machine { get { return machine; } }

        public SpinOutcome Spin(long lineBet)
        {
            int[] stops = new int[machine.Reels];
            for (int reel = 0; reel < machine.Reels; reel++)
                stops[reel] = ForcedStops != null ? ForcedStops[reel] % machine.Strips[reel].Length : random.Next(machine.Strips[reel].Length);
            ForcedStops = null;
            return Evaluate(stops, lineBet);
        }

        // The outcome of given stops, without randomness
        public SpinOutcome Evaluate(int[] stops, long lineBet)
        {
            SymbolGrid grid = SymbolGrid.FromStops(machine, stops);
            var outcome = new SpinOutcome(stops, grid, lineBet, lineBet * machine.Paylines.Length);
            evaluator.Evaluate(machine, outcome);
            return outcome;
        }
    }
}
