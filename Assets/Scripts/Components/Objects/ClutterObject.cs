using Assets.Scripts.Enums;
using Assets.Scripts.Yaml;
using UnityEngine;

namespace Assets.Scripts.Components.Objects
{
    public class ClutterObject : ObjectBase
    {
        [HideInInspector]
        public ClutterType ClutterType;

        public override ObjectType ObjectType => ObjectType.Clutter;

        public override void Compile(Transform root)
        {
            base.Compile(root);
            Properties = new()
            {
                ["ClutterType"] = ClutterType,
            };
        }

        public override void Decompile(Transform root)
        {
            base.Decompile(root);

            ClutterType = Properties.TryGetValue("ClutterType", out object clutterType) ? YamlHelpers.ParseEnum<ClutterType>(clutterType) : default;
        }
    }
}