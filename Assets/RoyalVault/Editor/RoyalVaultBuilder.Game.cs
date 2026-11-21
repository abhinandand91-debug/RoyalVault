using RoyalVault.Unity;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalVault.EditorTools
{
    public static partial class RoyalVaultBuilder
    {
        private const string PrefabPath = Root + "Prefabs/RoyalVaultGame.prefab";

        /// <summary>
        /// The whole game as one prefab: an overlay canvas laid out on a 1920 x 1080 design (the approved Figma mockup),
        /// with <see cref="GameBootstrap"/> on the root. The reels themselves are built at run time from the machine.
        /// </summary>
        private static GameObject BuildGamePrefab(MachineDefinitionAsset machine, ThemeDefinition theme)
        {
            EnsureFolder(Root + "Prefabs");
            var root = new GameObject("RoyalVaultGame", typeof(RectTransform));
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            root.AddComponent<GraphicRaycaster>();
            Transform t = root.transform;

            // backdrop: the vault, darkened at the edges
            Image background = Stretch("Background", t, theme.background);
            var fitter = background.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 16f / 9f;
            Stretch("Vignette", t, theme.vignette);

            // the guardians in their niches
            UiFactory.Image("Niche Left", t, theme.niche, new Vector2(-800, -13), new Vector2(232, 751));
            UiFactory.Image("Niche Right", t, theme.niche, new Vector2(800, -13), new Vector2(232, 751));
            CharacterView left = Character("Guardian Left", t, theme, -785f, false, 0f);
            CharacterView right = Character("Guardian Right", t, theme, 785f, true, 1.7f);

            // the reels (built at run time) inside their frame
            RectTransform machineRect = UiFactory.Stretch("Machine", t);
            var machineView = machineRect.gameObject.AddComponent<MachineView>();

            // logo and jackpots
            Image logo = UiFactory.Image("Logo", t, theme.logo, new Vector2(0, 455), new Vector2(480, 171));
            logo.preserveAspect = true;
            JackpotPanelView jackpots = Jackpots(t, theme);

            // console
            ConsoleView console = Console(t, theme);
            SealMeterView sealMeter = SealMeter(console.transform, theme);
            MessageToast toast = Toast(t, theme);

            // overlays
            BigWinView bigWin = BigWin(t, theme);
            PaytableView paytable = Paytable(t, theme);

            var presenter = root.AddComponent<WinPresenter>();
            var bootstrap = root.AddComponent<GameBootstrap>();
            bootstrap.Setup(machine, theme, machineView, console, jackpots, presenter, bigWin, paytable, toast, new[] { left, right }, sealMeter);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static Image Stretch(string name, Transform parent, Sprite sprite)
        {
            RectTransform rect = UiFactory.Stretch(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private static CharacterView Character(string name, Transform parent, ThemeDefinition theme, float x, bool mirrored, float phase)
        {
            RectTransform holder = UiFactory.Rect(name, parent, new Vector2(x, -380), new Vector2(250, 705));
            holder.pivot = new Vector2(0.5f, 0f);
            holder.anchoredPosition = new Vector2(x, -380);

            // a warm aura behind the figure
            Image aura = UiFactory.Image("Aura", holder, theme.glow, new Vector2(0, 40), new Vector2(560, 940));
            aura.material = theme.additive;
            aura.color = new Color(1f, 0.72f, 0.32f, 0.14f);

            Image image = UiFactory.Image("Figure", holder, null, Vector2.zero, new Vector2(250, 705));
            image.rectTransform.pivot = new Vector2(0.5f, 0f);
            image.rectTransform.anchoredPosition = new Vector2(0, -352.5f);
            image.preserveAspect = true;
            image.color = Color.white;
            if (mirrored) image.rectTransform.localScale = new Vector3(-1f, 1f, 1f);

            // gold dust drifting past the figure
            UiParticleEmitter dust = Emitter("Gold Dust", holder, theme, theme.glow, Vector2.zero);
            dust.Rate = 2.5f; dust.LifeRange = new Vector2(3f, 5f); dust.SizeRange = new Vector2(5f, 12f); dust.EndSize = 0.6f;
            dust.Area = new Vector2(260f, 640f); dust.VelocityMin = new Vector2(-8f, 12f); dust.VelocityMax = new Vector2(8f, 34f);
            dust.Swirl = 10f; dust.SwirlFrequency = 1.5f; dust.MaxParticles = 30;
            dust.StartColor = new Color(1f, 0.85f, 0.5f, 0.6f); dust.EndColor = new Color(1f, 0.8f, 0.4f, 0f);

            // the magic in the raised hand: a glow, a living flame, embers and (on big wins) a spray of gold sparks
            RectTransform hand = UiFactory.Rect("Hand", holder, Vector2.zero, Vector2.one * 10f);
            hand.anchorMin = hand.anchorMax = new Vector2(0.5f, 0f);
            Image handGlow = UiFactory.Image("Hand Glow", hand, theme.glow, Vector2.zero, Vector2.one * 130f);
            handGlow.material = theme.additive;

            UiParticleEmitter flame = Emitter("Flame", hand, theme, theme.glow, Vector2.zero);
            flame.Rate = 42f; flame.LifeRange = new Vector2(0.45f, 0.95f); flame.SizeRange = new Vector2(22f, 46f); flame.EndSize = 0.15f;
            flame.Area = new Vector2(18f, 10f); flame.VelocityMin = new Vector2(-25f, 70f); flame.VelocityMax = new Vector2(25f, 150f);
            flame.Gravity = new Vector2(0f, 60f); flame.Swirl = 40f; flame.SwirlFrequency = 9f; flame.MaxParticles = 110;

            UiParticleEmitter embers = Emitter("Embers", hand, theme, theme.glow, Vector2.zero);
            embers.Rate = 7f; embers.LifeRange = new Vector2(1.2f, 2.2f); embers.SizeRange = new Vector2(5f, 10f); embers.EndSize = 0.5f;
            embers.Area = new Vector2(30f, 20f); embers.VelocityMin = new Vector2(-50f, 60f); embers.VelocityMax = new Vector2(50f, 140f);
            embers.Gravity = new Vector2(0f, -15f); embers.Swirl = 25f; embers.SwirlFrequency = 4f; embers.MaxParticles = 70;

            UiParticleEmitter sparks = Emitter("Sparks", hand, theme, theme.sparkle, Vector2.zero);
            sparks.Rate = 0f; sparks.LifeRange = new Vector2(0.8f, 1.4f); sparks.SizeRange = new Vector2(14f, 30f); sparks.EndSize = 0.2f;
            sparks.VelocityMin = new Vector2(-80f, 280f); sparks.VelocityMax = new Vector2(80f, 520f);
            sparks.Gravity = new Vector2(0f, -700f); sparks.Drag = 0.6f; sparks.MaxParticles = 120;
            sparks.StartColor = new Color(1f, 0.9f, 0.6f, 1f); sparks.EndColor = new Color(1f, 0.6f, 0.2f, 0f);

            var view = holder.gameObject.AddComponent<CharacterView>();
            view.Setup(image, phase);
            view.SetupEffects(aura, hand, handGlow, flame, embers, sparks, dust);
            return view;
        }

        /// <summary>The Vault Seal medallion beside the spin button: the seal's face, its cracks, the pips that count seals.</summary>
        private static SealMeterView SealMeter(Transform parent, ThemeDefinition theme)
        {
            RectTransform holder = UiFactory.Rect("Seal Meter", parent, new Vector2(-250, -452), new Vector2(120, 120));

            Image magic = UiFactory.Image("Vault Light", holder, theme.glow, Vector2.zero, new Vector2(230, 230));
            magic.material = theme.additive;
            magic.color = new Color(0f, 0f, 0f, 0f);

            RectTransform seal = UiFactory.Rect("Seal", holder, new Vector2(0, 8), new Vector2(92, 92));
            Image face = UiFactory.Image("Face", seal, null, Vector2.zero, new Vector2(92, 92));
            face.preserveAspect = true;
            var cracks = new Image[3];
            var fractures = new Image[3];
            for (int i = 0; i < cracks.Length; i++)
            {
                fractures[i] = UiFactory.Image("Fracture " + (i + 1), seal, Sprite("Generated/crack_" + (i + 1)), Vector2.zero, new Vector2(90, 90));
                fractures[i].color = new Color(0.12f, 0.06f, 0.02f, 0.85f);
                fractures[i].enabled = false;
            }
            for (int i = 0; i < cracks.Length; i++)
            {
                cracks[i] = UiFactory.Image("Crack " + (i + 1), seal, Sprite("Generated/crack_" + (i + 1)), Vector2.zero, new Vector2(84, 84));
                cracks[i].material = theme.additive;
                cracks[i].enabled = false;
            }
            Image flash = UiFactory.Image("Flash", seal, theme.glow, Vector2.zero, new Vector2(190, 190));
            flash.material = theme.additive;
            flash.color = new Color(1f, 0.9f, 0.6f, 0f);

            // three small diamonds under the seal: one lights for every seal that lands
            var pips = new Image[3];
            for (int i = 0; i < pips.Length; i++)
            {
                pips[i] = UiFactory.Image("Pip " + (i + 1), holder, theme.white, new Vector2((i - 1) * 20f, -54f), new Vector2(8, 8));
                pips[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }

            UiParticleEmitter sparks = Emitter("Seal Sparks", seal, theme, theme.sparkle, Vector2.zero);
            sparks.Rate = 0f; sparks.LifeRange = new Vector2(0.5f, 1.1f); sparks.SizeRange = new Vector2(10f, 22f); sparks.EndSize = 0.2f;
            sparks.Area = new Vector2(40f, 40f); sparks.VelocityMin = new Vector2(-220f, 60f); sparks.VelocityMax = new Vector2(220f, 380f);
            sparks.Gravity = new Vector2(0f, -600f); sparks.Drag = 0.8f; sparks.MaxParticles = 120;
            sparks.EndColor = new Color(1f, 0.6f, 0.2f, 0f);

            var view = holder.gameObject.AddComponent<SealMeterView>();
            view.Setup(seal, face, cracks, pips, flash, magic, sparks);
            view.SetupFractures(fractures);
            return view;
        }

        private static UiParticleEmitter Emitter(string name, Transform parent, ThemeDefinition theme, Sprite sprite, Vector2 position)
        {
            RectTransform rect = UiFactory.Rect(name, parent, position, Vector2.one * 10f);
            var emitter = rect.gameObject.AddComponent<UiParticleEmitter>();
            emitter.Sprite = sprite;
            emitter.Material = theme.additive;
            return emitter;
        }

        private static JackpotPanelView Jackpots(Transform parent, ThemeDefinition theme)
        {
            RectTransform panel = UiFactory.Stretch("Jackpots", parent);
            var images = new Image[3];
            var names = new TextMeshProUGUI[3];
            var values = new CountingText[3];
            Vector2[] positions = { new Vector2(0, 345), new Vector2(-720, 457), new Vector2(720, 457) };
            Vector2[] sizes = { new Vector2(430, 110), new Vector2(340, 91), new Vector2(340, 91) };
            for (int i = 0; i < 3; i++)
            {
                images[i] = UiFactory.Image(i == 0 ? "Top Tier" : i == 1 ? "Second Tier" : "Third Tier", panel, i == 0 ? theme.jackpotGrand : theme.jackpotMinor, positions[i], sizes[i]);
                bool top = i == 0;
                names[i] = UiFactory.Text("Name", images[i].transform, theme.labelFont, "GRAND", top ? 15 : 14, theme.dimGold,
                                          new Vector2(0, top ? 23 : 20), new Vector2(sizes[i].x, 24), TextAlignmentOptions.Center, 30);
                TextMeshProUGUI value = UiFactory.Text("Value", images[i].transform, theme.headingFont, "0", top ? 36 : 28, top ? theme.paleGold : theme.gold,
                                                       new Vector2(0, top ? -12 : -9), new Vector2(sizes[i].x, 48));
                values[i] = value.gameObject.AddComponent<CountingText>();
            }
            var view = panel.gameObject.AddComponent<JackpotPanelView>();
            view.Setup(images, names, values);
            return view;
        }

        private static ConsoleView Console(Transform parent, ThemeDefinition theme)
        {
            RectTransform console = UiFactory.Stretch("Console", parent);

            // the bar spans the whole screen width (wide screens too): lacquer, a double gold rule, the spin socket
            RectTransform bar = BottomStrip("Bar", console, Sprite("Generated/bar"), 150, 0, 0).rectTransform;
            Image rule = BottomStrip("Rule", bar, Sprite("Generated/rule"), 3, 148, 0);
            Image inner = BottomStrip("Inner Rule", bar, Sprite("Generated/rule"), 2, 140, 160);
            inner.color = new Color(1f, 1f, 1f, 0.45f);
            _ = rule;
            UiFactory.Image("Spin Socket", console, Sprite("Generated/socket"), new Vector2(0, -460), new Vector2(320, 320));

            // readouts
            TextMeshProUGUI balanceLabel = Left("Balance Label", console, theme.labelFont, "BALANCE", 15, theme.dimGold, -810, -444, 28);
            TextMeshProUGUI balanceValue = Left("Balance", console, theme.headingFont, "0", 34, theme.ivory, -810, -482, 0);
            Image divider = UiFactory.Image("Divider", console, theme.white, new Vector2(-596, -462), new Vector2(1, 56));
            divider.color = new Color(theme.gold.r, theme.gold.g, theme.gold.b, 0.5f);
            TextMeshProUGUI winLabel = Left("Win Label", console, theme.labelFont, "WIN", 15, theme.dimGold, -560, -444, 28);
            TextMeshProUGUI winValue = Left("Win", console, theme.headingFont, "0", 34, theme.paleGold, -560, -482, 0);
            _ = balanceLabel;

            // spin
            Image glow = UiFactory.Image("Spin Glow", console, theme.glow, new Vector2(0, -460), new Vector2(330, 330));
            glow.color = new Color(1f, 0.8f, 0.4f, 0.4f);
            Image spinImage = UiFactory.Image("Spin", console, theme.spinButton, new Vector2(0, -460), new Vector2(206, 206));
            spinImage.preserveAspect = true;
            Button spinButton = UiFactory.Button(spinImage);
            spinButton.transition = Selectable.Transition.SpriteSwap;
            SpriteState state = spinButton.spriteState;
            state.pressedSprite = theme.spinButtonPressed;
            spinButton.spriteState = state;
            var spin = spinImage.gameObject.AddComponent<SpinButtonView>();
            spin.Setup(spinButton, glow);

            // auto play and the bet stepper
            Image autoPill = Pill("Auto", console, theme, new Vector2(362, -471), new Vector2(168, 64));
            TextMeshProUGUI autoText = UiFactory.Text("Label", autoPill.transform, theme.headingFont, "AUTO", 17, theme.gold, Vector2.zero, new Vector2(168, 64), TextAlignmentOptions.Center, 22);
            Image stepper = Pill("Bet Stepper", console, theme, new Vector2(640, -471), new Vector2(340, 64));
            stepper.raycastTarget = false;
            Button minus = RoundButton("Bet Minus", stepper.transform, theme, new Vector2(-138, 0), 48, false);
            Button plus = RoundButton("Bet Plus", stepper.transform, theme, new Vector2(138, 0), 48, true);
            UiFactory.Text("Total Bet Label", stepper.transform, theme.labelFont, "TOTAL BET", 12, theme.dimGold, new Vector2(0, 14), new Vector2(200, 20), TextAlignmentOptions.Center, 26);
            TextMeshProUGUI totalBet = UiFactory.Text("Total Bet", stepper.transform, theme.headingFont, "0", 26, theme.ivory, new Vector2(0, -9), new Vector2(200, 36));

            // menu and sound at the ends
            Button menu = IconButton("Menu", console, theme, theme.iconMenu, new Vector2(-884, -471), out _);
            Button sound = IconButton("Sound", console, theme, theme.iconSound, new Vector2(884, -471), out Image soundIcon);

            var view = console.gameObject.AddComponent<ConsoleView>();
            CountingText balance = balanceValue.gameObject.AddComponent<CountingText>();
            CountingText win = winValue.gameObject.AddComponent<CountingText>();
            view.Setup(balance, winLabel, win, totalBet, autoText, spin, minus, plus, UiFactory.Button(autoPill), menu, sound, soundIcon);
            return view;
        }

        /// <summary>A full-width strip anchored to the bottom of its parent, <paramref name="inset"/> pixels in from each side.</summary>
        private static Image BottomStrip(string name, Transform parent, Sprite sprite, float height, float y, float inset)
        {
            Image image = UiFactory.Image(name, parent, sprite, Vector2.zero, Vector2.zero);
            image.color = Color.white;
            RectTransform r = image.rectTransform;
            r.anchorMin = new Vector2(0, 0);
            r.anchorMax = new Vector2(1, 0);
            r.pivot = new Vector2(0.5f, 0f);
            r.sizeDelta = new Vector2(-2f * inset, height);
            r.anchoredPosition = new Vector2(0, y);
            return image;
        }

        private static TextMeshProUGUI Left(string name, Transform parent, TMP_FontAsset font, string text, float size, Color color, float x, float y, float spacing)
        {
            TextMeshProUGUI label = UiFactory.Text(name, parent, font, text, size, color, new Vector2(x, y), new Vector2(380, size * 1.5f), TextAlignmentOptions.Left, spacing);
            label.rectTransform.pivot = new Vector2(0f, 0.5f);
            label.rectTransform.anchoredPosition = new Vector2(x, y);
            return label;
        }

        private static Image Pill(string name, Transform parent, ThemeDefinition theme, Vector2 position, Vector2 size)
        {
            Image pill = UiFactory.Image(name, parent, theme.pillButton, position, size);
            pill.type = Image.Type.Sliced;
            pill.pixelsPerUnitMultiplier = 128f / size.y;
            return pill;
        }

        private static Button RoundButton(string name, Transform parent, ThemeDefinition theme, Vector2 position, float size, bool plus)
        {
            Image ring = UiFactory.Image(name, parent, theme.roundButton, position, Vector2.one * size);
            Button button = UiFactory.Button(ring);
            Image h = UiFactory.Image("Minus", ring.transform, theme.white, Vector2.zero, new Vector2(16, 2.5f));
            h.color = theme.gold;
            if (plus)
            {
                Image v = UiFactory.Image("Plus", ring.transform, theme.white, Vector2.zero, new Vector2(2.5f, 16));
                v.color = theme.gold;
            }
            return button;
        }

        private static Button IconButton(string name, Transform parent, ThemeDefinition theme, Sprite icon, Vector2 position, out Image iconImage)
        {
            Image ring = UiFactory.Image(name, parent, theme.roundButton, position, Vector2.one * 56);
            Button button = UiFactory.Button(ring);
            iconImage = UiFactory.Image("Icon", ring.transform, icon, Vector2.zero, Vector2.one * 30);
            iconImage.color = Color.white;
            return button;
        }

        private static MessageToast Toast(Transform parent, ThemeDefinition theme)
        {
            Image pill = Pill("Message", parent, theme, new Vector2(0, -320), new Vector2(720, 64));
            pill.raycastTarget = false;
            TextMeshProUGUI text = UiFactory.Text("Text", pill.transform, theme.bodyFont, "", 28, theme.ivory, Vector2.zero, new Vector2(680, 60));
            var group = pill.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            var toast = pill.gameObject.AddComponent<MessageToast>();
            toast.Setup(group, text);
            return toast;
        }

        private static BigWinView BigWin(Transform parent, ThemeDefinition theme)
        {
            RectTransform overlay = UiFactory.Stretch("Big Win", parent);
            var group = overlay.gameObject.AddComponent<CanvasGroup>();
            Image dim = Stretch("Dim", overlay, theme.white);
            dim.color = new Color(0.02f, 0.02f, 0.03f, 0.8f);
            Button skip = UiFactory.Button(dim);
            skip.transition = Selectable.Transition.None;

            Image rays = UiFactory.Image("Rays", overlay, Sprite("Generated/rays"), new Vector2(0, 90), new Vector2(1500, 1500));
            Image halo = UiFactory.Image("Halo", overlay, theme.glow, new Vector2(0, 90), new Vector2(1100, 700));
            halo.color = new Color(1f, 0.78f, 0.38f, 0.35f);
            Image banner = UiFactory.Image("Banner", overlay, theme.bigWinBanner, new Vector2(0, 130), new Vector2(1200, 400));
            banner.preserveAspect = true;
            TextMeshProUGUI title = UiFactory.Text("Title", overlay, theme.displayFont, "MEGA WIN", 128, Color.white, new Vector2(0, 130), new Vector2(1700, 180));
            title.enableVertexGradient = true;
            title.colorGradient = new VertexGradient(new Color32(255, 241, 193, 255), new Color32(255, 241, 193, 255), new Color32(184, 135, 59, 255), new Color32(184, 135, 59, 255));
            title.outlineWidth = 0.12f;
            title.outlineColor = new Color32(58, 37, 10, 255);
            TextMeshProUGUI subtitle = UiFactory.Text("Subtitle", overlay, theme.headingFont, "", 40, theme.paleGold, new Vector2(0, 20), new Vector2(1400, 60), TextAlignmentOptions.Center, 20);
            TextMeshProUGUI amountText = UiFactory.Text("Amount", overlay, theme.displayFont, "0", 112, theme.paleGold, new Vector2(0, -90), new Vector2(1400, 150));
            amountText.outlineWidth = 0.1f;
            amountText.outlineColor = new Color32(58, 37, 10, 255);
            CountingText amount = amountText.gameObject.AddComponent<CountingText>();
            RectTransform coins = UiFactory.Rect("Coins", overlay, new Vector2(0, -560), Vector2.one * 10);

            var view = overlay.gameObject.AddComponent<BigWinView>();
            view.Setup(group, banner, title, subtitle, amount, rays.rectTransform, coins, theme.coin, skip);
            return view;
        }

        private static PaytableView Paytable(Transform parent, ThemeDefinition theme)
        {
            RectTransform window = UiFactory.Stretch("Pay Table", parent);
            var group = window.gameObject.AddComponent<CanvasGroup>();
            Image dim = Stretch("Backdrop", window, theme.white);
            dim.color = new Color(0.02f, 0.02f, 0.03f, 0.78f);
            Button backdrop = UiFactory.Button(dim);
            backdrop.transition = Selectable.Transition.None;

            Image panel = UiFactory.Image("Panel", window, theme.popupPanel, new Vector2(0, 10), new Vector2(1440, 900), true);
            panel.type = Image.Type.Sliced;
            // everything sits inside the panel's dark centre, clear of the gold ornaments
            UiFactory.Text("Title", panel.transform, theme.headingFont, "PAY TABLE", 40, theme.gold, new Vector2(0, 248), new Vector2(800, 60), TextAlignmentOptions.Center, 22);
            RectTransform content = UiFactory.Rect("Content", panel.transform, Vector2.zero, new Vector2(1200, 580));
            Button close = IconButton("Close", panel.transform, theme, theme.iconClose, new Vector2(560, 248), out _);

            var view = window.gameObject.AddComponent<PaytableView>();
            view.Setup(group, content, close, backdrop);
            return view;
        }
    }
}
