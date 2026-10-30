#if UNITY_EDITOR
using System;
using UnityEngine;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Assets.Scripts.Yaml.CustomConverters
{
    public class IntegerEnumConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type) => type.IsEnum;

        public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            Scalar scalar = parser.Consume<Scalar>();
            string value = scalar.Value;

            if (int.TryParse(value, out int intValue))
                return Enum.ToObject(type, intValue);

            if (value.Contains(","))
            {
                int result = 0;
                foreach (string part in value.Split(','))
                {
                    string trimmed = part.Trim();
                    if (Enum.TryParse(type, trimmed, ignoreCase: true, out object parsed))
                    {
                        result |= (int)parsed;                        
                    }
                    else
                        Debug.LogWarning($"Could not parse '{trimmed}' as {type.Name}, skipping.");
                }
                return Enum.ToObject(type, result);
            }

            if (Enum.TryParse(type, value, ignoreCase: true, out object enumValue))
                return enumValue;

            Debug.LogWarning($"Could not parse '{value}' as {type.Name}, returning default.");
            return Enum.ToObject(type, 0);
        }

        public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer)
        {
            emitter.Emit(new Scalar(value.ToString()));
        }
    }
}
#endif