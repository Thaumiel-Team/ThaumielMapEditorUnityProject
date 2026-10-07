using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Collab.Editor
{
    [InitializeOnLoad]
    public static class CollabPresence
    {
        private static GUIStyle _labelStyle;

        static CollabPresence()
        {
            SceneView.duringSceneGui += OnSceneGui;
        }

        private static void OnSceneGui(SceneView sv)
        {
            if (CollabNet.Mode == CollabMode.Off)
                return;

            IReadOnlyDictionary<string, List<string>> selections = CollabApplier.RemoteSelections;
            if (selections.Count == 0)
                return;

            foreach (KeyValuePair<string, List<string>> kv in selections)
            {
                string userId = kv.Key;
                CollabApplier.Peers.TryGetValue(userId, out CollabApplier.RemotePeer peer);
                string name = peer?.name ?? "peer";
                Color color = Color.cyan;
                if (peer != null && ColorUtility.TryParseHtmlString(peer.color, out Color pc))
                    color = pc;

                foreach (string guid in kv.Value ?? new List<string>())
                {
                    GameObject go = CollabId.Find(guid);
                    if (go == null)
                        continue;

                    DrawSelection(go, name, color);
                }
            }
        }

        private static void DrawSelection(GameObject go, string userName, Color color)
        {
            Renderer rend = go.GetComponent<Renderer>();
            Bounds b;
            if (rend != null)
            {
                b = rend.bounds;
            }
            else
                b = new Bounds(go.transform.position, Vector3.one * 0.5f);

            Handles.color = color;
            Handles.DrawWireCube(b.center, b.size);

            Handles.BeginGUI();
            try
            {
                Vector3 sp = HandleUtility.WorldToGUIPoint(b.center + Vector3.up * (b.extents.y + 0.3f));
                _labelStyle ??= new GUIStyle(EditorStyles.miniLabel)
                {
                    fontStyle = FontStyle.Bold
                };
                
                Color prev = _labelStyle.normal.textColor;
                _labelStyle.normal.textColor = color;
                GUI.Label(new Rect(sp.x + 8, sp.y - 8, 220, 18), userName, _labelStyle);
                _labelStyle.normal.textColor = prev;
            }
            finally
            {
                Handles.EndGUI();
            }
        }
    }
}
