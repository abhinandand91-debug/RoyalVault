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
    // The Treasury: twelve sealed urns; each one the player opens shows the next prize SlotKit drew, until the guardian's
    // eye ends the round. The view never decides anything, it only shows the round
    public sealed class TreasuryView : MonoBehaviour
    {
        // The parts of one urn
        [Serializable]
        public sealed class UrnParts
        {
            public RectTransform root;
            public Button button;
            public Image body;
            public Image crack;
            public Image glow;
            public Image icon;
            public TextMeshProUGUI label;
        }

        private static readonly Color EyeRed = new Color(1f, 0.22f, 0.18f, 1f);

        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform board;
        [SerializeField] private TextMeshProUGUI prompt;
        [SerializeField] private CountingText collected;
        [SerializeField] private TextMeshProUGUI multiplier;
        [SerializeField] private Image shield;
        [SerializeField] private UrnParts[] urns = new UrnParts[0];
        [SerializeField] private UiParticleEmitter burst;
        [SerializeField] private Image flash;
        [SerializeField] private Button tapArea;
        [Tooltip("Cracks spreading over an urn, smallest first")]
        [SerializeField] private Sprite[] cracks = new Sprite[0];
        [Tooltip("Seconds without a tap before an urn is opened for the player")]
        [SerializeField, Range(2f, 20f)] private float idleSeconds = 8f;
        [Tooltip("Seconds between picks when the game opens the urns itself (auto play)")]
        [SerializeField, Range(0.2f, 2f)] private float autoPickSeconds = 0.7f;
        [Tooltip("Optional vault door over the board that turns and opens as the round begins")]
        [SerializeField] private Image door;

        private Tween[] pulses = new Tween[0];
        private long totalBet;
        private Sprite eye;
        private Sprite ankh;
        private bool[] opened = new bool[0];
        private int tapped = -1;
        private bool skipped;

        // An urn starts to crack
        public event Action Cracked;
        // An urn shows its prize; true when an eye got through and the round ends
        public event Action<UrnPrize, bool> Revealed;
        // The round's total is shown
        public event Action<long> Totalled;
        // The vault door turns open
        public event Action DoorOpened;

        public bool IsOpen { get; private set; }

        public void Setup(CanvasGroup canvasGroup, RectTransform boardRect, TextMeshProUGUI promptText, CountingText collectedText,
                          TextMeshProUGUI multiplierText, Image shieldIcon, UrnParts[] urnParts, UiParticleEmitter burstEmitter, Image flashImage,
                          Button tap, Sprite[] crackSprites)
        {
            group = canvasGroup;
            board = boardRect;
            prompt = promptText;
            collected = collectedText;
            multiplier = multiplierText;
            shield = shieldIcon;
            urns = urnParts;
            burst = burstEmitter;
            flash = flashImage;
            tapArea = tap;
            cracks = crackSprites;
        }

        public void SetupDoor(Image vaultDoor)
        {
            door = vaultDoor;
        }

        public void Bind(ThemeDefinition theme)
        {
            eye = theme.eye;
            ankh = theme.Symbol("ankh")?.sprite;
            shield.sprite = ankh;
            opened = new bool[urns.Length];
            pulses = new Tween[urns.Length];
            for (int i = 0; i < urns.Length; i++)
            {
                int index = i;
                urns[i].button.onClick.AddListener(() => Tap(index));
            }

            tapArea.onClick.AddListener(() => skipped = true);
            Hide();
        }

        // Runs the whole round; automatic opens the urns without waiting for taps
        public IEnumerator Play(TreasuryRound round, bool automatic)
        {
            totalBet = round.TotalBet;
            Open();
            yield return Tween.Alpha(group, 0f, 1f, 0.35f, Ease.OutCubic).ToYieldInstruction();
            if (door != null)
            {
                yield return OpenDoor();
            }

            for (int pick = 0; pick < round.Picks; pick++)
            {
                yield return WaitForPick(automatic);
                SetButtons(false);
                Urn prize = round.Order[pick];
                bool ends = prize.Prize == UrnPrize.Eye && round.ShieldsAfter(pick) == 0;
                yield return Break(tapped, prize, ends);
                ShowProgress(round, pick + 1);
            }

            yield return Tween.Delay(0.6f).ToYieldInstruction();
            ShowUnopened(round);
            yield return Total(round);
            yield return Hold(automatic ? 1.6f : 3.5f);
            yield return Tween.Alpha(group, 1f, 0f, 0.35f, Ease.InOutSine).ToYieldInstruction();
            Hide();
        }

        // The door turns a third of a turn, swings towards the player and fades, showing the urns behind it
        private IEnumerator OpenDoor()
        {
            yield return Tween.Delay(0.45f).ToYieldInstruction();
            DoorOpened?.Invoke();
            Tween.LocalEulerAngles(door.rectTransform, Vector3.zero, new Vector3(0f, 0f, -120f), 0.9f, Ease.InCubic);
            Tween.Scale(door.rectTransform, 1f, 2.3f, 0.9f, Ease.InCubic);
            yield return Tween.Alpha(door, 1f, 0f, 0.9f, Ease.InCubic).ToYieldInstruction();
            door.gameObject.SetActive(false);
        }

        private void Open()
        {
            IsOpen = true;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            group.blocksRaycasts = true;
            tapArea.gameObject.SetActive(false);
            prompt.text = "PICK AN URN";
            prompt.color = Color.white;
            collected.Set(0);
            multiplier.text = "x1";
            shield.enabled = false;
            if (door != null)
            {
                door.gameObject.SetActive(true);
                door.rectTransform.localRotation = Quaternion.identity;
                door.rectTransform.localScale = Vector3.one;
                door.color = Color.white;
            }

            flash.color = new Color(EyeRed.r, EyeRed.g, EyeRed.b, 0f);
            for (int i = 0; i < urns.Length; i++)
            {
                ResetUrn(urns[i], i);
            }
        }

        private void ResetUrn(UrnParts urn, int index)
        {
            opened[index] = false;
            urn.body.enabled = true;
            urn.body.color = Color.white;
            urn.crack.enabled = false;
            urn.icon.enabled = false;
            urn.label.text = "";
            urn.label.alpha = 1f;
            urn.root.localScale = Vector3.one;
            urn.root.localRotation = Quaternion.identity;
            Tween.Scale(urn.root, 0.3f, 1f, 0.4f, Ease.OutBack, startDelay: 0.25f + index * 0.04f);
            pulses[index].Stop();
            pulses[index] = Tween.Alpha(urn.glow, 0.15f, 0.55f, 1.1f, Ease.InOutSine, cycles: -1, cycleMode: CycleMode.Yoyo,
                                        startDelay: index * 0.09f);
        }

        // Waits for a tap, opening an urn for the player after a while (or straight away in auto play)
        private IEnumerator WaitForPick(bool automatic)
        {
            tapped = -1;
            SetButtons(true);
            float limit = automatic ? autoPickSeconds : idleSeconds;
            for (float waited = 0f; tapped < 0 && waited < limit; waited += Time.deltaTime)
            {
                yield return null;
            }

            if (tapped < 0)
            {
                tapped = RandomClosedUrn();
            }
        }

        // The first tap wins; the board locks at once so a second tap in the same frame cannot replace it
        private void Tap(int index)
        {
            if (tapped >= 0 || opened[index])
            {
                return;
            }

            tapped = index;
            SetButtons(false);
        }

        private int RandomClosedUrn()
        {
            int closed = 0;
            foreach (bool isOpen in opened)
            {
                closed += isOpen ? 0 : 1;
            }

            int pick = UnityEngine.Random.Range(0, closed);
            for (int i = 0; i < opened.Length; i++)
            {
                if (opened[i])
                {
                    continue;
                }

                if (pick == 0)
                {
                    return i;
                }

                pick--;
            }

            return 0;
        }

        private void SetButtons(bool live)
        {
            for (int i = 0; i < urns.Length; i++)
            {
                urns[i].button.interactable = live && !opened[i];
            }
        }

        // The urn shakes, cracks and bursts, and its prize rises out of the dust
        private IEnumerator Break(int index, Urn prize, bool ends)
        {
            UrnParts urn = urns[index];
            opened[index] = true;
            pulses[index].Stop();
            urn.glow.color = new Color(1f, 0.85f, 0.5f, 0.9f);
            Cracked?.Invoke();
            Tween.ShakeLocalRotation(urn.root, new Vector3(0f, 0f, 9f), 0.32f, 18);
            urn.crack.enabled = true;
            foreach (Sprite crack in cracks)
            {
                urn.crack.sprite = crack;
                yield return Tween.Delay(0.09f).ToYieldInstruction();
            }

            urn.crack.enabled = false;
            Tween.Scale(urn.body.transform, 1f, 1.3f, 0.18f, Ease.OutCubic);
            yield return Tween.Alpha(urn.body, 1f, 0f, 0.18f, Ease.OutCubic).ToYieldInstruction();
            urn.body.enabled = false;
            urn.body.transform.localScale = Vector3.one;
            burst.Origin = urn.root.anchoredPosition;
            burst.Burst(prize.Prize == UrnPrize.Eye ? 26 : 40, 2.2f);
            Paint(urn, prize, 1f);
            Tween.Scale(urn.icon.transform, 0.2f, 1f, 0.35f, Ease.OutBack);
            Tween.Scale(urn.label.transform, 0.2f, 1f, 0.35f, Ease.OutBack);
            Tween.Alpha(urn.glow, 0.9f, 0.2f, 0.6f, Ease.OutCubic);
            Revealed?.Invoke(prize.Prize, ends);
            if (prize.Prize == UrnPrize.Eye)
            {
                yield return EyeFound(ends);
            }
            else
            {
                prompt.text = Caption(prize);
                yield return Tween.Delay(0.35f).ToYieldInstruction();
            }
        }

        private IEnumerator EyeFound(bool ends)
        {
            prompt.text = ends ? "THE GUARDIAN'S EYE" : "THE ANKH SHIELDS YOU";
            prompt.color = ends ? EyeRed : Color.white;
            if (ends)
            {
                Tween.ShakeLocalPosition(board, new Vector3(18f, 10f, 0f), 0.45f, 22);
                yield return Tween.Alpha(flash, 0f, 0.45f, 0.08f, Ease.OutCubic).ToYieldInstruction();
                yield return Tween.Alpha(flash, 0.45f, 0f, 0.5f, Ease.InOutSine).ToYieldInstruction();
            }
            else
            {
                Tween.Scale(shield.transform, 1f, 1.35f, 0.25f, Ease.OutCubic, cycles: 2, cycleMode: CycleMode.Yoyo);
                yield return Tween.Delay(0.55f).ToYieldInstruction();
            }
        }

        private static string Caption(Urn prize)
        {
            switch (prize.Prize)
            {
                case UrnPrize.Multiplier:
                    return "THE TREASURE GROWS";
                case UrnPrize.Shield:
                    return "AN ANKH WILL SHIELD YOU FROM ONE EYE";
                default:
                    return "PICK AGAIN";
            }
        }

        // An urn's prize: credits as a number, a multiplier, the ankh, or the eye
        private void Paint(UrnParts urn, Urn prize, float alpha)
        {
            urn.icon.enabled = prize.Prize == UrnPrize.Eye || prize.Prize == UrnPrize.Shield;
            urn.icon.sprite = prize.Prize == UrnPrize.Eye ? eye : ankh;
            urn.icon.color = prize.Prize == UrnPrize.Eye ? new Color(EyeRed.r, EyeRed.g, EyeRed.b, alpha) : new Color(1f, 1f, 1f, alpha);
            urn.label.alpha = alpha;
            switch (prize.Prize)
            {
                case UrnPrize.Credits:
                    urn.label.text = NumberFormat.Credits(prize.TotalBets * totalBet);
                    break;
                case UrnPrize.Multiplier:
                    urn.label.text = "+1x\n<size=60%>MULTIPLIER</size>";
                    break;
                default:
                    urn.label.text = "";
                    break;
            }
        }

        private void ShowProgress(TreasuryRound round, int picks)
        {
            collected.Set(round.CreditsAfter(picks), 0.45f);
            int x = round.MultiplierAfter(picks);
            if (multiplier.text != "x" + x)
            {
                multiplier.text = "x" + x;
                Tween.Scale(multiplier.transform, 1.5f, 1f, 0.4f, Ease.OutBack);
            }

            shield.enabled = round.ShieldsAfter(picks) > 0;
        }

        // What the closed urns held, faded, so the player sees the whole board
        private void ShowUnopened(TreasuryRound round)
        {
            int next = round.Picks;
            for (int i = 0; i < urns.Length && next < round.Order.Length; i++)
            {
                if (opened[i])
                {
                    continue;
                }

                pulses[i].Stop();
                urns[i].glow.color = Color.clear;
                urns[i].body.color = new Color(1f, 1f, 1f, 0.25f);
                Paint(urns[i], round.Order[next], 0.45f);
                next++;
            }
        }

        // The credits times the multiplier
        private IEnumerator Total(TreasuryRound round)
        {
            prompt.color = Color.white;
            int x = round.MultiplierAfter(round.Picks);
            if (x > 1)
            {
                prompt.text = "TREASURE x" + x;
                yield return Tween.Delay(0.6f).ToYieldInstruction();
            }

            prompt.text = "TREASURY WIN";
            collected.Set(round.Amount, 1.2f);
            Tween.Scale(collected.transform, 1f, 1.25f, 0.3f, Ease.OutCubic, cycles: 2, cycleMode: CycleMode.Yoyo);
            Totalled?.Invoke(round.Amount);
            yield return Tween.Delay(1.2f).ToYieldInstruction();
        }

        private IEnumerator Hold(float seconds)
        {
            skipped = false;
            tapArea.gameObject.SetActive(true);
            for (float t = 0f; t < seconds && !skipped; t += Time.deltaTime)
            {
                yield return null;
            }

            tapArea.gameObject.SetActive(false);
        }

        private void Hide()
        {
            IsOpen = false;
            group.alpha = 0f;
            group.blocksRaycasts = false;
            for (int i = 0; i < pulses.Length; i++)
            {
                pulses[i].Stop();
            }
        }

        private void OnDisable()
        {
            for (int i = 0; i < pulses.Length; i++)
            {
                pulses[i].Stop();
            }
        }
    }
}
