using GameKit;
using PrimeTween;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalVault.EditorTools
{
    // The dialog manager with its shared backdrop, and the motion presets and dialog definitions it plays, saved as assets
    // so they can be tuned in the Inspector
    public static partial class RoyalVaultBuilder
    {
        private const string MotionPath = DataPath + "Motion/";
        private const string DialogPath = DataPath + "Dialogs/";

        // One manager on the game's root; its backdrop shares the dialogs' parent so it can sit just beneath them
        private static DialogManager Dialogs(GameObject root)
        {
            Image backdrop = Stretch("Dialog Backdrop", root.transform, Sprite("Generated/white"));
            backdrop.color = new Color(0.01f, 0.02f, 0.02f, 0f);
            backdrop.raycastTarget = false;
            Button tap = UiFactory.Button(backdrop);
            tap.transition = Selectable.Transition.None;
            var manager = root.AddComponent<DialogManager>();
            manager.Setup(backdrop, tap);
            return manager;
        }

        // The pay table: grows in a little from the centre, settles, and shrinks away
        private static DialogDefinition PaytableDialog()
        {
            MotionPreset entry = MotionAsset("Popup In",
                                        MotionPreset.StepOf(MotionChannel.Fade, 0f, 1f, 0.22f, Ease.OutCubic),
                                        MotionPreset.StepOf(MotionChannel.Scale, 0.94f, 1f, 0.3f, Ease.OutBack));
            MotionPreset exit = MotionAsset("Popup Out",
                                       MotionPreset.StepOf(MotionChannel.Fade, 1f, 0f, 0.2f, Ease.InCubic),
                                       MotionPreset.StepOf(MotionChannel.Scale, 1f, 0.97f, 0.2f, Ease.InCubic));
            DialogDefinition dialog = Dialog("Paytable", DialogLayer.Popup, DialogPolicy.Queue, entry, exit);
            dialog.backdropAlpha = 0.78f;
            dialog.closeOnBackdropTap = true;
            dialog.blocksInput = true;
            return dialog;
        }

        // The toast: rises a little as it fades in, fades out slowly, never darkens or blocks the game
        private static DialogDefinition ToastDialog()
        {
            MotionPreset entry = MotionAsset("Toast In",
                                        MotionPreset.StepOf(MotionChannel.Fade, 0f, 1f, 0.2f, Ease.OutCubic),
                                        MotionPreset.StepOf(MotionChannel.MoveY, -16f, 0f, 0.25f, Ease.OutCubic));
            MotionPreset exit = MotionAsset("Toast Out", MotionPreset.StepOf(MotionChannel.Fade, 1f, 0f, 0.4f, Ease.Linear));
            DialogDefinition dialog = Dialog("Toast", DialogLayer.Toast, DialogPolicy.Replace, entry, exit);
            dialog.backdropAlpha = 0f;
            dialog.closeOnBackdropTap = false;
            dialog.blocksInput = false;
            return dialog;
        }

        // A motion preset asset, rewritten on every build so the code stays the source of truth for its defaults
        private static MotionPreset MotionAsset(string name, params MotionPreset.Step[] steps)
        {
            MotionPreset preset = LoadOrCreate<MotionPreset>(MotionPath, name);
            preset.steps = steps;
            EditorUtility.SetDirty(preset);
            return preset;
        }

        private static DialogDefinition Dialog(string id, DialogLayer layer, DialogPolicy policy, MotionPreset entry, MotionPreset exit)
        {
            DialogDefinition dialog = LoadOrCreate<DialogDefinition>(DialogPath, id);
            dialog.id = id;
            dialog.layer = layer;
            dialog.policy = policy;
            dialog.entry = entry;
            dialog.exit = exit;
            EditorUtility.SetDirty(dialog);
            return dialog;
        }

        private static T LoadOrCreate<T>(string folder, string name) where T : ScriptableObject
        {
            EnsureFolder(folder);
            string path = folder + name + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }

            return asset;
        }
    }
}
