using System.Collections.Generic;
using Assets.Scripts.Enums;
using UnityEngine;

namespace Assets.Scripts.Yaml
{
    public class YamlCustomObject
    {
        public int ObjectId { get; set; }
        public int ParentId { get; set; }
        public string Name { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Scale { get; set; }
        public Vector3 Rotation { get; set; }
        public bool IsStatic { get; set; }
        public float MovementSmoothing { get; set; }
        public ObjectType ObjectType { get; set; }
        public Dictionary<string, object> Values { get; set; }
        public List<YamlTool> Tools { get; set; }
        public string AnimatorName { get; set; }
    }
}