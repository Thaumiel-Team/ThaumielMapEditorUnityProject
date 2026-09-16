using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class RemoveMissingScripts : EditorWindow
{
    private readonly List<GameObject> _prefabs = new();
    private Vector2 _scrollPos;

    [MenuItem("Thaumiel/Tools/Remove Missing Scripts")]
    public static void Open()
    {
        GetWindow<RemoveMissingScripts>("Remove Missing Scripts");
    }

    private void OnGUI()
    {
        GUILayout.Label("Drag & drop prefabs below to remove missing scripts.", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space();

        Rect dropArea = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Drop Prefabs Here");
        HandleDrop(dropArea);

        EditorGUILayout.Space();

        if (_prefabs.Count > 0)
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            for (int i = 0; i < _prefabs.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(_prefabs[i], typeof(GameObject), false);
                if (GUILayout.Button("-", GUILayout.Width(25)))
                {
                    _prefabs.RemoveAt(i);
                    i--;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginDisabledGroup(_prefabs.Count == 0);
        if (GUILayout.Button("Remove Missing Scripts"))
            RemoveAllMissing();

        EditorGUI.EndDisabledGroup();

        if (GUILayout.Button("Clear List", GUILayout.Width(80)))
            _prefabs.Clear();

        EditorGUILayout.EndHorizontal();
    }

    private void HandleDrop(Rect dropArea)
    {
        Event e = Event.current;

        if (!dropArea.Contains(e.mousePosition))
            return;

        if (e.type == EventType.DragUpdated)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            e.Use();
        }
        else if (e.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();

            foreach (Object obj in DragAndDrop.objectReferences)
            {
                if (obj is GameObject go && !_prefabs.Contains(go))
                    _prefabs.Add(go);
            }

            e.Use();
        }
    }

    private void RemoveAllMissing()
    {
        int totalRemoved = 0;
        int totalPrefabsProcessed = 0;

        foreach (GameObject prefab in _prefabs)
        {
            if (prefab == null)
                continue;

            string path = AssetDatabase.GetAssetPath(prefab);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning($"{prefab.name} is not a project asset, skipping.");
                continue;
            }

            int removed = 0;
            foreach (Transform t in prefab.GetComponentsInChildren<Transform>(true))
            {
                removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            }

            if (removed > 0)
            {
                PrefabUtility.SavePrefabAsset(prefab);
                Debug.Log($"Removed {removed} missing scripts from {path}");
                totalRemoved += removed;
            }
            else
                Debug.Log($"No missing scripts found on {prefab.name}");

            totalPrefabsProcessed++;
        }

        if (totalPrefabsProcessed > 0)
        {
            AssetDatabase.SaveAssets();
            Debug.Log($"Done! Removed {totalRemoved} missing scripts across {totalPrefabsProcessed} prefabs.");
        }
    }
}