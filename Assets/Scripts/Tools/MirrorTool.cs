using System;
using System.Collections.Generic;
using Assets.Scripts.Collab;
using Assets.Scripts.Components;
using Assets.Scripts.Components.Objects;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Tools
{
    public enum MirrorAxis
    {
        X,
        Y,
        Z
    }

    public class MirrorToolWindow : EditorWindow
    {
        private GameObject _host;
        private MirrorAxis _axis = MirrorAxis.X;
        private bool _duplicate = true;
        private bool _mirrorRotation = true;
        private bool _negateScale = true;
        private bool _showPreview = true;
        private Vector2 _scroll;

        private const string PrefAxis = "MTW_Axis";
        private const string PrefDuplicate = "MTW_Duplicate";
        private const string PrefMirrorRot = "MTW_MirrorRot";
        private const string PrefNegScale = "MTW_NegScale";
        private const string PrefPreview = "MTW_Preview";

        private static readonly Color ColPlane = new(0.30f, 0.70f, 1.00f);
        private static readonly Color ColMirror = new(0.35f, 1.00f, 0.45f);

        [MenuItem("Thaumiel/Tools/Mirror Tool %#m")]
        public static void Open()
        {
            MirrorToolWindow w = GetWindow<MirrorToolWindow>(false, "Mirror Tool");
            w.minSize = new Vector2(320f, 480f);
            w.Show();
        }

        private void OnEnable()
        {
            _axis = (MirrorAxis)EditorPrefs.GetInt(PrefAxis, (int)MirrorAxis.X);
            _duplicate = EditorPrefs.GetBool(PrefDuplicate, true);
            _mirrorRotation = EditorPrefs.GetBool(PrefMirrorRot, true);
            _negateScale = EditorPrefs.GetBool(PrefNegScale, true);
            _showPreview = EditorPrefs.GetBool(PrefPreview, true);
            SceneView.duringSceneGui += OnSceneGUI;
            Selection.selectionChanged += OnSelectionChanged;
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            Selection.selectionChanged -= OnSelectionChanged;
            Undo.undoRedoPerformed -= OnUndoRedo;
            EditorPrefs.SetInt(PrefAxis, (int)_axis);
            EditorPrefs.SetBool(PrefDuplicate, _duplicate);
            EditorPrefs.SetBool(PrefMirrorRot, _mirrorRotation);
            EditorPrefs.SetBool(PrefNegScale, _negateScale);
            EditorPrefs.SetBool(PrefPreview, _showPreview);
        }

        private void OnSelectionChanged() => Repaint();

        private void OnUndoRedo()
        {
            Repaint();
            SceneView.RepaintAll();
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            GUILayout.Label("Mirror Tool", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Mirror objects across a plane.", MessageType.Info);
            EditorGUILayout.Space(6);

            GUILayout.Label("Mirror Plane", EditorStyles.boldLabel);
            GameObject nextHost = (GameObject)EditorGUILayout.ObjectField("Host Object", _host, typeof(GameObject), true);
            if (nextHost != _host)
            {
                _host = nextHost;
                SceneView.RepaintAll();
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.enabled = Selection.activeGameObject != null;
            if (GUILayout.Button("Use Active Selection", GUILayout.Width(170), GUILayout.Height(24)))
            {
                _host = Selection.activeGameObject;
                SceneView.RepaintAll();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            _axis = (MirrorAxis)EditorGUILayout.EnumPopup("Mirror Axis", _axis);

            EditorGUILayout.Space(6);
            GUILayout.Label("Operation", EditorStyles.boldLabel);
            _duplicate = EditorGUILayout.Toggle("Duplicate (keep originals)", _duplicate);
            _mirrorRotation = EditorGUILayout.Toggle("Mirror Rotation", _mirrorRotation);
            _negateScale = EditorGUILayout.Toggle("Negate Scale (true mirror)", _negateScale);
            _showPreview = EditorGUILayout.Toggle("Scene Preview", _showPreview);

            EditorGUILayout.Space(6);
            List<GameObject> targets = GetMirrorTargets(_host);

            if (_host == null)
            {
                EditorGUILayout.HelpBox("Pick a host object first.", MessageType.Warning);
            }
            else if (targets.Count == 0)
                EditorGUILayout.HelpBox("Select one or more scene objects to mirror.", MessageType.Warning);

            EditorGUILayout.Space(4);
            bool canApply = _host != null && targets.Count > 0;
            GUI.enabled = canApply;
            GUI.backgroundColor = canApply ? new Color(0.3f, 0.9f, 0.3f) : Color.gray;
            if (GUILayout.Button(_duplicate ? "Mirror Duplicate" : "Mirror In Place", GUILayout.Height(40)))
            {
                ApplyMirror(_host, _axis, targets, _duplicate, _mirrorRotation, _negateScale);
                SceneView.RepaintAll();
            }

            GUI.backgroundColor = Color.white;
            GUI.enabled = true;

            EditorGUILayout.Space(6);
            EditorGUILayout.EndScrollView();
        }

        private void OnSceneGUI(SceneView sv)
        {
            if (!_showPreview || _host == null)
                return;

            if (!GetPlane(_host, _axis, out Vector3 point, out Vector3 normal))
                return;

            float radius = 4f;
            List<GameObject> targets = GetMirrorTargets(_host);
            foreach (GameObject go in targets)
            {
                if (go == null)
                    continue;

                radius = Mathf.Max(radius, Vector3.Distance(go.transform.position, point) + 1f);
            }

            radius = Mathf.Min(radius, 60f);
            Handles.color = ColPlane;
            Handles.DrawWireDisc(point, normal, radius);
            Handles.ArrowHandleCap(0, point, Quaternion.LookRotation(normal), HandleUtility.GetHandleSize(point) * 1.2f, EventType.Repaint);
            GUIStyle ls = new(EditorStyles.boldLabel);
            ls.normal.textColor = ColPlane;
            Handles.Label(point + normal * 0.5f, $"Mirror {_axis} @ {_host.name}", ls);

            Handles.color = ColMirror;
            foreach (GameObject go in targets)
            {
                if (go == null)
                    continue;

                Vector3 src = go.transform.position;
                Vector3 dst = ReflectPosition(src, point, normal);
                Handles.DrawDottedLine(src, dst, 4f);
                Handles.SphereHandleCap(0, dst, Quaternion.identity, HandleUtility.GetHandleSize(dst) * 0.12f, EventType.Repaint);
            }

            if (targets.Count > 0)
                Repaint();
        }

        public static bool GetPlane(GameObject host, MirrorAxis axis, out Vector3 point, out Vector3 normal)
        {
            point = Vector3.zero;
            normal = Vector3.right;
            if (host == null)
                return false;

            Transform t = host.transform;
            point = t.position;
            normal = axis switch
            {
                MirrorAxis.X => t.right,
                MirrorAxis.Y => t.up,
                MirrorAxis.Z => t.forward,
                _ => t.right,
            };

            if (normal.sqrMagnitude < 0.000001f)
                normal = Vector3.right;

            normal.Normalize();
            return true;
        }

        public static Vector3 ReflectPosition(Vector3 pos, Vector3 planePoint, Vector3 planeNormal) => pos - 2f * Vector3.Dot(pos - planePoint, planeNormal) * planeNormal;

        public static Matrix4x4 ReflectionMatrix(Vector3 n)
        {
            n.Normalize();
            Matrix4x4 m = Matrix4x4.identity;
            m.m00 = 1f - 2f * n.x * n.x;
            m.m01 = -2f * n.x * n.y;
            m.m02 = -2f * n.x * n.z;
            m.m10 = -2f * n.y * n.x;
            m.m11 = 1f - 2f * n.y * n.y;
            m.m12 = -2f * n.y * n.z;
            m.m20 = -2f * n.z * n.x;
            m.m21 = -2f * n.z * n.y;
            m.m22 = 1f - 2f * n.z * n.z;
            return m;
        }

        public static Quaternion MirrorRotation(Quaternion srcWorldRot, Vector3 planeNormal)
        {
            Matrix4x4 m = ReflectionMatrix(planeNormal);
            Matrix4x4 r = Matrix4x4.Rotate(srcWorldRot);
            Matrix4x4 mirrored = m * r * m;
            return Quaternion.LookRotation(mirrored.GetColumn(2), mirrored.GetColumn(1));
        }

        public static List<GameObject> GetMirrorTargets(GameObject host)
        {
            List<GameObject> result = new();
            if (Selection.gameObjects == null)
                return result;

            HashSet<GameObject> selected = new();
            foreach (GameObject go in Selection.gameObjects)
            {
                if (go == null || go == host)
                    continue;

                if (!go.scene.IsValid())
                    continue;

                if (PrefabUtility.IsPartOfPrefabAsset(go))
                    continue;

                if (go.GetComponent<Builder>() != null)
                    continue;

                if (go.GetComponent<ServerSide>() != null && go.GetComponent<ObjectBase>() == null)
                    continue;

                selected.Add(go);
            }

            foreach (GameObject go in selected)
            {
                bool nested = false;
                Transform p = go.transform.parent;
                while (p != null)
                {
                    if (selected.Contains(p.gameObject))
                    {
                        nested = true;
                        break;
                    }

                    p = p.parent;
                }

                if (!nested)
                    result.Add(go);
            }

            return result;
        }

        public static List<GameObject> ApplyMirror(GameObject host, MirrorAxis axis, List<GameObject> targets, bool duplicate, bool mirrorRotation, bool negateScale)
        {
            List<GameObject> affected = new();
            if (host == null || targets == null || targets.Count == 0)
                return affected;

            if (!GetPlane(host, axis, out Vector3 planePoint, out Vector3 planeNormal))
                return affected;

            if (!duplicate)
            {
                foreach (GameObject go in targets)
                {
                    if (go == null)
                        continue;

                    MirrorInPlace(go.transform, planePoint, planeNormal, mirrorRotation, negateScale);
                    affected.Add(go);
                }

                return affected;
            }

            Dictionary<Guid, Guid> teleporterIdMap = new();
            Dictionary<Guid, TeleporterObject> newTeleporters = new();
            List<(GameObject copy, GameObject src)> copies = new();

            foreach (GameObject src in targets)
            {
                if (src == null)
                    continue;

                Guid oldTeleporterId = Guid.Empty;
                if (src.TryGetComponent<TeleporterObject>(out var srcTp))
                    oldTeleporterId = srcTp.Id;

                GameObject copy = Instantiate(src);
                copy.transform.SetParent(src.transform.parent, false);
                copy.name = src.name + " (Mirrored)";

                if (copy.TryGetComponent<CollabId>(out var cid))
                {
                    cid.Regenerate();
                }
                else
                    copy.AddComponent<CollabId>();

                if (copy.TryGetComponent<TeleporterObject>(out var copyTp))
                {
                    Guid newId = Guid.NewGuid();
                    copyTp.Id = newId;
                    if (oldTeleporterId != Guid.Empty)
                    {
                        teleporterIdMap[oldTeleporterId] = newId;
                        newTeleporters[newId] = copyTp;
                    }
                }

                MirrorInPlace(copy.transform, planePoint, planeNormal, mirrorRotation, negateScale);
                Undo.RegisterCreatedObjectUndo(copy, "Mirror Duplicate");
                EditorUtility.SetDirty(copy.transform);
                affected.Add(copy);
                copies.Add((copy, src));
            }

            if (teleporterIdMap.Count > 0)
            {
                Dictionary<Guid, TeleporterObject> byId = new(newTeleporters);
                foreach (TeleporterObject tp in FindObjectsByType<TeleporterObject>(FindObjectsSortMode.None))
                {
                    if (tp == null || tp.Id == Guid.Empty || byId.ContainsKey(tp.Id))
                        continue;

                    byId[tp.Id] = tp;
                }

                foreach ((GameObject copy, GameObject _) in copies)
                {
                    if (!copy.TryGetComponent<TeleporterObject>(out var copyTp))
                        continue;

                    bool changed = false;
                    if (copyTp.TargetIds != null)
                    {
                        for (int i = 0; i < copyTp.TargetIds.Count; i++)
                        {
                            if (teleporterIdMap.TryGetValue(copyTp.TargetIds[i], out Guid mapped))
                            {
                                copyTp.TargetIds[i] = mapped;
                                changed = true;
                            }
                        }
                    }

                    if (copyTp.Targets != null)
                    {
                        for (int i = 0; i < copyTp.Targets.Count; i++)
                        {
                            TeleporterObject t = copyTp.Targets[i];
                            if (t != null && teleporterIdMap.TryGetValue(t.Id, out Guid mapped) && byId.TryGetValue(mapped, out TeleporterObject replacement))
                            {
                                copyTp.Targets[i] = replacement;
                                changed = true;
                            }
                        }
                    }

                    if (changed)
                        EditorUtility.SetDirty(copyTp);
                }
            }

            Selection.objects = affected.ToArray();
            return affected;
        }

        private static void MirrorInPlace(Transform t, Vector3 planePoint, Vector3 planeNormal, bool mirrorRotation, bool negateScale)
        {
            if (t == null)
                return;

            Undo.RecordObject(t, "Mirror");

            Vector3 mirroredWorldPos = ReflectPosition(t.position, planePoint, planeNormal);
            Quaternion mirroredWorldRot = mirrorRotation ? MirrorRotation(t.rotation, planeNormal) : t.rotation;
            t.SetPositionAndRotation(mirroredWorldPos, mirroredWorldRot);

            if (negateScale)
            {
                Vector3 nLocal = t.InverseTransformDirection(planeNormal);
                float ax = Mathf.Abs(nLocal.x);
                float ay = Mathf.Abs(nLocal.y);
                float az = Mathf.Abs(nLocal.z);

                Vector3 s = t.localScale;
                if (ax >= ay && ax >= az)
                {
                    s.x = -s.x;
                }
                else if (ay >= ax && ay >= az)
                {
                    s.y = -s.y;
                }
                else
                    s.z = -s.z;

                t.localScale = s;
            }

            EditorUtility.SetDirty(t);
        }
    }
}
