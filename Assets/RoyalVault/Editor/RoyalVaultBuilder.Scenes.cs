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
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            _ = events;
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

        /// <summary>The loading screen: the vault behind, the logo, a slim gold bar with a bright leading tip.</summary>
        private static void BuildBootScene(ThemeDefinition theme)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Camera();

            var root = new GameObject("Loading", typeof(RectTransform));
            Canvas canvas = root.AddComponent<Canvas>();
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
            RectTransform dust = UiFactory.Stretch("Gold Dust", t);

            Image halo = UiFactory.Image("Halo", t, theme.glow, new Vector2(0, 110), new Vector2(1300, 520));
            halo.color = new Color(1f, 0.75f, 0.35f, 0.22f);
            Image logo = UiFactory.Image("Logo", t, theme.logo, new Vector2(0, 110), new Vector2(760, 270));
            logo.preserveAspect = true;

            // the bar: the gold track, the fill inside it, a light sweeping along the fill and a bright tip
            Image track = UiFactory.Image("Bar", t, theme.levelBar, new Vector2(0, -120), new Vector2(780, 64));
            track.type = Image.Type.Sliced;
            track.pixelsPerUnitMultiplier = 131f / 64f;
            RectTransform fillArea = UiFactory.Rect("Fill Area", track.transform, Vector2.zero, new Vector2(734, 22));
            fillArea.gameObject.AddComponent<RectMask2D>();
            Image fill = UiFactory.Image("Fill", fillArea, theme.levelFill, Vector2.zero, new Vector2(734, 22));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            Image shine = UiFactory.Image("Shine", fillArea, theme.glow, Vector2.zero, new Vector2(170, 46));
            shine.color = new Color(1f, 1f, 0.92f, 0.55f);
            Image tip = UiFactory.Image("Tip", track.transform, theme.glow, Vector2.zero, new Vector2(70, 70));
            tip.color = new Color(1f, 0.93f, 0.7f, 0.9f);

            TextMeshProUGUI percent = UiFactory.Text("Percent", t, theme.headingFont, "0%", 26, theme.paleGold, new Vector2(0, -178), new Vector2(200, 40));
            TextMeshProUGUI tipText = UiFactory.Text("Tip Text", t, theme.labelFont, "", 19, theme.dimGold, new Vector2(0, -222), new Vector2(900, 30), TextAlignmentOptions.Center, 30);

            var screen = root.AddComponent<LoadingScreen>();
            screen.Setup(group, logo.rectTransform, fillArea, fill, tip.rectTransform, shine.rectTransform, percent, tipText, dust, theme.glow, theme.loadingTips);

            var entry = new GameObject("AppEntry").AddComponent<AppEntry>();
            entry.Setup(screen, "Game");
            EditorSceneManager.SaveScene(scene, ScenePath + "Boot.unity");
        }
    }
}
