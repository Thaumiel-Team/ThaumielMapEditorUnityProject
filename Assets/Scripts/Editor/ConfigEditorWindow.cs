using UnityEditor;
using UnityEngine;
using System.IO;
using System;

namespace Assets.Scripts
{
    public class ConfigEditorWindow : EditorWindow
    {
        private class PMERConfig
        {
            public bool OpenDirectoryAfterCompiling = false;
            public string ExportPath = string.Empty;
            public bool ZipCompiledSchematics = false;
        }

        private Config current;
        private Vector2 _scroll;

        [MenuItem("SchematicManager/Builder Config")]
        public static void ShowWindow()
        {
            ConfigEditorWindow window = GetWindow<ConfigEditorWindow>("Builder Config");
            window.minSize = new Vector2(420, 350);
            window.Show();
        }

        private void OnEnable()
        {
            current = ConfigBuilder.LoadConfig();
        }

        private void OnGUI()
        {
            current ??= ConfigBuilder.LoadConfig();
            current.PluginConfigs ??= new();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            GUILayout.Space(10);
            GUILayout.Label("Builder Configuration", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Configure export settings and post-build actions for the Schematic Builder.", MessageType.Info);
            GUILayout.Space(5);
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginVertical("box");
            GUILayout.BeginHorizontal();
            GUIContent pathLabel = new("Export Path", "The directory where compiled schematics will be saved.");
            current.ExportPath = EditorGUILayout.TextField(pathLabel, current.ExportPath);
            
            if (GUILayout.Button("Browse", GUILayout.Width(75)))
            {
                string defaultPath = string.IsNullOrEmpty(current.ExportPath) ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ThaumielMapEditor") : current.ExportPath;
                string selectedPath = EditorUtility.OpenFolderPanel("Select Export Directory", defaultPath, "");
                
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    GUI.FocusControl(null); 
                    current.ExportPath = selectedPath;
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUIContent compressLabel = new("Compress Export", "Zip the compiled schematics after building.");
            current.CompressExport = EditorGUILayout.Toggle(compressLabel, current.CompressExport);

            GUIContent openExportLabel = new("Open Export", "Automatically open the directory in File Explorer after compiling.");
            current.OpenExportAfterCompiling = EditorGUILayout.Toggle(openExportLabel, current.OpenExportAfterCompiling);
            
            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
            DrawPluginConfigs();
            GUILayout.Space(10);
            GUILayout.Label("Legacy Tools", EditorStyles.boldLabel);            
            EditorGUILayout.BeginVertical("box");
            GUIContent convertLabel = new("Import PMER Settings", "Converts and applies an old PMER JSON configuration file to the new builder.");
            if (GUILayout.Button(convertLabel, GUILayout.Height(30)))
                ConvertPMERSettings();

            EditorGUILayout.EndVertical();
            if (EditorGUI.EndChangeCheck())
                ConfigBuilder.SaveConfig(current);

            EditorGUILayout.EndScrollView();
            GUILayout.FlexibleSpace();            
            GUI.backgroundColor = new Color(0.8f, 1f, 0.8f);
            if (GUILayout.Button("Save Config", GUILayout.Height(30)))
            {
                ConfigBuilder.SaveConfig(current);
                Debug.Log("Builder Config saved successfully!");
            }

            GUI.backgroundColor = Color.white;
            GUILayout.Space(10);
        }

        private void DrawPluginConfigs()
        {
            GUILayout.Label("Plugin Configs", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Custom settings registered by plugins.", MessageType.Info);

            if (current.PluginConfigs.Count == 0)
            {
                EditorGUILayout.HelpBox("No plugins have registered custom configs.", MessageType.None);
                return;
            }

            foreach (PluginConfig section in current.PluginConfigs)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Plugin", section.PluginName ?? string.Empty);

                if (section.Config != null)
                {
                    foreach (PluginConfigValue entry in section.Config)
                    {
                        if (entry == null)
                            continue;

                        entry.Value = EditorGUILayout.TextField(entry.Name ?? string.Empty, entry.Value ?? string.Empty);
                    }
                }

                EditorGUILayout.EndVertical();
                GUILayout.Space(5);
            }
        }

        private void ConvertPMERSettings()
        {
            string path = EditorUtility.OpenFilePanel("Select PMER Config", "", "json");
            if (string.IsNullOrEmpty(path))
                return;

            try 
            {
                PMERConfig config = JsonUtility.FromJson<PMERConfig>(File.ReadAllText(path));
                if (config != null)
                {
                    current.CompressExport = config.ZipCompiledSchematics;
                    current.ExportPath = config.ExportPath;
                    current.OpenExportAfterCompiling = config.OpenDirectoryAfterCompiling;
                    
                    ConfigBuilder.SaveConfig(current);
                    GUI.FocusControl(null); 
                    Debug.Log("PMER settings imported and saved successfully!");
                }
                else
                    Debug.LogWarning("Selected file was not a valid PMER Config or was empty.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse PMER config. Please ensure it's a valid JSON file. Error: {ex.Message}");
            }
        }
    }
}