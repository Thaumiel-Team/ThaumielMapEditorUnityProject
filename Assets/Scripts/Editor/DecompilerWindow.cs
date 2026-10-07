using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Editor
{
    public class DecompilerWindow : EditorWindow
    {
        [MenuItem("Thaumiel/Decompiler")]
        public static void Open()
        {
            GetWindow<DecompilerWindow>("Decompiler");
        }

        private void OnGUI()
        {
            GUILayout.Label("Decompiler", EditorStyles.boldLabel);

            if (GUILayout.Button("Decompile Schematic"))
                Decompiler.DecompileData();
        }
    }
}
