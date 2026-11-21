using RoyalVault.Core;
using UnityEngine;

namespace RoyalVault.Unity
{
    /// <summary>
    /// Decides what the game sounds like. It listens to what already happens (the console's buttons, the spin flow,
    /// each reel landing, the seal cracking, celebrations opening and closing, free spins starting and ending) and asks
    /// the <see cref="SoundPlayer"/> for the matching <see cref="SoundCue"/>. The views know nothing about sound.
    /// It also owns the sound button: a mute that is kept between sessions.
    /// </summary>
    public sealed class SoundDirector : MonoBehaviour
    {
        private SoundPlayer sound;
        private GameSession session;
        private MachineView machine;
        private SpinOutcome outcome;
        private int scatter = -1;
        private int jackpot = -1;
        private int wild = -1;
        private int reels;

        public SoundPlayer Player => sound;

        public void Bind(SoundPlayer player, MachineDefinition definition, GameSession gameSession, SpinFlow flow, MachineView machineView,
                         ConsoleView console, BigWinView bigWin, SealMeterView seal)
        {
            sound = player;
            session = gameSession;
            machine = machineView;
            scatter = definition.FirstIndexOfKind(SymbolKind.Scatter);
            jackpot = definition.FirstIndexOfKind(SymbolKind.Jackpot);
            wild = definition.FirstIndexOfKind(SymbolKind.Wild);
            reels = definition.Reels;

            // the interface
            console.SpinPressed += () => sound.Play(SoundCue.SpinPress);
            console.BetUpPressed += () => sound.Play(SoundCue.BetChange, 1f, 1.06f);
            console.BetDownPressed += () => sound.Play(SoundCue.BetChange, 1f, 0.94f);
            console.AutoPressed += () => sound.Play(SoundCue.ButtonClick);
            console.MenuPressed += () => sound.Play(SoundCue.ButtonClick);
            console.SoundPressed += () =>
            {
                SoundPlayer.Muted = !SoundPlayer.Muted;
                console.SetSoundOn(!SoundPlayer.Muted);
                if (!SoundPlayer.Muted) sound.Play(SoundCue.ButtonClick);
            };
            console.SetSoundOn(!SoundPlayer.Muted);

            // the reels
            flow.SpinStarted += context =>
            {
                outcome = context.Outcome;
                sound.StartLoop(SoundCue.ReelSpinLoop);
            };
            machine.ReelStopped += OnReelStopped;

            // the seal on the vault door
            if (seal != null)
            {
                seal.Cracked += stage => sound.Play(SoundCue.SealCrack, 1f, 1f + 0.08f * (stage - 1));
                seal.Broke += () => sound.Play(SoundCue.SealBreak);
            }

            // wins: small ones as the lines light up, the rest when their celebration opens
            flow.SpinPaid += context =>
            {
                SpinOutcome o = context.Outcome;
                if (o.IsWin && o.Tier < WinTier.Big && o.JackpotTier < 0) sound.Play(SoundCue.SmallWin);
            };
            bigWin.Opened += OnCelebration;
            bigWin.Closed += () => sound.StopLoop(SoundCue.CoinShowerLoop, 0.4f);

            // music: the vault's theme, and the guardians' hall during free spins
            session.FreeSpins.Changed += state => PlayMusic(state.Active);
            PlayMusic(session.FreeSpins.Remaining > 0);
        }

        private void PlayMusic(bool freeSpins)
        {
            SoundBank bank = sound.Bank;
            if (bank == null) return;
            sound.PlayMusic(freeSpins && bank.freeSpinsMusic != null ? bank.freeSpinsMusic : bank.baseGameMusic);
        }

        private void OnReelStopped(int reel)
        {
            if (outcome == null) return;
            // each reel lands a touch higher, so the stops build towards the last one
            sound.Play(SoundCue.ReelStop, 1f, 1f + 0.03f * reel);
            bool seal = false, crown = false, wildLanded = false;
            for (int row = 0; row < outcome.Grid.Rows; row++)
            {
                int symbol = outcome.Grid[reel, row];
                seal |= symbol == scatter;
                crown |= symbol == jackpot;
                wildLanded |= symbol == wild;
            }
            if (seal) sound.Play(SoundCue.ScatterLand);
            if (crown) sound.Play(SoundCue.JackpotLand);
            if (wildLanded) sound.Play(SoundCue.WildLand);
            if (reel == reels - 1) sound.StopLoop(SoundCue.ReelSpinLoop);
        }

        private void OnCelebration(CelebrationKind kind, bool counting)
        {
            switch (kind)
            {
                case CelebrationKind.BigWin: sound.Play(SoundCue.BigWin); break;
                case CelebrationKind.MegaWin: sound.Play(SoundCue.MegaWin); break;
                case CelebrationKind.EpicWin: sound.Play(SoundCue.EpicWin); break;
                case CelebrationKind.Jackpot: sound.Play(SoundCue.JackpotWin); break;
                case CelebrationKind.FreeSpinsAwarded: sound.Play(SoundCue.FreeSpinsAwarded); break;
                case CelebrationKind.FreeSpinsTotal: sound.Play(SoundCue.FreeSpinsTotal); break;
            }
            // the guardians call up their flame for the big moments
            if (kind == CelebrationKind.BigWin || kind == CelebrationKind.MegaWin || kind == CelebrationKind.EpicWin || kind == CelebrationKind.Jackpot)
                sound.Play(SoundCue.GuardianFlame, 0.8f);
            if (counting) sound.StartLoop(SoundCue.CoinShowerLoop);
        }
    }
}
