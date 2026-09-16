using UnityEngine;
using Assets.Scripts.Enums;
using System;
using Assets.Scripts.Yaml;

namespace Assets.Scripts.Components.Objects
{
    public class InteractableObject : ObjectBase
    {
        [Header("Interactable Settings")]
        [Tooltip("The shape of the collider used to detect when a player is interacting with this object.")]
        public ColliderShape Shape;

        [Tooltip("How long in seconds the player must hold the interact key to trigger this object. Set to 0 for an instant interaction.")]
        public float Duration;

        [Tooltip("If enabled, players cannot interact with this object regardless of their permissions.")]
        public bool Locked;

        [Tooltip("The keycard permission flags required to interact with this object. Multiple flags can be combined.")]
        public DoorPermissionFlags Permissions;

        public override ObjectType ObjectType => ObjectType.Interactable;

        public override void Compile(Transform root)
        {
            base.Compile(root);
            Properties = new()
            {
                ["Shape"] = Shape,
                ["Duration"] = Duration,
                ["Locked"] = Locked,
                ["Permissions"] = Permissions
            };
        }

        private void OnValidate()
        {
            MeshFilter meshFilter = GetComponent<MeshFilter>();

            switch (Shape)
            {
                case ColliderShape.Box:
                    meshFilter.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                    break;

                case ColliderShape.Sphere:
                    meshFilter.mesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
                    break;

                case ColliderShape.Capsule:
                    meshFilter.mesh = Resources.GetBuiltinResource<Mesh>("Capsule.fbx");
                    break;
            }
        }

        public override void Decompile(Transform root)
        {
            base.Decompile(root);

            Shape = Properties.TryGetValue("Shape", out object shape) ? YamlHelpers.ParseEnum<ColliderShape>(shape) : default;
            Duration = Properties.TryGetValue("Duration", out object duration) ? Convert.ToSingle(duration) : default;
            Locked = Properties.TryGetValue("Locked", out object locked) && Convert.ToBoolean(locked);
            Permissions = Properties.TryGetValue("Permissions", out object perms) ? YamlHelpers.ParseEnum<DoorPermissionFlags>(perms) : default;
        }
    }
}