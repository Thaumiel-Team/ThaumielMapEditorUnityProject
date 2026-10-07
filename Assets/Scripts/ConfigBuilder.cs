using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using YamlDotNet.Serialization;

namespace Assets.Scripts
{
    public class ConfigBuilder
    {
        public static event Action<Config, string> OnConfigLoaded;
        public static event Action<string> OnConfigSaved;

        private static readonly string ConfigFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "config.json");

        public static Config LoadConfig()
        {
            Config config = new();

            if (File.Exists(ConfigFilePath))
            {
                try
                {
                    string yaml = File.ReadAllText(ConfigFilePath);
                    IDeserializer deserializer = new DeserializerBuilder().IgnoreUnmatchedProperties().Build();
                    config = deserializer.Deserialize<Config>(yaml) ?? config;
                    OnConfigLoaded?.Invoke(config, yaml);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ConfigBuilder] Failed to load config, using defaults. Error: {ex.Message}");
                }
            }

            SaveConfig(config);
            return config;
        }

        public static void SaveConfig(Config config)
        {
            try
            {
                config.PluginConfigs ??= new();
                ISerializer serializer = new SerializerBuilder().Build();
                string yaml = serializer.Serialize(config);
                File.WriteAllText(ConfigFilePath, yaml);
                OnConfigSaved?.Invoke(yaml);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigBuilder] Failed to save config. {ex.Message}");
            }
        }

        private static PluginConfig FindPlugin(Config config, string pluginName)
        {
            return config.PluginConfigs?.Find(p => string.Equals(p.PluginName, pluginName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Reads a value from a plugin's custom config section.
        /// </summary>
        /// <returns><paramref name="defaultValue"/> when the plugin or value is missing.</returns>
        public static string GetPluginValue(string pluginName, string name, string defaultValue = "")
        {
            Config config = LoadConfig();
            PluginConfigValue entry = FindPlugin(config, pluginName)?.Config?.Find(v => string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase));
            return entry?.Value ?? defaultValue;
        }

        /// <summary>
        /// Returns a copy of a plugin's custom config values.
        /// </summary>
        /// <returns>Empty when missing.</returns>
        public static List<PluginConfigValue> GetPluginValues(string pluginName)
        {
            Config config = LoadConfig();
            List<PluginConfigValue> values = FindPlugin(config, pluginName)?.Config;
            return values != null ? new List<PluginConfigValue>(values) : new();
        }

        /// <summary>
        /// Creates or updates a value in a plugin's custom config section and saves.
        /// </summary>
        public static void SetPluginValue(string pluginName, string name, string value)
        {
            if (string.IsNullOrWhiteSpace(pluginName))
                throw new ArgumentException("Plugin name must not be empty.", nameof(pluginName));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Value name must not be empty.", nameof(name));

            Config config = LoadConfig();
            config.PluginConfigs ??= new();

            PluginConfig section = FindPlugin(config, pluginName);
            if (section == null)
            {
                section = new PluginConfig { PluginName = pluginName };
                config.PluginConfigs.Add(section);
            }

            section.Config ??= new();
            PluginConfigValue entry = section.Config.Find(v => string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase));
            if (entry == null)
            {
                section.Config.Add(new PluginConfigValue { Name = name, Value = value ?? string.Empty });
            }
            else
                entry.Value = value ?? string.Empty;

            SaveConfig(config);
        }

        /// <summary>
        /// Removes a single value from a plugin's config section.
        /// </summary>
        /// <returns>False when missing.</returns>
        public static bool RemovePluginValue(string pluginName, string name)
        {
            Config config = LoadConfig();
            PluginConfig section = FindPlugin(config, pluginName);
            if (section?.Config == null)
                return false;

            int removed = section.Config.RemoveAll(v => string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase));
            if (removed > 0)
                SaveConfig(config);

            return removed > 0;
        }

        /// <summary>
        /// Removes a plugin's entire config section. 
        /// </summary>
        /// <returns>False when missing.</returns>
        public static bool RemovePlugin(string pluginName)
        {
            Config config = LoadConfig();
            if (config.PluginConfigs == null)
                return false;

            int removed = config.PluginConfigs.RemoveAll(p => string.Equals(p.PluginName, pluginName, StringComparison.OrdinalIgnoreCase));
            if (removed > 0)
                SaveConfig(config);

            return removed > 0;
        }
    }
}