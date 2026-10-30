#if UNITY_EDITOR
using System;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using UnityEngine;

namespace Assets.Scripts.Yaml.CustomConverters
{
    public class CustomColor32Converter : IYamlTypeConverter
    {
        public bool Accepts(Type type) => type == typeof(Color32);

        public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            string value = parser.Consume<Scalar>().Value;
            if (!ColorUtility.TryParseHtmlString(value, out var color))
                throw new ArgumentException("Unable to parse Color32 value of " + value);
                
            return (Color32)color;
        }

        public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer) =>
            emitter.Emit(new Scalar(ToHex((Color)(Color32)value) ?? ToHex(Color.white)));

        public static string ToHex(Color color, bool includeAlpha = false)
        {
            Color32 c = color;
            return includeAlpha ? $"#{c.r:X2}{c.g:X2}{c.b:X2}{c.a:X2}" : $"#{c.r:X2}{c.g:X2}{c.b:X2}";
        }
    }
}
#endif