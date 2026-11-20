using System;
using RoyalVault.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalVault.Unity
{
    /// <summary>
    /// The control console: balance and win readouts, the bet stepper, auto play, spin, menu and sound. It only shows
    /// state and reports what the player pressed; the round controller decides what happens.
    /// </summary>
    public sealed class ConsoleView : MonoBehaviour
    {
        [SerializeField] private CountingText balance;
        [SerializeField] private TextMeshProUGUI winLabel;
        [SerializeField] private CountingText win;
        [SerializeField] private TextMeshProUGUI totalBet;
        [SerializeField] private TextMeshProUGUI autoLabel;
        [SerializeField] private SpinButtonView spin;
        [SerializeField] private Button betMinus;
        [SerializeField] private Button betPlus;
        [SerializeField] private Button auto;
        [SerializeField] private Button menu;
        [SerializeField] private Button sound;
        [SerializeField] private Image soundIcon;

        public event Action SpinPressed;
        public event Action BetDownPressed;
        public event Action BetUpPressed;
        public event Action AutoPressed;
        public event Action MenuPressed;
        public event Action SoundPressed;

        public void Setup(CountingText balanceText, TextMeshProUGUI winCaption, CountingText winText, TextMeshProUGUI totalBetText,
                          TextMeshProUGUI autoText, SpinButtonView spinButton, Button minus, Button plus, Button autoButton,
                          Button menuButton, Button soundButton, Image soundImage)
        {
            balance = balanceText;
            winLabel = winCaption;
            win = winText;
            totalBet = totalBetText;
            autoLabel = autoText;
            spin = spinButton;
            betMinus = minus;
            betPlus = plus;
            auto = autoButton;
            menu = menuButton;
            sound = soundButton;
            soundIcon = soundImage;
        }

        private void Awake()
        {
            spin.Pressed += () => SpinPressed?.Invoke();
            betMinus.onClick.AddListener(() => BetDownPressed?.Invoke());
            betPlus.onClick.AddListener(() => BetUpPressed?.Invoke());
            auto.onClick.AddListener(() => AutoPressed?.Invoke());
            menu.onClick.AddListener(() => MenuPressed?.Invoke());
            sound.onClick.AddListener(() => SoundPressed?.Invoke());
        }

        public void SetBalance(long value, float seconds = 0f) { balance.Set(value, seconds); }

        /// <summary>Rolls the win readout up from 0.</summary>
        public void ShowWin(long value, float seconds) { win.Roll(0, value, seconds); }

        public void ClearWin() { win.Set(0); }

        public void SetBet(BetModel bet)
        {
            totalBet.text = NumberFormat.Credits(bet.TotalBet);
            betMinus.interactable = !bet.IsMin;
            betPlus.interactable = !bet.IsMax;
        }

        /// <summary>During free spins the WIN caption shows the spins played and the multiplier.</summary>
        public void SetFreeSpins(FreeSpinState state, int multiplier)
        {
            winLabel.text = state.Active ? $"FREE SPIN {Mathf.Max(1, state.Played)} / {state.Total}   ×{multiplier}" : "WIN";
        }

        public void SetAuto(bool active, int remaining)
        {
            autoLabel.text = active ? "STOP  " + remaining : "AUTO";
        }

        /// <summary>Which controls the player may use right now.</summary>
        public void SetControls(bool betting, bool spinning, BetModel bet)
        {
            betMinus.interactable = betting && !bet.IsMin;
            betPlus.interactable = betting && !bet.IsMax;
            menu.interactable = betting;
            spin.SetSpinning(spinning);
        }

        public void SetSoundOn(bool on)
        {
            if (soundIcon != null) soundIcon.color = new Color(1f, 1f, 1f, on ? 1f : 0.35f);
        }
    }
}
