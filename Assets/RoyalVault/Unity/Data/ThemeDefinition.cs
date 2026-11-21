using System;
using TMPro;
using UnityEngine;

namespace RoyalVault.Unity
{
    /// <summary>How a symbol celebrates: which light and particles surround it when it wins.</summary>
    public enum SymbolStyle
    {
        /// <summary>Card royals: a light sweep and a warm glow.</summary>
        Card,
        /// <summary>Gems: a coloured glow, a sway and twinkling stars.</summary>
        Gem,
        /// <summary>Treasures (gold, keys, relics): a light sweep and rising gold dust.</summary>
        Treasure,
        /// <summary>The wild: a big pop and flash, rotating rays, stars.</summary>
        Wild,
        /// <summary>The scatter: rays and sparks in the vault's blue magic.</summary>
        Scatter,
        /// <summary>The jackpot symbol: golden rays and jewel-coloured glints.</summary>
        Jackpot,
    }

    /// <summary>
    /// Everything a machine looks like: symbol art, machine and interface sprites, fonts, colours and the reel window.
    /// A reskin is a new theme asset; the game code and the machine stay the same.
    /// </summary>
    [CreateAssetMenu(menuName = "Royal Vault/Theme", fileName = "Theme")]
    public sealed class ThemeDefinition : ScriptableObject
    {
        [Serializable]
        public sealed class SymbolArt
        {
            [Tooltip("Matches the symbol id in the machine definition")] public string id;
            public Sprite sprite;
            [Tooltip("Shown while the reel spins fast")] public Sprite blur;
            [Tooltip("How it celebrates a win")] public SymbolStyle style;
            [Tooltip("Colour of its glow and light")] public Color tint = new Color(1f, 0.85f, 0.45f, 1f);
            [Tooltip("Optional baked win animation (for example exported from an animation tool); plays instead of the pulse")]
            public CharacterAnimation winAnimation;
        }

        [Header("Symbols")]
        public SymbolArt[] symbols = new SymbolArt[0];

        [Header("Machine")]
        public Sprite background;
        public Sprite vignette;
        public Sprite reelFrame;
        public Sprite reelCell;
        public Sprite logo;
        public Sprite niche;
        [Tooltip("Optional guardian character (licensed art may be missing in a public checkout)")] public Sprite character;
        [Tooltip("Where the character's magic burns, as a share of its art (0,0 = bottom left)")] public Vector2 characterEffectAnchor = new Vector2(0.89f, 0.837f);
        [Tooltip("Optional baked animation for the character; without it the still art breathes procedurally")] public CharacterAnimation characterAnimation;
        public Color characterMagic = new Color(0.35f, 0.75f, 1f, 1f);
        [Tooltip("Additive material for glows and particles")] public Material additive;
        [Tooltip("Material for reel symbols (RoyalVault/UI Symbol): the shine sweep and flash")] public Material symbolMaterial;
        [Tooltip("Rays drawn behind the grand symbols when they win")] public Sprite rays;

        [Header("Store VFX (optional particle prefabs, drawn in the UI by UIParticle)")]
        [Tooltip("Where the guardians' fireballs meet")] public GameObject clashImpactVfx;
        [Tooltip("A second burst with the clash, e.g. sparks")] public GameObject clashSparksVfx;
        [Tooltip("As the seal on the vault door shatters")] public GameObject sealBreakVfx;
        [Tooltip("Particle effects are made in metres for 3D scenes: canvas pixels per metre")] public float vfxScale = 170f;
        [Tooltip("Optional story told on the loading screen and at game moments")] public StoryDefinition story;
        [Tooltip("The game's sounds and music; empty cues are silent")] public SoundBank sounds;
        [Tooltip("Optional hero who celebrates big wins on the left (the prospector)")] public CharacterAnimation heroLeft;
        [Tooltip("Optional hero who celebrates big wins on the right (the sharpshooter, mirrored to face the centre)")] public CharacterAnimation heroRight;

        [Header("Interface")]
        public Sprite consoleBar;
        public Sprite spinButton;
        public Sprite spinButtonPressed;
        public Sprite pillButton;
        public Sprite roundButton;
        public Sprite jackpotGrand;
        public Sprite jackpotMinor;
        public Sprite popupPanel;
        public Sprite bigWinBanner;
        public Sprite coin;
        public Sprite sparkle;
        public Sprite glow;
        public Sprite line;
        public Sprite white;
        public Sprite levelBar;
        public Sprite levelFill;
        public Sprite iconMenu;
        public Sprite iconInfo;
        public Sprite iconSound;
        public Sprite iconClose;

        [Header("Fonts")]
        public TMP_FontAsset displayFont;
        public TMP_FontAsset headingFont;
        public TMP_FontAsset labelFont;
        public TMP_FontAsset bodyFont;

        [Header("Colours")]
        public Color gold = new Color32(232, 199, 122, 255);
        public Color paleGold = new Color32(255, 231, 166, 255);
        public Color ivory = new Color32(243, 233, 210, 255);
        public Color dimGold = new Color32(179, 154, 102, 255);
        public Color ink = new Color32(10, 9, 13, 255);

        [Header("Reel window (canvas units, 1920 x 1080 design)")]
        [Tooltip("The largest area the reels may use; the machine is centred in it")] public Rect reelArea = new Rect(-555, -251, 1110, 541);
        [Tooltip("Width / height of one cell")] public float cellAspect = 1.23f;
        public float cellGap = 4f;
        [Tooltip("Symbol size as a share of the cell height")] public float symbolScale = 0.94f;
        [Tooltip("How large the frame's 9-slice borders draw (1 = the art's own pixels)")] public float frameBorderScale = 0.937f;

        [Header("Loading screen")]
        public string[] loadingTips = { "Polishing the gems", "Counting the gold", "Waking the guardians", "Opening the vault" };

        public SymbolArt Symbol(string id)
        {
            foreach (SymbolArt art in symbols)
                if (art.id == id) return art;
            return null;
        }
    }
}
