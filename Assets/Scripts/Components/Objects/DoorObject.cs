using System;
using Assets.Scripts.Enums;
using Assets.Scripts.Yaml;
using UnityEngine;

namespace Assets.Scripts.Components.Objects
{
    public class DoorObject : ObjectBase
    {
        [HideInInspector]
        public DoorType DoorType;

        [Header("Door Settings")]
        [Tooltip("The keycard permission flags required to open this door. Multiple flags can be combined.")]
        public DoorPermissionFlags Permissions;

        [Tooltip("If enabled, the player must hold all of the listed permissions rather than just one.")]
        public bool RequireAllPermissions;

        [Tooltip("If enabled, SCP-2176 can bypass this door's permissions.")]
        public bool Bypass2176;

        [Tooltip("The maximum health of this door. Only applies to breakable door types such as LCZ and HCZ doors.")]
        public float MaxHealth;

        [Tooltip("The starting health of this door. Only applies to breakable door types such as LCZ and HCZ doors.")]
        public float Health;

        [Tooltip("Whether the door starts open when spawned.")]
        public bool IsOpen;

        [Tooltip("Whether the door starts locked when spawned. A locked door cannot be opened by players regardless of their permissions.")]
        public bool IsLocked;

        public override ObjectType ObjectType => ObjectType.Door;

        public override void Compile(Transform root)
        {
            base.Compile(root);
            Properties = new()
            {
                ["DoorType"] = DoorType,
                ["Permissions"] = Permissions,
                ["RequireAllPermissions"] = RequireAllPermissions,
                ["Bypass2176"] = Bypass2176,
                ["MaxHealth"] = MaxHealth,
                ["Health"] = Health,
                ["IsOpen"] = IsOpen,
                ["IsLocked"] = IsLocked,
            };
        }

        public override void Decompile(Transform root)
        {
            base.Decompile(root);

            DoorType = Properties.TryGetValue("DoorType", out object doorType) ? YamlHelpers.ParseEnum<DoorType>(doorType) : default;
            Permissions = Properties.TryGetValue("Permissions", out object permissions) ? YamlHelpers.ParseEnum<DoorPermissionFlags>(permissions) : default;
            RequireAllPermissions = Properties.TryGetValue("RequireAllPermissions", out object requireAllPermissions) && Convert.ToBoolean(requireAllPermissions);
            Bypass2176 = Properties.TryGetValue("Bypass2176", out object bypass2176) && Convert.ToBoolean(bypass2176);
            MaxHealth = Properties.TryGetValue("MaxHealth", out object maxHealth) ? Convert.ToSingle(maxHealth) : default;
            Health = Properties.TryGetValue("Health", out object health) ? Convert.ToSingle(health) : default;
            IsOpen = Properties.TryGetValue("IsOpen", out object isOpen) && Convert.ToBoolean(isOpen);
            IsLocked = Properties.TryGetValue("IsLocked", out object isLocked) && Convert.ToBoolean(isLocked);
        }
    }
}