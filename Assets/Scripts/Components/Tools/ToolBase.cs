using UnityEngine;
using Assets.Scripts.Enums;
using System.Collections.Generic;

#if TME_BLOCKY
using Assets.Scripts.Networking.Blocky;
#endif

namespace Assets.Scripts.Components.Tools
{
    public class ToolBase : MonoBehaviour
    {
        public virtual ToolType ToolType { get; }

        public virtual Dictionary<string, object> Properties { get; set; } = new();

        public virtual void Compile()
        {

        }

        public virtual void Decompile()
        {
            
        }

#if TME_BLOCKY
        public virtual void OnBlocklyExportReceived(CodeExportPayload payload, string targetEvent)
        {
            Debug.LogWarning($"[{gameObject.name}] Received Blockly Export, but this tool doesn't override OnBlocklyExportReceived.");
        }
#endif
    }
}