using System;
using System.Collections;
using GameKit;
using PrimeTween;
using SlotKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalVault.Unity
{
    // Ruby's Last Hand: the player calls a colour or a suit, Ruby turns a card, and the win doubles, quadruples or is lost
    public sealed class GambleView : DialogView
    {
        // The parts of one drawn card
        [Serializable]
        public sealed class CardParts
        {
            public RectTransform root;
            public Image face;
            public Image back;
            public Image suit;
            public TextMeshProUGUI rank;
        }

        private static readonly string[] Ranks = { "", "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };
        private static readonly Color Red = new Color(0.72f, 0.08f, 0.12f, 1f);
        private static readonly Color Ink = new Color(0.1f, 0.09f, 0.12f, 1f);

        [SerializeField] private TextMeshProUGUI result;
        [SerializeField] private CardParts card;
        [SerializeField] private CountingText stake;
        [Tooltip("One button per call, in GambleChoice order: red, black, hearts, diamonds, clubs, spades")]
        [SerializeField] private Button[] calls = new Button[6];
        [SerializeField] private Button collect;
        [SerializeField] private CardParts[] history = new CardParts[0];
        [SerializeField] private Image ruby;

        private Sprite[] suits = new Sprite[0];
        private GambleSession session;
        private bool busy;
        private bool faceUp;

        // A card is turned over
        public event Action Flipped;
        // A call is settled: true when it was right
        public event Action<bool> Settled;

        public void Setup(TextMeshProUGUI resultText, CardParts drawn, CountingText stakeText, Button[] callButtons, Button collectButton,
                          CardParts[] previous, Image rubyImage)
        {
            result = resultText;
            card = drawn;
            stake = stakeText;
            calls = callButtons;
            collect = collectButton;
            history = previous;
            ruby = rubyImage;
        }

        public void Bind(ThemeDefinition theme, CharacterAnimation rubyMotion)
        {
            suits = theme.suits;
            bool hasRuby = rubyMotion != null && rubyMotion.IsValid;
            ruby.gameObject.SetActive(hasRuby);
            if (hasRuby)
            {
                ruby.sprite = rubyMotion.idle.frames[0];
            }

            for (int i = 0; i < calls.Length; i++)
            {
                var choice = (GambleChoice)i;
                calls[i].onClick.AddListener(() => Call(choice));
            }

            collect.onClick.AddListener(Collect);
        }

        // Waits its turn with the dialog manager, then runs until the gamble ends; the definition fades it in and out
        public IEnumerator Play(GambleSession gamble)
        {
            session = gamble;
            yield return Present();
        }

        // Calls are played until the session ends; the last card stays up a moment before the table closes
        protected override IEnumerator Content()
        {
            while (session.Active || busy)
            {
                yield return null;
            }

            SetButtons(false);
            yield return Waits.Seconds(1.1f);
        }

        protected override void OnOpening()
        {
            busy = false;
            faceUp = false;
            ShowFace(card, false);
            card.root.localScale = Vector3.one;
            result.text = "CALL THE CARD";
            stake.Set(session.Stake);
            foreach (CardParts previous in history)
            {
                previous.root.gameObject.SetActive(false);
            }

            SetButtons(true);
        }

        private void Call(GambleChoice choice)
        {
            if (!busy && session != null && session.CanCall(choice))
            {
                StartCoroutine(Turn(choice));
            }
        }

        private void Collect()
        {
            if (!busy && session != null)
            {
                session.Collect();
                result.text = "COLLECTED";
            }
        }

        // Turns the last card back, draws, and turns the new one up
        private IEnumerator Turn(GambleChoice choice)
        {
            busy = true;
            SetButtons(false);
            if (faceUp)
            {
                yield return Flip(false, default);
            }

            Card drawn = session.Call(choice);
            yield return Flip(true, drawn);
            bool right = GambleSession.Matches(choice, drawn);
            result.text = right ? "RIGHT! x" + GambleSession.Multiplier(choice) : "THE HOUSE WINS";
            stake.Set(session.Stake, 0.5f);
            Remember();
            Settled?.Invoke(right);
            busy = false;
            SetButtons(session.Active);
        }

        // A card turns by squashing to nothing on its side and opening again
        private IEnumerator Flip(bool up, Card drawn)
        {
            yield return Tween.ScaleX(card.root, 1f, 0f, 0.13f, Ease.InSine).ToYieldInstruction();
            if (up)
            {
                Paint(card, drawn);
            }

            ShowFace(card, up);
            faceUp = up;
            Flipped?.Invoke();
            yield return Tween.ScaleX(card.root, 0f, 1f, 0.16f, Ease.OutBack).ToYieldInstruction();
        }

        private void Paint(CardParts parts, Card drawn)
        {
            Color ink = drawn.IsRed ? Red : Ink;
            parts.suit.sprite = suits[drawn.Suit];
            parts.suit.color = ink;
            parts.rank.text = Ranks[drawn.Rank];
            parts.rank.color = ink;
        }

        private static void ShowFace(CardParts parts, bool up)
        {
            parts.face.enabled = up;
            parts.suit.enabled = up;
            parts.rank.enabled = up;
            parts.back.enabled = !up;
        }

        // The newest card goes first in the row of previous cards
        private void Remember()
        {
            int shown = Mathf.Min(session.History.Count, history.Length);
            for (int i = 0; i < history.Length; i++)
            {
                bool used = i < shown;
                history[i].root.gameObject.SetActive(used);
                if (used)
                {
                    Paint(history[i], session.History[session.History.Count - 1 - i]);
                    ShowFace(history[i], true);
                }
            }

        }

        private void SetButtons(bool live)
        {
            for (int i = 0; i < calls.Length; i++)
            {
                calls[i].interactable = live && session != null && session.CanCall((GambleChoice)i);
            }

            collect.interactable = live;
        }
    }
}
