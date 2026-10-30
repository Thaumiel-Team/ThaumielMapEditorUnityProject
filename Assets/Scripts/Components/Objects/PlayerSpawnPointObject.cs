#if UNITY_EDITOR
using Assets.Scripts.Enums;
using Assets.Scripts.Yaml;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Components.Objects
{
    public class PlayerSpawnPointObject : ObjectBase
    {
        [Tooltip("The roles that are allowed to spawn here.")]
        public List<RoleTypeId> AllowedRoles;

        [Tooltip("The chance for this to be selected.")]
        [Range(0, 100)]
        public float Chance;

        [Tooltip("Flags that disable this spawn point.")]
        public DisableFlags Disable { get; set; }

        public override ObjectType ObjectType => ObjectType.PlayerSpawnPoint;

        private void OnDrawGizmos()
        {
            List<string> colors = new();

            foreach (RoleTypeId role in AllowedRoles)
            {
                colors.Add(GetColorForRole(role));
            }

            if (!ColorUtility.TryParseHtmlString(MeshColors(colors), out var parsedColor))
                parsedColor = Color.white;

            Gizmos.DrawIcon(transform.position, "spawn_icon", true, parsedColor);
        }

        private string GetColorForRole(RoleTypeId role)
        {
            return role switch
            {
                RoleTypeId.NtfCaptain or RoleTypeId.NtfPrivate or RoleTypeId.NtfSergeant or RoleTypeId.NtfSpecialist => "#87ceeb",
                RoleTypeId.ChaosConscript or RoleTypeId.ChaosMarauder or RoleTypeId.ChaosRepressor or RoleTypeId.ChaosRifleman => "#008000",
                RoleTypeId.ClassD => "#ffa500",
                RoleTypeId.Scientist => "#ffff00",
                RoleTypeId.FacilityGuard => "#d3d3d3",
                RoleTypeId.Scp173 or RoleTypeId.Scp3114 or RoleTypeId.Scp049 or RoleTypeId.Scp0492 or RoleTypeId.Scp079 or RoleTypeId.Scp096 or RoleTypeId.Scp106 or RoleTypeId.Scp939 => "#ff0000",
                RoleTypeId.Flamingo or RoleTypeId.AlphaFlamingo or RoleTypeId.ZombieFlamingo or RoleTypeId.NtfFlamingo or RoleTypeId.ChaosFlamingo or RoleTypeId.Tutorial => "#ffc0cb",
                _ => "#ffffff"
            };

        }

        private string MeshColors(IEnumerable<string> colors)
        {
            List<string> colorList = colors.ToList();
            if (colorList.Count == 0) return "#ffffff";

            int totalR = 0, totalG = 0, totalB = 0;

            foreach (string color in colorList)
            {
                string hex = color.TrimStart('#');
                totalR += Convert.ToInt32(hex.Substring(0, 2), 16);
                totalG += Convert.ToInt32(hex.Substring(2, 2), 16);
                totalB += Convert.ToInt32(hex.Substring(4, 2), 16);
            }

            int avgR = totalR / colorList.Count;
            int avgG = totalG / colorList.Count;
            int avgB = totalB / colorList.Count;

            return $"#{avgR:X2}{avgG:X2}{avgB:X2}";
        }

        public override void Compile(Transform root)
        {
            base.Compile(root);
            Properties = new()
            {
                ["AllowedRoles"] = AllowedRoles,
                ["Chance"] = Chance,
                ["DisableFlags"] = Disable
            };
        }

        public override void Decompile(Transform root)
        {
            base.Decompile(root);

            AllowedRoles = Properties.TryGetValue("AllowedRoles", out var roles) ? YamlHelpers.ParseEnumList<RoleTypeId>(roles) : new();
            Chance = Properties.TryGetValue("Chance", out var chance) ? Convert.ToSingle(chance) : 50f;
            Disable = Properties.TryGetValue("DisableFlags", out var disable) ? YamlHelpers.ParseEnum<DisableFlags>(disable) : default;
        }
    }
}
#endif