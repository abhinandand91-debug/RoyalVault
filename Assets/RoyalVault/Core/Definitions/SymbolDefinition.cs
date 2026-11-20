namespace RoyalVault.Core
{
    /// <summary>What a symbol does on the reels.</summary>
    public enum SymbolKind
    {
        /// <summary>Pays on a payline, left to right.</summary>
        Regular,
        /// <summary>Stands in for regular symbols on a payline; also pays on its own.</summary>
        Wild,
        /// <summary>Pays and awards free spins wherever it lands.</summary>
        Scatter,
        /// <summary>Wins a progressive jackpot tier when enough land anywhere.</summary>
        Jackpot,
    }

    /// <summary>One symbol of a machine.</summary>
    public sealed class SymbolDefinition
    {
        /// <summary>Stable name, also the key a theme uses to find the symbol's art (for example "ruby").</summary>
        public readonly string Id;
        public readonly SymbolKind Kind;
        /// <summary>Line pay by number of matching symbols: Pays[3] is three of a kind. Multiplies the line bet.</summary>
        public readonly int[] Pays;

        public SymbolDefinition(string id, SymbolKind kind, int[] pays = null)
        {
            Id = id;
            Kind = kind;
            Pays = pays ?? new int[0];
        }

        /// <summary>The line pay for <paramref name="count"/> of this symbol, or 0.</summary>
        public int PayFor(int count)
        {
            return count >= 0 && count < Pays.Length ? Pays[count] : 0;
        }
    }
}
