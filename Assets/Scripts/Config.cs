using System;
using System.Collections.Generic;
using System.IO;

namespace Assets.Scripts
{
    /// <summary>
    /// A single Name/Value entry in a plugin's custom config section.
    /// </summary>
    public class PluginConfigValue
    {
        public string Name { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// Custom config section owned by a plugin. Serialized as:
    /// <code>
    /// PluginName: NAME
    /// Config:
    /// - Name: NAME
    ///   Value: VALUE
    /// </code>
    /// </summary>
    public class PluginConfig
    {
        public string PluginName { get; set; } = string.Empty;

        public List<PluginConfigValue> Config { get; set; } = new();
    }

    public class Config
    {
        public bool OpenExportAfterCompiling { get; set; }

        public string ExportPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ThaumielMapEditor");
        
        public bool CompressExport { get; set; }

        /// <summary>
        /// Custom config sections owned by plugins.
        /// </summary>
        public List<PluginConfig> PluginConfigs { get; set; } = new();
    }
}