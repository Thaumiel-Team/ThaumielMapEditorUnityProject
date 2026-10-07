using System;
using System.Collections.Generic;
using Assets.Scripts.Components.Tools;
using Assets.Scripts.Enums;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Collab.Editor
{
    public static class CollabToolFactory
    {
        public static Type TypeFor(ToolType t)
        {
            return t switch
            {
                ToolType.Health => typeof(Health),
                ToolType.Physics => typeof(Components.Tools.Physics),
                ToolType.Doorlink => typeof(DoorLink),
                ToolType.ColliderTrigger => typeof(ColliderTrigger),
                ToolType.InteractableTrigger => typeof(InteractableTrigger),
                ToolType.BlockyRuntime => typeof(BlockyRuntime),
                _ => null,
            };

        }

        public static ToolType KindOf(ToolBase tb) => tb != null ? tb.ToolType : ToolType.Custom;

        public static List<SyncToolState> Extract(GameObject go)
        {
            List<SyncToolState> list = new();
            if (go == null)
                return list;

            foreach (ToolBase tb in go.GetComponents<ToolBase>())
            {
                if (tb == null)
                    continue;

                list.Add(new SyncToolState
                {
                    toolType = (int)KindOf(tb),
                    fields = CollabFieldCodec.Extract(tb)
                });
            }
            return list;
        }

        public static string ToolsHash(List<SyncToolState> tools)
        {
            if (tools == null || tools.Count == 0)
                return "0";

            tools.Sort((a, b) => a.toolType.CompareTo(b.toolType));
            int h = 17;
            foreach (SyncToolState t in tools)
            {
                h = h * 31 + t.toolType;
                h = h * 31 + (CollabFieldCodec.FieldsHash(t.fields)?.GetHashCode() ?? 0);
            }

            return h.ToString();
        }

        public static void Ensure(GameObject go, List<SyncToolState> desired)
        {
            if (go == null)
                return;
                
            desired ??= new List<SyncToolState>();
            ToolBase[] existing = go.GetComponents<ToolBase>();
            Dictionary<int, List<ToolBase>> byKind = new();
            foreach (ToolBase tb in existing)
            {
                if (tb == null)
                    continue;

                int k = (int)KindOf(tb);
                if (!byKind.TryGetValue(k, out List<ToolBase> l))
                {
                    l = new List<ToolBase>();
                    byKind[k] = l;
                }

                l.Add(tb);
            }
            Dictionary<int, int> wantCounts = new();
            foreach (SyncToolState d in desired)
                wantCounts[d.toolType] = wantCounts.TryGetValue(d.toolType, out int c) ? c + 1 : 1;

            foreach (KeyValuePair<int, List<ToolBase>> kv in byKind)
            {
                int kind = kv.Key;
                wantCounts.TryGetValue(kind, out int want);
                List<ToolBase> list = kv.Value;
                for (int i = list.Count - 1; i >= want; i--)
                {
                    UnityEngine.Object.DestroyImmediate(list[i]);
                }
            }

            Dictionary<int, Queue<ToolBase>> remaining = new();
            foreach (ToolBase tb in go.GetComponents<ToolBase>())
            {
                if (tb == null)
                    continue;

                int k = (int)KindOf(tb);
                if (!remaining.TryGetValue(k, out Queue<ToolBase> q))
                {
                    q = new Queue<ToolBase>();
                    remaining[k] = q;
                }

                q.Enqueue(tb);
            }
            foreach (SyncToolState d in desired)
            {
                ToolBase target;

                if (remaining.TryGetValue(d.toolType, out Queue<ToolBase> q) && q.Count > 0)
                {
                    target = q.Dequeue();
                }
                else
                {
                    Type tt = TypeFor((ToolType)d.toolType);
                    if (tt == null)
                        continue;

                    target = (ToolBase)go.AddComponent(tt);
                }
                
                if (target != null)
                {
                    CollabFieldCodec.Apply(target, d.fields);
                    EditorUtility.SetDirty(target);
                }
            }
        }
    }
}
