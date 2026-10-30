#if UNITY_EDITOR
using System;
using Assets.Scripts.Enums;

namespace Assets.Scripts.Components.Tools.Helpers
{
    [Serializable]
    public class Warhead
    {
        public WarheadAction Action;
        public bool SuppressSubtitles;
    }
}
#endif