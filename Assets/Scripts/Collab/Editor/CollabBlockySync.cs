using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Components.Tools;
using Assets.Scripts.Components.Tools.Helpers;
using Assets.Scripts.Enums;
using Assets.Scripts.Networking.Blocky;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Collab.Editor
{
    public static class CollabBlockySync
    {
        private static readonly Dictionary<string, string> _lastRemoteXml = new();
        private static readonly Dictionary<string, string> _lastRemoteCode = new();

        private static string TargetKey(string guid, int toolType, string field) => $"{guid}|{toolType}|{field}";

        public static bool IsEcho(string guid, int toolType, string field, CodeExportPayload payload)
        {
            if (payload == null)
                return true;

            string key = TargetKey(guid, toolType, field);
            return _lastRemoteXml.TryGetValue(key, out string xml) && _lastRemoteCode.TryGetValue(key, out string code) && string.Equals(xml ?? string.Empty, payload.Xml ?? string.Empty, StringComparison.Ordinal) && string.Equals(code ?? string.Empty, payload.Code ?? string.Empty, StringComparison.Ordinal);
        }

        public static bool TryBroadcastLocalExport(ToolBase tool, string fieldName, CodeExportPayload payload)
        {
            if (tool == null || string.IsNullOrEmpty(fieldName) || payload == null)
                return false;

            if (!CollabNet.Connected)
                return false;

            string guid = CollabId.Get(tool.gameObject);
            if (string.IsNullOrEmpty(guid))
                guid = CollabId.Ensure(tool.gameObject);

            if (string.IsNullOrEmpty(guid))
                return false;

            int toolType = (int)tool.ToolType;

            if (IsEcho(guid, toolType, fieldName, payload))
            {
                CollabNet.Log($"Blocky echo suppressed for {tool.gameObject.name}.{fieldName}");
                return false;
            }

            BlockyPayloadCodec.Normalize(payload);
            BlockyData data = new()
            {
                guid = guid,
                toolType = toolType,
                field = fieldName,
                payload = payload
            };

            CollabNet.BroadcastReliable(CollabTypes.Blocky, CollabJson.Serialize(data));
            return true;
        }

        public static string ApplyRemote(BlockyData data)
        {
            if (data == null || string.IsNullOrEmpty(data.guid) || data.payload == null)
                return null;

            GameObject go = CollabId.Find(data.guid);
            if (go == null)
                return null;

            ToolBase tool = FindOrCreateTool(go, data.toolType);
            if (tool == null)
                return null;

            BlockyPayloadCodec.Normalize(data.payload);
            string applied = ApplyToTool(tool, data.field, data.payload);
            if (applied == null)
                return null;

            string key = TargetKey(data.guid, data.toolType, data.field);
            _lastRemoteXml[key] = data.payload.Xml ?? string.Empty;
            _lastRemoteCode[key] = data.payload.Code ?? string.Empty;

            EditorUtility.SetDirty(tool);
            EditorUtility.SetDirty(go);
            return applied;
        }

        public static void NormalizeTool(ToolBase tool)
        {
            if (tool == null)
                return;

            switch (tool)
            {
                case BlockyRuntime runtime:
                    BlockyPayloadCodec.Normalize(runtime.Blocky);
                    break;

                case ColliderTrigger collider:
                    NormalizeContainer(collider.OnEntered?.Blocky);
                    NormalizeContainer(collider.OnExited?.Blocky);
                    NormalizeContainer(collider.OnSpawned?.Blocky);
                    NormalizeContainer(collider.OnDestroyed?.Blocky);
                    break;

                case InteractableTrigger interactable:
                    NormalizeContainer(interactable.OnInteracted?.Blocky);
                    NormalizeContainer(interactable.OnInteractionDenied?.Blocky);
                    NormalizeContainer(interactable.OnSpawned?.Blocky);
                    NormalizeContainer(interactable.OnDestroyed?.Blocky);
                    break;

                default:
                    NormalizeViaReflection(tool);
                    break;
            }
        }

        private static void NormalizeContainer(List<CodeExportPayload> list)
        {
            if (list == null)
                return;

            foreach (CodeExportPayload payload in list)
                BlockyPayloadCodec.Normalize(payload);
        }

        private static void NormalizeViaReflection(ToolBase tool)
        {
            FieldInfo[] fields = tool.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo f in fields)
            {
                try
                {
                    if (f.FieldType == typeof(CodeExportPayload))
                    {
                        BlockyPayloadCodec.Normalize((CodeExportPayload)f.GetValue(tool));
                    }
                    else
                    {
                        object container = f.GetValue(tool);
                        if (container == null)
                            continue;

                        FieldInfo blocky = container.GetType().GetField("Blocky", BindingFlags.Public | BindingFlags.Instance);
                        if (blocky != null && blocky.FieldType == typeof(List<CodeExportPayload>))
                            NormalizeContainer((List<CodeExportPayload>)blocky.GetValue(container));
                    }
                }
                catch { }
            }
        }

        private static ToolBase FindOrCreateTool(GameObject go, int toolType)
        {
            foreach (ToolBase tb in go.GetComponents<ToolBase>())
            {
                if (tb != null && (int)CollabToolFactory.KindOf(tb) == toolType)
                    return tb;
            }

            Type t = CollabToolFactory.TypeFor((ToolType)toolType);
            if (t == null)
                return null;

            return (ToolBase)go.AddComponent(t);
        }

        private static string ApplyToTool(ToolBase tool, string field, CodeExportPayload payload)
        {
            if (tool is BlockyRuntime runtime && field == nameof(BlockyRuntime.Blocky))
            {
                if (BlockyPayloadCodec.SameContent(runtime.Blocky, payload))
                    return null;

                runtime.Blocky = payload;
                return payload.Xml;
            }

            if (tool is ColliderTrigger collider)
            {
                ColliderClasses container = field switch
                {
                    nameof(ColliderTrigger.OnEntered) => collider.OnEntered ??= new ColliderClasses(),
                    nameof(ColliderTrigger.OnExited) => collider.OnExited ??= new ColliderClasses(),
                    nameof(ColliderTrigger.OnSpawned) => collider.OnSpawned ??= new ColliderClasses(),
                    nameof(ColliderTrigger.OnDestroyed) => collider.OnDestroyed ??= new ColliderClasses(),
                    _ => null,
                };

                if (container == null)
                    return null;

                return AppendToList(container, payload);
            }

            if (tool is InteractableTrigger interactable)
            {
                InteractableClasses container = field switch
                {
                    nameof(InteractableTrigger.OnInteracted) => interactable.OnInteracted ??= new InteractableClasses(),
                    nameof(InteractableTrigger.OnInteractionDenied) => interactable.OnInteractionDenied ??= new InteractableClasses(),
                    nameof(InteractableTrigger.OnSpawned) => interactable.OnSpawned ??= new InteractableClasses(),
                    nameof(InteractableTrigger.OnDestroyed) => interactable.OnDestroyed ??= new InteractableClasses(),
                    _ => null,
                };

                if (container == null)
                    return null;

                return AppendToList(container, payload);
            }

            return ApplyViaReflection(tool, field, payload);
        }

        private static string AppendToList(ColliderClasses container, CodeExportPayload payload)
        {
            container.Blocky ??= new List<CodeExportPayload>();
            if (BlockyPayloadCodec.ContainsContent(container.Blocky, payload))
                return null;

            container.Blocky.Add(payload);
            return payload.Xml;
        }

        private static string AppendToList(InteractableClasses container, CodeExportPayload payload)
        {
            container.Blocky ??= new List<CodeExportPayload>();
            if (BlockyPayloadCodec.ContainsContent(container.Blocky, payload))
                return null;

            container.Blocky.Add(payload);
            return payload.Xml;
        }

        private static string ApplyViaReflection(ToolBase tool, string field, CodeExportPayload payload)
        {
            FieldInfo f = tool.GetType().GetField(field, BindingFlags.Public | BindingFlags.Instance);
            if (f == null)
                return null;

            try
            {
                if (f.FieldType == typeof(CodeExportPayload))
                {
                    if (BlockyPayloadCodec.SameContent((CodeExportPayload)f.GetValue(tool), payload))
                        return null;

                    f.SetValue(tool, payload);
                    return payload.Xml;
                }

                object container = f.GetValue(tool);
                if (container == null)
                    return null;

                FieldInfo blocky = container.GetType().GetField("Blocky", BindingFlags.Public | BindingFlags.Instance);
                if (blocky == null || blocky.FieldType != typeof(List<CodeExportPayload>))
                    return null;

                List<CodeExportPayload> list = (List<CodeExportPayload>)blocky.GetValue(container);
                if (list == null)
                {
                    list = new List<CodeExportPayload>();
                    blocky.SetValue(container, list);
                }

                if (BlockyPayloadCodec.ContainsContent(list, payload))
                    return null;

                list.Add(payload);
                return payload.Xml;
            }
            catch
            {
                return null;
            }
        }
    }
}
