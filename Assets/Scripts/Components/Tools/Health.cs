#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Assets.Scripts.Enums;
using Assets.Scripts.Yaml;
using UnityEngine;

namespace Assets.Scripts.Components.Tools
{
    public class Health : ToolBase
    {
        public enum DestroyState
        {
            Animate,
            ApplyPhysics,
            Destroy
        }

        public override ToolType ToolType => ToolType.Health;

        [Header("Health Settings")]
        [Tooltip("What happens to the object when its health reaches zero. Animate plays the specified animation, ApplyPhysics enables physics on the object, and Destroy removes it immediately.")]
        public DestroyState State;

        [Tooltip("The maximum health of the object.")]
        public float MaxHealth;

        [Tooltip("How long in seconds the object waits before despawning after its health reaches zero.")]
        public float DespawnTime;

        [Tooltip("The damage types that can damage this object. Any damage type not in this list will be ignored.")]
        public List<DamageType> AllowedDamage;

        [Tooltip("The name of the animation to play when the object's health reaches zero. Only used when State is set to Animate.")]
        public string AnimationName;

        public override void Compile()
        {
            base.Compile();
            base.Properties = new()
            {
                ["State"] = State,
                ["MaxHealth"] = MaxHealth,
                ["AllowedDamage"] = AllowedDamage,
                ["Despawn"] = DespawnTime,
                ["StateName"] = AnimationName,
            };
        }

        public override void Decompile()
        {
            State = Properties.TryGetValue("State", out object stateobj) ? YamlHelpers.ParseEnum<DestroyState>(stateobj) : default;
            MaxHealth = Properties.TryGetValue("MaxHealth", out object maxobj) ? Convert.ToSingle(maxobj) : 100f;
            AllowedDamage = Properties.TryGetValue("AllowedDamage", out object allowedobj) ? YamlHelpers.ParseEnumList<DamageType>(allowedobj) : default;
            DespawnTime = Properties.TryGetValue("Despawn", out object despawnobj) ? Convert.ToSingle(despawnobj) : 5f;
            AnimationName = Properties.TryGetValue("StateName", out object animobj) ? Convert.ToString(animobj) : string.Empty;
        }
    }
}
#endif