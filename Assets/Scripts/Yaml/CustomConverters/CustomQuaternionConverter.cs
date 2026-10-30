#if UNITY_EDITOR
using System;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using UnityEngine;
using System.Globalization;

namespace Assets.Scripts.Yaml.CustomConverters
{
    public class QuaternionConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type) => type == typeof(Quaternion);

        public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            parser.Consume<MappingStart>();

            float x = ReadFloat(parser, "x");
            float y = ReadFloat(parser, "y");
            float z = ReadFloat(parser, "z");
            float w = ReadFloat(parser, "w");

            parser.Consume<MappingEnd>();

            return new Quaternion(x, y, z, w);
        }
        
        public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer)
        {
            Quaternion quaternion = (Quaternion)value;

            emitter.Emit(new MappingStart());
            WriteFloat(emitter, "x", quaternion.x);
            WriteFloat(emitter, "y", quaternion.y);
            WriteFloat(emitter, "z", quaternion.z);
            WriteFloat(emitter, "w", quaternion.w);
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