#if UNITY_EDITOR
using System;

namespace Assets.Scripts.Binary
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class BinaryPropertyAttribute : Attribute
    {
        public string Name { get; set; }
        public int Order { get; set; }
        public bool Required { get; set; }
    }
}
#endif