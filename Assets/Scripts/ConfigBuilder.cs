using System;
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
    }
}