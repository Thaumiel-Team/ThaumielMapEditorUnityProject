using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

namespace Assets.Scripts.Networking.Blocky.Definitions
{
    public class EnumDefs : DefBase
    {
        private static List<Type> EnumTypes { get; set; } = new();

        public override void Register()
        {
            BlocklyServer.RegisterCategory("Enums", "#29d4ff", "");

            RegisterEnums();
        }

        private Type[] GetAllEnumTypes()
        {
            if (EnumTypes.Count > 0)
                return EnumTypes.ToArray();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.FullName.StartsWith("System") || assembly.FullName.StartsWith("Unity"))
                    continue;

                try
                {
                    foreach (Type type in assembly.GetTypes())
                    {
                        if (type.IsEnum && type.Namespace != null && type.Namespace.StartsWith("Assets.Scripts.Enums"))
                        {
                            EnumTypes.Add(type);
                        }
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    foreach (Type type in ex.Types.Where(t => t != null))
                    {
                        if (type.IsEnum && type.Namespace?.StartsWith("Assets.Scripts.Enums") == true)
                        {
                            EnumTypes.Add(type);
                        }
                    }
                }
            }

            return EnumTypes.ToArray();
        }

        private void RegisterEnums()
        {
            try
            {
                foreach (Type type in GetAllEnumTypes())
                {
                    MethodInfo enumOptionsMethod = typeof(DefBase).GetMethod(nameof(EnumOptions)).MakeGenericMethod(type);
                    (string label, string value)[] options = ((string label, string value)[])enumOptionsMethod.Invoke(null, null);

                    string humanName = type.HumanName();
                    string idBase = $"enum_{humanName.Replace(' ', '_').ToLower()}";
                    bool isFlag = type.GetCustomAttribute<FlagsAttribute>() != null;
                    string finalId = isFlag ? $"{idBase}_flag" : idBase;

                    if (isFlag)
                    {
                        BlocklyServer.RegisterBlock(new BlockDefinition
                        {
                            Id = $"{finalId}_combine",
                            Category = "Enums",
                            Color = "#1e9cc2",
                            Tooltip = "Combine two flags. You can plug this into itself to add more!",
                            Explanation = "Combines two flags of " + humanName + " into one value. Plug the result into itself to combine more than two flags.",
                            Message = $"combine {humanName}: %1 + %2",
                            Connections = BlockConnectionType.Output,
                            Args = new List<Dictionary<string, object>>
                            {
                                BlockArg.Value("A"), 
                                BlockArg.Value("B")
                            },
                        });
                    }

                    BlocklyServer.RegisterBlock(new BlockDefinition
                    {
                        Id = finalId,
                        Category = "Enums",
                        Color = "#29d4ff",
                        Tooltip = "Enum value.",
                        Explanation = "Picks one " + humanName + " value from the dropdown.",
                        Message = $"{type.HumanName()}: %1",
                        Connections = BlockConnectionType.Output,
                        Args = new List<Dictionary<string, object>>
                        {
                            BlockArg.Dropdown("Value", options)
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EnumDefs]: Exception durring enum registration {ex}");
            }
        }
    }
}