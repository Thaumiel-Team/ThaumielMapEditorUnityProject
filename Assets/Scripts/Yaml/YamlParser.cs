using YamlDotNet.Serialization;
using Assets.Scripts.Yaml.CustomConverters;
using YamlDotNet.Serialization.NamingConventions;

namespace Assets.Scripts.Yaml
{
    public class YamlParser
    {
        public static ISerializer Serializer = new SerializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .WithTypeConverter(new Vector2Converter())
            .WithTypeConverter(new Vector3Converter())
            .WithTypeConverter(new Vector4Converter())
            .WithTypeConverter(new CustomColorConverter())
            .WithTypeConverter(new QuaternionConverter())
            .WithTypeConverter(new FloatTypeConverter())
            .Build();

        public static IDeserializer Deserializer = new DeserializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .WithTypeConverter(new Vector2Converter())
            .WithTypeConverter(new Vector3Converter())
            .WithTypeConverter(new Vector4Converter())
            .WithTypeConverter(new CustomColorConverter())
            .WithTypeConverter(new QuaternionConverter())
            .WithTypeConverter(new IntegerEnumConverter())
            .WithTypeConverter(new FloatTypeConverter())
            .Build();
    }
}