using RoyalVault.Core;
using UnityEngine;

namespace RoyalVault.Unity
{
    /// <summary>
    /// The game's single entry point and composition root. It reads the machine and theme assets, creates the Core
    /// objects (session, engine, features, flow) and hands them to the views. Nothing else in the game sets itself up:
    /// there are no singletons, and the views only receive what they need here.
    ///
    /// Drop the RoyalVaultGame prefab into any scene, assign a machine and a theme, and press Play.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Header("What to play")]
        [SerializeField] private MachineDefinitionAsset machine;
        [SerializeField] private ThemeDefinition theme;
        [Tooltip("Fixed random seed for repeatable sessions; 0 = a new random session each time")]
        [SerializeField] private int seed;
        [Tooltip("Keep credits, bet and jackpots between sessions")]
        [SerializeField] private bool saveProgress = true;

        [Header("Views")]
        [SerializeField] private MachineView machineView;
        [SerializeField] private ConsoleView console;
        [SerializeField] private JackpotPanelView jackpots;
        [SerializeField] private WinPresenter presenter;
        [SerializeField] private BigWinView bigWin;
        [SerializeField] private PaytableView paytable;
        [SerializeField] private MessageToast toast;
        [SerializeField] private CharacterView[] characters = new CharacterView[0];

        public GameSession Session { get; private set; }
        public SpinEngine Engine { get; private set; }
        public SpinFlow Flow { get; private set; }
        public MachineDefinition Machine { get; private set; }

        /// <summary>The machine to play, set before the object wakes (the boot scene or a test may choose).</summary>
        public MachineDefinitionAsset MachineAsset { get { return machine; } set { machine = value; } }

        public void Setup(MachineDefinitionAsset machineAsset, ThemeDefinition themeAsset, MachineView machineViewRef, ConsoleView consoleView,
                          JackpotPanelView jackpotView, WinPresenter winPresenter, BigWinView bigWinView, PaytableView paytableView,
                          MessageToast messageToast, CharacterView[] characterViews)
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

            // core
            ISaveStore store = saveProgress ? new PlayerPrefsSaveStore(Machine.Name) : (ISaveStore)new MemorySaveStore();
            Session = new GameSession(Machine, store);
            IRandom random = seed != 0 ? new SeededRandom(seed) : new SeededRandom();
            Engine = new SpinEngine(Machine, random);
            FeaturePipeline features = StandardFeatures.Create();
            Flow = new SpinFlow(Session, Engine, features);

            // presentation
            machineView.Build(Machine, theme);
            jackpots.Bind(Session.Jackpots);
            paytable.Build(Machine, theme);
            for (int i = 0; i < characters.Length; i++) characters[i].Bind(theme.character);
            presenter.Bind(machineView, console, bigWin, jackpots, characters);
            gameObject.AddComponent<RoundController>().Bind(Session, Flow, machineView, console, presenter, bigWin, paytable, toast);
        }

        /// <summary>Testing aid: the next spin stops at these reel positions.</summary>
        public void ForceNextStops(params int[] stops)
        {
            Engine.ForcedStops = stops;
        }
    }
}
