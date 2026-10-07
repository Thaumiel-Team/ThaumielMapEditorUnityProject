using System;
using System.Collections.Generic;
using Assets.Scripts.Enums;
using Assets.Scripts.Yaml;
using UnityEngine;

namespace Assets.Scripts.Components.Objects
{
    public class WorkstationObject : ObjectBase
    {
        public override ObjectType ObjectType => ObjectType.Workstation;

        [Header("Workstation Settings")]
        [Tooltip("The roles allowed to interact with this workstation.")]
        public List<RoleTypeId> AllowedRoles;

        [Tooltip("Whether or not people can interact with this workstation.")]
        public bool AllowInteractions;

        public override void Compile(Transform root)
        {
            base.Compile(root);
            Properties = new()
            {
                ["AllowedRoles"] = AllowedRoles,
                ["AllowInteractions"] = AllowInteractions
            };
        }

        public override void Decompile(Transform root)
        {
            base.Decompile(root);
            AllowedRoles = Properties.TryGetValue("AllowedRoles", out object rolesobj) ? YamlHelpers.ParseEnumList<RoleTypeId>(rolesobj) : default;
            AllowInteractions = !Properties.TryGetValue("AllowInteractions", out object allowobj) || Convert.ToBoolean(allowobj);
        }
    }
}