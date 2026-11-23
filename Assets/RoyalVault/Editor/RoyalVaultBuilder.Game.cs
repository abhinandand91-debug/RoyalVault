using GameKit;
using PrimeTween;
using RoyalVault.Unity;
using TMPro;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine;

namespace RoyalVault.EditorTools
{
    public static partial class RoyalVaultBuilder
    {
        private const string PrefabPath = Root + "Prefabs/RoyalVaultGame.prefab";

        // Builds the game prefab on a 1920 x 1080 canvas
        private static GameObject BuildGamePrefab(MachineDefinitionAsset machine, ThemeDefinition theme)
        {
            EnsureFolder(Root + "Prefabs");
            GameObject root = OverlayCanvas("RoyalVaultGame");
            Transform t = root.transform;
            DialogManager dialogs = Dialogs(root);
            bool emerald = theme.look == ThemeLook.Emerald;
            Backdrop(t, theme, Color.white);
            // the Art Deco guardians stand in niches behind the frame; the Emerald guardians stand in front of the housing
            CharacterView[] guardians = emerald ? null : Guardians(t, theme);
            RectTransform machineRect = UiFactory.Stretch("Machine", t);
            UiFactory.NestedCanvas(machineRect.gameObject, false);
            var machineView = machineRect.gameObject.AddComponent<MachineView>();
            if (emerald)
            {
                guardians = Guardians(t, theme);
                EmeraldLogo(t, theme);
            }
            else
            {
                Image logo = UiFactory.Image("Logo", t, theme.logo, new Vector2(0, 455), new Vector2(480, 171));
                logo.preserveAspect = true;
            }

            JackpotPanelView jackpots = emerald ? EmeraldJackpots(t, theme) : Jackpots(t, theme);
            ConsoleView console = Console(t, theme);
            SealMeterView sealMeter = SealMeter(console.transform, theme, LayoutFor(theme).Seal);
            FlameClashView flameClash = FlameClash(t, theme, machineRect);
            GuardianFireView guardianFire = GuardianFire(t, theme);
            WinStripView winStrip = emerald ? WinStrip(t, theme) : null;
            MessageToast toast = Toast(t, theme, dialogs);

            // overlays, last so they draw on top
            BigWinView bigWin = BigWin(t, theme, dialogs);
            RelicRevealView relicReveal = RelicReveal(t, theme, dialogs);
            GambleView gamble = Gamble(t, theme, dialogs);
            TreasuryView treasury = Treasury(t, theme, dialogs);
            BonusActivationView bonusActivation = emerald ? BonusActivation(t, theme, dialogs) : null;
            PaytableView paytable = Paytable(t, theme, dialogs);
            ChapterCardView chapterCard = ChapterCard(t, theme, dialogs);
            CoachMarkView coachMarks = CoachMarks(t, theme, dialogs);

            var presenter = root.AddComponent<WinPresenter>();
            var bootstrap = root.AddComponent<GameBootstrap>();
            bootstrap.Setup(machine, theme, machineView, console, jackpots, presenter, bigWin, paytable, toast, guardians,
                            sealMeter, flameClash, chapterCard, coachMarks, guardianFire, relicReveal, gamble, treasury);
            bootstrap.SetupEmerald(winStrip, bonusActivation);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // A 1920 x 1080 overlay canvas that scales to any screen and takes clicks
        private static GameObject OverlayCanvas(string name)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            root.AddComponent<GraphicRaycaster>();
            return root;
        }

        // The vault painting filling the screen, darkened at the edges
        private static void Backdrop(Transform t, ThemeDefinition theme, Color tint)
        {
            Image background = Stretch("Background", t, theme.background);
            background.color = tint;
            var fitter = background.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 16f / 9f;
            Stretch("Vignette", t, theme.vignette);
        }

        private static CharacterView[] Guardians(Transform t, ThemeDefinition theme)
        {
            if (theme.look == ThemeLook.Emerald)
            {
                return new[] { Character("Guardian", t, theme, -735f, false, 0f, new Vector2(800, 800)) };
            }

            if (theme.niche != null)
            {
                UiFactory.Image("Niche Left", t, theme.niche, new Vector2(-800, -13), new Vector2(232, 751));
                UiFactory.Image("Niche Right", t, theme.niche, new Vector2(800, -13), new Vector2(232, 751));
            }

            return new[] { Character("Guardian Left", t, theme, -785f, false, 0f), Character("Guardian Right", t, theme, 785f, true, 1.7f) };
        }

        private static Image Stretch(string name, Transform parent, Sprite sprite)
        {
            RectTransform rect = UiFactory.Stretch(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private static CharacterView Character(string name, Transform parent, ThemeDefinition theme, float x, bool mirrored, float phase,
                                               Vector2? figureSize = null)
        {
            RectTransform holder = UiFactory.Rect(name, parent, new Vector2(x, -380), new Vector2(250, 705));
            holder.pivot = new Vector2(0.5f, 0f);
            holder.anchoredPosition = new Vector2(x, -380);
            UiFactory.NestedCanvas(holder.gameObject, false);
            Image aura = UiFactory.Image("Aura", holder, theme.glow, new Vector2(0, 40), new Vector2(560, 940));
            aura.material = theme.additive;
            aura.color = theme.look == ThemeLook.Emerald
                ? new Color(theme.accent.r, theme.accent.g, theme.accent.b, 0.2f)
                : new Color(1f, 0.72f, 0.32f, 0.14f);
            Image figure = Figure(holder, mirrored, figureSize ?? new Vector2(250, 705));
            UiParticleEmitter dust = FigureDust(holder, theme);

            RectTransform hand = UiFactory.Rect("Hand", holder, Vector2.zero, Vector2.one * 10f);
            hand.anchorMin = hand.anchorMax = new Vector2(0.5f, 0f);
            Image handGlow = UiFactory.Image("Hand Glow", hand, theme.glow, Vector2.zero, Vector2.one * 130f);
            handGlow.material = theme.additive;

            var view = holder.gameObject.AddComponent<CharacterView>();
            view.Setup(figure, phase);
            view.SetupEffects(aura, hand, handGlow, HandFlame(hand, theme), HandEmbers(hand, theme), HandSparks(hand, theme), dust);
            if (figureSize.HasValue)
            {
                Image fade = Stretch("Pose Fade", figure.rectTransform, null);
                fade.preserveAspect = true;
                fade.gameObject.SetActive(false);
                view.SetupPoses(fade);
            }

            return view;
        }

        private static Image Figure(RectTransform holder, bool mirrored, Vector2 size)
        {
            Image image = UiFactory.Image("Figure", holder, null, Vector2.zero, size);
            image.rectTransform.pivot = new Vector2(0.5f, 0f);
            image.rectTransform.anchoredPosition = new Vector2(0, -352.5f);
            image.preserveAspect = true;
            image.useSpriteMesh = true;
            image.color = Color.white;
            if (mirrored)
            {
                image.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            }

            return image;
        }

        private static UiParticleEmitter FigureDust(RectTransform holder, ThemeDefinition theme)
        {
            UiParticleEmitter dust = Emitter("Gold Dust", holder, theme, theme.glow, Vector2.zero);
            dust.Rate = 2.5f;
            dust.LifeRange = new Vector2(3f, 5f);
            dust.SizeRange = new Vector2(5f, 12f);
            dust.EndSize = 0.6f;
            dust.Area = new Vector2(260f, 640f);
            dust.VelocityMin = new Vector2(-8f, 12f);
            dust.VelocityMax = new Vector2(8f, 34f);
            dust.Swirl = 10f;
            dust.SwirlFrequency = 1.5f;
            dust.MaxParticles = 30;
            dust.StartColor = new Color(1f, 0.85f, 0.5f, 0.6f);
            dust.EndColor = new Color(1f, 0.8f, 0.4f, 0f);
            return dust;
        }

        private static UiParticleEmitter HandFlame(RectTransform hand, ThemeDefinition theme)
        {
            UiParticleEmitter flame = Emitter("Flame", hand, theme, theme.glow, Vector2.zero);
            flame.Rate = 42f;
            flame.LifeRange = new Vector2(0.45f, 0.95f);
            flame.SizeRange = new Vector2(22f, 46f);
            flame.EndSize = 0.15f;
            flame.Area = new Vector2(18f, 10f);
            flame.VelocityMin = new Vector2(-25f, 70f);
            flame.VelocityMax = new Vector2(25f, 150f);
            flame.Gravity = new Vector2(0f, 60f);
            flame.Swirl = 40f;
            flame.SwirlFrequency = 9f;
            flame.MaxParticles = 110;
            return flame;
        }

        private static UiParticleEmitter HandEmbers(RectTransform hand, ThemeDefinition theme)
        {
            UiParticleEmitter embers = Emitter("Embers", hand, theme, theme.glow, Vector2.zero);
            embers.Rate = 7f;
            embers.LifeRange = new Vector2(1.2f, 2.2f);
            embers.SizeRange = new Vector2(5f, 10f);
            embers.EndSize = 0.5f;
            embers.Area = new Vector2(30f, 20f);
            embers.VelocityMin = new Vector2(-50f, 60f);
            embers.VelocityMax = new Vector2(50f, 140f);
            embers.Gravity = new Vector2(0f, -15f);
            embers.Swirl = 25f;
            embers.SwirlFrequency = 4f;
            embers.MaxParticles = 70;
            return embers;
        }

        // Silent until a big win sprays them
        private static UiParticleEmitter HandSparks(RectTransform hand, ThemeDefinition theme)
        {
            UiParticleEmitter sparks = Emitter("Sparks", hand, theme, theme.sparkle, Vector2.zero);
            sparks.Rate = 0f;
            sparks.LifeRange = new Vector2(0.8f, 1.4f);
            sparks.SizeRange = new Vector2(14f, 30f);
            sparks.EndSize = 0.2f;
            sparks.VelocityMin = new Vector2(-80f, 280f);
            sparks.VelocityMax = new Vector2(80f, 520f);
            sparks.Gravity = new Vector2(0f, -700f);
            sparks.Drag = 0.6f;
            sparks.MaxParticles = 120;
            sparks.StartColor = new Color(1f, 0.9f, 0.6f, 1f);
            sparks.EndColor = new Color(1f, 0.6f, 0.2f, 0f);
            return sparks;
        }

        // Both guardians hurl flames that meet over the reels before a big win
        private static FlameClashView FlameClash(Transform parent, ThemeDefinition theme, RectTransform machine)
        {
            RectTransform layer = UiFactory.Stretch("Flame Clash", parent);
            UiFactory.NestedCanvas(layer.gameObject, false);
            var trails = new UiParticleEmitter[2];
            var fireballs = new Image[2];
            var cores = new Image[2];
            var rings = new Image[2];
            for (int i = 0; i < 2; i++)
            {
                trails[i] = FireTrail(layer, theme, i);
                (fireballs[i], cores[i]) = Fireball(layer, theme, i);
                rings[i] = UiFactory.Image("Shockwave " + (i + 1), layer, Sprite("Generated/ring"), Vector2.zero, new Vector2(220, 220));
                rings[i].material = theme.additive;
                rings[i].gameObject.SetActive(false);
            }
            (UiParticleEmitter burst, UiParticleEmitter gold) = ClashBursts(layer, theme);
            Image flash = Stretch("Flash", layer, theme.white);
            flash.material = theme.additive;
            flash.color = new Color(1f, 1f, 1f, 0f);

            var view = layer.gameObject.AddComponent<FlameClashView>();
            view.Setup(layer, fireballs, cores, trails, flash, rings, burst, gold, machine);
            return view;
        }

        // The Sealed Relic's reveal: a card that flickers through the symbols and settles on the round's relic
        private static RelicRevealView RelicReveal(Transform parent, ThemeDefinition theme, DialogManager dialogs)
        {
            RectTransform overlay = UiFactory.Stretch("Relic Reveal", parent);
            var group = overlay.gameObject.AddComponent<CanvasGroup>();
            UiFactory.NestedCanvas(overlay.gameObject, true, group);
            Image dim = Stretch("Dim", overlay, theme.white);
            dim.color = new Color(0.01f, 0.01f, 0.02f, 0.82f);
            Button tap = UiFactory.Button(dim);
            tap.transition = Selectable.Transition.None;

            UiFactory.Text("Title", overlay, theme.headingFont, "SEALED<space=0.4em>RELIC", 56, theme.gold, new Vector2(0, 300), new Vector2(1000, 80),
                           TextAlignmentOptions.Center, 24);
            Image glow = UiFactory.Image("Glow", overlay, theme.glow, new Vector2(0, 30), new Vector2(560, 560));
            glow.material = theme.additive;
            Image card = UiFactory.Image("Card", overlay, theme.card, new Vector2(0, 30), new Vector2(340, 340));
            card.type = Image.Type.Sliced;
            Image symbol = UiFactory.Image("Symbol", card.transform, null, Vector2.zero, new Vector2(270, 270));
            symbol.preserveAspect = true;
            symbol.color = Color.white;
            UiParticleEmitter burst = Emitter("Burst", overlay, theme, theme.sparkle, new Vector2(0, 30));
            burst.Rate = 0f;
            burst.LifeRange = new Vector2(0.6f, 1.3f);
            burst.SizeRange = new Vector2(16f, 36f);
            burst.EndSize = 0.15f;
            burst.VelocityMin = new Vector2(-300f, 150f);
            burst.VelocityMax = new Vector2(300f, 600f);
            burst.Gravity = new Vector2(0f, -500f);
            burst.Drag = 0.5f;
            burst.MaxParticles = 120;
            TextMeshProUGUI caption = UiFactory.Text("Caption", overlay, theme.bodyFont, "On 3 or more reels it covers them all and pays on every line",
                                                     30, theme.ivory, new Vector2(0, -210), new Vector2(1100, 50));

            var view = overlay.gameObject.AddComponent<RelicRevealView>();
            view.Setup(card.rectTransform, symbol, glow, caption, burst, tap);
            view.SetupDialog(OverlayDialog("Relic Reveal", FadeIn("Overlay In", 0.3f), FadeOut("Overlay Out", 0.3f)), overlay, dialogs, group);
            return view;
        }

        // Guardian's Fire: flames thrown at single cells, each leaving a trail and bursting on the symbol it turns wild
        private static GuardianFireView GuardianFire(Transform parent, ThemeDefinition theme)
        {
            const int flames = 4;
            RectTransform layer = UiFactory.Stretch("Guardian Fire", parent);
            UiFactory.NestedCanvas(layer.gameObject, false);
            var trails = new UiParticleEmitter[flames];
            var fireballs = new Image[flames];
            for (int i = 0; i < flames; i++)
            {
                trails[i] = FireTrail(layer, theme, i);
                (fireballs[i], _) = Fireball(layer, theme, i);
                fireballs[i].rectTransform.sizeDelta = new Vector2(150f, 150f);
            }

            (UiParticleEmitter burst, _) = ClashBursts(layer, theme);
            var view = layer.gameObject.AddComponent<GuardianFireView>();
            view.Setup(layer, fireballs, trails, burst);
            return view;
        }

        private static UiParticleEmitter FireTrail(RectTransform layer, ThemeDefinition theme, int i)
        {
            UiParticleEmitter trail = Emitter("Trail " + (i + 1), layer, theme, theme.glow, Vector2.zero);
            trail.Rate = 150f;
            trail.LifeRange = new Vector2(0.25f, 0.55f);
            trail.SizeRange = new Vector2(26f, 54f);
            trail.EndSize = 0.1f;
            trail.Area = new Vector2(16f, 16f);
            trail.VelocityMin = new Vector2(-30f, -30f);
            trail.VelocityMax = new Vector2(30f, 30f);
            trail.Swirl = 30f;
            trail.SwirlFrequency = 12f;
            trail.MaxParticles = 140;
            trail.Intensity = 0f;
            return trail;
        }

        private static (Image, Image) Fireball(RectTransform layer, ThemeDefinition theme, int i)
        {
            Image ball = UiFactory.Image("Fireball " + (i + 1), layer, theme.glow, Vector2.zero, new Vector2(190, 190));
            ball.material = theme.additive;
            Image core = UiFactory.Image("Core", ball.transform, theme.glow, Vector2.zero, new Vector2(80, 80));
            core.material = theme.additive;
            core.color = Color.white;
            ball.gameObject.SetActive(false);
            return (ball, core);
        }

        // Blue sparks and gold embers sprayed where the flames meet
        private static (UiParticleEmitter, UiParticleEmitter) ClashBursts(RectTransform layer, ThemeDefinition theme)
        {
            UiParticleEmitter burst = Emitter("Clash Burst", layer, theme, theme.sparkle, Vector2.zero);
            burst.Rate = 0f;
            burst.LifeRange = new Vector2(0.6f, 1.3f);
            burst.SizeRange = new Vector2(16f, 40f);
            burst.EndSize = 0.15f;
            burst.VelocityMin = new Vector2(-300f, 200f);
            burst.VelocityMax = new Vector2(300f, 700f);
            burst.Gravity = new Vector2(0f, -500f);
            burst.Drag = 0.5f;
            burst.MaxParticles = 160;
            UiParticleEmitter gold = Emitter("Clash Gold", layer, theme, theme.glow, Vector2.zero);
            gold.Rate = 0f;
            gold.LifeRange = new Vector2(0.7f, 1.5f);
            gold.SizeRange = new Vector2(10f, 22f);
            gold.EndSize = 0.2f;
            gold.VelocityMin = new Vector2(-200f, 150f);
            gold.VelocityMax = new Vector2(200f, 520f);
            gold.Gravity = new Vector2(0f, -650f);
            gold.Drag = 0.4f;
            gold.MaxParticles = 90;
            gold.StartColor = new Color(1f, 0.88f, 0.55f, 1f);
            gold.EndColor = new Color(1f, 0.6f, 0.2f, 0f);
            return (burst, gold);
        }

        // The Vault Seal medallion: face, cracks and pips
        private static SealMeterView SealMeter(Transform parent, ThemeDefinition theme, Vector2 position)
        {
            RectTransform holder = UiFactory.Rect("Seal Meter", parent, position, new Vector2(120, 120));
            Image magic = UiFactory.Image("Vault Light", holder, theme.glow, Vector2.zero, new Vector2(230, 230));
            magic.material = theme.additive;
            magic.color = new Color(0f, 0f, 0f, 0f);

            RectTransform seal = UiFactory.Rect("Seal", holder, new Vector2(0, 8), new Vector2(92, 92));
            Image face = UiFactory.Image("Face", seal, null, Vector2.zero, new Vector2(92, 92));
            face.preserveAspect = true;
            Image relic = UiFactory.Image("Relic", seal, null, Vector2.zero, new Vector2(72, 72));
            relic.preserveAspect = true;
            relic.color = Color.white;
            relic.enabled = false;
            Image[] fractures = CrackLayers(seal, "Fracture", 90f, img => img.color = new Color(0.12f, 0.06f, 0.02f, 0.85f));
            Image[] cracks = CrackLayers(seal, "Crack", 84f, img => img.material = theme.additive);
            Image flash = UiFactory.Image("Flash", seal, theme.glow, Vector2.zero, new Vector2(190, 190));
            flash.material = theme.additive;
            flash.color = new Color(1f, 0.9f, 0.6f, 0f);

            var view = holder.gameObject.AddComponent<SealMeterView>();
            view.Setup(seal, face, cracks, Pips(holder, theme), flash, magic, SealSparks(seal, theme));
            view.SetupFractures(fractures);
            view.SetupRelic(relic);
            return view;
        }

        // One hidden image per crack stage
        private static Image[] CrackLayers(RectTransform seal, string name, float size, System.Action<Image> style)
        {
            var layers = new Image[3];
            for (int i = 0; i < layers.Length; i++)
            {
                layers[i] = UiFactory.Image(name + " " + (i + 1), seal, Sprite("Generated/crack_" + (i + 1)), Vector2.zero, new Vector2(size, size));
                style(layers[i]);
                layers[i].enabled = false;
            }
            return layers;
        }

        // Three small diamonds under the seal, one per landed seal
        private static Image[] Pips(RectTransform holder, ThemeDefinition theme)
        {
            var pips = new Image[3];
            for (int i = 0; i < pips.Length; i++)
            {
                pips[i] = UiFactory.Image("Pip " + (i + 1), holder, theme.white, new Vector2((i - 1) * 20f, -54f), new Vector2(8, 8));
                pips[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }
            return pips;
        }

        private static UiParticleEmitter SealSparks(RectTransform seal, ThemeDefinition theme)
        {
            UiParticleEmitter sparks = Emitter("Seal Sparks", seal, theme, theme.sparkle, Vector2.zero);
            sparks.Rate = 0f;
            sparks.LifeRange = new Vector2(0.5f, 1.1f);
            sparks.SizeRange = new Vector2(10f, 22f);
            sparks.EndSize = 0.2f;
            sparks.Area = new Vector2(40f, 40f);
            sparks.VelocityMin = new Vector2(-220f, 60f);
            sparks.VelocityMax = new Vector2(220f, 380f);
            sparks.Gravity = new Vector2(0f, -600f);
            sparks.Drag = 0.8f;
            sparks.MaxParticles = 120;
            sparks.EndColor = new Color(1f, 0.6f, 0.2f, 0f);
            return sparks;
        }

        private static UiParticleEmitter Emitter(string name, Transform parent, ThemeDefinition theme, Sprite sprite, Vector2 position)
        {
            RectTransform rect = UiFactory.Rect(name, parent, position, Vector2.one * 10f);
            var emitter = rect.gameObject.AddComponent<UiParticleEmitter>();
            emitter.Sprite = sprite;
            emitter.Material = theme.additive;
            return emitter;
        }

        // Glow, turning rays, the medallion with its light sweep, and the SPIN / STOP label
        private static SpinButtonView SpinButton(Transform parent, ThemeDefinition theme, Vector2 position)
        {
            Image glow = UiFactory.Image("Spin Glow", parent, theme.glow, position, new Vector2(330, 330));
            glow.color = new Color(1f, 0.8f, 0.4f, 0.4f);
            Image rays = UiFactory.Image("Spin Rays", parent, theme.rays, position, new Vector2(340, 340));
            rays.material = theme.additive;
            rays.color = new Color(1f, 0.82f, 0.45f, 0.35f);

            Image medallion = UiFactory.Image("Spin", parent, theme.spinButton, position, new Vector2(236, 236));
            medallion.preserveAspect = true;
            medallion.material = theme.symbolMaterial;
            Button button = UiFactory.Button(medallion);
            button.transition = Selectable.Transition.None;

            TextMeshProUGUI label = UiFactory.Text("Label", medallion.transform, theme.headingFont, "SPIN", 40, Color.white, new Vector2(0, -2), new Vector2(130, 60));
            label.enableVertexGradient = true;
            label.colorGradient = new VertexGradient(new Color32(255, 241, 193, 255), new Color32(255, 241, 193, 255), new Color32(196, 146, 62, 255), new Color32(196, 146, 62, 255));
            label.characterSpacing = 8f;

            var view = medallion.gameObject.AddComponent<SpinButtonView>();
            view.Setup(button, glow, rays, medallion, label);
            return view;
        }

        private static JackpotPanelView Jackpots(Transform parent, ThemeDefinition theme)
        {
            RectTransform panel = UiFactory.Stretch("Jackpots", parent);
            UiFactory.NestedCanvas(panel.gameObject, false);
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
                values[i] = CountingText.Attach(value);
            }
            var view = panel.gameObject.AddComponent<JackpotPanelView>();
            view.Setup(images, names, values);
            return view;
        }

        // Where each console control sits in a look
        private struct ConsoleLayout
        {
            public Vector2 Spin, Auto, AutoSize, Stepper, StepperSize, Menu, Sound, Gamble, Seal;
            public float BalanceX, DividerX, WinX, LabelY, ValueY;
        }

        // Art Deco: spin in the middle; Emerald: readouts on the left and spin in the right-hand corner, under the thumb
        private static ConsoleLayout LayoutFor(ThemeDefinition theme)
        {
            if (theme.look == ThemeLook.Emerald)
            {
                return new ConsoleLayout
                {
                    Spin = new Vector2(800, -458), Auto = new Vector2(590, -466), AutoSize = new Vector2(132, 60),
                    Stepper = new Vector2(355, -466), StepperSize = new Vector2(300, 64), Menu = new Vector2(-884, -466),
                    Sound = new Vector2(-812, -466), Gamble = new Vector2(110, -466), Seal = new Vector2(-60, -456),
                    BalanceX = -745, DividerX = -505, WinX = -470, LabelY = -444, ValueY = -482,
                };
            }

            return new ConsoleLayout
            {
                Spin = new Vector2(0, -460), Auto = new Vector2(362, -471), AutoSize = new Vector2(168, 64), Stepper = new Vector2(640, -471),
                StepperSize = new Vector2(340, 64), Menu = new Vector2(-884, -471), Sound = new Vector2(884, -471), Gamble = new Vector2(-388, -462),
                Seal = new Vector2(-250, -452), BalanceX = -810, DividerX = -596, WinX = -560, LabelY = -444, ValueY = -482,
            };
        }

        private static ConsoleView Console(Transform parent, ThemeDefinition theme)
        {
            ConsoleLayout layout = LayoutFor(theme);
            bool emerald = theme.look == ThemeLook.Emerald;
            RectTransform console = UiFactory.Stretch("Console", parent);
            UiFactory.NestedCanvas(console.gameObject, true);
            if (emerald)
            {
                EmeraldDeck(console, theme);
            }
            else
            {
                ConsoleBar(console);
            }

            (CountingText balance, TextMeshProUGUI winLabel, CountingText win) = Readouts(console, theme, layout);
            SpinButtonView spin = emerald ? EmeraldSpinButton(console, theme, layout.Spin) : SpinButton(console, theme, layout.Spin);

            Image autoPill = Pill("Auto", console, theme, layout.Auto, layout.AutoSize);
            Vector2 autoLabel = emerald ? new Vector2(16, 0) : Vector2.zero;
            TextMeshProUGUI autoText = UiFactory.Text("Label", autoPill.transform, theme.headingFont, "AUTO", 17, theme.gold, autoLabel, layout.AutoSize,
                                                      TextAlignmentOptions.Center, 22);
            Image autoIcon = null;
            if (emerald)
            {
                autoIcon = UiFactory.Image("Icon", autoPill.transform, Sprite("Generated/icon_auto"), new Vector2(-40, 0), new Vector2(26, 26));
                autoIcon.color = theme.gold;
            }

            (Button minus, Button plus, TextMeshProUGUI totalBet) = BetStepper(console, theme, layout);
            Button menu = IconButton("Menu", console, theme, theme.iconMenu, layout.Menu, out _);
            Button sound = IconButton("Sound", console, theme, theme.iconSound, layout.Sound, out Image soundIcon);

            (Button gambleButton, AttentionHighlight gambleHighlight) = GambleButton(console, theme, layout.Gamble);

            var view = console.gameObject.AddComponent<ConsoleView>();
            view.Setup(balance, winLabel, win, totalBet, autoText, spin, minus, plus, UiFactory.PressButton(autoPill), menu, sound, soundIcon);
            view.SetupGamble(gambleButton, gambleHighlight);
            if (autoIcon != null)
            {
                view.SetupAutoIcon(autoIcon);
            }

            return view;
        }

        // The gamble offer: a pill with a soft halo behind it and a band of light that sweeps across it while it waits
        private static (Button button, AttentionHighlight highlight) GambleButton(RectTransform console, ThemeDefinition theme, Vector2 position)
        {
            Vector2 size = new Vector2(136, 50);
            RectTransform holder = UiFactory.Rect("Gamble Offer", console, position, size);
            Image halo = UiFactory.Image("Halo", holder, theme.glow, Vector2.zero, new Vector2(340, 170));
            halo.material = theme.additive;
            halo.color = new Color(1f, 0.78f, 0.36f, 0f);
            Image pill = Pill("Gamble", holder, theme, Vector2.zero, size);
            RectTransform mask = UiFactory.Rect("Shine Mask", pill.transform, Vector2.zero, size - new Vector2(10, 8));
            mask.gameObject.AddComponent<RectMask2D>();
            Image shine = UiFactory.Image("Shine", mask, theme.glow, Vector2.zero, new Vector2(34, 110));
            shine.material = theme.additive;
            shine.color = new Color(1f, 0.92f, 0.7f, 0.85f);
            shine.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -22f);
            shine.gameObject.SetActive(false);
            UiFactory.Text("Label", pill.transform, theme.headingFont, "GAMBLE", 16, theme.paleGold, Vector2.zero, size,
                           TextAlignmentOptions.Center, 14);
            var highlight = holder.gameObject.AddComponent<AttentionHighlight>();
            highlight.Setup(pill.rectTransform, halo, shine.rectTransform);
            return (UiFactory.Button(pill), highlight);
        }

        // Full-width lacquer bar with a double gold rule and the spin socket
        private static void ConsoleBar(RectTransform console)
        {
            RectTransform bar = BottomStrip("Bar", console, Sprite("Generated/bar"), 150, 0, 0).rectTransform;
            BottomStrip("Rule", bar, Sprite("Generated/rule"), 3, 148, 0);
            Image inner = BottomStrip("Inner Rule", bar, Sprite("Generated/rule"), 2, 140, 160);
            inner.color = new Color(1f, 1f, 1f, 0.45f);
            UiFactory.Image("Spin Socket", console, Sprite("Generated/socket"), new Vector2(0, -460), new Vector2(320, 320));
        }

        private static (CountingText, TextMeshProUGUI, CountingText) Readouts(RectTransform console, ThemeDefinition theme, ConsoleLayout layout)
        {
            Left("Balance Label", console, theme.labelFont, "BALANCE", 15, theme.dimGold, layout.BalanceX, layout.LabelY, 28);
            TextMeshProUGUI balance = Left("Balance", console, theme.headingFont, "0", 34, theme.ivory, layout.BalanceX, layout.ValueY, 0);
            Image divider = UiFactory.Image("Divider", console, theme.white, new Vector2(layout.DividerX, (layout.LabelY + layout.ValueY) * 0.5f),
                                            new Vector2(1, 56));
            divider.color = new Color(theme.gold.r, theme.gold.g, theme.gold.b, 0.5f);
            TextMeshProUGUI winLabel = Left("Win Label", console, theme.labelFont, "WIN", 15, theme.dimGold, layout.WinX, layout.LabelY, 28);
            TextMeshProUGUI win = Left("Win", console, theme.headingFont, "0", 34, theme.paleGold, layout.WinX, layout.ValueY, 0);
            return (CountingText.Attach(balance), winLabel, CountingText.Attach(win));
        }

        private static (Button, Button, TextMeshProUGUI) BetStepper(RectTransform console, ThemeDefinition theme, ConsoleLayout layout)
        {
            Image stepper = Pill("Bet Stepper", console, theme, layout.Stepper, layout.StepperSize);
            stepper.raycastTarget = false;
            float end = layout.StepperSize.x * 0.5f - 32f;
            Button minus = RoundButton("Bet Minus", stepper.transform, theme, new Vector2(-end, 0), 48, false);
            Button plus = RoundButton("Bet Plus", stepper.transform, theme, new Vector2(end, 0), 48, true);
            UiFactory.Text("Total Bet Label", stepper.transform, theme.labelFont, "TOTAL BET", 12, theme.dimGold, new Vector2(0, 14), new Vector2(200, 20), TextAlignmentOptions.Center, 26);
            TextMeshProUGUI total = UiFactory.Text("Total Bet", stepper.transform, theme.headingFont, "0", 26, theme.ivory, new Vector2(0, -9), new Vector2(200, 36));
            return (minus, plus, total);
        }

        // A full-width strip anchored to the bottom of its parent, inset pixels in from each side
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
            Button button = UiFactory.PressButton(ring);
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
            Button button = UiFactory.PressButton(ring);
            iconImage = UiFactory.Image("Icon", ring.transform, icon, Vector2.zero, Vector2.one * 30);
            iconImage.color = Color.white;
            return button;
        }

        private static MessageToast Toast(Transform parent, ThemeDefinition theme, DialogManager dialogs)
        {
            Image pill = Pill("Message", parent, theme, new Vector2(0, -320), new Vector2(720, 64));
            pill.raycastTarget = false;
            TextMeshProUGUI text = UiFactory.Text("Text", pill.transform, theme.bodyFont, "", 28, theme.ivory, Vector2.zero, new Vector2(680, 60));
            var group = pill.gameObject.AddComponent<CanvasGroup>();
            UiFactory.NestedCanvas(pill.gameObject, false, group);
            group.blocksRaycasts = false;
            var toast = pill.gameObject.AddComponent<MessageToast>();
            toast.Setup(text);
            toast.SetupDialog(ToastDialog(), pill.rectTransform, dialogs, group);
            return toast;
        }

        private static BigWinView BigWin(Transform parent, ThemeDefinition theme, DialogManager dialogs)
        {
            RectTransform overlay = UiFactory.Stretch("Big Win", parent);
            var group = overlay.gameObject.AddComponent<CanvasGroup>();
            UiFactory.NestedCanvas(overlay.gameObject, true, group);
            Image dim = Stretch("Dim", overlay, theme.white);
            dim.color = new Color(0.02f, 0.02f, 0.03f, 0.8f);
            Button skip = UiFactory.Button(dim);
            skip.transition = Selectable.Transition.None;
            bool emerald = theme.look == ThemeLook.Emerald;
            Vector2 focus = emerald ? new Vector2(0, -35) : new Vector2(0, 90);
            Image rays = UiFactory.Image("Rays", overlay, Sprite("Generated/rays"), focus, new Vector2(1500, 1500));
            Image halo = UiFactory.Image("Halo", overlay, theme.glow, focus, new Vector2(1100, 700));
            halo.color = new Color(1f, 0.78f, 0.38f, 0.35f);
            // the Emerald look celebrates on the vault ring, in the sanctum's cyan light
            Image ring = null;
            if (emerald && theme.spinRotor != null)
            {
                halo.color = new Color(theme.accent.r, theme.accent.g, theme.accent.b, 0.3f);
                ring = UiFactory.Image("Vault Ring", overlay, theme.spinRotor, focus, new Vector2(900, 900));
                ring.preserveAspect = true;
            }


            CharacterAnimation[] motion = { theme.heroLeft, theme.heroRight };
            Vector2[] home = { new Vector2(-740, -560), new Vector2(760, -560) };
            Image[] heroes = { Hero("Hero Left", overlay, motion[0], home[0], false), Hero("Hero Right", overlay, motion[1], home[1], true) };
            Image banner = UiFactory.Image("Banner", overlay, theme.bigWinBanner, new Vector2(0, 130), new Vector2(1200, 400));
            banner.preserveAspect = true;
            // coins fall behind the words so the amount stays readable
            UiParticleEmitter coins = CoinShower(overlay, theme);
            (TextMeshProUGUI title, TextMeshProUGUI subtitle, CountingText amount) = BigWinText(overlay, theme);
            if (emerald)
            {
                title.fontSize = 104f;
            }


            var view = overlay.gameObject.AddComponent<BigWinView>();
            view.Setup(banner, title, subtitle, amount, rays.rectTransform, coins, skip);
            // the celebration pops its own heading in and keeps the heroes moving as it fades, so it has no presets
            view.SetupDialog(OverlayDialog("Celebration", null, null), overlay, dialogs, group);
            if (ring != null)
            {
                view.SetupRing(ring.rectTransform);
                view.SetupTiers(halo, ring, TierColours(theme));
            }

            if (emerald && theme.guardianPoses.Length > (int)GuardianPose.Jackpot)
            {
                (Image guardian, CanvasGroup words) = CelebrationGuardian(overlay, theme);
                view.SetupGuardian(guardian, theme.guardianPoses[(int)GuardianPose.Win], theme.guardianPoses[(int)GuardianPose.Jackpot], words);
            }

            view.SetupHeroes(heroes, motion, home);
            return view;
        }

        // Halo colours in CelebrationKind order: gold, amethyst, cyan, emerald, then the titles
        private static Color[] TierColours(ThemeDefinition theme)
        {
            Color gold = new Color32(255, 199, 97, 255);
            Color amethyst = new Color32(168, 107, 255, 255);
            Color emeraldLight = new Color32(46, 224, 150, 255);
            return new Color[] { gold, amethyst, theme.accent, emeraldLight, emeraldLight, gold, theme.accent };
        }

        // The guardian beside the heading, and its words under the amount
        private static (Image, CanvasGroup) CelebrationGuardian(RectTransform overlay, ThemeDefinition theme)
        {
            Image guardian = UiFactory.Image("Guardian", overlay, theme.guardianPoses[(int)GuardianPose.Win], new Vector2(-715, -545), new Vector2(660, 660));
            guardian.rectTransform.pivot = new Vector2(0.5f, 0f);
            guardian.rectTransform.anchoredPosition = new Vector2(-715, -545);
            guardian.preserveAspect = true;
            RectTransform words = UiFactory.Rect("Guardian Reaction", overlay, new Vector2(0, -225), new Vector2(900, 80));
            var group = words.gameObject.AddComponent<CanvasGroup>();
            UiFactory.Text("Approves", words, theme.headingFont, "THE GUARDIAN APPROVES", 30, theme.paleGold, new Vector2(0, 18), new Vector2(900, 40),
                           TextAlignmentOptions.Center, 16);
            UiFactory.Text("Line", words, theme.bodyFont, "A royal victory, worthy of the vault.", 23, theme.ivory, new Vector2(0, -20), new Vector2(900, 32));
            return (guardian, group);
        }

        // Gold coins thrown up from the foot of the screen, tumbling as they fall
        private static UiParticleEmitter CoinShower(RectTransform overlay, ThemeDefinition theme)
        {
            UiParticleEmitter coins = Emitter("Coins", overlay, theme, theme.coin, new Vector2(0, -600));
            coins.Material = null;
            coins.Fades = false;
            coins.Rate = 22f;
            coins.MaxParticles = 70;
            coins.LifeRange = new Vector2(2.6f, 2.6f);
            coins.SizeRange = new Vector2(46f, 96f);
            coins.EndSize = 1f;
            coins.Area = new Vector2(520f, 0f);
            coins.VelocityMin = new Vector2(-520f, 900f);
            coins.VelocityMax = new Vector2(520f, 1500f);
            coins.Gravity = new Vector2(0f, -1500f);
            coins.SpinRange = new Vector2(-360f, 360f);
            coins.FlipRange = new Vector2(4f, 9f);
            return coins;
        }

        // A hero beside the banner, feet at the foot of the screen, sized to its baked frames
        private static Image Hero(string name, RectTransform overlay, CharacterAnimation motion, Vector2 home, bool mirrored)
        {
            const float height = 740f;
            Image hero = UiFactory.Image(name, overlay, null, home, new Vector2(300, height));
            hero.rectTransform.pivot = new Vector2(0.5f, 0f);
            hero.useSpriteMesh = true;
            hero.color = Color.white;
            if (motion != null && motion.IsValid)
            {
                Sprite first = motion.idle.frames[0];
                hero.rectTransform.sizeDelta = new Vector2(height * first.rect.width / first.rect.height, height);
            }
            if (mirrored)
            {
                hero.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            }

            hero.gameObject.SetActive(false);
            return hero;
        }

        private static (TextMeshProUGUI, TextMeshProUGUI, CountingText) BigWinText(RectTransform overlay, ThemeDefinition theme)
        {
            var brown = new Color32(58, 37, 10, 255);
            TextMeshProUGUI title = UiFactory.Text("Title", overlay, theme.displayFont, "MEGA WIN", 128, Color.white, new Vector2(0, 130), new Vector2(1700, 180));
            title.enableVertexGradient = true;
            Color32 light = new Color32(255, 241, 193, 255), dark = new Color32(184, 135, 59, 255);
            title.colorGradient = new VertexGradient(light, light, dark, dark);
            title.outlineWidth = 0.12f;
            title.outlineColor = brown;
            TextMeshProUGUI subtitle = UiFactory.Text("Subtitle", overlay, theme.headingFont, "", 40, theme.paleGold, new Vector2(0, 20), new Vector2(1400, 60), TextAlignmentOptions.Center, 20);
            TextMeshProUGUI amount = UiFactory.Text("Amount", overlay, theme.displayFont, "0", 112, theme.paleGold, new Vector2(0, -90), new Vector2(1400, 150));
            amount.outlineWidth = 0.1f;
            amount.outlineColor = brown;
            return (title, subtitle, CountingText.Attach(amount));
        }

        // A full-screen click catcher that never tints
        private static Button TapButton(Image graphic)
        {
            Button button = UiFactory.Button(graphic);
            button.transition = Selectable.Transition.None;
            return button;
        }

        // A framed story chapter over the game: art in a window, chapter, title and typed beats
        private static ChapterCardView ChapterCard(Transform parent, ThemeDefinition theme, DialogManager dialogs)
        {
            RectTransform root = UiFactory.Stretch("Chapter Card", parent);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            UiFactory.NestedCanvas(root.gameObject, true, group);
            Image dim = Stretch("Dim", root, theme.white);
            dim.color = new Color(0.01f, 0.01f, 0.02f, 0.78f);
            dim.raycastTarget = true;

            // the Art Deco frame's ornaments fill its margins; the plain Emerald panel is cut to the story
            Vector2 panelSize = theme.look == ThemeLook.Emerald ? new Vector2(1150, 640) : new Vector2(1320, 820);
            Image panel = UiFactory.Image("Panel", root, theme.popupPanel, new Vector2(0, 10), panelSize);
            panel.type = Image.Type.Sliced;
            StoryCardView card = ChapterStory(panel.transform, theme);
            TextMeshProUGUI prompt = UiFactory.Text("Continue", root, theme.labelFont, "TAP TO CONTINUE", 15, theme.gold, new Vector2(0, -448), new Vector2(400, 30), TextAlignmentOptions.Center, 30);

            var view = root.gameObject.AddComponent<ChapterCardView>();
            view.Setup(card, TapButton(dim), prompt.gameObject.AddComponent<CanvasGroup>());
            view.SetupDialog(OverlayDialog("Chapter Card", CardIn(), FadeOut("Card Out", 0.35f)), panel.rectTransform, dialogs, group);
            return view;
        }

        // The chapter's story card laid out inside the frame's opening (1052 x 501 around y = 15): art above the words
        private static StoryCardView ChapterStory(Transform panel, ThemeDefinition theme)
        {
            RectTransform rect = UiFactory.Rect("Story", panel, Vector2.zero, new Vector2(1320, 820));
            var group = rect.gameObject.AddComponent<CanvasGroup>();
            RectTransform window = UiFactory.Rect("Art Window", rect, new Vector2(0, 96), new Vector2(1012, 300));
            window.gameObject.AddComponent<RectMask2D>();
            Image art = Stretch("Art", window, null);
            var fitter = art.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 1.5f;

            TextMeshProUGUI chapter = UiFactory.Text("Chapter", rect, theme.labelFont, "CHAPTER III", 17, theme.gold, new Vector2(0, -74), new Vector2(900, 28), TextAlignmentOptions.Center, 40);
            TextMeshProUGUI title = UiFactory.Text("Title", rect, theme.headingFont, "Title", 44, theme.paleGold, new Vector2(0, -114), new Vector2(980, 56));
            TextMeshProUGUI caption = UiFactory.Text("Caption", rect, theme.bodyFont, "", 28, new Color(0.9f, 0.86f, 0.8f, 1f), new Vector2(0, -185), new Vector2(960, 70), TextAlignmentOptions.Top);
            caption.textWrappingMode = TextWrappingModes.Normal;
            var typewriter = TypewriterText.Attach(caption);

            var view = rect.gameObject.AddComponent<StoryCardView>();
            view.Setup(group, art, new Image[0], chapter, title, caption, typewriter);
            return view;
        }

        // Four shades around a spotlight, a ring of light and a message bubble
        private static CoachMarkView CoachMarks(Transform parent, ThemeDefinition theme, DialogManager dialogs)
        {
            RectTransform root = UiFactory.Stretch("Coach Marks", parent);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            UiFactory.NestedCanvas(root.gameObject, true, group);
            Image tap = Stretch("Tap", root, theme.white);
            tap.color = new Color(0f, 0f, 0f, 0f);
            tap.raycastTarget = true;
            var shades = new RectTransform[4];
            for (int i = 0; i < 4; i++)
            {
                Image shade = UiFactory.Image("Shade", root, theme.white, Vector2.zero, Vector2.zero);
                shade.color = new Color(0.01f, 0.01f, 0.02f, 0.72f);
                shades[i] = shade.rectTransform;
            }
            Image ring = UiFactory.Image("Ring", root, Sprite("Generated/ring"), Vector2.zero, Vector2.one * 200f);
            ring.material = theme.additive;
            ring.color = new Color(1f, 0.85f, 0.5f, 1f);

            Image bubble = UiFactory.Image("Bubble", root, theme.popupPanel, Vector2.zero, new Vector2(880, 250));
            bubble.type = Image.Type.Sliced;
            bubble.pixelsPerUnitMultiplier = 3f;
            // a guardian's portrait at the bubble's left, when the art is there, moves the words to its right
            float textX = 0f;
            if (theme.guidePortrait != null)
            {
                Image portrait = UiFactory.Image("Guide", bubble.transform, theme.guidePortrait, new Vector2(-330, 8), new Vector2(230, 230));
                portrait.preserveAspect = true;
                textX = 95f;
            }

            TextMeshProUGUI message = UiFactory.Text("Message", bubble.transform, theme.bodyFont, "", 27, theme.ivory, new Vector2(textX, 12),
                                                     new Vector2(theme.guidePortrait != null ? 600 : 680, 104));
            message.textWrappingMode = TextWrappingModes.Normal;
            UiFactory.Text("Continue", bubble.transform, theme.labelFont, "TAP TO CONTINUE", 13, theme.gold, new Vector2(0, -60), new Vector2(400, 24), TextAlignmentOptions.Center, 30);

            var view = root.gameObject.AddComponent<CoachMarkView>();
            view.Setup(shades, ring.rectTransform, bubble.rectTransform, message, TapButton(tap));
            view.SetupDialog(OverlayDialog("Coach Mark", FadeIn("Hint In", 0.3f), FadeOut("Hint Out", 0.25f, Ease.Linear)), root, dialogs, group);
            return view;
        }

        // The pay table window; the dialog manager's shared backdrop darkens the game behind it and closes it on a tap
        private static PaytableView Paytable(Transform parent, ThemeDefinition theme, DialogManager dialogs)
        {
            RectTransform window = UiFactory.Stretch("Pay Table", parent);
            var group = window.gameObject.AddComponent<CanvasGroup>();
            UiFactory.NestedCanvas(window.gameObject, true, group);

            Image panel = UiFactory.Image("Panel", window, theme.popupPanel, new Vector2(0, 10), new Vector2(1440, 900), true);
            panel.type = Image.Type.Sliced;
            // everything sits inside the panel's dark centre, clear of the gold ornaments
            UiFactory.Text("Title", panel.transform, theme.headingFont, "PAY TABLE", 40, theme.gold, new Vector2(0, 248), new Vector2(800, 60), TextAlignmentOptions.Center, 22);
            RectTransform content = UiFactory.Rect("Content", panel.transform, Vector2.zero, new Vector2(1200, 580));
            Button close = IconButton("Close", panel.transform, theme, theme.iconClose, new Vector2(560, 248), out _);

            var view = window.gameObject.AddComponent<PaytableView>();
            view.Setup(content, close);
            view.SetupDialog(PaytableDialog(), panel.rectTransform, dialogs, group);
            return view;
        }
    }
}
