using System.Collections.Generic;
using Assets.Scripts.Components;
using Assets.Scripts.Components.Objects;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Collab.Editor
{
    [InitializeOnLoad]
    public static class CollabTracker
    {
        private class Tracked
        {
            public string guid = string.Empty;
            public GameObject go;
            public ObjectBase block;
            public string parentGuid = string.Empty;
            public string builderGuid = string.Empty;
            public string name = string.Empty;
            public Vector3 pos;
            public Vector3 rotE;
            public Vector3 scale;
            public bool isStatic;
            public int sibling;
            public string fieldsHash = string.Empty;
            public string toolsHash = string.Empty;
            public string propsHash = string.Empty;
        }

        private static readonly Dictionary<string, Tracked> _cache = new();
        private static double _nextTransformScan;
        private static double _nextPropsScan;
        private static bool _hierarchyDirty = true;
        private static double _diffReadyAt;
        private static bool _diffRunning;
        private static readonly List<string> _propsKeys = new();
        private static int _propsCursor;
        private static double _nextSelectionSend;
        private static double _nextViewSend;
        private static double _nextViewForce;
        private static Vector3 _lastSentPivot;
        private static Vector3 _lastSentCamPos;
        private static bool _hasSentPivot;
        private const double DiffDebounceSeconds = 0.35;
        private const double PropsCycleSeconds = 2.0;
        private const int PropsPerTick = 25;
        private const double SelectionHeartbeatSeconds = 2.0;
        private const double ViewSendSeconds = 0.25;
        private const double ViewForceSeconds = 2.0;

        static CollabTracker()
        {
            EditorApplication.hierarchyChanged += () =>
            {
                if (!CollabApplier.IsApplying)
                    _hierarchyDirty = true;
            };
            
            EditorApplication.update += OnUpdate;
            Selection.selectionChanged += OnSelectionChanged;
            AssemblyReloadEvents.beforeAssemblyReload += () => { _cache.Clear(); };
        }

        public static void NotifySessionStarted()
        {
            _hierarchyDirty = true;
            RebuildCache();
        }

        public static void RebuildCache()
        {
            if (CollabApplier.IsApplying)
            {
                _hierarchyDirty = true;
                return;
            }

            _cache.Clear();
            foreach (Tracked entry in EnumerateScene())
            {
                _cache[entry.guid] = entry;
            }

            _propsKeys.Clear();
            _propsCursor = 0;
            _nextPropsScan = 0.0;
            _diffReadyAt = 0.0;
            _nextSelectionSend = 0.0;
            _nextViewSend = 0.0;
            _hasSentPivot = false;
            _hierarchyDirty = false;
        }

        public static void NotifyRemoteTransform(string guid, Vector3 pos, Vector3 rotE, Vector3 scale)
        {
            if (string.IsNullOrEmpty(guid))
                return;

            if (_cache.TryGetValue(guid, out Tracked t))
            {
                t.pos = pos;
                t.rotE = rotE;
                t.scale = scale;
            }
        }

        public static void NotifyRemoteUpsert(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return;

            GameObject go = CollabId.Find(guid);
            if (go == null)
            {
                _cache.Remove(guid);
                return;
            }

            if (!go.TryGetComponent<ObjectBase>(out var block))
                return;

            string builderGuid = BuilderGuidOf(go);
            _cache[guid] = SnapshotTracked(go, block, guid, builderGuid);
        }
        
        public static void NotifyRemoteDelete(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return;

            _cache.Remove(guid);
        }

        private static List<Tracked> EnumerateScene()
        {
            List<Tracked> list = new();
            Builder[] builders = UnityEngine.Object.FindObjectsByType<Builder>(FindObjectsSortMode.None);
            foreach (Builder b in builders)
            {
                if (b == null)
                    continue;

                string bg = CollabId.Get(b.gameObject);
                if (string.IsNullOrEmpty(bg))
                    continue;

                foreach (ObjectBase block in b.GetComponentsInChildren<ObjectBase>(true))
                {
                    if (block == null)
                        continue;

                    GameObject go = block.gameObject;
                    string g = CollabId.Get(go);
                    if (string.IsNullOrEmpty(g))
                        continue;

                    list.Add(SnapshotTracked(go, block, g, bg));
                }
            }
            return list;
        }

        private static Tracked SnapshotTracked(GameObject go, ObjectBase block, string guid, string builderGuid)
        {
            string parentGuid = string.Empty;
            Transform parent = go.transform.parent;
            if (parent != null)
                parentGuid = CollabId.Get(parent.gameObject);

            List<SyncField> fields = CollabFieldCodec.Extract(block);
            List<SyncToolState> tools = CollabToolFactory.Extract(go);
            string fh = CollabFieldCodec.FieldsHash(fields);
            string th = CollabToolFactory.ToolsHash(tools);
            return new Tracked
            {
                guid = guid,
                go = go,
                block = block,
                parentGuid = parentGuid ?? string.Empty,
                builderGuid = builderGuid ?? string.Empty,
                name = go.name,
                pos = go.transform.localPosition,
                rotE = go.transform.localEulerAngles,
                scale = go.transform.localScale,
                isStatic = go.isStatic,
                sibling = go.transform.GetSiblingIndex(),
                fieldsHash = fh,
                toolsHash = th,
                propsHash = BuildPropsHash(go, block, fh, th),
            };
        }

        private static string BuildPropsHash(GameObject go, ObjectBase block, string fh, string th) => $"{go.name}|{go.isStatic}|{block.MovementSmoothing}|{fh}|{th}";

        private static void OnUpdate()
        {
            if (CollabNet.Mode == CollabMode.Off || !CollabNet.Connected)
                return;

            if (CollabApplier.IsApplying)
                return;

            if (Application.isPlaying)
                return;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            double now = EditorApplication.timeSinceStartup;
            if (_hierarchyDirty)
            {
                _hierarchyDirty = false;
                _diffReadyAt = now + DiffDebounceSeconds;
            }

            if (_diffReadyAt > 0.0 && now >= _diffReadyAt)
            {
                _diffReadyAt = 0.0;
                DiffHierarchy();
            }

            if (now >= _nextTransformScan)
            {
                _nextTransformScan = now + 0.1;
                ScanTransforms();
            }

            HeartbeatSelection(now);
            ScanView(now);
            ScanPropsSlice(now);
        }

        private static string BuilderGuidOf(GameObject go)
        {
            Builder b = go.GetComponentInParent<Builder>();
            if (b == null)
                return string.Empty;

            return CollabId.Ensure(b.gameObject);
        }

        private static string BuilderGuidOfCached(GameObject go)
        {
            Builder b = go.GetComponentInParent<Builder>();
            if (b == null)
                return string.Empty;

            return CollabId.Get(b.gameObject);
        }

        private static void DiffHierarchy()
        {
            if (_diffRunning)
            {
                _hierarchyDirty = true;
                return;
            }

            _diffRunning = true;
            try
            {
                DiffHierarchyCore();
            }
            finally
            {
                _diffRunning = false;
            }
        }

        private static void DiffHierarchyCore()
        {
            bool addedIds = false;
            Builder[] builders = UnityEngine.Object.FindObjectsByType<Builder>(FindObjectsSortMode.None);
            foreach (Builder b in builders)
            {
                if (b == null)
                    continue;

                if (string.IsNullOrEmpty(CollabId.Get(b.gameObject)))
                {
                    CollabId.Ensure(b.gameObject);
                    addedIds = true;
                }

                foreach (ObjectBase block in b.GetComponentsInChildren<ObjectBase>(true))
                {
                    if (block == null)
                        continue;

                    if (string.IsNullOrEmpty(CollabId.Get(block.gameObject)))
                    {
                        CollabId.Ensure(block.gameObject);
                        addedIds = true;
                    }
                }
            }

            Dictionary<string, Tracked> current = new();
            foreach (Builder b in builders)
            {
                if (b == null)
                    continue;

                string bg = CollabId.Get(b.gameObject);
                if (string.IsNullOrEmpty(bg))
                    continue;

                foreach (ObjectBase block in b.GetComponentsInChildren<ObjectBase>(true))
                {
                    if (block == null)
                        continue;

                    GameObject go = block.gameObject;
                    string g = CollabId.Get(go);
                    if (string.IsNullOrEmpty(g))
                        continue;

                    if (current.ContainsKey(g))
                        continue;

                    string parentGuid = string.Empty;
                    Transform parent = go.transform.parent;
                    if (parent != null)
                        parentGuid = CollabId.Get(parent.gameObject);

                    current[g] = new Tracked
                    {
                        guid = g,
                        go = go,
                        block = block,
                        parentGuid = parentGuid ?? string.Empty,
                        builderGuid = bg ?? string.Empty,
                        sibling = go.transform.GetSiblingIndex()
                    };
                }
            }

            foreach (KeyValuePair<string, Tracked> kv in current)
            {
                if (_cache.ContainsKey(kv.Key))
                    continue;

                Tracked cur = kv.Value;
                if (cur.go == null || cur.block == null)
                    continue;

                _cache[kv.Key] = SnapshotTracked(cur.go, cur.block, cur.guid, cur.builderGuid);
                SyncObject so = CollabSyncReader.FromGameObject(cur.go, cur.builderGuid);
                if (so == null)
                    continue;

                CollabNet.BroadcastReliable(CollabTypes.Create, CollabJson.Serialize(so));
            }

            List<string> gone = new();
            foreach (KeyValuePair<string, Tracked> kv in _cache)
            {
                if (!current.ContainsKey(kv.Key))
                    gone.Add(kv.Key);
            }

            foreach (string g in gone)
            {
                _cache.Remove(g);
                CollabNet.BroadcastReliable(CollabTypes.Delete, CollabJson.Serialize(new DeleteData { guid = g }));
            }

            foreach (KeyValuePair<string, Tracked> kv in current)
            {
                if (!_cache.TryGetValue(kv.Key, out Tracked old))
                    continue;

                Tracked cur = kv.Value;
                if (cur.go != null)
                    old.go = cur.go;

                if (cur.block != null)
                    old.block = cur.block;

                if (old.parentGuid != cur.parentGuid || old.builderGuid != cur.builderGuid || old.sibling != cur.sibling)
                {
                    old.parentGuid = cur.parentGuid;
                    old.builderGuid = cur.builderGuid;
                    old.sibling = cur.sibling;
                    CollabNet.BroadcastReliable(CollabTypes.Reparent, CollabJson.Serialize(new ReparentData
                    {
                        guid = cur.guid,
                        parentGuid = cur.parentGuid,
                        builderGuid = cur.builderGuid,
                        siblingIndex = cur.sibling
                    }));
                }
            }

            if (addedIds)
            {
                EditorApplication.delayCall += () =>
                {
                    _hierarchyDirty = true;
                };
            }
        }

        private static void ScanTransforms()
        {
            foreach (KeyValuePair<string, Tracked> kv in _cache)
            {
                Tracked old = kv.Value;
                GameObject go = old.go;
                if (go == null)
                    continue;

                Transform t = go.transform;
                Vector3 p = t.localPosition;
                Vector3 r = t.localEulerAngles;
                Vector3 s = t.localScale;
                if (p != old.pos || r != old.rotE || s != old.scale)
                {
                    old.pos = p;
                    old.rotE = r;
                    old.scale = s;
                    CollabNet.BroadcastUnreliable(CollabTypes.Transform, CollabJson.Serialize(new TransformData
                    {
                        guid = kv.Key,
                        pos = new float[] { p.x, p.y, p.z },
                        rot = new float[] { r.x, r.y, r.z },
                        scale = new float[] { s.x, s.y, s.z }
                    }));
                }

                if (go.name != old.name || go.isStatic != old.isStatic)
                    CaptureAndSendProps(go, kv.Key, old);
            }
        }

        private static void ScanPropsSlice(double now)
        {
            if (_propsKeys.Count == 0 && _propsCursor == 0)
            {
                if (now < _nextPropsScan)
                    return;

                _propsKeys.AddRange(_cache.Keys);
                if (_propsKeys.Count == 0)
                {
                    _nextPropsScan = now + PropsCycleSeconds;
                    return;
                }
            }

            int processed = 0;
            while (_propsCursor < _propsKeys.Count && processed < PropsPerTick)
            {
                string key = _propsKeys[_propsCursor];
                _propsCursor++;
                processed++;
                if (!_cache.TryGetValue(key, out Tracked old))
                    continue;

                CheckPropsEntry(key, old);
            }

            if (_propsCursor >= _propsKeys.Count)
            {
                _propsKeys.Clear();
                _propsCursor = 0;
                _nextPropsScan = now + PropsCycleSeconds;
            }
        }

        private static void CheckPropsEntry(string key, Tracked old)
        {
            GameObject go = old.go;
            if (go == null)
                return;

            ObjectBase block = old.block;
            if (block == null)
            {
                block = go.GetComponent<ObjectBase>();
                if (block == null)
                    return;

                old.block = block;
            }

            string pg = string.Empty;
            Transform parent = go.transform.parent;
            if (parent != null)
                pg = CollabId.Get(parent.gameObject);

            int sibling = go.transform.GetSiblingIndex();
            if (pg != old.parentGuid || sibling != old.sibling)
            {
                old.parentGuid = pg ?? string.Empty;
                old.builderGuid = BuilderGuidOfCached(go);
                old.sibling = sibling;
                CollabNet.BroadcastReliable(CollabTypes.Reparent, CollabJson.Serialize(new ReparentData
                {
                    guid = key,
                    parentGuid = old.parentGuid,
                    builderGuid = old.builderGuid,
                    siblingIndex = sibling
                }));
                return;
            }

            if (go.name != old.name || go.isStatic != old.isStatic)
            {
                CaptureAndSendProps(go, key, old);
                return;
            }

            List<SyncField> fields = CollabFieldCodec.Extract(block);
            List<SyncToolState> tools = CollabToolFactory.Extract(go);
            string fh = CollabFieldCodec.FieldsHash(fields);
            string th = CollabToolFactory.ToolsHash(tools);
            string propsHash = BuildPropsHash(go, block, fh, th);
            if (propsHash != old.propsHash)
            {
                old.fieldsHash = fh;
                old.toolsHash = th;
                old.propsHash = propsHash;
                old.name = go.name;
                old.isStatic = go.isStatic;
                SendProps(go, key, fields, tools);
            }
        }

        private static void CaptureAndSendProps(GameObject go, string guid, Tracked old)
        {
            if (!go.TryGetComponent<ObjectBase>(out var block))
                return;

            old.block = block;
            List<SyncField> fields = CollabFieldCodec.Extract(block);
            List<SyncToolState> tools = CollabToolFactory.Extract(go);
            old.fieldsHash = CollabFieldCodec.FieldsHash(fields);
            old.toolsHash = CollabToolFactory.ToolsHash(tools);
            old.propsHash = BuildPropsHash(go, block, old.fieldsHash, old.toolsHash);
            old.name = go.name;
            old.isStatic = go.isStatic;
            old.sibling = go.transform.GetSiblingIndex();
            SendProps(go, guid, fields, tools);
        }

        private static void SendProps(GameObject go, string guid, List<SyncField> fields, List<SyncToolState> tools)
        {
            if (!go.TryGetComponent<ObjectBase>(out var block))
                return;

            fields ??= CollabFieldCodec.Extract(block);
            tools ??= CollabToolFactory.Extract(go);

            float[] culling = null;
            if (block.CullingSettings != null)
                culling = new float[] { block.CullingSettings.Bounds.x, block.CullingSettings.Bounds.y, block.CullingSettings.Bounds.z };
                
            ServerSide ss = go.GetComponentInParent<ServerSide>();
            bool serverSide = ss != null && go.transform.IsChildOf(ss.transform);
            PropsData props = new()
            {
                guid = guid,
                name = go.name,
                isStatic = go.isStatic,
                movementSmoothing = block.MovementSmoothing,
                culling = culling,
                animatorName = block.AnimatorName ?? string.Empty,
                serverSide = serverSide,
                fields = fields,
                tools = tools
            };
            CollabNet.BroadcastReliable(CollabTypes.Props, CollabJson.Serialize(props));
        }

        private static void OnSelectionChanged()
        {
            BroadcastSelection();
            _nextSelectionSend = EditorApplication.timeSinceStartup + SelectionHeartbeatSeconds;
        }

        private static void HeartbeatSelection(double now)
        {
            if (now < _nextSelectionSend)
                return;

            _nextSelectionSend = now + SelectionHeartbeatSeconds;
            BroadcastSelection();
        }

        private static void BroadcastSelection()
        {
            if (CollabNet.Mode == CollabMode.Off || !CollabNet.Connected)
                return;

            if (CollabApplier.IsApplying)
                return;

            List<string> guids = new();
            foreach (GameObject go in Selection.gameObjects)
            {
                if (go == null)
                    continue;

                string g = CollabId.Get(go);
                if (!string.IsNullOrEmpty(g))
                    guids.Add(g);
            }

            CollabNet.BroadcastUnreliable(CollabTypes.Selection, CollabJson.Serialize(new SelectionData { guids = guids }));
        }
        
        private static void ScanView(double now)
        {
            if (now < _nextViewSend)
                return;

            _nextViewSend = now + ViewSendSeconds;
            SceneView sv = SceneView.lastActiveSceneView;
            if (sv == null || sv.camera == null)
                return;

            Vector3 pivot = sv.pivot;
            Vector3 camPos = sv.camera.transform.position;
            if (_hasSentPivot && (pivot - _lastSentPivot).sqrMagnitude < 0.0025f && (camPos - _lastSentCamPos).sqrMagnitude < 0.0025f && now < _nextViewForce)
                return;

            _hasSentPivot = true;
            _lastSentPivot = pivot;
            _lastSentCamPos = camPos;
            _nextViewForce = now + ViewForceSeconds;
            CollabNet.BroadcastUnreliable(CollabTypes.View, CollabJson.Serialize(new ViewData
            {
                pivot = new float[] { pivot.x, pivot.y, pivot.z },
                camPos = new float[] { camPos.x, camPos.y, camPos.z }
            }));
        }
    }
}
