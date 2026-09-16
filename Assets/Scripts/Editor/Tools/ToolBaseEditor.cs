using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Assets.Scripts.Components.Tools;
using Assets.Scripts.Components.Tools.Helpers;
using Assets.Scripts.Networking.Blocky;

namespace Assets.Scripts.Editor.Tools
{
    [CustomEditor(typeof(ToolBase), true)]
    public class ToolBaseEditor : UnityEditor.Editor
    {
        private static ToolBase _activeTool;
        private static string _activeTargetEvent;
        private static bool _isSubscribed = false;

        private readonly Dictionary<string, int> _selectedBlockyIndex = new();

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(15);
            GUIStyle btnStyle = new(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                fixedHeight = 35
            };

            GUIStyle reopenStyle = new(GUI.skin.button)
            {
                fixedHeight = 28
            };

            FieldInfo[] fields = target.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            ToolBase tool = (ToolBase)target;

            foreach (FieldInfo field in fields)
            {
                if (field.FieldType != typeof(InteractableClasses) && field.FieldType != typeof(ColliderClasses) && field.FieldType != typeof(CodeExportPayload))
                    continue;

                if (GUILayout.Button($"Open Blocky Editor for {field.Name}", btnStyle))
                {
                    OpenBlockyEditor(tool, field.Name, xml: null);
                }

                List<CodeExportPayload> blockyList = GetBlockyList(field, tool);
                List<CodeExportPayload> validEntries = blockyList?.FindAll(e => !string.IsNullOrEmpty(e?.Xml));

                if (validEntries == null || validEntries.Count == 0)
                {
                    EditorGUILayout.HelpBox("No saved workspace yet — export from Blocky at least once to enable reopen.", MessageType.None);
                    GUILayout.Space(5);
                    continue;
                }

                string dictKey = $"{tool.gameObject.name}::{field.Name}";
                if (!_selectedBlockyIndex.ContainsKey(dictKey))
                    _selectedBlockyIndex[dictKey] = 0;

                _selectedBlockyIndex[dictKey] = Mathf.Clamp(_selectedBlockyIndex[dictKey], 0, validEntries.Count - 1);
                int selected = _selectedBlockyIndex[dictKey];

                if (validEntries.Count == 1)
                {
                    if (GUILayout.Button($"↩  Reopen Workspace for {field.Name}", reopenStyle))
                        OpenBlockyEditor(tool, field.Name, xml: validEntries[0].Xml);
                }
                else
                {
                    string[] labels = BuildEntryLabels(validEntries);

                    EditorGUILayout.BeginHorizontal();

                    _selectedBlockyIndex[dictKey] = EditorGUILayout.Popup(selected, labels, GUILayout.ExpandWidth(true));

                    if (GUILayout.Button("↩  Reopen", reopenStyle, GUILayout.Width(90)))
                        OpenBlockyEditor(tool, field.Name, xml: validEntries[_selectedBlockyIndex[dictKey]].Xml);

                    EditorGUILayout.EndHorizontal();
                }

                GUILayout.Space(5);
            }
        }

        private static string[] BuildEntryLabels(List<CodeExportPayload> entries)
        {
            string[] labels = new string[entries.Count];
            for (int i = 0; i < entries.Count; i++)
            {
                CodeExportPayload e = entries[i];
                string time = string.IsNullOrEmpty(e.Timestamp) ? "" : $"  [{e.Timestamp[..Math.Min(19, e.Timestamp.Length)]}]";
                labels[i] = $"{i + 1}. {time}";
            }
            return labels;
        }

        private static List<CodeExportPayload> GetBlockyList(FieldInfo field, ToolBase tool)
        {
            object fieldValue = field.GetValue(tool);
            if (fieldValue == null)
                return null;

            if (fieldValue is CodeExportPayload directPayload)
            {
                return new List<CodeExportPayload> { directPayload };
            }

            FieldInfo blockyField = fieldValue.GetType().GetField("Blocky");
            if (blockyField == null)
                return null;

            return blockyField.GetValue(fieldValue) as List<CodeExportPayload>;
        }

        private static void OpenBlockyEditor(ToolBase tool, string fieldName, string xml)
        {
            _activeTool = tool;
            _activeTargetEvent = fieldName;

            if (!_isSubscribed)
            {
                BlocklyServer.OnCodeExport += HandleCodeExportReceived;
                _isSubscribed = true;
            }

            string filePath = Path.Combine(Application.streamingAssetsPath, "Blocky", "index.html").Replace(@"\", @"/");
            string absolutePath = Path.GetFullPath(filePath);
            string fileUri = new Uri(absolutePath).AbsoluteUri;

            if (!File.Exists(filePath))
            {
                Debug.LogError($"[Blocky] Could not find HTML file at: {filePath}");
                return;
            }

            Application.OpenURL(fileUri);
            Debug.Log($"[Blocky] Awaiting exports for {tool.gameObject.name} -> {fieldName}...");

            if (!string.IsNullOrEmpty(xml))
            {
                if (BlocklyServer.IsClientConnected)
                {
                    BlocklyServer.LoadXml(xml);
                    Debug.Log($"[Blocky] Restored workspace XML for {fieldName}.");
                }
                else
                {
                    string xmlCapture = xml;
                    void OnConnected()
                    {
                        BlocklyServer.LoadXml(xmlCapture);
                        BlocklyServer.OnClientConnected -= OnConnected;
                        Debug.Log($"[Blocky] Restored workspace XML for {fieldName} after reconnect.");
                    }
                    BlocklyServer.OnClientConnected += OnConnected;
                }
            }
        }

        private static void HandleCodeExportReceived(CodeExportPayload payload, string type)
        {
            if (_activeTool != null && !string.IsNullOrEmpty(_activeTargetEvent))
            {
                _activeTool.OnBlocklyExportReceived(payload, _activeTargetEvent);
            }
            else
            {
                Debug.LogWarning("[Blocky] Received export from browser, but no tool/event is actively waiting for it. Please click an 'Open Blocky Editor' button on a tool first.");
            }
        }
    }
}