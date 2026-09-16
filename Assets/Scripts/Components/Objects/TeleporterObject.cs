using System;
using System.Collections.Generic;
using Assets.Scripts.Enums;
using Assets.Scripts.Yaml;
using UnityEngine;

namespace Assets.Scripts.Components.Objects
{
    public class TeleporterObject : ObjectBase
    {
        [Header("Teleporter Settings")]
        [Tooltip("The unique identifier for this teleporter.")]
        [HideInInspector]
        public Guid Id;

        [Tooltip("The teleporter that this object will send players to.")]
        public List<TeleporterObject> Targets = new();

        public List<Guid> TargetIds = new();

        private List<string> RawIds = new();

        [Tooltip("The cooldown in seconds before this teleporter can be used again.")]
        public float CoolDown;

        [Tooltip("The roles that are allowed to use this teleporter.")]
        public List<RoleTypeId> AllowedRoles = new();

        [Tooltip("If enabled, each player has their own separate cooldown.")]
        public bool PerPlayerCooldown;

        [Tooltip("Additional behavior flags that modify how this teleporter works.")]
        public TeleporterFlags Flags;

        public override ObjectType ObjectType => ObjectType.Teleporter;

        private void OnValidate()
        {
            if (Id == Guid.Empty)
            {
                Id = Guid.NewGuid();
                UnityEditor.EditorUtility.SetDirty(this);
            }

            List<Guid> ids = new();
            foreach (TeleporterObject target in Targets)
            {
                if (target != null)
                {
                    if (target.Id == Guid.Empty)
                    {
                        target.Id = Guid.NewGuid();
                        UnityEditor.EditorUtility.SetDirty(target);
                    }

                    ids.Add(target.Id);
                }
            }

            TargetIds = ids;
        }

        public void GetTargetById()
        {
            if (TargetIds == null || TargetIds.Count == 0)
                return;

            if (Targets == null)
            {
                Targets = new();
            }
            else
                Targets.Clear();

            Builder builder = GetComponentInParent<Builder>();
            if (builder == null)
            {
                Debug.LogWarning($"No Builder found for TeleporterObject '{name}'");
                return;
            }

            HashSet<Guid> idSet = new(TargetIds);
            foreach (TeleporterObject child in builder.GetComponentsInChildren<TeleporterObject>())
            {
                if (child == this)
                    continue;

                if (child.Id == Guid.Empty)
                {
                    Debug.LogWarning($"TeleporterObject '{child.name}' has empty Guid");
                    continue;
                }

                if (idSet.Contains(child.Id))
                    Targets.Add(child);
            }
        }

        public void ParseIds()
        {
            List<Guid> ids = new();
            foreach (string rawid in RawIds)
            {
                if (!Guid.TryParse(rawid, out var result))
                {
                    Debug.LogWarning($"Failed to parse id {rawid} into Guid for TeleporterObject '{name}'.");
                    continue;
                }

                ids.Add(result);
            }

            TargetIds = ids;
            RawIds.Clear();
        }

        public void CheckId()
        {
            if (Id == Guid.Empty)
                Id = Guid.NewGuid();
        }

        public override void Compile(Transform root)
        {
            if (Targets.Count == 0)
            {
                Debug.LogWarning($"TeleporterObject '{name}' has no Targets assigned. Skipping compile.", this);
                return;
            }

            base.Compile(root);

            Properties = new()
            {
                ["Id"] = Id,
                ["Target"] = TargetIds,
                ["CoolDown"] = CoolDown,
                ["AllowedRoles"] = AllowedRoles,
                ["PerPlayerCooldown"] = PerPlayerCooldown,
                ["Flags"] = Flags
            };
        }

        public override void Decompile(Transform root)
        {
            base.Decompile(root);

            if (Properties == null || Properties.Count == 0)
            {
                Debug.LogWarning($"TeleporterObject '{name}' has no properties to decompile.", this);
                Id = Guid.NewGuid();
                return;
            }

            Id = Properties.TryGetValue("Id", out object id) ? Guid.Parse(Convert.ToString(id)) : default;
            RawIds = Properties.TryGetValue("Target", out object targetid) ? YamlHelpers.ParseList<string>(targetid) : default;
            CoolDown = Properties.TryGetValue("CoolDown", out object cooldown) ? Convert.ToSingle(cooldown) : default;
            AllowedRoles = Properties.TryGetValue("AllowedRoles", out object allowed) ? YamlHelpers.ParseEnumList<RoleTypeId>(allowed) : new();
            PerPlayerCooldown = Properties.TryGetValue("PerPlayerCooldown", out object perplayer) && Convert.ToBoolean(perplayer);
            Flags = Properties.TryGetValue("Flags", out object teleporterFlags) ? YamlHelpers.ParseEnum<TeleporterFlags>(teleporterFlags) : default;

            CheckId();
            ParseIds();
        }
    }
}