using UnityEngine;

namespace GameKit
{
    // Where a dialog sits; each layer shows one dialog at a time
    public enum DialogLayer
    {
        // Pay table, settings: over the game
        Popup,
        // Full-screen moments: big wins, free spins, the Treasury
        Overlay,
        // Small notices that never block input
        Toast,
    }

    // What happens when a dialog is asked for while another is open on its layer
    public enum DialogPolicy
    {
        // Waits its turn
        Queue,
        // Closes the open one first
        Replace,
        // Opens on top; closing it returns to the one beneath
        Stack,
    }

    // How a dialog looks and behaves, tuned in the Inspector without touching code
    [CreateAssetMenu(menuName = "GameKit/Dialog Definition", fileName = "Dialog")]
    public sealed class DialogDefinition : ScriptableObject
    {
        [Tooltip("A name for sound, analytics and logs")] public string id;
        public DialogLayer layer = DialogLayer.Popup;
        public DialogPolicy policy = DialogPolicy.Queue;
        [Tooltip("Higher goes first when several wait on the same layer")] public int priority;

        [Header("Motion")]
        [Tooltip("Plays as the dialog opens; none appears at once")] public MotionPreset entry;
        [Tooltip("Plays as the dialog closes; none disappears at once")] public MotionPreset exit;

        [Header("Backdrop")]
        [Tooltip("How dark the shared backdrop goes behind this dialog (0 = none)")]
        [Range(0f, 1f)] public float backdropAlpha = 0.78f;
        [Tooltip("A tap on the backdrop closes this dialog")] public bool closeOnBackdropTap = true;
        [Tooltip("Taps cannot reach anything beneath while this dialog is open")] public bool blocksInput = true;

        // A definition made in code, for defaults and tests
        public static DialogDefinition Create(string id, DialogLayer layer, DialogPolicy policy, MotionPreset entry = null, MotionPreset exit = null)
        {
            var definition = CreateInstance<DialogDefinition>();
            definition.id = id;
            definition.layer = layer;
            definition.policy = policy;
            definition.entry = entry;
            definition.exit = exit;
            return definition;
        }
    }
}
