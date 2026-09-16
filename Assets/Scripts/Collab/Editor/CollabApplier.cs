using System;
using System.Collections.Generic;
using Assets.Scripts.Components;
using Assets.Scripts.Components.Objects;
using Assets.Scripts.Enums;
using Assets.Scripts.Yaml;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Collab.Editor
{
    [InitializeOnLoad]
    public static class CollabApplier
    {
        private static int _suppress;
        private static readonly Dictionary<string, long> _lastSeq = new();
        private static readonly Dictionary<string, long> _lastTs = new();
        private static readonly Dictionary<string, List<string>> _remoteSelections = new();
        private static readonly Dictionary<string, RemotePeer> _peers = new();
        private static readonly Dictionary<string, RemoteView> _remoteViews = new();
        private static readonly Dictionary<string, long> _remoteSelectionTicks = new();

        public class RemotePeer
        {
            public string id = string.Empty;
            public string name = string.Empty;
            public string color = "#33AAFF";
        }

        public class RemoteView
        {
            public string id = string.Empty;
            public Vector3 pivot;
            public Vector3 camPos;
            public long lastTs;
            public long seenTicks;
        }

        public static IReadOnlyDictionary<string, List<string>> RemoteSelections => _remoteSelections;
        public static IReadOnlyDictionary<string, RemotePeer> Peers => _peers;
        public static IReadOnlyDictionary<string, RemoteView> RemoteViews => _remoteViews;

        public static bool IsApplying => _suppress > 0;

        static CollabApplier()
        {
            CollabNet.OnEnvelope += OnEnvelope;
        }

        public static IDisposable Suppress()
        {
            _suppress++;
            return new Scope();
        }

        private class Scope : IDisposable
        {
            private bool _done;
            public void Dispose()
            {
                if (_done)
                    return;

                _done = true;
                _suppress = Math.Max(0, _suppress - 1);
            }
        }

        private static void OnEnvelope(CollabEnvelope env, System.Net.IPEndPoint from)
        {
            if (IsApplying)
            {
                if (env.type != CollabTypes.SnapshotChunk && env.type != CollabTypes.SnapshotDone && env.type != CollabTypes.Welcome)
                    return;
            }
            try
            {
                switch (env.type)
                {
                    case CollabTypes.SnapshotChunk:
                        {
                            SnapshotChunkData c = CollabJson.Deserialize<SnapshotChunkData>(env.data);
                            CollabSnapshot.OnChunk(c);
                            break;
                        }

                    case CollabTypes.SnapshotDone:
                        {
                            SnapshotChunkData c = CollabJson.Deserialize<SnapshotChunkData>(env.data);
                            if (c != null)
                                CollabSnapshot.OnChunk(c);

                            break;
                        }

                    case CollabTypes.Welcome:
                        break;

                    case CollabTypes.Create:
                        {
                            if (!CheckSeq(env))
                                break;

                            SyncObject o = CollabJson.Deserialize<SyncObject>(env.data);
                            using (Suppress())
                            {
                                ApplyCreateOrUpdate(o, null, false);
                            }

                            AfterRemoteChange(o != null ? o.guid : string.Empty, false);
                            break;
                        }

                    case CollabTypes.Delete:
                        {
                            if (!CheckSeq(env))
                                break;

                            DeleteData d = CollabJson.Deserialize<DeleteData>(env.data);
                            using (Suppress())
                            {
                                ApplyDelete(d?.guid);
                            }

                            AfterRemoteChange(d != null ? d.guid : string.Empty, true);
                            break;
                        }

                    case CollabTypes.Reparent:
                        {
                            if (!CheckSeq(env))
                                break;
                                
                            ReparentData r = CollabJson.Deserialize<ReparentData>(env.data);
                            using (Suppress())
                            {
                                ApplyReparent(r);
                            }

                            AfterRemoteChange(r != null ? r.guid : string.Empty, false);
                            break;
                        }

                    case CollabTypes.Transform:
                        {
                            if (!CheckTs(env))
                                break;

                            TransformData t = CollabJson.Deserialize<TransformData>(env.data);
                            using (Suppress())
                            {
                                ApplyTransform(t);
                            }

                            break;
                        }
                        
                    case CollabTypes.Props:
                        {
                            if (!CheckSeq(env))
                                break;

                            PropsData p = CollabJson.Deserialize<PropsData>(env.data);
                            using (Suppress())
                            {
                                ApplyProps(p);
                            }

                            AfterRemoteChange(p != null ? p.guid : string.Empty, false);
                            break;
                        }

                    case CollabTypes.Selection:
                        {
                            SelectionData s = CollabJson.Deserialize<SelectionData>(env.data);
                            _remoteSelections[env.sender] = s?.guids ?? new List<string>();
                            _remoteSelectionTicks[env.sender] = DateTime.UtcNow.Ticks;
                            _peers[env.sender] = new RemotePeer { id = env.sender, name = env.senderName, color = env.senderColor };
                            SceneView.RepaintAll();
                            break;
                        }

                    case CollabTypes.View:
                        {
                            ViewData v = CollabJson.Deserialize<ViewData>(env.data);
                            if (v == null || v.pivot == null || v.pivot.Length != 3)
                                break;

                            if (_remoteViews.TryGetValue(env.sender, out RemoteView existing) && env.ts != 0 && env.ts < existing.lastTs)
                                break;

                            Vector3 pivot = new(v.pivot[0], v.pivot[1], v.pivot[2]);
                            Vector3 camPos = pivot;
                            if (v.camPos != null && v.camPos.Length == 3)
                                camPos = new Vector3(v.camPos[0], v.camPos[1], v.camPos[2]);

                            _remoteViews[env.sender] = new RemoteView
                            {
                                id = env.sender,
                                pivot = pivot,
                                camPos = camPos,
                                lastTs = env.ts,
                                seenTicks = DateTime.UtcNow.Ticks
                            };
                            _peers[env.sender] = new RemotePeer { id = env.sender, name = env.senderName, color = env.senderColor };
                            SceneView.RepaintAll();
                            break;
                        }

                    case CollabTypes.Presence:
                        {
                            PresenceData p = CollabJson.Deserialize<PresenceData>(env.data);
                            if (p?.users != null)
                            {
                                _peers.Clear();
                                HashSet<string> alive = new();
                                foreach (UserInfo u in p.users)
                                {
                                    if (string.IsNullOrEmpty(u.id))
                                        continue;

                                    alive.Add(u.id);
                                    if (u.id == CollabNet.UserId)
                                        continue;

                                    _peers[u.id] = new RemotePeer { id = u.id, name = u.name, color = u.color };
                                }

                                PruneDepartedPeers(alive);
                            }
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Collab] Apply {env.type} failed: {ex.Message}");
            }
        }

        private static string KeyFor(CollabEnvelope env, string guid)
        {
            return env.type + ":" + guid;
        }

        private static bool CheckSeq(CollabEnvelope env)
        {
            string guid = ExtractGuid(env);
            string key = KeyFor(env, guid);
            lock (_lastSeq)
            {
                if (_lastSeq.TryGetValue(key, out long last) && env.seq != 0 && env.seq < last)
                    return false;

                _lastSeq[key] = env.seq;
                if (_lastSeq.Count > 5000)
                    _lastSeq.Clear();
            }
            return true;
        }

        private static bool CheckTs(CollabEnvelope env)
        {
            string guid = ExtractGuid(env);
            string key = KeyFor(env, guid);
            lock (_lastTs)
            {
                if (_lastTs.TryGetValue(key, out long last) && env.ts < last)
                    return false;

                _lastTs[key] = env.ts;
                if (_lastTs.Count > 5000)
                    _lastTs.Clear();
            }
            return true;
        }

        private static string ExtractGuid(CollabEnvelope env)
        {
            try
            {
                switch (env.type)
                {
                    case CollabTypes.Create:
                        return CollabJson.Deserialize<SyncObject>(env.data)?.guid ?? string.Empty;

                    case CollabTypes.Delete:
                        return CollabJson.Deserialize<DeleteData>(env.data)?.guid ?? string.Empty;

                    case CollabTypes.Reparent:
                        return CollabJson.Deserialize<ReparentData>(env.data)?.guid ?? string.Empty;

                    case CollabTypes.Transform:
                        return CollabJson.Deserialize<TransformData>(env.data)?.guid ?? string.Empty;
                        
                    case CollabTypes.Props:
                        return CollabJson.Deserialize<PropsData>(env.data)?.guid ?? string.Empty;
                }
            }
            catch { }
            return env.msgId ?? string.Empty;
        }

        private static void AfterRemoteChange(string guid, bool deleted)
        {
            if (deleted)
            {
                CollabTracker.NotifyRemoteDelete(guid);
            }
            else
                CollabTracker.NotifyRemoteUpsert(guid);

            SceneView.RepaintAll();
        }

        private static void PruneDepartedPeers(HashSet<string> alive)
        {
            if (alive == null)
                return;

            List<string> goneViews = new();
            foreach (KeyValuePair<string, RemoteView> kv in _remoteViews)
            {
                if (!alive.Contains(kv.Key))
                    goneViews.Add(kv.Key);
            }

            foreach (string id in goneViews)
            {
                _remoteViews.Remove(id);
            }

            List<string> goneSelections = new();
            foreach (string id in _remoteSelections.Keys)
            {
                if (!alive.Contains(id))
                    goneSelections.Add(id);
            }

            foreach (string id in goneSelections)
            {
                _remoteSelections.Remove(id);
                _remoteSelectionTicks.Remove(id);
            }

            if (goneViews.Count > 0 || goneSelections.Count > 0)
                SceneView.RepaintAll();
        }

        public static void PruneStaleRemotePeers(long cutoffTicks)
        {
            List<string> staleViews = new();
            foreach (KeyValuePair<string, RemoteView> kv in _remoteViews)
            {
                if (kv.Value == null || kv.Value.seenTicks < cutoffTicks)
                    staleViews.Add(kv.Key);
            }

            foreach (string id in staleViews)
            {
                _remoteViews.Remove(id);
            }

            List<string> staleSelections = new();
            foreach (KeyValuePair<string, long> kv in _remoteSelectionTicks)
            {
                if (kv.Value < cutoffTicks)
                    staleSelections.Add(kv.Key);
            }

            foreach (string id in staleSelections)
            {
                _remoteSelectionTicks.Remove(id);
                _remoteSelections.Remove(id);
            }

            if (staleViews.Count > 0 || staleSelections.Count > 0)
                SceneView.RepaintAll();
        }

        public static void ApplyCreateOrUpdate(SyncObject o, Dictionary<string, GameObject> builders, bool forceRoot = false)
        {
            if (o == null || string.IsNullOrEmpty(o.guid))
                return;

            GameObject go = CollabId.Find(o.guid);
            if (go == null)
            {
                go = InstantiateFor(o, builders, forceRoot);
                if (go == null)
                    return;
            }

            GameObject wantParent = ResolveParent(o, builders, forceRoot);
            if (wantParent != null && go.transform.parent != wantParent.transform)
                go.transform.SetParent(wantParent.transform, true);

            ApplyStateTo(go, o);
        }

        private static GameObject InstantiateFor(SyncObject o, Dictionary<string, GameObject> builders, bool forceRoot)
        {
            GameObject prefab = CollabPrefabMap.Resolve(o.prefabPath, (ObjectType)o.objectType, o.prefabHint);
            GameObject go;
            if (prefab != null)
            {
                try
                {
                    go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    if (go == null)
                        go = UnityEngine.Object.Instantiate(prefab);
                }
                catch
                {
                    go = UnityEngine.Object.Instantiate(prefab);
                }
            }
            else
            {
                go = new GameObject(string.IsNullOrEmpty(o.name) ? "SyncedObject" : o.name);
                Type compType = CollabPrefabMap.ComponentTypeFor((ObjectType)o.objectType);
                if (go.GetComponent(compType) == null)
                    go.AddComponent(compType);
            }

            if (!go.TryGetComponent<CollabId>(out var cid))
                cid = go.AddComponent<CollabId>();

            cid.ForceSet(o.guid);

            GameObject parent = ResolveParent(o, builders, forceRoot);
            if (parent != null)
                go.transform.SetParent(parent.transform, false);

            if (go.transform.parent == null)
            {
                Builder b = UnityEngine.Object.FindFirstObjectByType<Builder>();
                if (b != null)
                    go.transform.SetParent(b.transform, false);
            }

            ApplyStateTo(go, o);
            return go;
        }

        private static GameObject ResolveParent(SyncObject o, Dictionary<string, GameObject> builders, bool forceRoot)
        {
            GameObject builderRoot = null;
            if (builders != null && !string.IsNullOrEmpty(o.builderGuid) && builders.TryGetValue(o.builderGuid, out GameObject bb))
                builderRoot = bb;

            if (!forceRoot && !string.IsNullOrEmpty(o.parentGuid))
            {
                GameObject p = CollabId.Find(o.parentGuid);
                if (p != null)
                    return p;

                if (builders != null && builders.TryGetValue(o.parentGuid, out GameObject b))
                    return b;
            }

            if (forceRoot && builderRoot == null)
            {
                Builder first = UnityEngine.Object.FindFirstObjectByType<Builder>();
                builderRoot = first != null ? first.gameObject : null;
            }

            if (o.serverSide)
            {
                GameObject host = builderRoot != null ? builderRoot : (!string.IsNullOrEmpty(o.builderGuid) ? CollabId.Find(o.builderGuid) : null);
                if (host != null)
                {
                    ServerSide ss = host.GetComponentInChildren<ServerSide>(true);
                    if (ss != null)
                        return ss.gameObject;
                }
            }

            return builderRoot;
        }

        private static void ApplyStateTo(GameObject go, SyncObject o)
        {
            go.name = string.IsNullOrEmpty(o.name) ? go.name : o.name;
            go.transform.localPosition = new Vector3(o.pos[0], o.pos[1], o.pos[2]);
            go.transform.localEulerAngles = new Vector3(o.rot[0], o.rot[1], o.rot[2]);
            go.transform.localScale = new Vector3(o.scale[0], o.scale[1], o.scale[2]);
            try
            {
                go.transform.SetSiblingIndex(Math.Max(0, o.siblingIndex));
            } catch { }
            go.isStatic = o.isStatic;

            if (!go.TryGetComponent<ObjectBase>(out var block))
            {
                Type compType = CollabPrefabMap.ComponentTypeFor((ObjectType)o.objectType);
                block = (ObjectBase)go.AddComponent(compType);
            }

            block.MovementSmoothing = o.movementSmoothing;
            block.AnimatorName = o.animatorName ?? string.Empty;
            if (o.culling != null && o.culling.Length == 3)
            {
                block.CullingSettings ??= new YamlCullingSettings();
                block.CullingSettings.Bounds = new Vector3(o.culling[0], o.culling[1], o.culling[2]);
            }
            CollabFieldCodec.Apply(block, o.fields);
            if (block is TeleporterObject tp)
            {
                try
                {
                    tp.TargetIds.Clear();
                    foreach (TeleporterObject tgt in tp.Targets)
                    {
                        if (tgt != null && tgt.Id != Guid.Empty)
                            tp.TargetIds.Add(tgt.Id);
                    }
                }
                catch { }
            }
            CollabToolFactory.Ensure(go, o.tools);
            EditorUtility.SetDirty(go);
            EditorUtility.SetDirty(block);
        }

        public static void ApplyDelete(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return;

            GameObject go = CollabId.Find(guid);
            if (go == null)
                return;

            if (go.GetComponent<Builder>() != null)
            {
                Debug.LogWarning("[Collab] Refusing to delete Builder root from remote delete.");
                return;
            }
            UnityEngine.Object.DestroyImmediate(go);
        }

        public static void ApplyReparent(ReparentData r)
        {
            if (r == null || string.IsNullOrEmpty(r.guid))
                return;

            GameObject go = CollabId.Find(r.guid);
            if (go == null)
                return;

            GameObject parent = null;
            if (!string.IsNullOrEmpty(r.parentGuid))
                parent = CollabId.Find(r.parentGuid);

            if (parent == null && !string.IsNullOrEmpty(r.builderGuid))
                parent = CollabId.Find(r.builderGuid);

            if (parent != null && go.transform.parent != parent.transform)
                go.transform.SetParent(parent.transform, true);

            try
            {
                go.transform.SetSiblingIndex(Math.Max(0, r.siblingIndex));
            } catch { }
            EditorUtility.SetDirty(go);
        }

        public static void ApplyTransform(TransformData t)
        {
            if (t == null || string.IsNullOrEmpty(t.guid))
                return;

            GameObject go = CollabId.Find(t.guid);
            if (go == null)
                return;

            Vector3 pos = new(t.pos[0], t.pos[1], t.pos[2]);
            Vector3 rot = new(t.rot[0], t.rot[1], t.rot[2]);
            Vector3 scl = new(t.scale[0], t.scale[1], t.scale[2]);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = rot;
            go.transform.localScale = scl;
            EditorUtility.SetDirty(go);
            CollabTracker.NotifyRemoteTransform(t.guid, pos, rot, scl);
        }

        public static void ApplyProps(PropsData p)
        {
            if (p == null || string.IsNullOrEmpty(p.guid))
                return;

            GameObject go = CollabId.Find(p.guid);
            if (go == null)
                return;

            go.name = string.IsNullOrEmpty(p.name) ? go.name : p.name;
            go.isStatic = p.isStatic;
            if (!go.TryGetComponent<ObjectBase>(out var block))
                return;

            block.MovementSmoothing = p.movementSmoothing;
            block.AnimatorName = p.animatorName ?? string.Empty;
            if (p.culling != null && p.culling.Length == 3)
            {
                block.CullingSettings ??= new YamlCullingSettings();
                block.CullingSettings.Bounds = new Vector3(p.culling[0], p.culling[1], p.culling[2]);
            }
            CollabFieldCodec.Apply(block, p.fields);
            if (block is TeleporterObject tp2)
            {
                try
                {
                    tp2.TargetIds.Clear();
                    foreach (TeleporterObject tgt in tp2.Targets)
                    {
                        if (tgt != null && tgt.Id != Guid.Empty)
                            tp2.TargetIds.Add(tgt.Id);
                    }
                }
                catch { }
            }
            CollabToolFactory.Ensure(go, p.tools);
            EnsureServerSideParenting(go, p.serverSide);
            EditorUtility.SetDirty(go);
            EditorUtility.SetDirty(block);
        }

        private static void EnsureServerSideParenting(GameObject go, bool serverSide)
        {
            Builder builder = go.GetComponentInParent<Builder>();
            if (builder == null)
                return;

            ServerSide ss = builder.GetComponentInChildren<ServerSide>(true);
            if (ss == null || go == ss.gameObject)
                return;

            bool underSs = go.transform.IsChildOf(ss.transform);
            if (serverSide && !underSs)
            {
                go.transform.SetParent(ss.transform, false);
            }
            else if (!serverSide && go.transform.parent != null && go.transform.parent.gameObject == ss.gameObject)
            {
                go.transform.SetParent(builder.transform, false);
            }
        }
    }
}
