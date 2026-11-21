using RoyalVault.Unity;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RoyalVault.EditorTools
{
    public static partial class RoyalVaultBuilder
    {
        private const string ScenePath = Root + "Scenes/";

        /// <summary>
        /// Boot (the loading screen, first in the build), Game (the Royal Vault machine) and Game_Classic3x3 (the same
        /// prefab playing the 3 x 3 machine, to show a different grid is only a different asset).
        /// </summary>
        private static void BuildScenes(GameObject gamePrefab, ThemeDefinition theme, MachineDefinitionAsset classic)
        {
            // opening a scene unloads unused assets, so the scenes take asset paths and load what they need themselves
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

        // Without these, buttons on the scene's canvas never hear a click.
        private static void Events() => new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        // First-launch intro: its own story cards, letterbox bars, skip, continue and enter.
        private static StoryIntroView StoryIntro(Transform parent, ThemeDefinition theme)
        {
            RectTransform root = UiFactory.Stretch("Story Intro", parent);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            Image tap = Stretch("Tap", root, theme.white);
            tap.color = new Color(0f, 0f, 0f, 1f);
            tap.raycastTarget = true;
            var cards = new[] { StoryCard("Intro Card A", root, theme), StoryCard("Intro Card B", root, theme) };

            RectTransform letterbox = UiFactory.Stretch("Letterbox", root);
            RectTransform top = Bar("Top Bar", letterbox, theme, 1f);
            RectTransform bottom = Bar("Bottom Bar", letterbox, theme, 0f);
            Button skip = TextButton("Skip", letterbox, theme, "SKIP STORY", new Vector2(820, 492), 16);
            TextMeshProUGUI prompt = UiFactory.Text("Continue", letterbox, theme.labelFont, "TAP TO CONTINUE", 16, theme.gold, new Vector2(760, -492), new Vector2(340, 40), TextAlignmentOptions.Right, 30);
            var promptGroup = prompt.gameObject.AddComponent<CanvasGroup>();
            Button enter = EnterButton(letterbox, theme);

            var view = root.gameObject.AddComponent<StoryIntroView>();
            view.Setup(group, cards, top, bottom, TapButton(tap), skip, promptGroup, enter);
            return view;
        }

        // A black letterbox bar pinned to the top (1) or bottom (0) edge; the intro animates its height.
        private static RectTransform Bar(string name, Transform parent, ThemeDefinition theme, float edge)
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

        /// <summary>A full-screen story scene: backdrop, up to three standing figures, a fade and the captions.</summary>
        private static StoryCardView StoryCard(string name, Transform parent, ThemeDefinition theme)
        {
            RectTransform rect = UiFactory.Stretch(name, parent);
            var group = rect.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;

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

        /// <summary>The loading screen: the vault behind, the logo, a slim gold bar with a bright leading tip.</summary>
        private static void BuildBootScene(ThemeDefinition theme)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Camera();
            Events();

            var root = new GameObject("Loading", typeof(RectTransform));
            Canvas canvas = root.AddComponent<Canvas>();
            root.AddComponent<GraphicRaycaster>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var group = root.AddComponent<CanvasGroup>();
            Transform t = root.transform;

            Image background = Stretch("Background", t, theme.background);
            background.color = new Color(0.45f, 0.42f, 0.4f, 1f);
            var fitter = background.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 16f / 9f;
            Stretch("Vignette", t, theme.vignette);

            // with a story, its scenes fill the screen and the logo and bar make room for the captions
            bool told = theme.story != null && theme.story.At(StoryMoment.Intro).Length > 0;
            StoryCardView[] cards = told ? new[] { StoryCard("Story Card A", t, theme), StoryCard("Story Card B", t, theme) } : new StoryCardView[0];
            RectTransform dust = UiFactory.Stretch("Gold Dust", t);
            RectTransform chromeRect = UiFactory.Stretch("Story Chrome", t);
            var chrome = chromeRect.gameObject.AddComponent<CanvasGroup>();
            Image topFade = Stretch("Top Fade", chromeRect, AssetDatabase.LoadAssetAtPath<Sprite>(Generated + "fade.png"));
            topFade.rectTransform.anchorMin = new Vector2(0f, 1f);
            topFade.rectTransform.anchorMax = new Vector2(1f, 1f);
            topFade.rectTransform.sizeDelta = new Vector2(0f, 340f);
            topFade.rectTransform.anchoredPosition = new Vector2(0f, -170f);
            topFade.rectTransform.localScale = new Vector3(1f, -1f, 1f);
            topFade.color = new Color(1f, 1f, 1f, 0.75f);

            Vector2 logoAt = told ? new Vector2(0, 430) : new Vector2(0, 110);
            Vector2 logoSize = told ? new Vector2(400, 142) : new Vector2(760, 270);
            Image halo = UiFactory.Image("Halo", t, theme.glow, logoAt, logoSize * new Vector2(1.7f, 1.9f));
            halo.color = new Color(1f, 0.75f, 0.35f, told ? 0.16f : 0.22f);
            Image logo = UiFactory.Image("Logo", t, theme.logo, logoAt, logoSize);
            logo.preserveAspect = true;

            // the bar: the gold track, the fill inside it, a light sweeping along the fill and a bright tip
            float barY = told ? -452f : -120f;
            Image track = UiFactory.Image("Bar", t, theme.levelBar, new Vector2(0, barY), new Vector2(told ? 620 : 780, told ? 50 : 64));
            track.type = Image.Type.Sliced;
            track.pixelsPerUnitMultiplier = 131f / 64f;
            Vector2 fillSize = told ? new Vector2(584, 17) : new Vector2(734, 22);
            RectTransform fillArea = UiFactory.Rect("Fill Area", track.transform, Vector2.zero, fillSize);
            fillArea.gameObject.AddComponent<RectMask2D>();
            Image fill = UiFactory.Image("Fill", fillArea, theme.levelFill, Vector2.zero, fillSize);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            Image shine = UiFactory.Image("Shine", fillArea, theme.glow, Vector2.zero, new Vector2(170, 46));
            shine.color = new Color(1f, 1f, 0.92f, 0.55f);
            Image tip = UiFactory.Image("Tip", track.transform, theme.glow, Vector2.zero, new Vector2(70, 70));
            tip.color = new Color(1f, 0.93f, 0.7f, 0.9f);

            TextMeshProUGUI percent = told
                ? UiFactory.Text("Percent", t, theme.headingFont, "0%", 20, theme.paleGold, new Vector2(370, barY), new Vector2(90, 30), TextAlignmentOptions.Left)
                : UiFactory.Text("Percent", t, theme.headingFont, "0%", 26, theme.paleGold, new Vector2(0, -178), new Vector2(200, 40));
            TextMeshProUGUI tipText = UiFactory.Text("Tip Text", t, theme.labelFont, "", told ? 15 : 19, theme.dimGold,
                                                     new Vector2(0, told ? -498 : -222), new Vector2(900, 30), TextAlignmentOptions.Center, 30);

            var screen = root.AddComponent<LoadingScreen>();
            screen.Setup(group, logo.rectTransform, fillArea, fill, tip.rectTransform, shine.rectTransform, percent, tipText, dust, theme.glow, theme.loadingTips);
            StoryIntroView intro = told ? StoryIntro(t, theme) : null;
            screen.SetupStory(told ? theme.story : null, cards, chrome, intro);
            var sound = new GameObject("Sound").AddComponent<SoundPlayer>();
            sound.Setup(theme.sounds);
            screen.SetupSound(sound);

            var entry = new GameObject("AppEntry").AddComponent<AppEntry>();
            entry.Setup(screen, "Game");
            EditorSceneManager.SaveScene(scene, ScenePath + "Boot.unity");
        }
    }
}
