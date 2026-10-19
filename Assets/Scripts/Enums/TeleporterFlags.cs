using System;

namespace Assets.Scripts.Enums
{
    [Flags]
    public enum TeleporterFlags
    {
        None = 0,
        AllowPickups = 1 << 0,
        AllowPlayers = 1 << 1,
        AllowProjectiles = 1 << 2
    }
}