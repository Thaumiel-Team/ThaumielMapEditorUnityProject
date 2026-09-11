using System.Diagnostics;
using System.IO;
using Assets.Scripts;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

[InitializeOnLoad]
public class SchematicManager : EditorWindow
{
    [MenuItem("SchematicManager/Compile %#d")]
    public static void Compile()
    {
        Debug.ClearDeveloperConsole();
        Builder[] builders = FindObjectsByType<Builder>(FindObjectsSortMode.InstanceID);
        if (builders.Length > 0)
        {
            foreach (Builder schematic in builders)
            {
                schematic.CompileData();
            }
        }
        else
        {
            Debug.LogError("Compilation failed: No Builder script found in the scene. \n Please add the Builder script to a GameObject to compile your schematic.");
        }
    }

    [MenuItem("SchematicManager/Open Directory")]
    private static void OpenDirectory()
    {
        Config config = ConfigBuilder.LoadConfig();

        if (!Directory.Exists(config.ExportPath))
            Directory.CreateDirectory(config.ExportPath);

        Process.Start(config.ExportPath);
    }

    [MenuItem("SchematicManager/Decompile Schematic %#e")]
    public static void Decompile()
    {
        string[] guids = AssetDatabase.FindAssets("t:BuilderPrefabRegistry");
        if (guids.Length == 0)
        {
            Debug.LogError("No BuilderPrefabRegistry found. Create one via Thaumiel/Decompiler.");
            return;
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        BuilderPrefabRegistry registry = AssetDatabase.LoadAssetAtPath<BuilderPrefabRegistry>(path);
        Decompiler.DecompileData(registry);
    }
}
