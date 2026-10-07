using System;
using System.Collections.Generic;
using Assets.Scripts.Components.Objects;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Collab.Editor
{
    public static class CollabSnapshot
    {
        public static List<SnapshotChunkData> BuildChunks(string snapshotId, int perChunk = 4)
        {
            Builder[] builders = UnityEngine.Object.FindObjectsByType<Builder>(FindObjectsSortMode.None);
            List<BuilderInfo> builderInfos = new();
            List<SyncObject> objects = new();

            foreach (Builder b in builders)
            {
                if (b == null)
                    continue;

                string bg = CollabId.Ensure(b.gameObject);
                Transform bt = b.transform;
                Vector3 be = bt.position;
                Vector3 br = bt.rotation.eulerAngles;
                builderInfos.Add(new BuilderInfo
                {
                    guid = bg,
                    name = b.gameObject.name,
                    pos = new float[] { be.x, be.y, be.z },
                    rot = new float[] { br.x, br.y, br.z },
                    scale = new float[] { bt.localScale.x, bt.localScale.y, bt.localScale.z }
                });

                foreach (ObjectBase block in b.GetComponentsInChildren<ObjectBase>(true))
                {
                    if (block == null)
                        continue;

                    SyncObject so = CollabSyncReader.FromGameObject(block.gameObject, bg);
                    if (so != null)
                        objects.Add(so);
                }
            }

            List<SnapshotChunkData> chunks = new();
            int total = Math.Max(1, (objects.Count + perChunk - 1) / perChunk);
            for (int i = 0; i < total; i++)
            {
                int start = i * perChunk;
                int count = Math.Min(perChunk, objects.Count - start);
                List<SyncObject> slice = count > 0 ? objects.GetRange(start, count) : new List<SyncObject>();
                chunks.Add(new SnapshotChunkData
                {
                    snapshotId = snapshotId,
                    index = i,
                    total = total,
                    objects = slice,
                    builders = i == 0 ? builderInfos : new List<BuilderInfo>()
                });
            }
            return chunks;
        }

        private static readonly Dictionary<string, Dictionary<int, SnapshotChunkData>> _assemble = new();

        public static void OnChunk(SnapshotChunkData chunk)
        {
            if (chunk == null || string.IsNullOrEmpty(chunk.snapshotId))
                return;

            if (!_assemble.TryGetValue(chunk.snapshotId, out Dictionary<int, SnapshotChunkData> map))
            {
                map = new Dictionary<int, SnapshotChunkData>();
                _assemble[chunk.snapshotId] = map;
            }

            if (chunk.index < chunk.total)
                map[chunk.index] = chunk;

            if (map.Count >= chunk.total)
            {
                List<BuilderInfo> builders = new();
                List<SyncObject> objects = new();
                for (int i = 0; i < chunk.total; i++)
                {
                    if (!map.TryGetValue(i, out SnapshotChunkData c))
                        return;

                    if (c.builders != null)
                        builders.AddRange(c.builders);

                    if (c.objects != null)
                        objects.AddRange(c.objects);
                }

                _assemble.Remove(chunk.snapshotId);
                ApplyFull(builders, objects);
            }
        }

        public static void ApplyFull(List<BuilderInfo> builders, List<SyncObject> objects)
        {
            builders ??= new List<BuilderInfo>();
            objects ??= new List<SyncObject>();
            using (CollabApplier.Suppress())
            {
                Dictionary<string, GameObject> builderByGuid = new();
                foreach (BuilderInfo bi in builders)
                {
                    GameObject root = CollabId.Find(bi.guid);
                    if (root == null || root.GetComponent<Builder>() == null)
                    {
                        GameObject byName = GameObject.Find(bi.name);
                        if (byName != null && byName.GetComponent<Builder>() != null)
                        {
                            root = byName;
                            if (!root.TryGetComponent<CollabId>(out var cid0))
                                cid0 = root.AddComponent<CollabId>();
                                
                            cid0.ForceSet(bi.guid);
                        }
                        else
                        {
                            root = new GameObject(string.IsNullOrEmpty(bi.name) ? "Schematic" : bi.name);
                            root.AddComponent<Builder>();
                            CollabId cid1 = root.AddComponent<CollabId>();
                            cid1.ForceSet(bi.guid);
                        }
                    }

                    root.transform.SetPositionAndRotation(new Vector3(bi.pos[0], bi.pos[1], bi.pos[2]), Quaternion.Euler(bi.rot[0], bi.rot[1], bi.rot[2]));
                    root.transform.localScale = new Vector3(bi.scale[0], bi.scale[1], bi.scale[2]);
                    EditorUtility.SetDirty(root);
                    builderByGuid[bi.guid] = root;
                }

                Dictionary<string, SyncObject> incoming = new();
                foreach (SyncObject o in objects)
                {
                    if (o != null && !string.IsNullOrEmpty(o.guid))
                        incoming[o.guid] = o;
                }

                foreach (KeyValuePair<string, GameObject> kv in builderByGuid)
                {
                    ObjectBase[] blocks = kv.Value.GetComponentsInChildren<ObjectBase>(true);
                    foreach (ObjectBase b in blocks)
                    {
                        if (b == null)
                            continue;

                        string g = CollabId.Get(b.gameObject);
                        if (!string.IsNullOrEmpty(g) && !incoming.ContainsKey(g))
                            UnityEngine.Object.DestroyImmediate(b.gameObject);
                    }
                }

                List<SyncObject> pending = new(incoming.Values);
                int guard = pending.Count * 2 + 8;
                while (pending.Count > 0 && guard-- > 0)
                {
                    int progressed = 0;
                    for (int i = pending.Count - 1; i >= 0; i--)
                    {
                        SyncObject o = pending[i];
                        if (CanPlace(o, builderByGuid))
                        {
                            CollabApplier.ApplyCreateOrUpdate(o, builderByGuid, false);
                            pending.RemoveAt(i);
                            progressed++;
                        }
                    }
                    if (progressed == 0)
                    {
                        foreach (SyncObject o in pending)
                        {
                            CollabApplier.ApplyCreateOrUpdate(o, builderByGuid, true);
                        }

                        pending.Clear();
                    }
                }
            }

            CollabTracker.RebuildCache();
            SceneView.RepaintAll();
            CollabNet.Log($"Snapshot applied ({objects.Count} objects, {builders.Count} builders)");
        }

        private static bool CanPlace(SyncObject o, Dictionary<string, GameObject> builders)
        {
            if (string.IsNullOrEmpty(o.parentGuid))
                return true;

            if (CollabId.Find(o.parentGuid) != null)
                return true;
                
            return builders.ContainsKey(o.parentGuid);
        }
    }
}
