using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Components;
using Assets.Scripts.Components.Objects;
using Newtonsoft.Json;
using UnityEngine;

namespace Assets.Scripts.Collab.Editor
{
    public static class CollabSyncReader
    {
        public static SyncObject FromGameObject(GameObject go, string builderGuid)
        {
            if (go == null)
                return null;

            if (!go.TryGetComponent<ObjectBase>(out var block))
                return null;

            string guid = CollabId.Ensure(go);
            string parentGuid = string.Empty;
            Transform parent = go.transform.parent;
            if (parent != null)
            {
                CollabId pId = parent.GetComponent<CollabId>();
                if (pId != null && (parent.GetComponent<ObjectBase>() != null || parent.GetComponent<Builder>() != null || parent.GetComponent<ServerSide>() != null || parent.GetComponent<CollabId>() != null))
                    parentGuid = pId.GuidString;
            }

            bool serverSide = false;
            ServerSide ss = go.GetComponentInParent<ServerSide>();
            if (ss != null && go.transform.IsChildOf(ss.transform))
                serverSide = true;

            string hint = ExtractHint(block);
            string prefabPath = CollabPrefabMap.GetPrefabPath(block.ObjectType, hint);

            Transform t = go.transform;
            Vector3 e = t.localEulerAngles;
            return new SyncObject
            {
                guid = guid,
                parentGuid = parentGuid,
                builderGuid = builderGuid ?? string.Empty,
                objectType = (int)block.ObjectType,
                prefabPath = prefabPath ?? string.Empty,
                prefabHint = hint ?? string.Empty,
                name = go.name,
                pos = new float[] { t.localPosition.x, t.localPosition.y, t.localPosition.z },
                rot = new float[] { e.x, e.y, e.z },
                scale = new float[] { t.localScale.x, t.localScale.y, t.localScale.z },
                isStatic = go.isStatic,
                movementSmoothing = block.MovementSmoothing,
                culling = block.CullingSettings != null && block.CullingSettings.Bounds != Vector3.zero ? new float[] { block.CullingSettings.Bounds.x, block.CullingSettings.Bounds.y, block.CullingSettings.Bounds.z } : null,
                animatorName = block.AnimatorName ?? string.Empty,
                serverSide = serverSide,
                siblingIndex = t.GetSiblingIndex(),
                fields = CollabFieldCodec.Extract(block),
                tools = CollabToolFactory.Extract(go),
            };
        }

        public static PropsData ToProps(SyncObject o)
        {
            return new PropsData
            {
                guid = o.guid,
                name = o.name,
                isStatic = o.isStatic,
                movementSmoothing = o.movementSmoothing,
                culling = o.culling,
                animatorName = o.animatorName ?? string.Empty,
                serverSide = o.serverSide,
                fields = o.fields,
                tools = o.tools,
            };
        }

        public static TransformData ToTransform(SyncObject o) => new() { guid = o.guid, pos = o.pos, rot = o.rot, scale = o.scale };

        private static string ExtractHint(ObjectBase block)
        {
            try
            {
                switch (block)
                {
                    case PrimitiveObject p:
                        return p.PrimitiveType.ToString();

                    case DoorObject d:
                        return d.DoorType.ToString();

                    case CameraObject c:
                        {
                            FieldInfo f = c.GetType().GetField("CameraType");
                            if (f != null)
                                return Convert.ToString(f.GetValue(c));

                            return string.Empty;
                        }

                    case ClutterObject cl:
                        {
                            FieldInfo f = cl.GetType().GetField("ClutterType");
                            if (f != null)
                                return Convert.ToString(f.GetValue(cl));

                            return FindFieldJson(block, "ClutterType");
                        }

                    case LockerObject l:
                        return FindFieldJson(block, "LockerType");

                    case TargetObject tg:
                        return FindFieldJson(block, "TargetType");

                    default: return string.Empty;
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string FindFieldJson(ObjectBase block, string key)
        {
            try
            {
                List<SyncField> fields = CollabFieldCodec.Extract(block);
                foreach (SyncField f in fields)
                {
                    if (f.key == key)
                    {
                        try
                        {
                            string s = JsonConvert.DeserializeObject<string>(f.json);
                            if (!string.IsNullOrEmpty(s))
                                return s;
                        }
                        catch { }
                        return f.json?.Trim('"') ?? string.Empty;
                    }
                }
            }
            catch { }
            return string.Empty;
        }
    }
}
