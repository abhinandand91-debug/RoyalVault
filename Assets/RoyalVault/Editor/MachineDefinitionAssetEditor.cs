using System.Collections.Generic;
using RoyalVault.Core;
using RoyalVault.Unity;
using UnityEditor;
using UnityEngine;

namespace RoyalVault.EditorTools
{
    /// <summary>
    /// Inspector for machine assets: shows what is wrong with the machine (if anything) and simulates a million paid
    /// spins through the real spin flow to report the return to player before you ship a change to the maths.
    /// </summary>
    [CustomEditor(typeof(MachineDefinitionAsset))]
    public sealed class MachineDefinitionAssetEditor : UnityEditor.Editor
    {
        private string report;

        public override void OnInspectorGUI()
        {
            var machine = (MachineDefinitionAsset)target;
            List<string> errors = machine.Validate();
            if (errors.Count == 0)
                EditorGUILayout.HelpBox($"{machine.reels} x {machine.rows} machine, {machine.paylines.Length} paylines, {machine.symbols.Length} symbols. Ready to play.", MessageType.Info);
            else
                EditorGUILayout.HelpBox(string.Join("\n", errors), MessageType.Error);

            using (new EditorGUI.DisabledScope(errors.Count > 0))
            {
                if (GUILayout.Button("Simulate 1,000,000 spins (RTP)"))
                {
                    EditorUtility.DisplayProgressBar("Royal Vault", "Simulating 1,000,000 paid spins...", 0.5f);
                    try { report = RtpSimulator.Run(machine.ToDefinition(), 1000000).ToString(); }
                    finally { EditorUtility.ClearProgressBar(); }
                }
            }
            if (!string.IsNullOrEmpty(report)) EditorGUILayout.HelpBox(report, MessageType.None);

            EditorGUILayout.Space();
            DrawDefaultInspector();
        }
    }
}
