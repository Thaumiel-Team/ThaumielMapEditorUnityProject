#if UNITY_EDITOR
using System;
using System.Globalization;
using UnityEngine;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Assets.Scripts.Yaml.CustomConverters
{
    public class CustomColorConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type) => type == typeof(Color);

        public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            parser.Consume<MappingStart>();

            float r = ReadFloat(parser, "r");
            float g = ReadFloat(parser, "g");
            float b = ReadFloat(parser, "b");
            float a = ReadFloat(parser, "a");

            parser.Consume<MappingEnd>();

            return new Color(r, g, b, a);
        }

        public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer)
        {
            Color color = (Color)value;

            emitter.Emit(new MappingStart());
            WriteFloat(emitter, "r", color.r);
            WriteFloat(emitter, "g", color.g);
            WriteFloat(emitter, "b", color.b);
            WriteFloat(emitter, "a", color.a);
            emitter.Emit(new MappingEnd());
        }

        private float ReadFloat(IParser parser, string key)
        {
            parser.Consume<Scalar>();
            Scalar scalar = parser.Consume<Scalar>();
            return float.Parse(scalar.Value, CultureInfo.InvariantCulture);
        }

        private void WriteFloat(IEmitter emitter, string key, float value)
        {
            emitter.Emit(new Scalar(key));
            emitter.Emit(new Scalar(value.ToString("0.####################", CultureInfo.InvariantCulture)));
        }
    }
}
#endif