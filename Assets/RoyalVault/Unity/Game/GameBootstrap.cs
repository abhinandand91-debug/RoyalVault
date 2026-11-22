using GameKit;
using SlotKit;
using UnityEngine;

namespace RoyalVault.Unity
{
    // The game's single entry point and composition root
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Header("What to play")]
        [SerializeField] private MachineDefinitionAsset machine;
        [SerializeField] private ThemeDefinition theme;
        [Tooltip("Fixed random seed for repeatable sessions; 0 = a new random session each time")]
        [SerializeField] private int seed;
        [Tooltip("Keep credits, bet and jackpots between sessions")]
        [SerializeField] private bool saveProgress = true;

        [Header("Testing")]
        [Tooltip("Every paid spin lands the next result in the list below, to check all the celebrations and effects. " +
                 "Real outcomes are found by the normal rules; progress is not saved while this is on at start.")]
        [SerializeField] private bool showcaseWins;
        [Tooltip("The results the showcase lands, in order, then from the top again")]
        [SerializeField]
        private ShowcaseWin[] showcaseOrder =
        {
            ShowcaseWin.LineWin, ShowcaseWin.BigWin, ShowcaseWin.MegaWin, ShowcaseWin.EpicWin, ShowcaseWin.FreeSpins,
            ShowcaseWin.MinorJackpot, ShowcaseWin.MajorJackpot, ShowcaseWin.GrandJackpot,
        };

        [Header("Views")]
        [SerializeField] private MachineView machineView;
        [SerializeField] private ConsoleView console;
        [SerializeField] private JackpotPanelView jackpots;
        [SerializeField] private WinPresenter presenter;
        [SerializeField] private BigWinView bigWin;
        [SerializeField] private PaytableView paytable;
        [SerializeField] private MessageToast toast;
        [SerializeField] private CharacterView[] characters = new CharacterView[0];
        [SerializeField] private SealMeterView sealMeter;
        [SerializeField] private FlameClashView flameClash;
        [SerializeField] private ChapterCardView chapterCard;
        [SerializeField] private CoachMarkView coachMarks;
        [SerializeField] private GuardianFireView guardianFire;
        [SerializeField] private RelicRevealView relicReveal;
        [SerializeField] private GambleView gambleView;
        [SerializeField] private TreasuryView treasuryView;

        public GameSession Session { get; private set; }
        public SpinEngine Engine { get; private set; }
        public SpinFlow Flow { get; private set; }
        public GambleSession Gamble { get; private set; }
        public MachineDefinition Machine { get; private set; }
        public SoundDirector Sound { get; private set; }
        public WinShowcase Showcase { get; private set; }
        public StoryDirector Story { get; private set; }

        // Turns the win showcase on or off (it can also be ticked in the Inspector while playing)
        public bool ShowcaseWins
        {
            get => showcaseWins; set
            {
                showcaseWins = value;
                Showcase?.Arm();
            }
        }
        public ShowcaseWin[] ShowcaseOrder
        {
            get => showcaseOrder; set
            {
                showcaseOrder = value;
                Showcase?.Arm();
            }
        }

        // The machine to play, set before the object wakes (the boot scene or a test may choose)
        public MachineDefinitionAsset MachineAsset
        {
            get
            {
                return machine;
            }
            set
            {
                machine = value;
            }
        }

        public void Setup(MachineDefinitionAsset machineAsset, ThemeDefinition themeAsset, MachineView machineViewRef, ConsoleView consoleView,
                          JackpotPanelView jackpotView, WinPresenter winPresenter, BigWinView bigWinView, PaytableView paytableView,
                          MessageToast messageToast, CharacterView[] characterViews, SealMeterView sealMeterView,
                          FlameClashView flameClashView = null, ChapterCardView chapterCardView = null, CoachMarkView coachMarkView = null,
                          GuardianFireView guardianFireView = null, RelicRevealView relicRevealView = null, GambleView gamble = null,
                          TreasuryView treasury = null)
        {
            machine = machineAsset;
            theme = themeAsset;
            machineView = machineViewRef;
            console = consoleView;
            jackpots = jackpotView;
            presenter = winPresenter;
            bigWin = bigWinView;
            paytable = paytableView;
            toast = messageToast;
            characters = characterViews;
            sealMeter = sealMeterView;
            flameClash = flameClashView;
            chapterCard = chapterCardView;
            coachMarks = coachMarkView;
            guardianFire = guardianFireView;
            relicReveal = relicRevealView;
            gambleView = gamble;
            treasuryView = treasury;
        }

        private void Awake()
        {
            Machine = machine.ToDefinition();
            var errors = Machine.Validate();
            if (errors.Count > 0)
            {
                Debug.LogError($"{machine.name} cannot be played: {string.Join(" ", errors)}", machine);
                enabled = false;
                return;
            }
            CreateCore();
            BindViews();
            BindRound();
        }

        private void CreateCore()
        {
            // showcase credits are never saved
            ISaveStore store = saveProgress && !showcaseWins ? new PlayerPrefsSaveStore(Machine.Name) : (ISaveStore)new MemorySaveStore();
            Session = new GameSession(Machine, store);
            IRandom random = seed != 0 ? new SeededRandom(seed) : new SeededRandom();
            Engine = new SpinEngine(Machine, random);
            Flow = new SpinFlow(Session, Engine, StandardFeatures.Create(random));
            Gamble = new GambleSession(Session.Wallet, random, Machine.Gamble);
        }

        private void BindViews()
        {
            machineView.Build(Machine, theme);
            jackpots.Bind(Session.Jackpots);
            paytable.Build(Machine, theme, Session.Bet);
            foreach (CharacterView c in characters)
            {
                c.Bind(theme.character, theme.characterEffectAnchor, theme.characterMagic, theme.characterAnimation);
            }

            if (flameClash != null)
            {
                flameClash.Bind(characters, theme.characterMagic);
                flameClash.SetStoreEffects(theme.clashImpactVfx, theme.clashSparksVfx, theme.vfxScale);
            }
            if (sealMeter != null)
            {
                sealMeter.Bind(Machine, theme, machineView, Flow, Session);
            }

            if (guardianFire != null)
            {
                guardianFire.Bind(characters, machineView, theme.characterMagic);
            }

            if (relicReveal != null)
            {
                relicReveal.Bind(Machine, theme);
                if (sealMeter != null)
                {
                    relicReveal.Revealed += sealMeter.ShowRelic;
                }
            }

            machineView.AnticipationStarted += reel =>
            {
                foreach (CharacterView c in characters)
                {
                    c.Surge();
                }
            };
        }

        // Story, presenter, round and sound; sound listens, the views stay silent
        private void BindRound()
        {
            Story = gameObject.AddComponent<StoryDirector>();
            Story.Bind(theme.story, chapterCard, coachMarks, FirstVisitHints());
            presenter.Bind(machineView, console, bigWin, jackpots, characters, flameClash, Story);
            var player = gameObject.AddComponent<SoundPlayer>();
            player.Setup(theme.sounds);
            Sound = gameObject.AddComponent<SoundDirector>();
            var round = gameObject.AddComponent<RoundController>();
            round.Bind(Session, Flow, machineView, console, presenter, bigWin, paytable, toast, Story);
            Sound.Bind(player, Machine, Session, Flow, machineView, console, bigWin, sealMeter, flameClash);
            if (guardianFire != null)
            {
                round.AddLandingStep(guardianFire);
                Sound.ListenTo(guardianFire);
            }

            round.AddLandingStep(new RelicLandingStep(machineView));
            Sound.ListenToRelic(relicReveal, machineView);
            if (gambleView != null)
            {
                gambleView.Bind(theme, theme.heroRight);
                round.SetGamble(Gamble, gambleView);
                Sound.ListenTo(gambleView);
            }

            if (treasuryView != null)
            {
                treasuryView.Bind(theme);
                Sound.ListenTo(treasuryView);
            }

            AddPresentationSteps(round);
            Showcase = new WinShowcase(Engine, Session, Flow, toast, () => showcaseWins, () => showcaseOrder);
        }

        // The moments after a spin's wins, in the order they play: the Treasury, then free spins starting, more free spins,
        // and a round's closing total
        private void AddPresentationSteps(RoundController round)
        {
            var autoPlay = Flow.Features.Get<AutoPlayFeature>();
            round.AddPresentationStep(new TreasuryStep(Session, machineView, bigWin, treasuryView, console, autoPlay));
            round.AddPresentationStep(new FreeSpinsAwardedStep(Session, Story, bigWin, relicReveal, round.Tally));
            round.AddPresentationStep(new FreeSpinsRetriggeredStep(bigWin));
            round.AddPresentationStep(new FreeSpinsTotalStep(Session, bigWin, round.Tally));
        }

        // Spin button, seal meter and jackpots, with the story's hint for each
        private StoryDirector.Hint[] FirstVisitHints()
        {
            string[] texts = theme.story != null ? theme.story.controlHints : new string[0];
            RectTransform[] targets = { console.SpinButtonRect, sealMeter != null ? (RectTransform)sealMeter.transform : null, jackpots.TopPlaque };
            bool[] above = { true, true, false };
            var hints = new StoryDirector.Hint[Mathf.Min(texts.Length, targets.Length)];
            for (int i = 0; i < hints.Length; i++)
            {
                hints[i] = new StoryDirector.Hint { target = targets[i], text = texts[i], above = above[i] };
            }

            return hints;
        }

        // Ticking or unticking the showcase in the Inspector while playing takes effect on the next spin
        private void OnValidate()
        {
            if (!Application.isPlaying || Showcase == null)
            {
                return;
            }

            bool armed = Showcase.Armed != null;
            if (showcaseWins != armed)
            {
                Showcase.Arm();
            }
        }

        // Testing aid: the next spin stops at these reel positions
        public void ForceNextStops(params int[] stops)
        {
            Engine.ForcedStops = stops;
        }

        // Testing aid: the next free spin round's relic is the symbol with this id
        public void ForceRelic(string symbolId)
        {
            Flow.Features.Get<ExpandingSymbolFeature>().ForcedRelic = Machine.SymbolIndex(symbolId);
        }

        // Testing aid: the next base-game spin opens the Treasury
        public void ForceTreasury()
        {
            Flow.Features.Get<TreasuryFeature>().ForceNext = true;
        }

        // Testing aid: the next losing base-game spin gets Guardian's Fire
        public void ForceGuardianFire()
        {
            Flow.Features.Get<RandomWildsFeature>().ForceNext = true;
        }
    }
}
