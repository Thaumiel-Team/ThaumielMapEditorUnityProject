using System;
using Assets.Scripts.Enums;
using UnityEngine;

namespace Assets.Scripts.Components.Objects
{
    public class CapyBaraObject : ObjectBase
    {
        public bool Collisions;

        public override ObjectType ObjectType => ObjectType.Capybara;

        public override void Compile(Transform root)
        {
            base.Compile(root);
            Properties = new()
            {
                ["Collisions"] = Collisions,
            };
        }

        public override void Decompile(Transform root)
        {
            base.Decompile(root);

            Collisions = Properties.TryGetValue("Collisions", out object collisions) && Convert.ToBoolean(collisions);
        }
    }
}