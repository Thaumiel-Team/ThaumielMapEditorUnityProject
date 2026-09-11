using System.Collections.Generic;
using Assets.Scripts.Enums;
using Assets.Scripts.Yaml;
using UnityEngine;
using YamlDotNet.Serialization;

namespace Assets.Scripts.Components.Objects
{
#pragma warning disable CS0618
    public class ObjectBase : MonoBehaviour
    {
        [YamlIgnore]
        public virtual ObjectType ObjectType { get; internal set; }

        public int ObjectId { get; set; }
        public int ParentId { get; set; } = 0;

        public string Name { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Scale { get; set; }
        public Vector3 Rotation { get; set; }

        [field: SerializeField]
        public bool Static { get; set; }

        [field: SerializeField]
        public float MovementSmoothing { get; set; }

        public YamlCullingSettings CullingSettings;

        public Dictionary<string, object> Properties { get; set; }

        public bool ServerSide { get; set; }
        
        public string AnimatorName { get; set; }

        public virtual void Compile(Transform root)
        {
            Transform t = transform;
            Name = t.name;

            ObjectId = transform.gameObject.GetInstanceID();

            if (ServerSide && transform.parent != null && transform.parent.parent != null)
            {
                ParentId = transform.parent.parent.gameObject.GetInstanceID();
            }
            else
                ParentId = transform.parent.gameObject.GetInstanceID();

            Position = t.localPosition;
            Rotation = t.localEulerAngles;
            Scale = t.localScale;
            Static = gameObject.isStatic;
        }

        public virtual void Decompile(Transform root)
        {
            Transform t = transform;
            t.name = Name;

            t.SetPositionAndRotation(root.TransformPoint(Position), root.rotation * Quaternion.Euler(Rotation));
            t.localScale = Scale;
            gameObject.isStatic = Static;
        }
    }
}