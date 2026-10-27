using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Editor
{
    public class DecompilerWindow : EditorWindow
    {
        [MenuItem("Thaumiel/Decompiler")]
        public static void Open() =>
            Decompiler.DecompileData();
    }
}
