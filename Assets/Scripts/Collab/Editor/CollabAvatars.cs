using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Collab.Editor
{
    [InitializeOnLoad]
    public static class CollabAvatars
    {
        private const string MenuPath = "Thaumiel/Tools/Collab Avatars";
        private const double SweepSeconds = 1.0;
        private const double StaleSeconds = 12.0;
        private const int MaxSelectionProbe = 10;
        
        private const float SphereRadius = 0.5f;
        private const float FacingExtra = 1.0f;
        private const float FacingThickness = 0.2f;

        private class AvatarState
        {
            public string id = string.Empty;
            public Vector3 shown;
            public Vector3 shownDir = Vector3.forward;
            public bool placed;
        }

        private static readonly Dictionary<string, AvatarState> _shown = new();
        private static double _nextSweep;
        private static double _lastTick;
        private static GUIStyle _labelStyle;
        private static bool _showAvatars = true;

        static CollabAvatars()
        {
            EditorApplication.update += OnUpdate;
            SceneView.duringSceneGui += OnSceneGui;
            AssemblyReloadEvents.beforeAssemblyReload += Clear;
            Menu.SetChecked(MenuPath, _showAvatars);
        }

        [MenuItem(MenuPath)]
        private static void ToggleAvatars()
        {
            _showAvatars = !_showAvatars;
            Menu.SetChecked(MenuPath, _showAvatars);
            SceneView.RepaintAll();
        }

        public static void Clear()
        {
            _shown.Clear();
            _lastTick = 0.0;
        }

        private static void OnUpdate()
        {
            if (CollabNet.Mode == CollabMode.Off)
            {
                if (_shown.Count > 0)
                    _shown.Clear();

                _lastTick = 0.0;
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            float dt = _lastTick > 0.0f ? (float)Math.Min(0.5, now - _lastTick) : 0.05f;
            _lastTick = now;
            float blend = 1f - Mathf.Exp(-8f * dt);

            foreach (KeyValuePair<string, CollabApplier.RemoteView> kv in CollabApplier.RemoteViews)
            {
                if (kv.Value == null)
                    continue;

                Vector3 facing = kv.Value.pivot - kv.Value.camPos;
                bool hasDir = facing.sqrMagnitude > 1e-6f;
                if (hasDir)
                    facing.Normalize();

                MoveTowards(kv.Key, kv.Value.camPos, facing, hasDir, blend);
            }

            foreach (KeyValuePair<string, List<string>> kv in CollabApplier.RemoteSelections)
            {
                if (CollabApplier.RemoteViews.ContainsKey(kv.Key))
                    continue;

                if (!TrySelectionCentroid(kv.Value, out Vector3 centroid))
                    continue;

                MoveTowards(kv.Key, centroid, Vector3.forward, false, blend);
            }

            if (now < _nextSweep)
                return;

            _nextSweep = now + SweepSeconds;
            long cutoff = DateTime.UtcNow.Ticks - (long)(StaleSeconds * 10000000.0);
            CollabApplier.PruneStaleRemotePeers(cutoff);

            List<string> gone = new();
            foreach (string id in _shown.Keys)
            {
                if (!CollabApplier.RemoteViews.ContainsKey(id) && !CollabApplier.RemoteSelections.ContainsKey(id))
                    gone.Add(id);
            }

            foreach (string id in gone)
            {
                _shown.Remove(id);
            }
        }

        private static void MoveTowards(string id, Vector3 target, Vector3 targetDir, bool hasDir, float blend)
        {
            if (!_shown.TryGetValue(id, out AvatarState state) || state == null)
            {
                state = new AvatarState
                {
                    id = id,
                    shown = target,
                    shownDir = hasDir ? targetDir : Vector3.forward,
                    placed = true
                };
                _shown[id] = state;
                return;
            }

            if (!state.placed)
            {
                state.shown = target;
                if (hasDir)
                    state.shownDir = targetDir;

                state.placed = true;
                return;
            }

            state.shown = Vector3.Lerp(state.shown, target, blend);
            if (hasDir)
            {
                Vector3 smoothed = Vector3.Lerp(state.shownDir, targetDir, blend);
                if (smoothed.sqrMagnitude > 1e-8f)
                    state.shownDir = smoothed.normalized;
            }
        }

        private static bool TrySelectionCentroid(List<string> guids, out Vector3 centroid)
        {
            centroid = Vector3.zero;
            if (guids == null || guids.Count == 0)
                return false;

            Vector3 sum = Vector3.zero;
            int found = 0;
            int examined = 0;
            foreach (string guid in guids)
            {
                if (examined >= MaxSelectionProbe)
                    break;

                examined++;
                if (string.IsNullOrEmpty(guid))
                    continue;

                GameObject go = CollabId.Find(guid);
                if (go == null)
                    continue;

                sum += go.transform.position;
                found++;
            }

            if (found == 0)
                return false;

            centroid = sum / found;
            return true;
        }

        private static void OnSceneGui(SceneView sv)
        {
            if (!_showAvatars)
                return;

            if (CollabNet.Mode == CollabMode.Off)
                return;

            if (_shown.Count == 0)
                return;

            foreach (KeyValuePair<string, AvatarState> kv in _shown)
            {
                if (kv.Value == null || !kv.Value.placed)
                    continue;

                CollabApplier.Peers.TryGetValue(kv.Key, out CollabApplier.RemotePeer peer);
                string name = peer?.name ?? "peer";
                Color color = Color.cyan;
                if (peer != null && ColorUtility.TryParseHtmlString(peer.color, out Color parsed))
                    color = parsed;

                DrawAvatar(kv.Value.shown, kv.Value.shownDir, name, color);
            }
        }

        private static void DrawAvatar(Vector3 center, Vector3 facingDir, string name, Color color)
        {
            Vector3 fwd = facingDir;
            if (fwd.sqrMagnitude < 1e-6f)
            {
                fwd = Vector3.forward;
            }
            else
                fwd.Normalize();

            Handles.color = color;
            Handles.SphereHandleCap(0, center, Quaternion.identity, SphereRadius * 2f, EventType.Repaint);

            float facingLength = SphereRadius + FacingExtra;
            Quaternion rot;
            if (Mathf.Abs(Vector3.Dot(fwd, Vector3.up)) > 0.999f)
            {
                rot = Quaternion.LookRotation(fwd, Vector3.forward);
            }
            else
                rot = Quaternion.LookRotation(fwd, Vector3.up);

            Vector3 cubeCenter = center + fwd * (facingLength * 0.5f);
            Vector3 cubeSize = new(FacingThickness, FacingThickness, facingLength);

            Matrix4x4 prevMatrix = Handles.matrix;
            Handles.matrix = Matrix4x4.TRS(cubeCenter, rot, cubeSize);
            try
            {
                Handles.color = color;
                Handles.CubeHandleCap(0, Vector3.zero, Quaternion.identity, 1f, EventType.Repaint);
            }
            finally
            {
                Handles.matrix = prevMatrix;
            }

            DrawAvatarLabel(center + Vector3.up * (SphereRadius + 0.3f), name, color);
        }

        private static void DrawAvatarLabel(Vector3 anchor, string name, Color color)
        {
            Handles.BeginGUI();
            try
            {
                _labelStyle ??= new GUIStyle(EditorStyles.miniLabel)
                {
                    fontStyle = FontStyle.Bold
                };

                Vector3 sp = HandleUtility.WorldToGUIPoint(anchor);
                Color prev = _labelStyle.normal.textColor;
                _labelStyle.normal.textColor = color;
                GUI.Label(new Rect(sp.x + 8, sp.y - 8, 220, 18), name, _labelStyle);
                _labelStyle.normal.textColor = prev;
            }
            finally
            {
                Handles.EndGUI();
            }
        }
    }
}
