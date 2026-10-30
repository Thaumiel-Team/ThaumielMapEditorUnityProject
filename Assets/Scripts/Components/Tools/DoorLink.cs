#if UNITY_EDITOR
using System;
using Assets.Scripts.Enums;
using UnityEngine;

namespace Assets.Scripts.Components.Tools
{
    public class DoorLink : ToolBase
    {
        public override ToolType ToolType => ToolType.Doorlink;

        [Header("This should be attached to a door")]
        [Header("If not it will not work.")]

        [Header("Door Link Settings")]
        [Tooltip("All doors sharing the same Group ID will be linked together. When one door opens, all others in the group will close and vice versa.")]
        public string GroupId;

        public override void Compile()
        {
            Properties = new()
            {
                ["GroupId"] = GroupId
            };
        }

        public override void Decompile()
        {
            GroupId = Properties.TryGetValue("GroupId", out var idobj) ? Convert.ToString(idobj) : "FAILED";
        }
    }
}
#endif