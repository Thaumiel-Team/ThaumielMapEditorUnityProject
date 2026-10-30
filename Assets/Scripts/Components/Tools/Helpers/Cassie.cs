#if UNITY_EDITOR
using System;

namespace Assets.Scripts.Components.Tools.Helpers
{
    [Serializable]
    public class SendCassieMessage
    {
        public string Message;
        public string CustomSubtitles;
        public bool PlayBackground;
        public float Priority;
        public float GlitchScale;
    }
}
#endif