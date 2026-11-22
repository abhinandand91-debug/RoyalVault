using GameKit;
using RoyalVault.Unity;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine;

namespace RoyalVault.EditorTools
{
    public static partial class RoyalVaultBuilder
    {
        private const string ScenePath = Root + "Scenes/";

        // Builds the Boot and Game scenes
        private static void BuildScenes(GameObject gamePrefab, ThemeDefinition theme, MachineDefinitionAsset classic)
        {
            // opening a scene unloads assets, so each scene loads its own
            string prefabPath = AssetDatabase.GetAssetPath(gamePrefab);
            string classicPath = AssetDatabase.GetAssetPath(classic);
            EnsureFolder(ScenePath);
            BuildBootScene(theme);
            BuildGameScene("Game", prefabPath, null);
            BuildGameScene("Game_Classic3x3", prefabPath, classicPath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath + "Boot.unity", true),
                new EditorBuildSettingsScene(ScenePath + "Game.unity", true),
                new EditorBuildSettingsScene(ScenePath + "Game_Classic3x3.unity", true),
            };
        }

        private static void BuildGameScene(string name, string prefabPath, string machinePath)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var gamePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            MachineDefinitionAsset machineOverride = machinePath != null ? AssetDatabase.LoadAssetAtPath<MachineDefinitionAsset>(machinePath) : null;
            Camera();
            Events();
            var game = (GameObject)PrefabUtility.InstantiatePrefab(gamePrefab, scene);
            if (machineOverride != null)
            {
                // a prefab override on the instance: the scene plays a different machine with the same prefab
                var serialized = new SerializedObject(game.GetComponent<GameBootstrap>());
                serialized.FindProperty("machine").objectReferenceValue = machineOverride;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
            }
            EditorSceneManager.SaveScene(scene, ScenePath + name + ".unity");
        }

        private static void Camera()
        {
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
        }

        // Without these, buttons on the scene's canvas never hear a click
        private static void Events() => new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        // First-launch intro: its own story cards, letterbox bars, skip, continue and enter
        private static StoryIntroView StoryIntro(Transform parent, ThemeDefinition theme)
        {
            RectTransform root = UiFactory.Stretch("Story Intro", parent);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            UiFactory.NestedCanvas(root.gameObject, true, true);
            Image tap = Stretch("Tap", root, theme.white);
            tap.color = new Color(0f, 0f, 0f, 1f);
            tap.raycastTarget = true;
            var cards = new[] { StoryCard("Intro Card A", root, theme), StoryCard("Intro Card B", root, theme) };

            RectTransform letterbox = UiFactory.Stretch("Letterbox", root);
            RectTransform top = LetterboxBar("Top Bar", letterbox, theme, 1f);
            RectTransform bottom = LetterboxBar("Bottom Bar", letterbox, theme, 0f);
            Button skip = TextButton("Skip", letterbox, theme, "SKIP STORY", new Vector2(820, 492), 16);
            TextMeshProUGUI prompt = UiFactory.Text("Continue", letterbox, theme.labelFont, "TAP TO CONTINUE", 16, theme.gold, new Vector2(760, -492), new Vector2(340, 40), TextAlignmentOptions.Right, 30);
            var promptGroup = prompt.gameObject.AddComponent<CanvasGroup>();
            Button enter = EnterButton(letterbox, theme);

            var view = root.gameObject.AddComponent<StoryIntroView>();
            view.Setup(group, cards, top, bottom, TapButton(tap), skip, promptGroup, enter);
            return view;
        }

        // A black letterbox bar pinned to the top (1) or bottom (0) edge; the intro animates its height
        private static RectTransform LetterboxBar(string name, Transform parent, ThemeDefinition theme, float edge)
        {
            Image bar = UiFactory.Image(name, parent, theme.white, Vector2.zero, Vector2.zero);
            bar.color = Color.black;
            RectTransform r = bar.rectTransform;
            r.anchorMin = new Vector2(0f, edge);
            r.anchorMax = new Vector2(1f, edge);
            r.pivot = new Vector2(0.5f, edge);
            r.anchoredPosition = Vector2.zero;
            r.sizeDelta = Vector2.zero;
            return r;
        }

        private static Button TextButton(string name, Transform parent, ThemeDefinition theme, string text, Vector2 position, float size)
        {
            TextMeshProUGUI label = UiFactory.Text(name, parent, theme.labelFont, text, size, theme.dimGold, position, new Vector2(260, 44), TextAlignmentOptions.Center, 30);
            label.raycastTarget = true;
            var button = label.gameObject.AddComponent<Button>();
            button.targetGraphic = label;
            return button;
        }

        private static Button EnterButton(Transform parent, ThemeDefinition theme)
        {
            Image pill = UiFactory.Image("Enter", parent, theme.pillButton, new Vector2(0, -470), new Vector2(380, 76), true);
            pill.type = Image.Type.Sliced;
            UiFactory.Text("Label", pill.transform, theme.headingFont, "ENTER THE VAULT", 22, theme.paleGold, Vector2.zero, new Vector2(380, 76), TextAlignmentOptions.Center, 26);
            Button button = UiFactory.Button(pill);
            pill.gameObject.SetActive(false);
            return button;
        }

        // A full-screen story scene: backdrop, up to three standing figures, a fade and the captions
        private static StoryCardView StoryCard(string name, Transform parent, ThemeDefinition theme)
        {
            RectTransform rect = UiFactory.Stretch(name, parent);
            var group = rect.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            UiFactory.NestedCanvas(rect.gameObject, false, true);

            Image art = Stretch("Art", rect, null);
            var fitter = art.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 1.5f;

            var figures = new Image[3];
            for (int i = 0; i < figures.Length; i++)
            {
                figures[i] = UiFactory.Image("Figure " + (i + 1), rect, null, Vector2.zero, new Vector2(300, 760));
                figures[i].rectTransform.pivot = new Vector2(0.5f, 0f);
                figures[i].gameObject.SetActive(false);
            }

            Image fade = Stretch("Fade", rect, AssetDatabase.LoadAssetAtPath<Sprite>(Generated + "fade.png"));
            fade.color = new Color(1f, 1f, 1f, 0.95f);

            TextMeshProUGUI chapter = UiFactory.Text("Chapter", rect, theme.labelFont, "CHAPTER I", 18, theme.gold, new Vector2(0, -222), new Vector2(900, 28),
                                                     TextAlignmentOptions.Center, 40);
            TextMeshProUGUI title = UiFactory.Text("Title", rect, theme.headingFont, "Title", 52, theme.paleGold, new Vector2(0, -276), new Vector2(1100, 70));
            TextMeshProUGUI caption = UiFactory.Text("Caption", rect, theme.bodyFont, "Caption", 30, new Color(0.9f, 0.86f, 0.8f, 1f),
                                                     new Vector2(0, -358), new Vector2(880, 96), TextAlignmentOptions.Top);
            caption.textWrappingMode = TextWrappingModes.Normal;
            caption.lineSpacing = -6f;
            var typewriter = caption.gameObject.AddComponent<TypewriterText>();

            var view = rect.gameObject.AddComponent<StoryCardView>();
            view.Setup(group, art, figures, chapter, title, caption, typewriter);
            return view;
        }

        // The loading screen: the vault behind, the logo, a slim gold bar with a bright leading tip
        private static void BuildBootScene(ThemeDefinition theme)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Camera();
            Events();
            GameObject root = OverlayCanvas("Loading");
            var group = root.AddComponent<CanvasGroup>();
            Transform t = root.transform;
            Backdrop(t, theme, new Color(0.45f, 0.42f, 0.4f, 1f));

            // with a story, its scenes fill the screen and the logo and bar make room for the captions
            bool told = theme.story != null && theme.story.At(StoryMoment.Intro).Length > 0;
            StoryCardView[] cards = told ? new[] { StoryCard("Story Card A", t, theme), StoryCard("Story Card B", t, theme) } : new StoryCardView[0];
            RectTransform dust = UiFactory.Stretch("Gold Dust", t);
            UiFactory.NestedCanvas(dust.gameObject, false);
            CanvasGroup chrome = StoryChrome(t);
            RectTransform logo = Logo(t, theme, told);
            LoadingBar bar = ProgressBar(t, theme, told);

            var screen = t.gameObject.AddComponent<LoadingScreen>();
            screen.Setup(group, logo, bar.fillArea, bar.fill, bar.tip, bar.shine, bar.percent, bar.tipText, dust, theme.glow, theme.loadingTips);
            screen.SetupStory(told ? theme.story : null, cards, chrome, told ? StoryIntro(t, theme) : null);
            var sound = new GameObject("Sound").AddComponent<SoundPlayer>();
            sound.Setup(theme.sounds);
            screen.SetupSound(sound);
            new GameObject("AppEntry").AddComponent<AppEntry>().Setup(screen, "Game");
            EditorSceneManager.SaveScene(scene, ScenePath + "Boot.unity");
        }

        // A dark fade along the top edge behind the story's logo
        private static CanvasGroup StoryChrome(Transform t)
        {
            RectTransform chrome = UiFactory.Stretch("Story Chrome", t);
            Image fade = Stretch("Top Fade", chrome, AssetDatabase.LoadAssetAtPath<Sprite>(Generated + "fade.png"));
            RectTransform r = fade.rectTransform;
            r.anchorMin = new Vector2(0f, 1f);
            r.anchorMax = new Vector2(1f, 1f);
            r.sizeDelta = new Vector2(0f, 340f);
            r.anchoredPosition = new Vector2(0f, -170f);
            r.localScale = new Vector3(1f, -1f, 1f);
            fade.color = new Color(1f, 1f, 1f, 0.75f);
            return chrome.gameObject.AddComponent<CanvasGroup>();
        }

        private static RectTransform Logo(Transform t, ThemeDefinition theme, bool told)
        {
            Vector2 at = told ? new Vector2(0, 430) : new Vector2(0, 110);
            Vector2 size = told ? new Vector2(400, 142) : new Vector2(760, 270);
            Image halo = UiFactory.Image("Halo", t, theme.glow, at, size * new Vector2(1.7f, 1.9f));
            halo.color = new Color(1f, 0.75f, 0.35f, told ? 0.16f : 0.22f);
            if (theme.look == ThemeLook.Emerald)
            {
                return TextLogo(t, theme, at, size);
            }

            Image logo = UiFactory.Image("Logo", t, theme.logo, at, size);
            logo.preserveAspect = true;
            return logo.rectTransform;
        }

        // The Emerald title in Cinzel with the sanctum's name beneath, sized to the logo's box
        private static RectTransform TextLogo(Transform t, ThemeDefinition theme, Vector2 at, Vector2 size)
        {
            RectTransform logo = UiFactory.Rect("Logo", t, at, size);
            TextMeshProUGUI title = UiFactory.Text("Title", logo, theme.headingFont, "ROYAL VAULT", size.y * 0.42f, Color.white,
                                                   new Vector2(0, size.y * 0.1f), new Vector2(size.x * 1.4f, size.y * 0.55f), TextAlignmentOptions.Center, 6);
            title.enableVertexGradient = true;
            title.colorGradient = new VertexGradient(theme.paleGold, theme.paleGold, theme.gold, theme.gold);
            UiFactory.Text("Subtitle", logo, theme.labelFont, "THE<space=1.2em>EMERALD<space=1.2em>SANCTUM", size.y * 0.11f, theme.dimGold,
                           new Vector2(0, -size.y * 0.26f), new Vector2(size.x * 1.2f, size.y * 0.2f), TextAlignmentOptions.Center, 24);
            return logo;
        }

        private struct LoadingBar
        {
            public RectTransform fillArea, tip, shine;
            public Image fill;
            public TextMeshProUGUI percent, tipText;
        }

        // The gold track, its fill, a light sweeping along it, a bright tip, the percentage and the tips line
        private static LoadingBar ProgressBar(Transform t, ThemeDefinition theme, bool told)
        {
            float y = told ? -452f : -120f;
            Image track = UiFactory.Image("Bar", t, theme.levelBar, new Vector2(0, y), new Vector2(told ? 620 : 780, told ? 50 : 64));
            track.type = Image.Type.Sliced;
            track.pixelsPerUnitMultiplier = 131f / 64f;
            Vector2 fillSize = told ? new Vector2(584, 17) : new Vector2(734, 22);
            var bar = new LoadingBar { fillArea = UiFactory.Rect("Fill Area", track.transform, Vector2.zero, fillSize) };
            bar.fillArea.gameObject.AddComponent<RectMask2D>();
            bar.fill = UiFactory.Image("Fill", bar.fillArea, theme.levelFill, Vector2.zero, fillSize);
            bar.fill.type = Image.Type.Filled;
            bar.fill.fillMethod = Image.FillMethod.Horizontal;
            bar.fill.fillOrigin = 0;
            Image shine = UiFactory.Image("Shine", bar.fillArea, theme.glow, Vector2.zero, new Vector2(170, 46));
            shine.color = new Color(1f, 1f, 0.92f, 0.55f);
            bar.shine = shine.rectTransform;
            Image tip = UiFactory.Image("Tip", track.transform, theme.glow, Vector2.zero, new Vector2(70, 70));
            tip.color = new Color(1f, 0.93f, 0.7f, 0.9f);
            bar.tip = tip.rectTransform;
            BarText(ref bar, t, theme, told, y);
            return bar;
        }

        private static void BarText(ref LoadingBar bar, Transform t, ThemeDefinition theme, bool told, float y)
        {
            bar.percent = told
                ? UiFactory.Text("Percent", t, theme.headingFont, "0%", 20, theme.paleGold, new Vector2(370, y), new Vector2(90, 30), TextAlignmentOptions.Left)
                : UiFactory.Text("Percent", t, theme.headingFont, "0%", 26, theme.paleGold, new Vector2(0, -178), new Vector2(200, 40));
            bar.tipText = UiFactory.Text("Tip Text", t, theme.labelFont, "", told ? 15 : 19, theme.dimGold,
                                         new Vector2(0, told ? -498 : -222), new Vector2(900, 30), TextAlignmentOptions.Center, 30);
        }
    }
}
