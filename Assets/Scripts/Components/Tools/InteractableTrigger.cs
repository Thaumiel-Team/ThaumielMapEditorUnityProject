using System;
using System.Collections.Generic;
using Assets.Scripts.Components.Tools.Helpers;
using Assets.Scripts.Enums;
using Assets.Scripts.Networking.Blocky;
using Assets.Scripts.Yaml;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Components.Tools
{
    public class InteractableTrigger : ToolBase
    {
        public Vector3 Bounds;

        public ColliderShape Shape;

        public float InteractionTime;

        public Permission Permissions;

        public InteractableClasses OnInteracted;

        public InteractableClasses OnInteractionDenied;

        public InteractableClasses OnSpawned;

        public InteractableClasses OnDestroyed;

        public override ToolType ToolType => ToolType.InteractableTrigger;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.orange;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);

            switch (Shape)
            {
                case ColliderShape.Box:
                    Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                    Gizmos.DrawWireCube(Vector3.zero, Bounds);
                    break;

                case ColliderShape.Sphere:
                    Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
                    float radius = Bounds.x * transform.lossyScale.x * 0.5f;
                    Gizmos.DrawWireSphere(Vector3.zero, radius);
                    break;

                case ColliderShape.Capsule:
                    Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
                    float cRadius = Mathf.Min(Bounds.x * transform.lossyScale.x, Bounds.z * transform.lossyScale.z) * 0.5f;
                    float cHeight = Bounds.y * transform.lossyScale.y * 0.5f - cRadius;

                    Gizmos.DrawWireSphere(new Vector3(0,  cHeight, 0), cRadius);
                    Gizmos.DrawWireSphere(new Vector3(0, -cHeight, 0), cRadius);

                    Gizmos.DrawLine(new Vector3( cRadius,  cHeight, 0), new Vector3( cRadius, -cHeight, 0));
                    Gizmos.DrawLine(new Vector3(-cRadius,  cHeight, 0), new Vector3(-cRadius, -cHeight, 0));
                    Gizmos.DrawLine(new Vector3(0,  cHeight,  cRadius), new Vector3(0, -cHeight,  cRadius));
                    Gizmos.DrawLine(new Vector3(0,  cHeight, -cRadius), new Vector3(0, -cHeight, -cRadius));
                    break;
            }
        }

        public override void Compile()
        {
            Properties = new()
            {
                ["Bounds"] = Bounds,
                ["Shape"] = Shape,
                ["Permission"] = Permissions,
                ["InteractionTime"] = InteractionTime,
                ["OnInteracted"] = OnInteracted,
                ["OnInteractionDenied"] = OnInteractionDenied,
                ["OnSpawned"] = OnSpawned,
                ["OnDestroyed"] = OnDestroyed
            };
        }

        public override void Decompile()
        {
            if (Properties.TryGetValue("OnInteracted", out var interacted))
                OnInteracted = YamlHelpers.ParseObject<InteractableClasses>(interacted);

            if (Properties.TryGetValue("OnInteractionDenied", out var denied))
                OnInteractionDenied = YamlHelpers.ParseObject<InteractableClasses>(denied);      

            if (Properties.TryGetValue("OnSpawned", out var spawned))
                OnSpawned = YamlHelpers.ParseObject<InteractableClasses>(spawned);

            if (Properties.TryGetValue("OnDestroyed", out var destroyed))
                OnDestroyed = YamlHelpers.ParseObject<InteractableClasses>(destroyed);

            if (Properties.TryGetValue("InteractionTime", out var time))
                InteractionTime = Convert.ToSingle(time);

            if (Properties.TryGetValue("Shape", out var shape))
                Shape = YamlHelpers.ParseEnum<ColliderShape>(shape);

            if (Properties.TryGetValue("Bounds", out var bounds))
                Bounds = YamlHelpers.ParseVector3(bounds);

            if (Properties.TryGetValue("Permission", out var permission))
                Permissions = YamlHelpers.ParseObject<Permission>(permission);
        }

        public override void OnBlocklyExportReceived(CodeExportPayload payload, string targetEvent)
        {
            if (targetEvent == nameof(OnInteracted))
            {
                OnInteracted ??= new InteractableClasses();
                OnInteracted.Blocky ??= new List<CodeExportPayload>();
                OnInteracted.Blocky.Add(payload);
                Debug.Log($"[InteractableTrigger] Successfully added export to {gameObject.name}'s OnInteracted.Blocky list.");
            }
            else if (targetEvent == nameof(OnInteractionDenied))
            {
                OnInteractionDenied ??= new InteractableClasses();
                OnInteractionDenied.Blocky ??= new List<CodeExportPayload>();
                OnInteractionDenied.Blocky.Add(payload);
                Debug.Log($"[InteractableTrigger] Successfully added export to {gameObject.name}'s OnInteractionDenied.Blocky list.");
            }
            else if (targetEvent == nameof(OnSpawned))
            {
                OnSpawned ??= new InteractableClasses();
                OnSpawned.Blocky ??= new List<CodeExportPayload>();
                OnSpawned.Blocky.Add(payload);
                Debug.Log($"[InteractableTrigger] Successfully added export to {gameObject.name}'s OnSpawned.Blocky list.");
            }
            else if (targetEvent == nameof(OnDestroyed))
            {
                OnDestroyed ??= new InteractableClasses();
                OnDestroyed.Blocky ??= new List<CodeExportPayload>();
                OnDestroyed.Blocky.Add(payload);
                Debug.Log($"[InteractableTrigger] Successfully added export to {gameObject.name}'s OnDestroyed.Blocky list.");
            }

            EditorUtility.SetDirty(this);
        }
    }
}