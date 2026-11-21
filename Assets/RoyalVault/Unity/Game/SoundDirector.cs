using RoyalVault.Core;
using UnityEngine;

namespace RoyalVault.Unity
{
    // Decides what the game sounds like
    public sealed class SoundDirector : MonoBehaviour
    {
        private SoundPlayer sound;
        private GameSession session;
        private MachineView machine;
        private SpinOutcome outcome;
        private int scatter = -1;
        private int jackpot = -1;
        private int wild = -1;
        private int landed;
        private int lastStopFrame = -1;

        public SoundPlayer Player => sound;

        public void Bind(SoundPlayer player, MachineDefinition definition, GameSession gameSession, SpinFlow flow, MachineView machineView,
                         ConsoleView console, BigWinView bigWin, SealMeterView seal, FlameClashView clash = null)
        {
            sound = player;
            session = gameSession;
            machine = machineView;
            scatter = definition.FirstIndexOfKind(SymbolKind.Scatter);
            jackpot = definition.FirstIndexOfKind(SymbolKind.Jackpot);
            wild = definition.FirstIndexOfKind(SymbolKind.Wild);

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
                landed = 0;
                sound.StartLoop(SoundCue.ReelSpinLoop);
            };
            machine.ReelStopped += OnReelStopped;
            machine.AllReelsStopped += () => sound.StopLoop(SoundCue.ReelSpinLoop);
            // the long wait on the last reels: a rising loop, and its tail when the last one lands
            machine.AnticipationStarted += reel => sound.StartLoop(SoundCue.AnticipationLoop);
            machine.AnticipationEnded += () =>
            {
                sound.StopLoop(SoundCue.AnticipationLoop, 0.12f);
                sound.Play(SoundCue.AnticipationEnd);
            };

            // the guardians' flames: the throw, and the burst where they meet
            if (clash != null)
            {
                clash.Thrown += () => sound.Play(SoundCue.FlameThrow);
                clash.Clashed += () => sound.Play(SoundCue.FlameClash);
            }

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
            // each stop a touch higher; reels landing in the same frame make one sound
            if (Time.frameCount != lastStopFrame) sound.Play(SoundCue.ReelStop, 1f, 1f + 0.03f * landed);
            lastStopFrame = Time.frameCount;
            landed++;
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
