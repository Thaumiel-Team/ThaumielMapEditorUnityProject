using Assets.Scripts.Enums;
using Assets.Scripts.Yaml;
using System;
using UnityEngine;

namespace Assets.Scripts.Components.Tools
{
    public class Physics : ToolBase
    {
        public override ToolType ToolType => ToolType.Physics;

        [Header("Physics Settings")]
        [Tooltip("The mass of the object in kilograms. Higher values make the object harder to move and more resistant to forces.")]
        public float Weight;

        [Tooltip("How much air resistance is applied to the object, slowing down linear movement over time. 0 means no drag.")]
        public float Drag;

        [Tooltip("How much air resistance is applied to the object's rotation, slowing it down over time. 0 means no angular drag.")]
        public float AngularDrag;

        [Tooltip("How collisions are detected on this object. Discrete is cheapest but may miss fast moving collisions. Use Continuous or ContinuousDynamic for fast moving objects to prevent tunnelling.")]
        public CollisionDetectionMode CollisionMode;

        [Tooltip("Whether physics are active on this object at spawn. If disabled, the object will remain static until enabled at runtime.")]
        public bool Enabled;

        public override void Compile()
        {
            base.Compile();
            base.Properties = new()
            {
                ["Weight"] = Weight,
                ["Enabled"] = Enabled,
                ["Drag"] = Drag,
                ["AngularDrag"] = AngularDrag,
                ["CollisionMode"] = CollisionMode,
            };
        }

        public override void Decompile()
        {
            Weight = Properties.TryGetValue("Weight", out object weightobj) ? Convert.ToSingle(weightobj) : 10f;
            Enabled = Properties.TryGetValue("Enabled", out object enablobj) && Convert.ToBoolean(enablobj);
            Drag = Properties.TryGetValue("Drag", out object dragobj) ? Convert.ToSingle(dragobj) : 5f;
            AngularDrag = Properties.TryGetValue("AngularDrag", out object angularDragobj) ? Convert.ToSingle(angularDragobj) : 5f;
            CollisionMode = Properties.TryGetValue("CollisionMode", out object collobj) ? YamlHelpers.ParseEnum<CollisionDetectionMode>(collobj) : default;
        }
    }
}