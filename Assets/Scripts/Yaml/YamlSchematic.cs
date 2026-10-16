using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Yaml
{
    public class YamlSchematic
    {
        public int RootObjectId { get; set; }
        public string FileName { get; set; }
        public Vector3 Rotation { get; set; }
        public Vector3 Scale { get; set; }
        public List<YamlCustomObject> Objects { get; set; } = new();
        public List<YamlCustomObject> ServerSideObjects { get; set; } = new();
        public List<YamlLOD> LOD { get; set; } = new();
    }
}