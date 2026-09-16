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

        private enum QSState
        {
            Idle,
            WaitObj1,
            WaitObj2,
            WaitVerts
        }

        private static readonly Color ColObj1 = new(0.20f, 0.60f, 1.00f);
        private static readonly Color ColObj2 = new(1.00f, 0.40f, 0.20f);
        private static readonly Color ColSel = Color.yellow;
        private GameObject _obj1;
        private Vector3[] _verts1 = new Vector3[0];
        private int _v1 = -1;
        private GameObject _obj2;
        private Vector3[] _verts2 = new Vector3[0];
        private int _v2 = -1;
        private ConnectMode _mode = ConnectMode.CornerMeet;
        private bool _lockY = true;
        private bool _yMidpoint = true;
        private Vector2 _scroll;
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

        private bool _capAct, _capToggle, _capLockY;
        private QSState _qs = QSState.Idle;
        private bool _openedViaKeybind;
        private static VertexToolWindow _instance;

        [InitializeOnLoadMethod]
        private static void InitAlwaysOn()
        {
            SceneView.duringSceneGui -= StaticSceneGUI;
            SceneView.duringSceneGui += StaticSceneGUI;
        }

        private static QSState _hState = QSState.Idle;
        private static ConnectMode _hMode = ConnectMode.Standard;
        private static bool _hLockY = true;
        private static bool _hYMid = true;
        private static GameObject _hObj1, _hObj2;
        private static Vector3[] _hVerts1 = new Vector3[0];
        private static Vector3[] _hVerts2 = new Vector3[0];
        private static int _hV1 = -1, _hV2 = -1;

        private static void StaticSceneGUI(SceneView sv)
        {
            if (_instance != null)
                return;

            Event e = Event.current;
            if (e == null)
                return;

            if (e.type == EventType.KeyDown && KeyToggleLockY != KeyCode.None && e.keyCode == KeyToggleLockY)
            {
                _hLockY = !EditorPrefs.GetBool(PrefLockY, true);
                EditorPrefs.SetBool(PrefLockY, _hLockY);
                sv.Repaint();
                e.Use();
                return;
            }

            if (e.type == EventType.KeyDown && e.keyCode == KeyToggleMode && _hState != QSState.Idle)
            {
                _hMode = _hMode switch
                {
                    ConnectMode.Standard => ConnectMode.CornerMeet,
                    ConnectMode.CornerMeet => ConnectMode.FullResize,
                    _ => ConnectMode.Standard
                };

                EditorPrefs.SetInt(PrefMode, (int)_hMode);
                sv.Repaint();
                e.Use();
                return;
            }

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape && _hState != QSState.Idle)
            {
                _hState = QSState.Idle; _hObj1 = _hObj2 = null;
                _hVerts1 = _hVerts2 = new Vector3[0]; _hV1 = _hV2 = -1;
                sv.Repaint();
                e.Use();
                return;
            }

            if (e.type == EventType.KeyDown && e.keyCode == KeyActivate)
            {
                if (_hState == QSState.Idle)
                {
                    _hState = QSState.WaitObj1;
                    _hMode = (ConnectMode)EditorPrefs.GetInt(PrefMode, (int)ConnectMode.Standard);
                    _hLockY = EditorPrefs.GetBool(PrefLockY, true);
                    _hYMid = EditorPrefs.GetBool(PrefYMid, true);
                    _hObj1 = _hObj2 = null; _hVerts1 = _hVerts2 = new Vector3[0]; _hV1 = _hV2 = -1;
                    sv.Repaint();
                }
                else if (_hState == QSState.WaitVerts && _hV1 >= 0 && _hV2 >= 0)
                {
                    HeadlessConnect(); _hState = QSState.Idle; sv.Repaint();
                }
                else
                {
                    _hState = QSState.Idle; sv.Repaint();
                }
                e.Use();
                return;
            }

            if ((_hState == QSState.WaitObj1 || _hState == QSState.WaitObj2) && e.type == EventType.MouseDown && e.button == 0)
            {
                GameObject hit = HandleUtility.PickGameObject(e.mousePosition, false);
                if (hit != null ? hit.GetComponent<MeshFilter>().sharedMesh : null != null)
                {
                    if (_hState == QSState.WaitObj1)
                    {
                        _hObj1 = hit; _hVerts1 = GetUniqueVerts(hit); _hV1 = -1;
                        _hState = QSState.WaitObj2;
                    }
                    else if (hit != _hObj1)
                    {
                        _hObj2 = hit; _hVerts2 = GetUniqueVerts(hit); _hV2 = -1;
                        _hState = QSState.WaitVerts;
                    }
                    sv.Repaint();
                    e.Use();
                }
            }

            if (_hState != QSState.Idle)
            {
                _hLockY = EditorPrefs.GetBool(PrefLockY, true);
                _hYMid = EditorPrefs.GetBool(PrefYMid, true);
                DrawHandlesStatic(_hObj1, _hVerts1, ref _hV1, ColObj1, "Obj1", sv);
                DrawHandlesStatic(_hObj2, _hVerts2, ref _hV2, ColObj2, "Obj2", sv);

                if (_hV1 >= 0 && _hV2 >= 0 && _hObj1 != null && _hObj2 != null)
                {
                    Vector3 vw1 = _hObj1.transform.TransformPoint(_hVerts1[_hV1]);
                    Vector3 vw2 = _hObj2.transform.TransformPoint(_hVerts2[_hV2]);
                    DrawConnectionPreview(vw1, vw2, _hObj1.transform, _hObj2.transform, _hVerts1[_hV1], _hVerts2[_hV2], _hMode, _hLockY, _hYMid);
                }

                DrawSceneOverlay(sv, ModeLabel(_hMode), _hState switch
                {
                    QSState.WaitObj1 => "Click Object 1 in scene",
                    QSState.WaitObj2 => "Click Object 2 in scene",
                    QSState.WaitVerts => _hV1 < 0 ? "Click a vertex on Object 1" : _hV2 < 0 ? "Click a vertex on Object 2" : "Confirm when ready",
                    _ => ""
                });
            }
        }

        [MenuItem("Thaumiel/Tools/Vertex Tool")]
        public static void Open()
        {
            VertexToolWindow w = GetWindow<VertexToolWindow>(false, "Vertex Connector");
            w.minSize = new Vector2(340f, 640f);
            w._openedViaKeybind = false;
            w.Show();
        }

        private void OnEnable()
        {
            _instance = this;
            SceneView.duringSceneGui += OnSceneGUI;
            _mode = (ConnectMode)EditorPrefs.GetInt(PrefMode, (int)ConnectMode.CornerMeet);
            _lockY = EditorPrefs.GetBool(PrefLockY, true);
            _yMidpoint = EditorPrefs.GetBool(PrefYMid, true);
        }

        private void OnDisable()
        {
            if (_instance == this)
                _instance = null;

            SceneView.duringSceneGui -= OnSceneGUI;
            EditorPrefs.SetInt(PrefMode, (int)_mode);
            EditorPrefs.SetBool(PrefLockY, _lockY);
            EditorPrefs.SetBool(PrefYMid, _yMidpoint);
            _qs = QSState.Idle;
            _capAct = _capToggle = _capLockY = false;
        }

        private void OnSceneGUI(SceneView sv)
        {
            HandleKeybinds(sv);

            if (_qs != QSState.Idle)
            {
                DrawSceneOverlay(sv, ModeLabel(_mode), _qs switch
                {
                    QSState.WaitObj1 => "Click Object 1 in scene",
                    QSState.WaitObj2 => "Click Object 2 in scene",
                    QSState.WaitVerts => _v1 < 0 ? "Click a vertex on Object 1" : _v2 < 0 ? "Click a vertex on Object 2" : "Confirm when ready",
                    _ => ""
                });
            }

            DrawHandlesInstance(_obj1, _verts1, ref _v1, ColObj1, "Obj1");
            DrawHandlesInstance(_obj2, _verts2, ref _v2, ColObj2, "Obj2");

            if (_v1 >= 0 && _v2 >= 0 && _obj1 != null && _obj2 != null)
            {
                Vector3 vw1 = _obj1.transform.TransformPoint(_verts1[_v1]);
                Vector3 vw2 = _obj2.transform.TransformPoint(_verts2[_v2]);
                DrawConnectionPreview(vw1, vw2, _obj1.transform, _obj2.transform, _verts1[_v1], _verts2[_v2], _mode, _lockY, _yMidpoint);
            }

            Repaint();
        }

        private void DrawHandlesInstance(GameObject obj, Vector3[] verts, ref int sel, Color col, string prefix)
        {
            if (obj == null || verts.Length == 0)
                return;

            Transform t = obj.transform;
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 wp = t.TransformPoint(verts[i]);
                bool isSel = i == sel;
                float size = HandleUtility.GetHandleSize(wp) * (isSel ? 0.12f : 0.08f);
                Handles.color = isSel ? ColSel : col;
                if (Handles.Button(wp, Quaternion.identity, size, size, Handles.SphereHandleCap))
                {
                    sel = i;
                    Repaint();
                }

                GUIStyle ls = new(EditorStyles.boldLabel);
                ls.normal.textColor = isSel ? Color.yellow : col;
                Handles.Label(wp + 1.5f * size * Vector3.up, $"{prefix} V{i}", ls);
            }
        }

        private static void DrawHandlesStatic(GameObject obj, Vector3[] verts, ref int sel, Color col, string prefix, SceneView sv)
        {
            if (obj == null || verts.Length == 0)
                return;

            Transform t = obj.transform;
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 wp = t.TransformPoint(verts[i]);
                bool isSel = i == sel;
                float size = HandleUtility.GetHandleSize(wp) * (isSel ? 0.12f : 0.08f);
                Handles.color = isSel ? ColSel : col;
                if (Handles.Button(wp, Quaternion.identity, size, size, Handles.SphereHandleCap))
                {
                    sel = i;
                    sv.Repaint();
                }

                GUIStyle ls = new(EditorStyles.boldLabel);
                ls.normal.textColor = isSel ? Color.yellow : col;
                Handles.Label(wp + 1.5f * size * Vector3.up, $"{prefix} V{i}", ls);
            }
        }

        private static void DrawConnectionPreview(Vector3 vw1, Vector3 vw2, Transform t1, Transform t2, Vector3 vl1, Vector3 vl2, ConnectMode mode, bool lockY, bool yMid)
        {
            if (mode == ConnectMode.Standard || mode == ConnectMode.CornerMeet)
            {
                Vector3 corner = GetCornerPoint(vw1, t1, vl1, vw2, t2, vl2, lockY, out bool parallel);
                if (!lockY)
                    corner.y = yMid ? (vw1.y + vw2.y) * 0.5f : vw2.y;

                Vector2 wd1 = WallAxis2D(t1, vl1, vw1, vw2);
                Vector2 wd2 = WallAxis2D(t2, vl2, vw2, vw1);
                Vector3 leg1 = -new Vector3(wd1.x, 0f, wd1.y);
                Vector3 leg2 = -new Vector3(wd2.x, 0f, wd2.y);

                Handles.color = ColObj1; Handles.DrawDottedLine(vw1, corner, 4f);
                Handles.color = ColObj2; Handles.DrawDottedLine(vw2, corner, 4f);
                DrawRightAngleMarker(corner, leg1, leg2);

                Handles.color = new Color(0.3f, 1f, 0.4f);
                Handles.SphereHandleCap(0, corner, Quaternion.identity, HandleUtility.GetHandleSize(corner) * 0.15f, EventType.Repaint);
                Handles.Label(corner + Vector3.up * 0.25f, parallel ? "Midpoint (parallel walls)" : "Corner", EditorStyles.boldLabel);
            }
            else
            {
                Vector3 target = GetFullResizeTarget(t1, vl1, t2, vl2, lockY, yMid);
                Handles.color = Color.yellow;
                Handles.DrawDottedLine(vw1, target, 4f);
                Handles.color = new Color(1f, 0.9f, 0.2f);
                Handles.SphereHandleCap(0, target, Quaternion.identity, HandleUtility.GetHandleSize(target) * 0.1f, EventType.Repaint);
                Handles.Label(target + Vector3.up * 0.2f, "Target", EditorStyles.boldLabel);
            }
        }

        private static void DrawRightAngleMarker(Vector3 corner, Vector3 d1, Vector3 d2)
        {
            float sz = HandleUtility.GetHandleSize(corner) * 0.18f;
            Vector3 p1 = corner + d1 * sz, p2 = corner + d2 * sz, pm = p1 + d2 * sz;
            Handles.color = new Color(1f, 1f, 1f, 0.7f);
            Handles.DrawLine(corner, p1); Handles.DrawLine(corner, p2);
            Handles.DrawLine(p1, pm); Handles.DrawLine(p2, pm);
        }

        private static void DrawSceneOverlay(SceneView sv, string modeLabel, string stateMsg)
        {
            Handles.BeginGUI();
            const float w = 380f, h = 68f;
            float x = (sv.position.width - w) * 0.5f;
            float y = sv.position.height - h - 40f;
            EditorGUI.DrawRect(new Rect(x - 6, y - 6, w + 12, h + 12), new Color(0f, 0f, 0f, 0.6f));
            EditorGUI.DrawRect(new Rect(x, y, w, 20f), new Color(0.15f, 0.35f, 0.5f));
            GUI.Label(new Rect(x, y, w, 20f), $"  Mode: {modeLabel}  ({KeyToggleMode} to toggle)", new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.white }, alignment = TextAnchor.MiddleLeft });
            GUI.Label(new Rect(x, y + 22f, w, 24f), $"  {stateMsg}", new GUIStyle(EditorStyles.label) { normal = { textColor = Color.white }, fontSize = 12 });
            GUI.Label(new Rect(x, y + 46f, w, 20f), $"  [{KeyActivate}] confirm   [Esc] cancel", new GUIStyle(EditorStyles.label) { normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }, fontSize = 11 });
            Handles.EndGUI();
        }

        private void HandleKeybinds(SceneView sv)
        {
            Event e = Event.current;
            if (e == null)
                return;

            if (e.type == EventType.KeyDown && KeyToggleLockY != KeyCode.None && e.keyCode == KeyToggleLockY)
            {
                _lockY = !_lockY; EditorPrefs.SetBool(PrefLockY, _lockY);
                Repaint(); sv.Repaint(); e.Use(); return;
            }

            if (e.type == EventType.KeyDown && e.keyCode == KeyToggleMode)
            {
                _mode = _mode switch
                {
                    ConnectMode.Standard => ConnectMode.CornerMeet,
                    ConnectMode.CornerMeet => ConnectMode.FullResize,
                    _ => ConnectMode.Standard
                };
                EditorPrefs.SetInt(PrefMode, (int)_mode);
                Repaint(); sv.Repaint(); e.Use(); return;
            }

            if (e.type == EventType.KeyDown && e.keyCode == KeyActivate)
            {
                switch (_qs)
                {
                    case QSState.Idle:
                        _obj1 = _obj2 = null; _verts1 = _verts2 = new Vector3[0]; _v1 = _v2 = -1;
                        _qs = QSState.WaitObj1; Repaint(); break;

                    case QSState.WaitVerts:
                        if (_v1 >= 0 && _v2 >= 0 && _obj1 != null && _obj2 != null)
                        {
                            DoConnect(); _qs = QSState.Idle; _openedViaKeybind = false; Repaint();
                        }
                        break;

                    default:
                        _qs = QSState.Idle; _openedViaKeybind = false; Repaint(); break;
                }
                e.Use();
                return;
            }

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape && _qs != QSState.Idle)
            {
                _qs = QSState.Idle; _openedViaKeybind = false;
                Repaint();
                sv.Repaint();
                e.Use();
                return;
            }

            if ((_qs == QSState.WaitObj1 || _qs == QSState.WaitObj2) && e.type == EventType.MouseDown && e.button == 0)
            {
                GameObject hit = HandleUtility.PickGameObject(e.mousePosition, false);
                if (hit != null ? hit.GetComponent<MeshFilter>().sharedMesh : null != null)
                {
                    if (_qs == QSState.WaitObj1)
                    {
                        _obj1 = hit; _verts1 = GetUniqueVerts(hit); _v1 = -1;
                        _qs = QSState.WaitObj2;
                    }
                    else if (hit != _obj1)
                    {
                        _obj2 = hit; _verts2 = GetUniqueVerts(hit); _v2 = -1;
                        _qs = QSState.WaitVerts;
                    }

                    Repaint();
                    sv.Repaint();
                    e.Use();
                }
            }
        }

        private void OnGUI()
        {
            if (_openedViaKeybind)
                return;

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            GUILayout.Label("Vertex Connector", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Click colored dots in the Scene View to pick vertices, or use the buttons below.", MessageType.Info);
            EditorGUILayout.Space(8);

            DrawColoredLabel("Object 1 — Resize This  (Blue dots)", ColObj1);
            DrawObjectField(ref _obj1, ref _verts1, ref _v1);
            if (_verts1.Length > 0)
                DrawVertexGrid(_verts1, _obj1, ref _v1, ColObj1);

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.backgroundColor = new Color(1f, 0.85f, 0.2f);
            if (GUILayout.Button("⇅  Swap Objects", GUILayout.Width(150), GUILayout.Height(26)))
                SwapObjects();

            GUI.backgroundColor = Color.white;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);

            DrawColoredLabel("Object 2 — Snap Target  (Orange dots)", ColObj2);
            DrawObjectField(ref _obj2, ref _verts2, ref _v2);
            if (_verts2.Length > 0)
                DrawVertexGrid(_verts2, _obj2, ref _v2, ColObj2);

            EditorGUILayout.Space(8);
            if (_v1 >= 0 && _obj1 != null)
            {
                Vector3 v = _obj1.transform.TransformPoint(_verts1[_v1]);
                EditorGUILayout.LabelField($"V1 World: ({v.x:F3}, {v.y:F3}, {v.z:F3})");
            }
            if (_v2 >= 0 && _obj2 != null)
            {
                Vector3 v = _obj2.transform.TransformPoint(_verts2[_v2]);
                EditorGUILayout.LabelField($"V2 World: ({v.x:F3}, {v.y:F3}, {v.z:F3})");
            }

            EditorGUILayout.Space(8);
            GUILayout.Label("Y Axis", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = _lockY ? new Color(1f, 0.4f, 0.4f) : new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button(_lockY ? "🔒 Y  Locked" : "🔓 Y  Free", GUILayout.Height(28)))
            { 
                _lockY = !_lockY;
                EditorPrefs.SetBool(PrefLockY, _lockY);
                SceneView.RepaintAll();
            }

            GUI.enabled = !_lockY;
            GUI.backgroundColor = (!_lockY && _yMidpoint) ? new Color(0.3f, 0.7f, 1f) : Color.white;
            if (GUILayout.Button("Midpoint Y", GUILayout.Height(28)))
            {
                _yMidpoint = true;
                EditorPrefs.SetBool(PrefYMid, true);
                SceneView.RepaintAll();
            }

            GUI.backgroundColor = (!_lockY && !_yMidpoint) ? new Color(1f, 0.6f, 0.2f) : Color.white;
            if (GUILayout.Button("Object 2 Y", GUILayout.Height(28)))
            {
                _yMidpoint = false;
                EditorPrefs.SetBool(PrefYMid, false);
                SceneView.RepaintAll();
            }

            GUI.enabled = true;
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox(_lockY ? "Y locked — connects at the same height." : _yMidpoint ? "Y free — both objects meet at the midpoint height." : "Y free — both objects extend to Object 2's vertex height.", MessageType.None);
            EditorGUILayout.Space(8);
            GUILayout.Label("Connection Mode", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = _mode == ConnectMode.Standard ? new Color(0.3f, 0.7f, 1f) : Color.white;
            if (GUILayout.Button("Standard", GUILayout.Height(26)))
            {
                _mode = ConnectMode.Standard;
                EditorPrefs.SetInt(PrefMode, (int)_mode);
                SceneView.RepaintAll();
            }

            GUI.backgroundColor = _mode == ConnectMode.CornerMeet ? new Color(0.3f, 1f, 0.5f) : Color.white;
            if (GUILayout.Button("Corner Meet", GUILayout.Height(26)))
            {
                _mode = ConnectMode.CornerMeet;
                EditorPrefs.SetInt(PrefMode, (int)_mode);
                SceneView.RepaintAll();
            }

            GUI.backgroundColor = _mode == ConnectMode.FullResize ? new Color(1f, 0.8f, 0.2f) : Color.white;
            if (GUILayout.Button("Full Resize", GUILayout.Height(26)))
            {
                _mode = ConnectMode.FullResize;
                EditorPrefs.SetInt(PrefMode, (int)_mode);
                SceneView.RepaintAll();
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox(_mode switch
            {
                ConnectMode.Standard => "Only Object 1 resizes to the right-angle corner where edges would meet.",
                ConnectMode.CornerMeet => "Both objects extend to the right-angle corner where their edges intersect.",
                _ => "Object 1 scales on ALL axes so its vertex lands exactly on Object 2's vertex."
            }, MessageType.None);

            EditorGUILayout.Space(8);
            bool canConnect = _obj1 != null && _obj2 != null && _v1 >= 0 && _v2 >= 0;
            GUI.enabled = canConnect;
            GUI.backgroundColor = canConnect ? new Color(0.3f, 0.9f, 0.3f) : Color.gray;
            if (GUILayout.Button("Connect Vertices", GUILayout.Height(45)))
                DoConnect();

            GUI.backgroundColor = Color.white;
            GUI.enabled = true;
            if (!canConnect)
                EditorGUILayout.HelpBox("Select both objects and one vertex each to enable.", MessageType.None);

            EditorGUILayout.Space(5);
            if (GUILayout.Button("Refresh Vertices"))
                RefreshAll();

            EditorGUILayout.Space(10);
            GUILayout.Label("Keybinds", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("These keys work in the Scene View at all times, even when this window is closed.", MessageType.None);
            DrawKeybindRow("Activate / Confirm", ref _capAct, ref _capToggle, KeyActivate);
            DrawKeybindRow("Toggle Mode", ref _capToggle, ref _capAct, KeyToggleMode);
            DrawKeybindRow("Toggle Y Lock", ref _capLockY, ref _capAct, KeyToggleLockY);
            EditorGUILayout.HelpBox("Toggle Y Lock works everywhere in the Scene View. Set to None to disable.", MessageType.None);

            if (_capAct || _capToggle || _capLockY)
            {
                Event e = Event.current;
                if (e != null && e.type == EventType.KeyDown)
                {
                    if (e.keyCode == KeyCode.Escape)
                    {
                        _capAct = _capToggle = _capLockY = false;
                    }
                    else if (e.keyCode != KeyCode.None)
                    {
                        if (_capAct)
                        {
                            KeyActivate = e.keyCode;
                        }
                        else if (_capToggle)
                        {
                            KeyToggleMode = e.keyCode;
                        }
                        else
                            KeyToggleLockY = e.keyCode;

                        _capAct = _capToggle = _capLockY = false;
                    }

                    e.Use();
                    Repaint();
                    SceneView.RepaintAll();
                }
                Focus();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DoConnect()
        {
            switch (_mode)
            {
                case ConnectMode.Standard:
                    ConnectStandard();
                    break;

                case ConnectMode.CornerMeet:
                    ConnectCornerMeet();
                    break;

                default:
                    ConnectFullResize();
                    break;
            }
        }

        private void ConnectStandard()
        {
            Vector3 vl1 = _verts1[_v1], vl2 = _verts2[_v2];
            Vector3 vw1 = _obj1.transform.TransformPoint(vl1);
            Vector3 vw2 = _obj2.transform.TransformPoint(vl2);
            Vector3 corner = GetCornerPoint(vw1, _obj1.transform, vl1, vw2, _obj2.transform, vl2, _lockY, out bool parallel);
            if (!_lockY)
                corner.y = _yMidpoint ? (vw1.y + vw2.y) * 0.5f : vw2.y;

            Undo.RecordObject(_obj1.transform, "Vertex Connector: Standard");
            ScaleObjectToReach(_obj1, vl1, vw1, corner, parallel);
            if (!_lockY)
                ScaleObjectToReachY(_obj1, vl1, corner);

            RefreshAll();
        }

        private void ConnectCornerMeet()
        {
            Vector3 vl1 = _verts1[_v1], vl2 = _verts2[_v2];
            Vector3 vw1 = _obj1.transform.TransformPoint(vl1);
            Vector3 vw2 = _obj2.transform.TransformPoint(vl2);
            Vector3 corner = GetCornerPoint(vw1, _obj1.transform, vl1, vw2, _obj2.transform, vl2, _lockY, out bool parallel);
            if (!_lockY) corner.y = _yMidpoint ? (vw1.y + vw2.y) * 0.5f : vw2.y;

            Undo.RecordObject(_obj1.transform, "Vertex Connector: Corner Meet");
            Undo.RecordObject(_obj2.transform, "Vertex Connector: Corner Meet");
            ScaleObjectToReach(_obj1, vl1, vw1, corner, parallel);
            ScaleObjectToReach(_obj2, vl2, vw2, corner, parallel);
            if (!_lockY)
            {
                ScaleObjectToReachY(_obj1, vl1, corner);
                ScaleObjectToReachY(_obj2, vl2, corner);
            }
            RefreshAll();
        }

        private void ConnectFullResize()
        {
            Vector3 vl1 = _verts1[_v1];
            Vector3 target = GetFullResizeTarget(_obj1.transform, vl1, _obj2.transform, _verts2[_v2], _lockY, _yMidpoint);

            Undo.RecordObject(_obj1.transform, "Vertex Connector: Full Resize");
            ScaleObjectToReachAllAxes(_obj1, vl1, target, _lockY);
            RefreshAll();
        }

        private static Vector3 GetFullResizeTarget(Transform t1, Vector3 vl1, Transform t2, Vector3 vl2, bool lockY, bool yMid)
        {
            Vector3 vw1 = t1.TransformPoint(vl1);
            Vector3 target = t2.TransformPoint(vl2);
            if (!lockY && yMid)
                target.y = (vw1.y + target.y) * 0.5f;

            return target;
        }

        private static void HeadlessConnect()
        {
            if (_hObj1 == null || _hObj2 == null) return;
            Vector3 vl1 = _hVerts1[_hV1], vl2 = _hVerts2[_hV2];
            Vector3 vw1 = _hObj1.transform.TransformPoint(vl1);
            Vector3 vw2 = _hObj2.transform.TransformPoint(vl2);

            Undo.RecordObject(_hObj1.transform, "Vertex Connector: Headless");
            Undo.RecordObject(_hObj2.transform, "Vertex Connector: Headless");

            if (_hMode == ConnectMode.Standard)
            {
                Vector3 corner = GetCornerPoint(vw1, _hObj1.transform, vl1, vw2, _hObj2.transform, vl2, _hLockY, out bool parallel);
                if (!_hLockY)
                    corner.y = _hYMid ? (vw1.y + vw2.y) * 0.5f : vw2.y;

                ScaleObjectToReach(_hObj1, vl1, vw1, corner, parallel);
                if (!_hLockY)
                    ScaleObjectToReachY(_hObj1, vl1, corner);
            }
            else if (_hMode == ConnectMode.CornerMeet)
            {
                Vector3 corner = GetCornerPoint(vw1, _hObj1.transform, vl1, vw2, _hObj2.transform, vl2, _hLockY, out bool parallel);
                if (!_hLockY) corner.y = _hYMid ? (vw1.y + vw2.y) * 0.5f : vw2.y;
                ScaleObjectToReach(_hObj1, vl1, vw1, corner, parallel);
                ScaleObjectToReach(_hObj2, vl2, vw2, corner, parallel);
                if (!_hLockY)
                {
                    ScaleObjectToReachY(_hObj1, vl1, corner);
                    ScaleObjectToReachY(_hObj2, vl2, corner);
                }
            }
            else
            {
                Vector3 target = GetFullResizeTarget(_hObj1.transform, vl1, _hObj2.transform, vl2, _hLockY, _hYMid);
                ScaleObjectToReachAllAxes(_hObj1, vl1, target, _hLockY);
            }
        }

        private static void ScaleObjectToReach(GameObject obj, Vector3 vLocal, Vector3 vWorld, Vector3 target, bool bothAxes)
        {
            if (Vector2.Distance(new Vector2(vWorld.x, vWorld.z), new Vector2(target.x, target.z)) < 0.001f)
                return;

            Transform t = obj.transform;
            float wx = Mathf.Abs(vLocal.x) * t.localScale.x;
            float wz = Mathf.Abs(vLocal.z) * t.localScale.z;
            bool primaryIsX = wx >= wz;

            ScaleXZAxisToReach(obj, primaryIsX, primaryIsX ? vLocal.x : vLocal.z, target);
            if (bothAxes)
                ScaleXZAxisToReach(obj, !primaryIsX, !primaryIsX ? vLocal.x : vLocal.z, target);
        }

        private static void ScaleXZAxisToReach(GameObject obj, bool isX, float vertComp, Vector3 target)
        {
            if (Mathf.Abs(vertComp) < 0.001f)
            {
                Debug.LogWarning($"Vertex Connector: '{obj.name}' vertex is centered on the {(isX ? "X" : "Z")} axis and cannot be stretched. Try Full Resize.");
                return;
            }

            Transform t = obj.transform;
            Vector3 worldAxis = isX ? t.right : t.forward;
            float axisScale = isX ? t.localScale.x : t.localScale.z;

            float sign = Mathf.Sign(vertComp);
            Vector3 fixedFace = t.position - worldAxis * (axisScale * 0.5f * sign);
            float newWidth = Vector3.Dot(target - fixedFace, worldAxis) * sign;
            if (newWidth < 0.001f)
            {
                Debug.LogWarning($"Vertex Connector: {obj.name} would require negative scale.");
                return;
            }

            Vector3 ns = t.localScale;
            if (isX)
            {
                ns.x = newWidth;
            }
            else
                ns.z = newWidth;

            t.localScale = ns;
            t.position = fixedFace + worldAxis * (newWidth * 0.5f * sign);
        }

        private static void ScaleObjectToReachY(GameObject obj, Vector3 vLocal, Vector3 target)
        {
            if (Mathf.Abs(vLocal.y) < 0.001f)
                return;

            Transform t = obj.transform;
            float sign = Mathf.Sign(vLocal.y);
            Vector3 fix = t.position - t.up * (t.localScale.y * 0.5f * sign);
            float newH = Vector3.Dot(target - fix, t.up) * sign;
            if (newH < 0.001f)
            {
                Debug.LogWarning($"Vertex Connector: Y resize on {obj.name} would go negative.");
                return;
            }

            Vector3 ns = t.localScale; ns.y = newH;
            t.localScale = ns; t.position = fix + t.up * (newH * 0.5f * sign);
        }

        private static void ScaleObjectToReachAllAxes(GameObject obj, Vector3 vLocal, Vector3 target, bool lockY)
        {
            Transform t = obj.transform; Vector3 scale = t.localScale;
            void Ax(Vector3 axis, float sc, float comp)
            {
                if (Mathf.Abs(comp) < 0.001f)
                    return;

                if (axis == t.up && lockY)
                    return;

                float sign = Mathf.Sign(comp);
                Vector3 fix = t.position - axis * (sc * 0.5f * sign);
                float nw = Vector3.Dot(target - fix, axis) * sign;
                if (nw < 0.001f)
                {
                    Debug.LogWarning($"Vertex Connector FullResize: axis on {obj.name} would go negative.");
                    return;
                }

                Vector3 ns = t.localScale;
                if (axis == t.right) 
                {
                    ns.x = nw;
                }
                else if (axis == t.forward) 
                {
                    ns.z = nw;
                }
                else
                    ns.y = nw;

                t.localScale = ns; t.position = fix + axis * (nw * 0.5f * sign);
            }

            Ax(t.right, scale.x, vLocal.x);
            scale = t.localScale;
            Ax(t.up, scale.y, vLocal.y);
            scale = t.localScale;
            Ax(t.forward, scale.z, vLocal.z);
        }

        private static Vector3 GetCornerPoint(Vector3 v1, Transform t1, Vector3 vl1, Vector3 v2, Transform t2, Vector3 vl2, bool lockY, out bool parallel)
        {
            Vector2 p1 = new(v1.x, v1.z), p2 = new(v2.x, v2.z);
            Vector2 d1 = WallAxis2D(t1, vl1, v1, v2), d2 = WallAxis2D(t2, vl2, v2, v1);

            float denom = d1.x * (-d2.y) - d1.y * (-d2.x);
            Vector2 ix;
            if (Mathf.Abs(denom) < 0.0001f)
            {
                parallel = true;
                ix = (p1 + p2) * 0.5f;
            }
            else
            {
                parallel = false;
                Vector2 dp = p2 - p1;
                float t = (dp.x * (-d2.y) - dp.y * (-d2.x)) / denom;
                ix = p1 + t * d1;
            }
            float cornerY = lockY ? v1.y : (v1.y + v2.y) * 0.5f;
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
            float sign = Vector3.Dot(chosen, span) >= 0f ? 1f : -1f;
            return new Vector2(chosen.x, chosen.z) * sign;
        }

        private void DrawObjectField(ref GameObject obj, ref Vector3[] verts, ref int sel)
        {
            GameObject next = (GameObject)EditorGUILayout.ObjectField("Object", obj, typeof(GameObject), true);
            if (next != obj)
            {
                obj = next; verts = GetUniqueVerts(obj);
                sel = -1;
                SceneView.RepaintAll();
            }
            if (obj != null && verts.Length == 0)
                EditorGUILayout.HelpBox("No mesh found on this object.", MessageType.Warning);
        }

        private void DrawVertexGrid(Vector3[] verts, GameObject obj, ref int sel, Color col)
        {
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

                    GUI.backgroundColor = (i == sel) ? Color.yellow : col * 0.8f;
                    Vector3 wp = obj.transform.TransformPoint(verts[i]);
                    if (GUILayout.Button(new GUIContent($"V{i}", $"({wp.x:F2}, {wp.y:F2}, {wp.z:F2})"), GUILayout.Width(50)))
                    { 
                        sel = i;
                        SceneView.RepaintAll();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            GUI.backgroundColor = Color.white;
        }

        private static void DrawColoredLabel(string text, Color col)
        {
            GUIStyle s = new(EditorStyles.boldLabel);
            s.normal.textColor = col;
            GUILayout.Label(text, s);
        }

        private void DrawKeybindRow(string label, ref bool capturing, ref bool other, KeyCode current)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(160));
            string btn = capturing ? "[ Press a key... ]" : current.ToString();
            GUI.backgroundColor = capturing ? new Color(1f, 0.8f, 0.2f) : Color.white;
            if (GUILayout.Button(btn, GUILayout.Height(22)))
            {
                if (!capturing)
                {
                    capturing = true;
                    other = false;
                }
                else 
                    capturing = false;

                Repaint();
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        private void SwapObjects()
        {
            (_obj1, _obj2) = (_obj2, _obj1);
            (_verts1, _verts2) = (_verts2, _verts1);
            (_v1, _v2) = (_v2, _v1);
            SceneView.RepaintAll(); Repaint();
        }

        private void RefreshAll()
        {
            _verts1 = GetUniqueVerts(_obj1); _verts2 = GetUniqueVerts(_obj2);
            _v1 = -1; _v2 = -1; SceneView.RepaintAll(); Repaint();
        }

        private static Vector3[] GetUniqueVerts(GameObject obj)
        {
            if (obj == null)
                return new Vector3[0];

            MeshFilter mf = obj.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
                return new Vector3[0];

            List<Vector3> unique = new();
            foreach (Vector3 v in mf.sharedMesh.vertices)
            {
                bool found = false;
                foreach (Vector3 u in unique)
                {
                    if (Vector3.Distance(v, u) < 0.001f)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    unique.Add(v);
            }
            return unique.ToArray();
        }

        private static string ModeLabel(ConnectMode m) => m switch
        {
            ConnectMode.Standard => "Standard",
            ConnectMode.CornerMeet => "Corner Meet",
            _ => "Full Resize"
        };
    }
}