using System;

namespace Assets.Scripts.Binary
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class BinaryIgnoreAttribute : Attribute
    {
    }
}