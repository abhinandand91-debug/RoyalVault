using System.Collections.Generic;
using System;
using GameKit;
using SlotKit;

namespace RoyalVault.Unity
{
    // A kind of result the showcase can land
    public enum ShowcaseWin
    {
        // A line win below the big win threshold
        LineWin,
        BigWin,
        MegaWin,
        EpicWin,
        // The first jackpot tier (fewest crowns)
        MinorJackpot,
        // The second jackpot tier
        MajorJackpot,
        // The third jackpot tier (most crowns)
        GrandJackpot,
        // Enough seals for free spins
        FreeSpins,
        NoWin,
    }

    // Testing aid: each paid spin lands the next result in the list
    public sealed class WinShowcase
    {
        private const int SearchTries = 600000;

        private readonly SpinEngine engine;
        private readonly GameSession session;
        private readonly MessageToast toast;
        private readonly Func<bool> enabled;
        private readonly Func<ShowcaseWin[]> order;
        private readonly Dictionary<ShowcaseWin, int[]> found = new Dictionary<ShowcaseWin, int[]>();
        private readonly Random random = new Random(20261121);
        private int next;

        // The result forced for the next spin, if any
        public ShowcaseWin? Armed { get; private set; }

        public WinShowcase(SpinEngine spinEngine, GameSession gameSession, SpinFlow flow, MessageToast messageToast,
                           Func<bool> isEnabled, Func<ShowcaseWin[]> winOrder)
        {
            engine = spinEngine;
            session = gameSession;
            toast = messageToast;
            enabled = isEnabled;
            order = winOrder;
            // a rejected spin leaves the forced stops waiting for the next try, so only finished spins move on
            flow.SpinFinished += _ => Arm();
            Arm();
        }

        // Forces the next result in the list onto the next paid spin (nothing during free spins)
        public void Arm()
        {
            // only clear stops the showcase set itself (tests force their own)
            if (Armed != null)
            {
                engine.ForcedStops = null;
            }

            Armed = null;
            ShowcaseWin[] list = order();
            if (!enabled() || list == null || list.Length == 0 || session.FreeSpins.Remaining > 0)
            {
                return;
            }

            for (int attempt = 0; attempt < list.Length; attempt++)
            {
                ShowcaseWin kind = list[next % list.Length];
                next++;
                int[] stops = StopsFor(kind);
                if (stops == null)
                {
                    continue;
                }

                engine.ForcedStops = (int[])stops.Clone();
                Armed = kind;
                if (toast != null)
                {
                    toast.Show("SHOWCASE · NEXT: " + Label(kind), 2.5f);
                }

                return;
            }
        }

        // Reel stops that really produce kind, or null when the machine cannot
        public int[] StopsFor(ShowcaseWin kind)
        {
            if (found.TryGetValue(kind, out int[] cached))
            {
                return cached;
            }

            MachineDefinition machine = engine.Machine;
            int[] stops = new int[machine.Reels];
            int[] result = null;
            for (int i = 0; i < SearchTries && result == null; i++)
            {
                for (int r = 0; r < stops.Length; r++)
                {
                    stops[r] = random.Next(machine.Strips[r].Length);
                }

                if (Matches(kind, engine.Evaluate(stops, 1), machine))
                {
                    result = (int[])stops.Clone();
                }
            }
            found[kind] = result;
            return result;
        }

        // Whether an outcome (before features) is the given kind of result
        public static bool Matches(ShowcaseWin kind, SpinOutcome outcome, MachineDefinition machine)
        {
            bool freeSpins = outcome.FreeSpinsAwarded > 0;
            int jackpot = outcome.JackpotTier;
            long win = outcome.TotalWin;
            long bet = Math.Max(1, outcome.TotalBet);
            int[] tiers = machine.WinTierMultipliers;
            long big = tiers.Length > 0 ? tiers[0] * bet : long.MaxValue;
            long mega = tiers.Length > 1 ? tiers[1] * bet : long.MaxValue;
            long epic = tiers.Length > 2 ? tiers[2] * bet : long.MaxValue;
            bool plain = jackpot < 0 && !freeSpins;

            switch (kind)
            {
                case ShowcaseWin.NoWin:
                    return plain && win == 0;
                case ShowcaseWin.LineWin:
                    return plain && outcome.Scatter == null && win > 0 && win < big;
                case ShowcaseWin.BigWin:
                    return plain && win >= big && win < mega;
                case ShowcaseWin.MegaWin:
                    return plain && win >= mega && win < epic;
                case ShowcaseWin.EpicWin:
                    return plain && win >= epic;
                case ShowcaseWin.MinorJackpot:
                    return !freeSpins && jackpot == 0;
                case ShowcaseWin.MajorJackpot:
                    return !freeSpins && jackpot == 1 && machine.JackpotTiers.Length > 1;
                case ShowcaseWin.GrandJackpot:
                    return !freeSpins && jackpot == 2 && machine.JackpotTiers.Length > 2;
                case ShowcaseWin.FreeSpins:
                    return freeSpins && jackpot < 0;
                default:
                    return false;
            }
        }

        private string Label(ShowcaseWin kind)
        {
            JackpotTierDefinition[] tiers = engine.Machine.JackpotTiers;
            switch (kind)
            {
                case ShowcaseWin.LineWin:
                    return "LINE WIN";
                case ShowcaseWin.BigWin:
                    return "BIG WIN";
                case ShowcaseWin.MegaWin:
                    return "MEGA WIN";
                case ShowcaseWin.EpicWin:
                    return "EPIC WIN";
                case ShowcaseWin.MinorJackpot:
                    return (tiers.Length > 0 ? tiers[0].Name : "MINOR") + " JACKPOT";
                case ShowcaseWin.MajorJackpot:
                    return (tiers.Length > 1 ? tiers[1].Name : "MAJOR") + " JACKPOT";
                case ShowcaseWin.GrandJackpot:
                    return (tiers.Length > 2 ? tiers[2].Name : "GRAND") + " JACKPOT";
                case ShowcaseWin.FreeSpins:
                    return "FREE SPINS";
                default:
                    return "NO WIN";
            }
        }
    }
}
