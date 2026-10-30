#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Yaml
{
    [Serializable]
    public class YamlLOD
    {
        [field: SerializeField]
        public Vector3 Bounds { get; set; }
        
        [field: SerializeField]
        public List<PrimitiveType> Primitives { get; set; }
    }
}
#endif