using System;

namespace Assets.Scripts.Enums
{
    [Flags]
    public enum PrimitiveFlags
    {
        None = 0,
        Collidable = 1 << 0,
        Visible = 2 << 1
    }
}