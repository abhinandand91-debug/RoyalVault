namespace SlotKit
{
    // What a symbol does on the reels
    public enum SymbolKind
    {
        // Pays on a payline, left to right
        Regular,
        // Stands in for regular symbols on a payline; also pays on its own
        Wild,
        // Pays and awards free spins wherever it lands
        Scatter,
        // Wins a progressive jackpot tier when enough land anywhere
        Jackpot,
    }

    // One symbol of a machine
    public sealed class SymbolDefinition
    {
        // Stable name, also the key a theme uses to find the symbol's art (for example "ruby")
        public readonly string Id;
        public readonly SymbolKind Kind;
        // Line pay by number of matching symbols: Pays[3] is three of a kind
        public readonly int[] Pays;

        public SymbolDefinition(string id, SymbolKind kind, int[] pays = null)
        {
            Id = id;
            Kind = kind;
            Pays = pays ?? new int[0];
        }

        // The line pay for count of this symbol, or 0
        public int PayFor(int count)
        {
            return count >= 0 && count < Pays.Length ? Pays[count] : 0;
        }
    }
}
