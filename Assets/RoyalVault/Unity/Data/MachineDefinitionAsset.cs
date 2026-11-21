using System.Collections.Generic;
using System;
using SlotKit;
using UnityEngine;

namespace RoyalVault.Unity
{
    // A slot machine as an editable asset
    [CreateAssetMenu(menuName = "Royal Vault/Machine Definition", fileName = "Machine")]
    public sealed class MachineDefinitionAsset : ScriptableObject
    {
        [Serializable]
        public sealed class SymbolEntry
        {
            [Tooltip("Stable id; the theme finds the symbol's art by it")] public string id;
            public SymbolKind kind;
            [Tooltip("Line pay by count: pays[3] is three of a kind, as a multiple of the line bet")] public int[] pays = new int[0];
        }

        [Serializable]
        public sealed class IntRow
        {
            public int[] values = new int[0];
        }

        [Serializable]
        public sealed class ScatterEntry
        {
            public int count = 3;
            [Tooltip("Pays the total bet times this")] public int totalBetMultiplier = 1;
            public int freeSpins;
        }

        [Serializable]
        public sealed class JackpotEntry
        {
            public string name = "MINOR";
            [Tooltip("Jackpot symbols anywhere needed to win this tier")] public int symbolCount = 3;
            public long seed = 500;
            [Tooltip("Percent of every paid bet added to this tier")] public double contributionPercent = 0.5;
        }

        [Header("Machine")]
        public string machineName = "Machine";
        [Min(1)] public int reels = 5;
        [Min(1)] public int rows = 3;

        [Header("Symbols and reels")]
        public SymbolEntry[] symbols = new SymbolEntry[0];
        [Tooltip("One strip of symbol indices per reel")] public IntRow[] strips = new IntRow[0];
        [Tooltip("For each payline, the row (0 = top) it crosses on each reel")] public IntRow[] paylines = new IntRow[0];

        [Header("Awards")]
        public ScatterEntry[] scatterAwards = new ScatterEntry[0];
        public JackpotEntry[] jackpotTiers = new JackpotEntry[0];
        [Min(1)] public int freeSpinMultiplier = 2;
        [Tooltip("Win tiers as multiples of the total bet: Big, Mega, Epic")] public int[] winTierMultipliers = { 10, 25, 50 };

        [Header("Random wilds (Guardian's Fire)")]
        [Tooltip("Losing base-game spins rescued with extra wilds, per thousand; 0 turns the feature off")]
        [Range(0, 1000)] public int randomWildChancePerMille;
        [Min(1)] public int randomWildMin = 2;
        [Min(1)] public int randomWildMax = 3;
        [Tooltip("Version of the presets this asset was last brought up to; the builder adds new settings once")]
        [HideInInspector] public int presetVersion;

        [Header("Betting")]
        public int[] lineBets = { 1 };
        public int defaultLineBetIndex;
        public long startBalance = 50000;

        // The Core definition this asset describes
        public MachineDefinition ToDefinition()
        {
            var definition = new MachineDefinition
            {
                Name = machineName,
                Reels = reels,
                Rows = rows,
                Symbols = new SymbolDefinition[symbols.Length],
                Strips = Rows(strips),
                Paylines = Rows(paylines),
                ScatterAwards = new ScatterAward[scatterAwards.Length],
                JackpotTiers = new JackpotTierDefinition[jackpotTiers.Length],
                LineBets = (int[])lineBets.Clone(),
                DefaultLineBetIndex = defaultLineBetIndex,
                StartBalance = startBalance,
                FreeSpinMultiplier = freeSpinMultiplier,
                WinTierMultipliers = (int[])winTierMultipliers.Clone(),
                RandomWilds = new RandomWildSettings { ChancePerMille = randomWildChancePerMille, MinWilds = randomWildMin, MaxWilds = randomWildMax },
            };
            for (int i = 0; i < symbols.Length; i++)
            {
                definition.Symbols[i] = new SymbolDefinition(symbols[i].id, symbols[i].kind, (int[])symbols[i].pays.Clone());
            }

            for (int i = 0; i < scatterAwards.Length; i++)
            {
                definition.ScatterAwards[i] = new ScatterAward(scatterAwards[i].count, scatterAwards[i].totalBetMultiplier, scatterAwards[i].freeSpins);
            }

            for (int i = 0; i < jackpotTiers.Length; i++)
            {
                JackpotEntry j = jackpotTiers[i];
                definition.JackpotTiers[i] = new JackpotTierDefinition(j.name, j.symbolCount, j.seed, j.contributionPercent);
            }
            return definition;
        }

        // Fills this asset from a Core definition (used to create the shipped presets)
        public void CopyFrom(MachineDefinition definition)
        {
            machineName = definition.Name;
            reels = definition.Reels;
            rows = definition.Rows;
            symbols = new SymbolEntry[definition.Symbols.Length];
            for (int i = 0; i < symbols.Length; i++)
            {
                SymbolDefinition s = definition.Symbols[i];
                symbols[i] = new SymbolEntry { id = s.Id, kind = s.Kind, pays = (int[])s.Pays.Clone() };
            }
            strips = Entries(definition.Strips);
            paylines = Entries(definition.Paylines);
            scatterAwards = new ScatterEntry[definition.ScatterAwards.Length];
            for (int i = 0; i < scatterAwards.Length; i++)
            {
                ScatterAward a = definition.ScatterAwards[i];
                scatterAwards[i] = new ScatterEntry { count = a.Count, totalBetMultiplier = a.TotalBetMultiplier, freeSpins = a.FreeSpins };
            }
            jackpotTiers = new JackpotEntry[definition.JackpotTiers.Length];
            for (int i = 0; i < jackpotTiers.Length; i++)
            {
                JackpotTierDefinition t = definition.JackpotTiers[i];
                jackpotTiers[i] = new JackpotEntry { name = t.Name, symbolCount = t.SymbolCount, seed = t.Seed, contributionPercent = t.ContributionPercent };
            }
            lineBets = (int[])definition.LineBets.Clone();
            defaultLineBetIndex = definition.DefaultLineBetIndex;
            startBalance = definition.StartBalance;
            freeSpinMultiplier = definition.FreeSpinMultiplier;
            winTierMultipliers = (int[])definition.WinTierMultipliers.Clone();
            ApplyPresetChanges(definition);
        }

        // Brings an asset made from an older preset up to date once: 1 added random wilds, 2 rebalanced line pays for them
        public void ApplyPresetChanges(MachineDefinition definition)
        {
            randomWildChancePerMille = definition.RandomWilds.ChancePerMille;
            randomWildMin = definition.RandomWilds.MinWilds;
            randomWildMax = definition.RandomWilds.MaxWilds;
            for (int i = 0; i < symbols.Length && i < definition.Symbols.Length; i++)
            {
                if (symbols[i].id == definition.Symbols[i].Id)
                {
                    symbols[i].pays = (int[])definition.Symbols[i].Pays.Clone();
                }
            }

            presetVersion = CurrentPresetVersion;
        }

        public const int CurrentPresetVersion = 2;

        // Problems with this machine; empty when it is ready to play
        public List<string> Validate()
        {
            return ToDefinition().Validate();
        }

        private static int[][] Rows(IntRow[] rowsIn)
        {
            var result = new int[rowsIn.Length][];
            for (int i = 0; i < rowsIn.Length; i++)
            {
                result[i] = (int[])(rowsIn[i]?.values ?? new int[0]).Clone();
            }

            return result;
        }

        private static IntRow[] Entries(int[][] rowsIn)
        {
            var result = new IntRow[rowsIn.Length];
            for (int i = 0; i < rowsIn.Length; i++)
            {
                result[i] = new IntRow { values = (int[])rowsIn[i].Clone() };
            }

            return result;
        }
    }
}
