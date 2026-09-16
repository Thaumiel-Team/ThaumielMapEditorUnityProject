using System.Collections.Generic;
using Assets.Scripts.Components.Tools.Helpers;
using Assets.Scripts.Enums;
using Assets.Scripts.Networking.Blocky;
using Assets.Scripts.Yaml;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Components.Tools
{
    public class ColliderTrigger : ToolBase
    {
        public Vector3 Bounds;

        public ColliderClasses OnEntered;

        public ColliderClasses OnExited;

        public ColliderClasses OnSpawned;

        public ColliderClasses OnDestroyed;

        public Permission Permissions;

        public override ToolType ToolType => ToolType.ColliderTrigger;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.DrawWireCube(Vector3.zero, Bounds);
        }

        public override void Compile()
        {
            Properties = new()
            {
                ["Bounds"] = Bounds,
                ["OnEntered"] = OnEntered,
                ["OnExited"] = OnExited,
                ["OnSpawned"] = OnSpawned,
                ["OnDestroyed"] = OnDestroyed,
                ["Permission"] = Permissions
            };
        }

        public override void Decompile()
        {
            if (Properties.TryGetValue("OnEntered", out var entered))
                OnEntered = YamlHelpers.ParseObject<ColliderClasses>(entered);

            if (Properties.TryGetValue("OnExited", out var exited))
                OnExited = YamlHelpers.ParseObject<ColliderClasses>(exited);

            if (Properties.TryGetValue("OnSpawned", out var spawned))
                OnSpawned = YamlHelpers.ParseObject<ColliderClasses>(spawned);

            if (Properties.TryGetValue("OnDestroyed", out var destroyed))
                OnDestroyed = YamlHelpers.ParseObject<ColliderClasses>(destroyed);

            if (Properties.TryGetValue("Bounds", out var bounds))
                Bounds = YamlHelpers.ParseVector3(bounds);

            if (Properties.TryGetValue("Permission", out var perms))
                Permissions = YamlHelpers.ParseObject<Permission>(perms);
        }

        public override void OnBlocklyExportReceived(CodeExportPayload payload, string targetEvent)
        {
            if (targetEvent == nameof(OnEntered))
            {
                OnEntered ??= new ColliderClasses();
                OnEntered.Blocky ??= new List<CodeExportPayload>();
                OnEntered.Blocky.Add(payload);
                Debug.Log($"[ColliderTrigger] Successfully added export to {gameObject.name}'s OnEntered.Blocky list.");
            }
            else if (targetEvent == nameof(OnExited))
            {
                OnExited ??= new ColliderClasses();
                OnExited.Blocky ??= new List<CodeExportPayload>();
                OnExited.Blocky.Add(payload);
                Debug.Log($"[ColliderTrigger] Successfully added export to {gameObject.name}'s OnExited.Blocky list.");
            }
            else if (targetEvent == nameof(OnSpawned))
            {
                OnSpawned ??= new ColliderClasses();
                OnSpawned.Blocky ??= new List<CodeExportPayload>();
                OnSpawned.Blocky.Add(payload);
                Debug.Log($"[ColliderTrigger] Successfully added export to {gameObject.name}'s OnSpawned.Blocky list.");
            }
            else if (targetEvent == nameof(OnDestroyed))
            {
                OnDestroyed ??= new ColliderClasses();
                OnDestroyed.Blocky ??= new List<CodeExportPayload>();
                OnDestroyed.Blocky.Add(payload);
                Debug.Log($"[ColliderTrigger] Successfully added export to {gameObject.name}'s OnDestroyed.Blocky list.");
            }

            EditorUtility.SetDirty(this);
        }
    }
}