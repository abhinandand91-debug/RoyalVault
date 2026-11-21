using System;
using GameKit;
using PrimeTween;
using SlotKit;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

namespace RoyalVault.Unity
{
    // The control console: balance and win readouts, the bet stepper, auto play, spin, menu and sound
    public sealed class ConsoleView : MonoBehaviour
    {
        [SerializeField] private CountingText balance;
        [SerializeField] private TextMeshProUGUI winLabel;
        [SerializeField] private CountingText win;
        [SerializeField] private TextMeshProUGUI totalBet;
        [SerializeField] private TextMeshProUGUI autoLabel;
        [SerializeField] private SpinButtonView spin;

        public RectTransform SpinButtonRect => (RectTransform)spin.transform;
        [SerializeField] private Button betMinus;
        [SerializeField] private Button betPlus;
        [SerializeField] private Button auto;
        [SerializeField] private Button menu;
        [SerializeField] private Button sound;
        [SerializeField] private Image soundIcon;
        [Tooltip("Offered after a base-game win: double or nothing on Ruby's Last Hand")]
        [SerializeField] private Button gamble;

        public event Action SpinPressed;
        public event Action BetDownPressed;
        public event Action BetUpPressed;
        public event Action AutoPressed;
        public event Action MenuPressed;
        public event Action SoundPressed;
        public event Action GamblePressed;

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
            if (gamble != null)
            {
                gamble.onClick.AddListener(() => GamblePressed?.Invoke());
                OfferGamble(false);
            }
        }

        public void SetBalance(long value, float seconds = 0f)
        {
            balance.Set(value, seconds);
        }

        // Rolls the win readout up from 0
        public void ShowWin(long value, float seconds)
        {
            win.Roll(0, value, seconds);
        }

        public void ClearWin()
        {
            win.Set(0);
        }

        // Sets the win readout straight to value (after a gamble)
        public void SetWin(long value)
        {
            win.Set(value, 0.4f);
        }

        public void SetupGamble(Button gambleButton)
        {
            gamble = gambleButton;
        }

        public void OfferGamble(bool offered)
        {
            if (gamble == null)
            {
                return;
            }

            gamble.gameObject.SetActive(offered);
            if (offered)
            {
                Tween.Scale(gamble.transform, 0.6f, 1f, 0.35f, Ease.OutBack);
            }
        }

        public void SetBet(BetModel bet)
        {
            totalBet.text = NumberFormat.Credits(bet.TotalBet);
            betMinus.interactable = !bet.IsMin;
            betPlus.interactable = !bet.IsMax;
        }

        // During free spins the WIN caption shows the spins played, and the multiplier when there is one
        public void SetFreeSpins(FreeSpinState state, int multiplier)
        {
            if (!state.Active)
            {
                winLabel.text = "WIN";
                return;
            }

            string spins = "FREE SPIN " + Mathf.Max(1, state.Played) + " / " + state.Total;
            winLabel.text = multiplier > 1 ? spins + "   ×" + multiplier : spins;
        }

        public void SetAuto(bool active, int remaining)
        {
            autoLabel.text = active ? "STOP  " + remaining : "AUTO";
        }

        // Which controls the player may use right now
        public void SetControls(bool betting, bool spinning, BetModel bet)
        {
            betMinus.interactable = betting && !bet.IsMin;
            betPlus.interactable = betting && !bet.IsMax;
            menu.interactable = betting;
            spin.SetSpinning(spinning);
        }

        public void SetSoundOn(bool on)
        {
            if (soundIcon != null)
            {
                soundIcon.color = new Color(1f, 1f, 1f, on ? 1f : 0.35f);
            }
        }
    }
}
