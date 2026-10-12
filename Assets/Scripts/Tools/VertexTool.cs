using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Tools
{
    public class VertexToolWindow : EditorWindow
    {
        private enum ConnectMode
        {
            Standard,
            CornerMeet,
            FullResize
        }

        private enum FlowState
        {
            Idle,
            WaitObj1,
            WaitObj2,
            WaitVerts
        }

        private enum Axis
        {
            X,
            Y,
            Z
        }

        private sealed class Session
        {
            public GameObject Obj1;
            public Vector3[] Verts1 = Array.Empty<Vector3>();
            public int V1 = -1;
            public GameObject Obj2;
            public Vector3[] Verts2 = Array.Empty<Vector3>();
            public int V2 = -1;
            public FlowState State = FlowState.Idle;

            public bool HasObjects => Obj1 != null && Obj2 != null;
            public bool HasVerts => HasObjects && V1 >= 0 && V1 < Verts1.Length && V2 >= 0 && V2 < Verts2.Length;

            public void ResetPicks()
            {
                Obj1 = Obj2 = null;
                Verts1 = Verts2 = Array.Empty<Vector3>();
                V1 = V2 = -1;
            }

            public void ClampIndices()
            {
                if (V1 < 0 || V1 >= Verts1.Length)
                    V1 = -1;

                if (V2 < 0 || V2 >= Verts2.Length)
                    V2 = -1;

                if (Obj1 == null)
                {
                    Verts1 = Array.Empty<Vector3>();
                    V1 = -1;
                }

                if (Obj2 == null)
                {
                    Verts2 = Array.Empty<Vector3>();
                    V2 = -1;
                }
            }
        }

        private const string PrefMode = "VTW_Mode";
        private const string PrefLockY = "VTW_LockY";
        private const string PrefYMid = "VTW_YMid";
        private const string PrefKeyAct = "VTW_KeyActivate";
        private const string PrefKeyToggle = "VTW_KeyToggle";
        private const string PrefKeyLockY = "VTW_KeyToggleLockY";

        private static KeyCode KeyActivate
        {
            get => (KeyCode)EditorPrefs.GetInt(PrefKeyAct, (int)KeyCode.V);
            set => EditorPrefs.SetInt(PrefKeyAct, (int)value);
        }

        private static KeyCode KeyToggleMode
        {
            get => (KeyCode)EditorPrefs.GetInt(PrefKeyToggle, (int)KeyCode.B);
            set => EditorPrefs.SetInt(PrefKeyToggle, (int)value);
        }

        private static KeyCode KeyToggleLockY
        {
            get => (KeyCode)EditorPrefs.GetInt(PrefKeyLockY, (int)KeyCode.None);
            set => EditorPrefs.SetInt(PrefKeyLockY, (int)value);
        }

        private ConnectMode Mode
        {
            get => (ConnectMode)EditorPrefs.GetInt(PrefMode, (int)ConnectMode.CornerMeet);
            set => EditorPrefs.SetInt(PrefMode, (int)value);
        }

        private bool LockY
        {
            get => EditorPrefs.GetBool(PrefLockY, true);
            set => EditorPrefs.SetBool(PrefLockY, value);
        }

        private bool YMidpoint
        {
            get => EditorPrefs.GetBool(PrefYMid, true);
            set => EditorPrefs.SetBool(PrefYMid, value);
        }

        private static readonly Color ColObj1 = new(0.20f, 0.60f, 1.00f);
        private static readonly Color ColObj2 = new(1.00f, 0.40f, 0.20f);
        private static readonly Color ColSel = Color.yellow;

        private static VertexToolWindow _instance;
        private static readonly Session _headless = new();

        private readonly Session _session = new();
        private Vector2 _scroll;
        private int _capturingKey = -1;

        private static GUIStyle _handleLabelStyle;
        private static GUIStyle _overlayTitleStyle;
        private static GUIStyle _overlayBodyStyle;
        private static GUIStyle _overlayHintStyle;

        [InitializeOnLoadMethod]
        private static void InitAlwaysOn()
        {
            SceneView.duringSceneGui -= StaticSceneGUI;
            SceneView.duringSceneGui += StaticSceneGUI;
        }

        [MenuItem("Thaumiel/Tools/Vertex Tool")]
        public static void Open()
        {
            VertexToolWindow w = GetWindow<VertexToolWindow>(false, "Vertex Connector");
            w.minSize = new Vector2(340f, 640f);
            w.Show();
        }

        private void OnEnable()
        {
            _instance = this;
            SceneView.duringSceneGui += OnSceneGUI;
            Selection.selectionChanged += OnSelectionChanged;
            Undo.undoRedoPerformed += OnUndoRedo;
            _session.ClampIndices();
        }

        private void OnDisable()
        {
            if (_instance == this)
                _instance = null;

            SceneView.duringSceneGui -= OnSceneGUI;
            Selection.selectionChanged -= OnSelectionChanged;
            Undo.undoRedoPerformed -= OnUndoRedo;
            _session.State = FlowState.Idle;
            _capturingKey = -1;
        }

        private void OnSelectionChanged() => Repaint();
        private void OnUndoRedo()
        {
            RefreshVerts(preserveSelection: true);
            Repaint();
            SceneView.RepaintAll();
        }

        private static void StaticSceneGUI(SceneView sv)
        {
            if (_instance != null)
                return;

            RunSceneFlow(sv, _headless, isHeadless: true);
        }

        private void OnSceneGUI(SceneView sv) =>
            RunSceneFlow(sv, _session, isHeadless: false);

        private static void RunSceneFlow(SceneView sv, Session s, bool isHeadless)
        {
            s.ClampIndices();

            if (HandleKeybinds(sv, s, isHeadless))
                return;

            if (s.State == FlowState.WaitObj1 || s.State == FlowState.WaitObj2)
                HandleObjectClick(sv, s, isHeadless);

            if (s.State != FlowState.Idle)
                DrawSceneOverlay(sv, ModeLabel(CurrentMode()), StateMessage(s));

            ConnectMode mode = CurrentMode();
            bool lockY = EditorPrefs.GetBool(PrefLockY, true);
            bool yMid = EditorPrefs.GetBool(PrefYMid, true);

            DrawVertexHandles(s.Obj1, s.Verts1, 0, s, ColObj1, "Obj1", sv, isHeadless);
            DrawVertexHandles(s.Obj2, s.Verts2, 1, s, ColObj2, "Obj2", sv, isHeadless);

            if (s.HasVerts)
            {
                Vector3 vw1 = s.Obj1.transform.TransformPoint(s.Verts1[s.V1]);
                Vector3 vw2 = s.Obj2.transform.TransformPoint(s.Verts2[s.V2]);
                DrawConnectionPreview(vw1, vw2, s.Obj1.transform, s.Obj2.transform,
                    s.Verts1[s.V1], s.Verts2[s.V2], mode, lockY, yMid);
            }
        }

        private static ConnectMode CurrentMode() =>
            (ConnectMode)EditorPrefs.GetInt(PrefMode, (int)ConnectMode.CornerMeet);

        private static bool HandleKeybinds(SceneView sv, Session s, bool isHeadless)
        {
            Event e = Event.current;
            if (e == null || e.type != EventType.KeyDown)
                return false;

            void repaint()
            {
                sv.Repaint();
                if (!isHeadless && _instance != null)
                    _instance.Repaint();
            }

            if (KeyToggleLockY != KeyCode.None && e.keyCode == KeyToggleLockY)
            {
                EditorPrefs.SetBool(PrefLockY, !EditorPrefs.GetBool(PrefLockY, true));
                repaint();
                e.Use();
                return true;
            }

            if (e.keyCode == KeyToggleMode && s.State != FlowState.Idle)
            {
                EditorPrefs.SetInt(PrefMode, (int)CycleMode(CurrentMode()));
                repaint();
                e.Use();
                return true;
            }

            if (e.keyCode == KeyCode.Escape && s.State != FlowState.Idle)
            {
                s.State = FlowState.Idle;
                repaint();
                e.Use();
                return true;
            }

            if (e.keyCode == KeyActivate)
            {
                switch (s.State)
                {
                    case FlowState.Idle:
                        s.ResetPicks();
                        s.State = FlowState.WaitObj1;
                        break;

                    case FlowState.WaitVerts when s.HasVerts:
                        TryConnect(s, CurrentMode(), EditorPrefs.GetBool(PrefLockY, true), EditorPrefs.GetBool(PrefYMid, true));
                        s.State = FlowState.Idle;
                        break;

                    default:
                        s.State = FlowState.Idle;
                        break;
                }

                repaint();
                e.Use();
                return true;
            }

            return false;
        }

        private static void HandleObjectClick(SceneView sv, Session s, bool isHeadless)
        {
            Event e = Event.current;
            if (e == null || e.type != EventType.MouseDown || e.button != 0)
                return;

            if (!TryPickMeshObject(e.mousePosition, out GameObject hit))
                return;

            if (s.State == FlowState.WaitObj1)
            {
                s.Obj1 = hit;
                s.Verts1 = GetUniqueVerts(hit);
                s.V1 = -1;
                s.State = FlowState.WaitObj2;
            }
            else if (hit != s.Obj1)
            {
                s.Obj2 = hit;
                s.Verts2 = GetUniqueVerts(hit);
                s.V2 = -1;
                s.State = FlowState.WaitVerts;
            }
            else
                return;

            sv.Repaint();
            if (!isHeadless && _instance != null)
                _instance.Repaint();
                
            e.Use();
        }

        private static bool TryPickMeshObject(Vector2 mousePos, out GameObject hit)
        {
            hit = null;
            GameObject picked = HandleUtility.PickGameObject(mousePos, false);
            if (picked == null)
                return false;

            if (!picked.TryGetComponent(out MeshFilter mf) || mf == null || mf.sharedMesh == null)
                return false;

            hit = picked;
            return true;
        }

        private static void DrawVertexHandles(GameObject obj, Vector3[] verts, int slot,
            Session s, Color col, string prefix, SceneView sv, bool isHeadless)
        {
            if (obj == null || verts == null || verts.Length == 0)
                return;

            EnsureHandleStyle();
            Transform t = obj.transform;

            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 wp = t.TransformPoint(verts[i]);
                bool isSel = (slot == 0 ? s.V1 : s.V2) == i;
                float size = HandleUtility.GetHandleSize(wp) * (isSel ? 0.12f : 0.08f);
                Handles.color = isSel ? ColSel : col;

                if (Handles.Button(wp, Quaternion.identity, size, size, Handles.SphereHandleCap))
                {
                    if (slot == 0)
                    {
                        s.V1 = i;
                    }
                    else
                        s.V2 = i;

                    sv.Repaint();
                    if (!isHeadless && _instance != null)
                        _instance.Repaint();
                }

                _handleLabelStyle.normal.textColor = isSel ? Color.yellow : col;
                Handles.Label(wp + Vector3.up * (1.5f * size), $"{prefix} V{i}", _handleLabelStyle);
            }
        }

        private static void DrawConnectionPreview(Vector3 vw1, Vector3 vw2,
            Transform t1, Transform t2, Vector3 vl1, Vector3 vl2,
            ConnectMode mode, bool lockY, bool yMid)
        {
            if (mode == ConnectMode.FullResize)
            {
                Vector3 target = GetFullResizeTarget(t1, vl1, t2, vl2, lockY, yMid);
                Handles.color = Color.yellow;
                Handles.DrawDottedLine(vw1, target, 4f);
                Handles.color = new Color(1f, 0.9f, 0.2f);
                Handles.SphereHandleCap(0, target, Quaternion.identity, HandleUtility.GetHandleSize(target) * 0.1f, EventType.Repaint);
                Handles.Label(target + Vector3.up * 0.2f, "Target", EditorStyles.boldLabel);
                return;
            }

            Vector3 corner = GetCornerPoint(vw1, t1, vl1, vw2, t2, vl2, lockY, yMid, out bool parallel);

            Vector2 wd1 = WallAxis2D(t1, vl1, vw1, vw2);
            Vector2 wd2 = WallAxis2D(t2, vl2, vw2, vw1);
            Vector3 leg1 = -new Vector3(wd1.x, 0f, wd1.y);
            Vector3 leg2 = -new Vector3(wd2.x, 0f, wd2.y);

            Handles.color = ColObj1;
            Handles.DrawDottedLine(vw1, corner, 4f);
            Handles.color = ColObj2;
            Handles.DrawDottedLine(vw2, corner, 4f);
            DrawRightAngleMarker(corner, leg1, leg2);

            Handles.color = new Color(0.3f, 1f, 0.4f);
            Handles.SphereHandleCap(0, corner, Quaternion.identity, HandleUtility.GetHandleSize(corner) * 0.15f, EventType.Repaint);
            Handles.Label(corner + Vector3.up * 0.25f, parallel ? "Midpoint (parallel walls)" : "Corner", EditorStyles.boldLabel);
        }

        private static void DrawRightAngleMarker(Vector3 corner, Vector3 d1, Vector3 d2)
        {
            if (d1.sqrMagnitude < 0.0001f || d2.sqrMagnitude < 0.0001f)
                return;

            d1.Normalize();
            d2.Normalize();
            float sz = HandleUtility.GetHandleSize(corner) * 0.18f;
            Vector3 p1 = corner + d1 * sz;
            Vector3 p2 = corner + d2 * sz;
            Vector3 pm = p1 + d2 * sz;
            Handles.color = new Color(1f, 1f, 1f, 0.7f);
            Handles.DrawLine(corner, p1);
            Handles.DrawLine(corner, p2);
            Handles.DrawLine(p1, pm);
            Handles.DrawLine(p2, pm);
        }

        private static void DrawSceneOverlay(SceneView sv, string modeLabel, string stateMsg)
        {
            EnsureOverlayStyles();
            Handles.BeginGUI();
            const float w = 380f, h = 68f;
            float x = (sv.position.width - w) * 0.5f;
            float y = sv.position.height - h - 40f;
            EditorGUI.DrawRect(new Rect(x - 6, y - 6, w + 12, h + 12), new Color(0f, 0f, 0f, 0.6f));
            EditorGUI.DrawRect(new Rect(x, y, w, 20f), new Color(0.15f, 0.35f, 0.5f));
            GUI.Label(new Rect(x, y, w, 20f), $"  Mode: {modeLabel}  ({KeyToggleMode} to toggle)", _overlayTitleStyle);
            GUI.Label(new Rect(x, y + 22f, w, 24f), $"  {stateMsg}", _overlayBodyStyle);
            GUI.Label(new Rect(x, y + 46f, w, 20f), $"  [{KeyActivate}] confirm   [Esc] cancel", _overlayHintStyle);
            Handles.EndGUI();
        }

        private static void EnsureHandleStyle() => _handleLabelStyle ??= new GUIStyle(EditorStyles.boldLabel);

        private static void EnsureOverlayStyles()
        {
            _overlayTitleStyle ??= new GUIStyle(EditorStyles.boldLabel)
            {
                normal =
                {
                    textColor = Color.white
                },
                alignment = TextAnchor.MiddleLeft
            };

            _overlayBodyStyle ??= new GUIStyle(EditorStyles.label)
            {
                normal =
                {
                    textColor = Color.white
                },
                fontSize = 12
            };

            _overlayHintStyle ??= new GUIStyle(EditorStyles.label)
            {
                normal =
                {
                    textColor = new Color(0.7f, 0.7f, 0.7f)
                },
                fontSize = 11
            };
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            GUILayout.Label("Vertex Connector", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Pick two objects + one vertex each (in the Scene View dots or the grids below), then Connect.", MessageType.Info);
            DrawQuickStartRow();

            EditorGUILayout.Space(8);
            DrawColoredLabel("Object 1 — Resize This  (Blue dots)", ColObj1);
            DrawObjectField(_session, slot: 0);
            if (_session.Verts1.Length > 0)
                DrawVertexGrid(_session.Verts1, _session.Obj1, isFirst: true, ColObj1);

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.backgroundColor = new Color(1f, 0.85f, 0.2f);
            if (GUILayout.Button("Swap Objects", GUILayout.Width(150), GUILayout.Height(26)))
                SwapObjects();

            GUI.backgroundColor = Color.white;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);

            DrawColoredLabel("Object 2 — Snap Target  (Orange dots)", ColObj2);
            DrawObjectField(_session, slot: 1);
            if (_session.Verts2.Length > 0)
                DrawVertexGrid(_session.Verts2, _session.Obj2, isFirst: false, ColObj2);

            EditorGUILayout.Space(8);
            DrawWorldReadout();

            EditorGUILayout.Space(8);
            DrawYControls();

            EditorGUILayout.Space(8);
            DrawModeControls();

            EditorGUILayout.Space(8);
            DrawConnectSection();

            EditorGUILayout.Space(10);
            DrawKeybindSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawQuickStartRow()
        {
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = _session.State == FlowState.Idle;
            if (GUILayout.Button("Start Picking (V)", GUILayout.Height(26)))
            {
                _session.ResetPicks();
                _session.State = FlowState.WaitObj1;
                SceneView.RepaintAll();
            }

            GUI.enabled = _session.State != FlowState.Idle;
            if (GUILayout.Button("Cancel (Esc)", GUILayout.Height(26)))
            {
                _session.State = FlowState.Idle;
                SceneView.RepaintAll();
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            if (_session.State != FlowState.Idle)
                EditorGUILayout.HelpBox(StateMessage(_session), MessageType.None);
        }

        private void DrawObjectField(Session s, int slot)
        {
            GameObject cur = slot == 0 ? s.Obj1 : s.Obj2;
            GameObject next = (GameObject)EditorGUILayout.ObjectField("Object", cur, typeof(GameObject), true);
            if (next == cur)
            {
                if (cur != null && (slot == 0 ? s.Verts1.Length : s.Verts2.Length) == 0)
                    EditorGUILayout.HelpBox("No mesh found on this object.", MessageType.Warning);

                return;
            }

            SetSessionObject(s, slot, next);
        }

        private void SetSessionObject(Session s, int slot, GameObject go)
        {
            if (slot == 0)
            {
                s.Obj1 = go;
                s.Verts1 = GetUniqueVerts(go);
                s.V1 = -1;
            }
            else
            {
                s.Obj2 = go;
                s.Verts2 = GetUniqueVerts(go);
                s.V2 = -1;
            }

            SceneView.RepaintAll();
            Repaint();
        }

        private void DrawVertexGrid(Vector3[] verts, GameObject obj, bool isFirst, Color col)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{verts.Length} vertices", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            GUI.enabled = Selection.activeGameObject != null;
            if (GUILayout.Button("Use Selection", GUILayout.Width(110)))
                SetSessionObject(_session, isFirst ? 0 : 1, Selection.activeGameObject);

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            const int cols = 4;
            int rows = Mathf.CeilToInt(verts.Length / (float)cols);
            for (int row = 0; row < rows; row++)
            {
                EditorGUILayout.BeginHorizontal();
                for (int c = 0; c < cols; c++)
                {
                    int i = row * cols + c;
                    if (i >= verts.Length)
                        break;

                    int sel = isFirst ? _session.V1 : _session.V2;
                    GUI.backgroundColor = i == sel ? Color.yellow : col * 0.8f;
                    Vector3 wp = obj.transform.TransformPoint(verts[i]);
                    if (GUILayout.Button(new GUIContent($"V{i}", $"Local {verts[i]:F2}\nWorld {wp:F2}"), GUILayout.Width(56)))
                    {
                        if (isFirst)
                        {
                            _session.V1 = i;
                        }
                        else
                            _session.V2 = i;

                        SceneView.RepaintAll();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            GUI.backgroundColor = Color.white;
        }

        private void DrawWorldReadout()
        {
            if (_session.V1 >= 0 && _session.Obj1 != null && _session.V1 < _session.Verts1.Length)
            {
                Vector3 v = _session.Obj1.transform.TransformPoint(_session.Verts1[_session.V1]);
                EditorGUILayout.LabelField($"V1 World: ({v.x:F3}, {v.y:F3}, {v.z:F3})");
            }

            if (_session.V2 >= 0 && _session.Obj2 != null && _session.V2 < _session.Verts2.Length)
            {
                Vector3 v = _session.Obj2.transform.TransformPoint(_session.Verts2[_session.V2]);
                EditorGUILayout.LabelField($"V2 World: ({v.x:F3}, {v.y:F3}, {v.z:F3})");
            }

            if (_session.HasVerts)
            {
                Vector3 vw1 = _session.Obj1.transform.TransformPoint(_session.Verts1[_session.V1]);
                Vector3 vw2 = _session.Obj2.transform.TransformPoint(_session.Verts2[_session.V2]);
                float flat = Vector2.Distance(new Vector2(vw1.x, vw1.z), new Vector2(vw2.x, vw2.z));
                EditorGUILayout.LabelField($"Planar distance: {flat:F3}m   Height delta: {Mathf.Abs(vw1.y - vw2.y):F3}m");
            }
        }

        private void DrawYControls()
        {
            GUILayout.Label("Y Axis", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = LockY ? new Color(1f, 0.4f, 0.4f) : new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button(LockY ? "Y Locked" : "Y Free", GUILayout.Height(28)))
            {
                LockY = !LockY;
                SceneView.RepaintAll();
            }

            GUI.enabled = !LockY;
            GUI.backgroundColor = !LockY && YMidpoint ? new Color(0.3f, 0.7f, 1f) : Color.white;
            if (GUILayout.Button("Midpoint Y", GUILayout.Height(28)))
            {
                YMidpoint = true;
                SceneView.RepaintAll();
            }

            GUI.backgroundColor = !LockY && !YMidpoint ? new Color(1f, 0.6f, 0.2f) : Color.white;
            if (GUILayout.Button("Object 2 Y", GUILayout.Height(28)))
            {
                YMidpoint = false;
                SceneView.RepaintAll();
            }

            GUI.enabled = true;
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox(LockY
                ? "Y locked — connects at the same height."
                : YMidpoint
                    ? "Y free — both objects meet at the midpoint height."
                    : "Y free — both objects extend to Object 2's vertex height.", MessageType.None);
        }

        private void DrawModeControls()
        {
            ConnectMode mode = Mode;
            GUILayout.Label("Connection Mode", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            ModeButton("Standard", ConnectMode.Standard, mode, new Color(0.3f, 0.7f, 1f));
            ModeButton("Corner Meet", ConnectMode.CornerMeet, mode, new Color(0.3f, 1f, 0.5f));
            ModeButton("Full Resize", ConnectMode.FullResize, mode, new Color(1f, 0.8f, 0.2f));
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox(Mode switch
            {
                ConnectMode.Standard => "Only Object 1 resizes to the right-angle corner where edges would meet.",
                ConnectMode.CornerMeet => "Both objects extend to the right-angle corner where their edges intersect.",
                _ => "Object 1 scales on ALL axes so its vertex lands exactly on Object 2's vertex."
            }, MessageType.None);
        }

        private void ModeButton(string label, ConnectMode value, ConnectMode current, Color active)
        {
            GUI.backgroundColor = current == value ? active : Color.white;
            if (GUILayout.Button(label, GUILayout.Height(26)))
            {
                Mode = value;
                SceneView.RepaintAll();
            }
        }

        private void DrawConnectSection()
        {
            bool canConnect = CanConnect(_session, out string reason);
            GUI.enabled = canConnect;
            GUI.backgroundColor = canConnect ? new Color(0.3f, 0.9f, 0.3f) : Color.gray;
            if (GUILayout.Button("Connect Vertices", GUILayout.Height(45)))
            {
                TryConnect(_session, Mode, LockY, YMidpoint);
                RefreshVerts(preserveSelection: true);
            }

            GUI.backgroundColor = Color.white;
            GUI.enabled = true;
            if (!canConnect)
                EditorGUILayout.HelpBox(reason, MessageType.None);

            EditorGUILayout.Space(5);
            if (GUILayout.Button("Refresh Vertices"))
                RefreshVerts(preserveSelection: true);
        }

        private void DrawKeybindSection()
        {
            GUILayout.Label("Keybinds", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("These keys work in the Scene View at all times, even when this window is closed.", MessageType.None);
            DrawKeybindRow("Activate / Confirm", 0, KeyActivate, v => KeyActivate = v);
            DrawKeybindRow("Toggle Mode", 1, KeyToggleMode, v => KeyToggleMode = v);
            DrawKeybindRow("Toggle Y Lock", 2, KeyToggleLockY, v => KeyToggleLockY = v);
            EditorGUILayout.HelpBox("Toggle Y Lock works everywhere in the Scene View. Set to None to disable.", MessageType.None);

            if (_capturingKey >= 0)
            {
                Event e = Event.current;
                if (e != null && e.type == EventType.KeyDown)
                {
                    if (e.keyCode == KeyCode.Escape)
                    {
                        _capturingKey = -1;
                    }
                    else if (e.keyCode != KeyCode.None)
                    {
                        if (_capturingKey == 0)
                        {
                            KeyActivate = e.keyCode;
                        }
                        else if (_capturingKey == 1)
                        {
                            KeyToggleMode = e.keyCode;
                        }
                        else
                            KeyToggleLockY = e.keyCode;

                        _capturingKey = -1;
                    }

                    e.Use();
                    Repaint();
                    SceneView.RepaintAll();
                }
            }
        }

        private void DrawKeybindRow(string label, int index, KeyCode current, Action<KeyCode> set)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(160));
            bool capturing = _capturingKey == index;
            string btn = capturing ? "[ Press a key... ]" : current.ToString();
            GUI.backgroundColor = capturing ? new Color(1f, 0.8f, 0.2f) : Color.white;
            if (GUILayout.Button(btn, GUILayout.Height(22)))
                _capturingKey = capturing ? -1 : index;

            GUI.backgroundColor = Color.white;

            if (index == 2 && current != KeyCode.None && _capturingKey < 0)
            {
                if (GUILayout.Button("None", GUILayout.Width(52)))
                {
                    set(KeyCode.None);
                    SceneView.RepaintAll();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawColoredLabel(string text, Color col)
        {
            GUIStyle s = new(EditorStyles.boldLabel);
            s.normal.textColor = col;
            GUILayout.Label(text, s);
        }

        private static bool CanConnect(Session s, out string reason)
        {
            if (s.Obj1 == null || s.Obj2 == null)
            {
                reason = "Select both objects and one vertex each to enable.";
                return false;
            }

            if (s.Obj1 == s.Obj2)
            {
                reason = "Pick two different objects.";
                return false;
            }

            if (s.V1 < 0 || s.V2 < 0 || s.V1 >= s.Verts1.Length || s.V2 >= s.Verts2.Length)
            {
                reason = "Click one vertex dot on each object to enable.";
                return false;
            }

            reason = "";
            return true;
        }

        private static bool TryConnect(Session s, ConnectMode mode, bool lockY, bool yMid)
        {
            if (!CanConnect(s, out string reason))
            {
                Debug.LogWarning($"Vertex Connector: {reason}");
                return false;
            }

            return mode switch
            {
                ConnectMode.Standard => ConnectStandard(s, lockY, yMid),
                ConnectMode.CornerMeet => ConnectCornerMeet(s, lockY, yMid),
                _ => ConnectFullResize(s, lockY, yMid),
            };

        }

        private static bool ConnectStandard(Session s, bool lockY, bool yMid)
        {
            Vector3 vl1 = s.Verts1[s.V1], vl2 = s.Verts2[s.V2];
            Vector3 vw1 = s.Obj1.transform.TransformPoint(vl1);
            Vector3 vw2 = s.Obj2.transform.TransformPoint(vl2);
            Vector3 corner = GetCornerPoint(vw1, s.Obj1.transform, vl1, vw2, s.Obj2.transform, vl2, lockY, yMid, out bool parallel);

            Undo.RecordObject(s.Obj1.transform, "Vertex Connector: Standard");
            ScaleObjectToReach(s.Obj1, vl1, vw1, corner, parallel);
            if (!lockY)
                ScaleAxisToReach(s.Obj1, Axis.Y, vl1.y, corner);

            EditorUtility.SetDirty(s.Obj1.transform);
            return true;
        }

        private static bool ConnectCornerMeet(Session s, bool lockY, bool yMid)
        {
            Vector3 vl1 = s.Verts1[s.V1], vl2 = s.Verts2[s.V2];
            Vector3 vw1 = s.Obj1.transform.TransformPoint(vl1);
            Vector3 vw2 = s.Obj2.transform.TransformPoint(vl2);
            Vector3 corner = GetCornerPoint(vw1, s.Obj1.transform, vl1, vw2, s.Obj2.transform, vl2, lockY, yMid, out bool parallel);

            Undo.RecordObject(s.Obj1.transform, "Vertex Connector: Corner Meet");
            Undo.RecordObject(s.Obj2.transform, "Vertex Connector: Corner Meet");
            ScaleObjectToReach(s.Obj1, vl1, vw1, corner, parallel);
            ScaleObjectToReach(s.Obj2, vl2, vw2, corner, parallel);
            if (!lockY)
            {
                ScaleAxisToReach(s.Obj1, Axis.Y, vl1.y, corner);
                ScaleAxisToReach(s.Obj2, Axis.Y, vl2.y, corner);
            }

            EditorUtility.SetDirty(s.Obj1.transform);
            EditorUtility.SetDirty(s.Obj2.transform);
            return true;
        }

        private static bool ConnectFullResize(Session s, bool lockY, bool yMid)
        {
            Vector3 vl1 = s.Verts1[s.V1];
            Vector3 target = GetFullResizeTarget(s.Obj1.transform, vl1, s.Obj2.transform, s.Verts2[s.V2], lockY, yMid);

            Undo.RecordObject(s.Obj1.transform, "Vertex Connector: Full Resize");
            ScaleObjectToReachAllAxes(s.Obj1, vl1, target, lockY);
            EditorUtility.SetDirty(s.Obj1.transform);
            return true;
        }

        private static Vector3 GetFullResizeTarget(Transform t1, Vector3 vl1, Transform t2, Vector3 vl2, bool lockY, bool yMid)
        {
            Vector3 vw1 = t1.TransformPoint(vl1);
            Vector3 target = t2.TransformPoint(vl2);
            if (!lockY && yMid)
                target.y = (vw1.y + target.y) * 0.5f;

            return target;
        }

        private static void ScaleObjectToReach(GameObject obj, Vector3 vLocal, Vector3 vWorld, Vector3 target, bool bothAxes)
        {
            if (Vector2.Distance(new Vector2(vWorld.x, vWorld.z), new Vector2(target.x, target.z)) < 0.001f)
                return;

            float wx = Mathf.Abs(vLocal.x) * obj.transform.localScale.x;
            float wz = Mathf.Abs(vLocal.z) * obj.transform.localScale.z;
            bool primaryIsX = wx >= wz;

            ScaleAxisToReach(obj, primaryIsX ? Axis.X : Axis.Z, primaryIsX ? vLocal.x : vLocal.z, target);
            if (bothAxes)
                ScaleAxisToReach(obj, !primaryIsX ? Axis.X : Axis.Z, !primaryIsX ? vLocal.x : vLocal.z, target);
        }

        private static void ScaleAxisToReach(GameObject obj, Axis axis, float vertComp, Vector3 target)
        {
            if (Mathf.Abs(vertComp) < 0.001f)
            {
                Debug.LogWarning($"Vertex Connector: '{obj.name}' vertex is centered on the {axis} axis and cannot be stretched. Try Full Resize.");
                return;
            }

            Transform t = obj.transform;
            Vector3 worldAxis = axis switch
            {
                Axis.X => t.right,
                Axis.Y => t.up,
                _ => t.forward
            };

            float axisScale = axis switch
            {
                Axis.X => t.localScale.x,
                Axis.Y => t.localScale.y,
                _ => t.localScale.z
            };

            float sign = Mathf.Sign(vertComp);
            Vector3 fixedFace = t.position - worldAxis * (axisScale * 0.5f * sign);
            float newWidth = Vector3.Dot(target - fixedFace, worldAxis) * sign;
            if (newWidth < 0.001f)
            {
                Debug.LogWarning($"Vertex Connector: {obj.name} would require negative scale on {axis}.");
                return;
            }

            Vector3 ns = t.localScale;
            if (axis == Axis.X)
            {
                ns.x = newWidth;
            }
            else if (axis == Axis.Y)
            {
                ns.y = newWidth;
            }
            else
                ns.z = newWidth;

            t.localScale = ns;
            t.position = fixedFace + worldAxis * (newWidth * 0.5f * sign);
        }

        private static void ScaleObjectToReachAllAxes(GameObject obj, Vector3 vLocal, Vector3 target, bool lockY)
        {
            ScaleAxisToReach(obj, Axis.X, vLocal.x, target);
            if (!lockY)
                ScaleAxisToReach(obj, Axis.Y, vLocal.y, target);

            ScaleAxisToReach(obj, Axis.Z, vLocal.z, target);
        }

        private static Vector3 GetCornerPoint(Vector3 v1, Transform t1, Vector3 vl1, Vector3 v2, Transform t2, Vector3 vl2, bool lockY, bool yMid, out bool parallel)
        {
            Vector2 p1 = new(v1.x, v1.z), p2 = new(v2.x, v2.z);
            Vector2 d1 = WallAxis2D(t1, vl1, v1, v2);
            Vector2 d2 = WallAxis2D(t2, vl2, v2, v1);

            float denom = d1.x * -d2.y - d1.y * -d2.x;
            Vector2 ix;
            if (Mathf.Abs(denom) < 0.0001f || d1.sqrMagnitude < 0.0001f || d2.sqrMagnitude < 0.0001f)
            {
                parallel = true;
                ix = (p1 + p2) * 0.5f;
            }
            else
            {
                parallel = false;
                Vector2 dp = p2 - p1;
                float t = (dp.x * -d2.y - dp.y * -d2.x) / denom;
                ix = p1 + t * d1;
            }

            float cornerY = lockY ? v1.y : yMid ? (v1.y + v2.y) * 0.5f : v2.y;
            return new Vector3(ix.x, cornerY, ix.y);
        }

        private static Vector2 WallAxis2D(Transform t, Vector3 vLocal, Vector3 from, Vector3 to)
        {
            Vector3 axX = t.right; axX.y = 0f;
            Vector3 axZ = t.forward; axZ.y = 0f;
            float wx = Mathf.Abs(vLocal.x) * t.localScale.x;
            float wz = Mathf.Abs(vLocal.z) * t.localScale.z;
            Vector3 chosen = wx >= wz ? (axX.sqrMagnitude > 0.0001f ? axX.normalized : Vector3.right) : (axZ.sqrMagnitude > 0.0001f ? axZ.normalized : Vector3.forward);

            Vector3 span = to - from; span.y = 0f;
            if (span.sqrMagnitude < 0.000001f)
                return new Vector2(chosen.x, chosen.z);

            float sign = Vector3.Dot(chosen, span) >= 0f ? 1f : -1f;
            return new Vector2(chosen.x, chosen.z) * sign;
        }

        private static ConnectMode CycleMode(ConnectMode m) => m switch
        {
            ConnectMode.Standard => ConnectMode.CornerMeet,
            ConnectMode.CornerMeet => ConnectMode.FullResize,
            _ => ConnectMode.Standard
        };

        private static string ModeLabel(ConnectMode m) => m switch
        {
            ConnectMode.Standard => "Standard",
            ConnectMode.CornerMeet => "Corner Meet",
            _ => "Full Resize"
        };

        private static string StateMessage(Session s) => s.State switch
        {
            FlowState.WaitObj1 => "Click Object 1 in scene",
            FlowState.WaitObj2 => "Click Object 2 in scene",
            FlowState.WaitVerts => s.V1 < 0 ? "Click a vertex on Object 1" : s.V2 < 0 ? "Click a vertex on Object 2" : "Confirm when ready",
            _ => ""
        };

        private void SwapObjects()
        {
            (_session.Obj1, _session.Obj2) = (_session.Obj2, _session.Obj1);
            (_session.Verts1, _session.Verts2) = (_session.Verts2, _session.Verts1);
            (_session.V1, _session.V2) = (_session.V2, _session.V1);
            SceneView.RepaintAll();
            Repaint();
        }

        private void RefreshVerts(bool preserveSelection)
        {
            int keep1 = preserveSelection ? _session.V1 : -1;
            int keep2 = preserveSelection ? _session.V2 : -1;
            Vector3[] next1 = GetUniqueVerts(_session.Obj1);
            Vector3[] next2 = GetUniqueVerts(_session.Obj2);
            _session.Verts1 = next1;
            _session.Verts2 = next2;
            _session.V1 = keep1 >= 0 && keep1 < next1.Length ? keep1 : -1;
            _session.V2 = keep2 >= 0 && keep2 < next2.Length ? keep2 : -1;
            SceneView.RepaintAll();
            Repaint();
        }

        private static Vector3[] GetUniqueVerts(GameObject obj)
        {
            if (obj == null)
                return Array.Empty<Vector3>();

            if (!obj.TryGetComponent(out MeshFilter mf) || mf == null || mf.sharedMesh == null)
                return Array.Empty<Vector3>();

            Vector3[] src = mf.sharedMesh.vertices;
            List<Vector3> unique = new(src.Length);
            HashSet<Vector3Int> seen = new(src.Length);
            foreach (Vector3 v in src)
            {
                Vector3Int key = new(Mathf.RoundToInt(v.x * 1000f), Mathf.RoundToInt(v.y * 1000f), Mathf.RoundToInt(v.z * 1000f));
                if (seen.Add(key))
                    unique.Add(v);
            }

            return unique.ToArray();
        }
    }
}
