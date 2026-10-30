#if UNITY_EDITOR
using Assets.Scripts.Enums;
using UnityEngine;

namespace Assets.Scripts.Components.Objects
{
    public class EmptyGameObject : ObjectBase
    {
        public override ObjectType ObjectType => ObjectType.GameObject;
        
        public override void Compile(Transform root)
        {
            base.Compile(root);
        }

        public override void Decompile(Transform root)
        {
            base.Decompile(root);
        }
    }
}
#endif