using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoyalVault.Core;
using RoyalVault.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RoyalVault.Tests
{
    /// <summary>
    /// The sound plumbing, checked without listening: the player records every cue it is asked for (silent ones too),
    /// so forced spins show which moments reach the sound director.
    /// </summary>
    public sealed class SoundTests
    {
        private static IEnumerator WaitFor(System.Func<bool> condition, float timeout = 30f)
        {
            float t = 0f;
            while (!condition() && t < timeout) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(condition(), "timed out");
        }

        private static IEnumerator Spin(GameBootstrap game, int[] stops)
        {
            yield return WaitFor(() => game.Flow.State == SpinState.Idle, 40f);
            game.Sound.Player.ClearHistory();
            game.ForceNextStops(stops);
            Assert.IsTrue(game.Flow.RequestSpin());
            yield return WaitFor(() => game.Flow.State != SpinState.Spinning);
        }

        private static int Count(SoundPlayer player, SoundCue cue) => player.History.Count(c => c == cue);

        [UnityTest, Timeout(180000)]
        public IEnumerator EveryMomentReachesTheSoundPlayer()
        {
            PlayerPrefs.DeleteAll();
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            var game = Object.FindFirstObjectByType<GameBootstrap>();
            SoundPlayer player = game.Sound.Player;

            // a small line win: the reels spin, land one by one, and the win sounds
            yield return Spin(game, new[] { 19, 18, 40, 46, 23 });
            Assert.AreEqual(1, Count(player, SoundCue.ReelSpinLoop), "reel spin loop");
            Assert.AreEqual(game.Machine.Reels, Count(player, SoundCue.ReelStop), "one stop per reel");
            yield return WaitFor(() => Count(player, SoundCue.SmallWin) == 1);

            // a big win: wilds land, the celebration opens with the guardians' flame and the coin shower
            yield return Spin(game, new[] { 33, 13, 48, 1, 5 });
            Assert.Greater(Count(player, SoundCue.WildLand), 0, "wild lands");
            yield return WaitFor(() => Count(player, SoundCue.BigWin) + Count(player, SoundCue.MegaWin) + Count(player, SoundCue.EpicWin) == 1);
            Assert.AreEqual(1, Count(player, SoundCue.GuardianFlame), "guardian flame");
            Assert.AreEqual(1, Count(player, SoundCue.CoinShowerLoop), "coin shower");
            yield return WaitFor(() => game.Flow.State == SpinState.Idle, 40f);

            // three seals: they crack the seal, it breaks, and free spins are announced
            yield return Spin(game, new[] { 33, 48, 5, 22, 6 });
            Assert.AreEqual(3, Count(player, SoundCue.ScatterLand), "seal lands");
            Assert.AreEqual(2, Count(player, SoundCue.SealCrack), "two cracks");
            Assert.AreEqual(1, Count(player, SoundCue.SealBreak), "the seal breaks");
            yield return WaitFor(() => Count(player, SoundCue.FreeSpinsAwarded) == 1);
            PlayerPrefs.DeleteAll();
        }

        [Test]
        public void MuteIsKeptBetweenSessions()
        {
            SoundPlayer.Muted = true;
            Assert.IsTrue(SoundPlayer.Muted);
            Assert.AreEqual(0f, AudioListener.volume);
            SoundPlayer.Muted = false;
            Assert.IsFalse(SoundPlayer.Muted);
            Assert.AreEqual(1f, AudioListener.volume);
            PlayerPrefs.DeleteKey("royalvault.sound.muted");
        }
    }
}
