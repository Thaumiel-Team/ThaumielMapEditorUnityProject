#if UNITY_EDITOR
using System;

namespace Assets.Scripts.Components.Tools.Helpers
{
    [Serializable]
    public class PlayAudio
    {
        public string Path;
        public float Volume;
        public float MinDistance;
        public float MaxDistance;
        public bool IsSpatial;
    }
}
#endif