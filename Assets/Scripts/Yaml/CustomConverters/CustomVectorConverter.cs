#if UNITY_EDITOR
using System;
using System.Globalization;
using UnityEngine;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Assets.Scripts.Yaml.CustomConverters
{
    public class Vector2Converter : IYamlTypeConverter
    {
        public bool Accepts(Type type) => type == typeof(Vector2);

        public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            parser.Consume<MappingStart>();

            float x = ReadFloat(parser, "x");
            float y = ReadFloat(parser, "y");

            parser.Consume<MappingEnd>();

            return new Vector2(x, y);
        }

        public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer)
        {
            Vector2 vector = (Vector2)value;

            emitter.Emit(new MappingStart());
            WriteFloat(emitter, "x", vector.x);
            WriteFloat(emitter, "y", vector.y);
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

    public class Vector3Converter : IYamlTypeConverter
    {
        public bool Accepts(Type type) => type == typeof(Vector3);

        public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            parser.Consume<MappingStart>();

            float x = ReadFloat(parser, "x");
            float y = ReadFloat(parser, "y");
            float z = ReadFloat(parser, "z");

            parser.Consume<MappingEnd>();

            return new Vector3(x, y, z);
        }

        public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer)
        {
            Vector3 vector = (Vector3)value;

            emitter.Emit(new MappingStart());
            WriteFloat(emitter, "x", vector.x);
            WriteFloat(emitter, "y", vector.y);
            WriteFloat(emitter, "z", vector.z);
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

    public class Vector4Converter : IYamlTypeConverter
    {
        public bool Accepts(Type type) => type == typeof(Vector4);

        public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            parser.Consume<MappingStart>();

            float x = ReadFloat(parser, "x");
            float y = ReadFloat(parser, "y");
            float z = ReadFloat(parser, "z");
            float w = ReadFloat(parser, "w");

            parser.Consume<MappingEnd>();

            return new Vector4(x, y, z, w);
        }

        public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer)
        {
            Vector4 vector = (Vector4)value;

            emitter.Emit(new MappingStart());
            WriteFloat(emitter, "x", vector.x);
            WriteFloat(emitter, "y", vector.y);
            WriteFloat(emitter, "z", vector.z);
            WriteFloat(emitter, "w", vector.w);
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