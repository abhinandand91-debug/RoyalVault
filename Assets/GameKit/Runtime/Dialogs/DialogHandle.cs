using UnityEngine;

namespace GameKit
{
    // A dialog that has been asked for: yield on it to wait until it has closed
    public sealed class DialogHandle : CustomYieldInstruction
    {
        public DialogView View { get; }
        // It has finished its exit motion
        public bool IsClosed { get; internal set; }

        public override bool keepWaiting => !IsClosed;

        internal DialogHandle(DialogView view)
        {
            View = view;
        }

        // Asks the dialog to close (a waiting dialog is simply dropped)
        public void Close()
        {
            View.Manager.Close(this);
        }
    }
}
