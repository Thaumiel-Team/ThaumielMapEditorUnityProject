#if UNITY_EDITOR
using Assets.Scripts.Enums;
using Assets.Scripts.Yaml;
using UnityEditor;
using UnityEngine;

#if TME_BLOCKY
using Assets.Scripts.Networking.Blocky;
#endif

namespace Assets.Scripts.Components.Tools
{
    public class BlockyRuntime : ToolBase
    {
#if TME_BLOCKY
        public CodeExportPayload Blocky;

        public override ToolType ToolType => ToolType.BlockyRuntime;

        public override void Compile()
        {
            Properties = new()
            {
                ["Blocky"] = Blocky
            };
        }

        public override void Decompile()
        {
            if (Properties.TryGetValue("Blocky", out var block))
                Blocky = YamlHelpers.ParseObject<CodeExportPayload>(block);
        }

        public override void OnBlocklyExportReceived(CodeExportPayload payload, string targetEvent)
        {
            if (targetEvent == nameof(Blocky))
            {
                Blocky = payload;
                Debug.Log($"[BlockyRuntime] Successfully added export to {gameObject.name}'s blocky property.");
            }

            EditorUtility.SetDirty(this);
        }
#endif
    }
}
#endif