#if UNITY_EDITOR
using System.Collections.Generic;

namespace Assets.Scripts.Yaml
{
    public class YamlTool
    {
        public string ToolName { get; set; } = string.Empty;

        public Dictionary<string, object> Properties { get; set; } = new();
    }
}
#endif