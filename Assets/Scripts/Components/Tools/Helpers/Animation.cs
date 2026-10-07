using System;
using UnityEngine;
using YamlDotNet.Serialization;

namespace Assets.Scripts.Components.Tools.Helpers
{
    [Serializable]
    public class PlayAnimation
    {
        [YamlIgnore]
        public Animator Animator;

        [HideInInspector]
        [YamlIgnore]
        public string AnimationName;

        [HideInInspector]
        public string ResolvedAnimationName => !string.IsNullOrEmpty(AnimationName) ? AnimationName : Animator != null ? Animator.runtimeAnimatorController != null ? Animator.runtimeAnimatorController.name : null : null;
    }
}