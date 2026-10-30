#if UNITY_EDITOR
using System;
using Assets.Scripts.Enums;

namespace Assets.Scripts.Components.Tools.Helpers
{
    [Serializable]
    public class GiveEffect
    {
        public int Intensity;
        public float Duration;
        public EffectType Effect;
    }

    [Serializable]
    public class RemoveEffect
    {
        public int Intensity;
        public EffectType Effect;
    }
}
#endif