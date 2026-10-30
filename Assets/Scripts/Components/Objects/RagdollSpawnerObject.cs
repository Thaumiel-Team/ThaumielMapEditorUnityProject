#if UNITY_EDITOR
using System;
using Assets.Scripts.Enums;
using Assets.Scripts.Yaml;
using UnityEngine;

namespace Assets.Scripts.Components.Objects
{
    public class RagdollSpawner : ObjectBase
    {
        public RoleTypeId RoleType;

        [Range(0, 100)]
        public float SpawnChance;

        public string DeathReason;

        public string DollName;

        public override ObjectType ObjectType => ObjectType.RagdollSpawner;

        private void OnDrawGizmos()
        {
            switch (RoleType)
            {
                case RoleTypeId.ChaosConscript or RoleTypeId.ChaosMarauder or RoleTypeId.ChaosRepressor or RoleTypeId.ChaosRifleman:
                    Gizmos.DrawIcon(transform.position, "chaos_icon", true);
                    break;

                case RoleTypeId.NtfCaptain or RoleTypeId.NtfPrivate or RoleTypeId.NtfSergeant or RoleTypeId.NtfSpecialist:
                    Gizmos.DrawIcon(transform.position, "ntf_icon", true);
                    break;

                case RoleTypeId.Tutorial:
                    Gizmos.DrawIcon(transform.position, "tutorial_icon", true);
                    break;

                case RoleTypeId.ClassD:
                    Gizmos.DrawIcon(transform.position, "classd_icon", true);
                    break;

                case RoleTypeId.Scientist:
                    Gizmos.DrawIcon(transform.position, "scientist_icon", true);
                    break;

                case RoleTypeId.FacilityGuard:
                    Gizmos.DrawIcon(transform.position, "facilityguard_icon", true);
                    break;

                case RoleTypeId.Scp049:
                    Gizmos.DrawIcon(transform.position, "scp049_icon", true);
                    break;

                case RoleTypeId.Scp0492:
                    Gizmos.DrawIcon(transform.position, "scp0492_icon", true);
                    break;

                case RoleTypeId.Scp079:
                    Gizmos.DrawIcon(transform.position, "scp079_icon", true);
                    break;

                case RoleTypeId.Scp096:
                    Gizmos.DrawIcon(transform.position, "scp096_icon", true);
                    break;

                case RoleTypeId.Scp106:
                    Gizmos.DrawIcon(transform.position, "scp106_icon", true);
                    break;

                case RoleTypeId.Scp173:
                    Gizmos.DrawIcon(transform.position, "scp173_icon", true);
                    break;

                case RoleTypeId.Scp3114:
                    Gizmos.DrawIcon(transform.position, "scp3114_icon", true);
                    break;

                case RoleTypeId.Scp939:
                    Gizmos.DrawIcon(transform.position, "scp939_icon", true);
                    break;

                case RoleTypeId.Flamingo or RoleTypeId.NtfFlamingo or RoleTypeId.AlphaFlamingo or RoleTypeId.ChaosFlamingo or RoleTypeId.ZombieFlamingo:
                    Gizmos.DrawIcon(transform.position, "flamingo_icon", true);
                    break;
            }
        }

        public override void Compile(Transform root)
        {
            base.Compile(root);
            Properties = new()
            {
                ["RoleType"] = RoleType,
                ["Chance"] = SpawnChance,
                ["DeathReason"] = DeathReason,
                ["DollName"] = DollName
            };
        }

        public override void Decompile(Transform root)
        {
            RoleType = Properties.TryGetValue("RoleType", out object role) ? YamlHelpers.ParseEnum<RoleTypeId>(role) : default;
            SpawnChance = Properties.TryGetValue("Chance", out object chance) ? Convert.ToSingle(chance) : default;
            DeathReason = Properties.TryGetValue("DeathReason", out object death) ? Convert.ToString(death) : default;
            DollName = Properties.TryGetValue("DollName", out object doll) ? Convert.ToString(doll) : default;
        }
    }
}
#endif