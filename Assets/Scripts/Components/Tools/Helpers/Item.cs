#if UNITY_EDITOR
using System;
using Assets.Scripts.Enums;

namespace Assets.Scripts.Components.Tools.Helpers
{
    [Serializable]
    public class GiveItem
    {
        public ItemType Item;
        public uint Count;
    }

    [Serializable]
    public class RemoveItem
    {
        public ItemType Item;
        public uint Count;
    }
}
#endif