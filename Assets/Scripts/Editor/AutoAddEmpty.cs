using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using Assets.Scripts.Components.Objects;
using Assets.Scripts.Components;
using Assets.Scripts.Collab.Editor;

namespace Assets.Scripts.Editor
{
    [InitializeOnLoad]
    public class AutoAddEmpty
    {
        private static readonly HashSet<int> existingObjects = new();
        private static bool _busy;
        private static bool _rescanScheduled;

        static AutoAddEmpty()
        {
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (GameObject go in allObjects)
            {
                existingObjects.Add(go.GetInstanceID());
            }

            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }

        private static void OnHierarchyChanged()
        {
            if (_busy)
                return;

            if (CollabApplier.IsApplying)
                return;

            _busy = true;
            try
            {
                RunPass();
            }
            finally
            {
                _busy = false;
            }
        }
        
        private static void RunPass()
        {
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            List<EmptyGameObject> toRemove = null;
            bool added = false;

            foreach (GameObject go in allObjects)
            {
                if (go == null)
                    continue;

                if (go.TryGetComponent<EmptyGameObject>(out var empty))
                {
                    Component[] components = go.GetComponents<Component>();
                    bool hasOtherComponent = false;

                    foreach (Component c in components)
                    {
                        if (c == null)
                            continue;

                        if (c is Transform || c is EmptyGameObject)
                            continue;

                        if (c is Collab.CollabId)
                            continue;

                        hasOtherComponent = true;
                        break;
                    }

                    if (hasOtherComponent)
                    {
                        toRemove ??= new List<EmptyGameObject>();
                        toRemove.Add(empty);
                        continue;
                    }
                }

                if (!existingObjects.Contains(go.GetInstanceID()))
                {
                    existingObjects.Add(go.GetInstanceID());
                    if (go.GetComponent<ObjectBase>() == null && go.GetComponent<Builder>() == null && go.GetComponentInParent<ObjectBase>() == null && go.GetComponent<ServerSide>() == null && go.name.Contains("GameObject"))
                    {
                        go.AddComponent<EmptyGameObject>();
                        added = true;
                        Debug.Log($"Automatically added EmptyGameObject to {go.name}");
                    }
                }
            }

            if (toRemove != null && toRemove.Count > 0)
            {
                EditorApplication.delayCall += () =>
                {
                    foreach (EmptyGameObject e in toRemove)
                    {
                        if (e == null)
                            continue;

                        Object.DestroyImmediate(e);
                        Debug.Log($"Removed EmptyGameObject from {e.gameObject.name} because another component was added.");
                    }
                };
            }
            else if (added && !_rescanScheduled)
            {
                _rescanScheduled = true;
                EditorApplication.delayCall += () =>
                {
                    _rescanScheduled = false;
                    if (!_busy)
                        OnHierarchyChanged();
                };
            }
        }
    }
}
