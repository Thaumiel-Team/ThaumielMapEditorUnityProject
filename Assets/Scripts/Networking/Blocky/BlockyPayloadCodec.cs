using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Assets.Scripts.Networking.Blocky
{
    public static class BlockyPayloadCodec
    {
        public static CodeExportPayload Normalize(CodeExportPayload payload)
        {
            if (payload == null)
                return null;

            payload.Blocks = NormalizeBlocks(payload.Blocks);
            return payload;
        }

        public static List<object> NormalizeBlocks(List<object> blocks)
        {
            if (blocks == null)
                return null;

            List<object> clean = new(blocks.Count);
            foreach (object raw in blocks)
            {
                object norm = NormalizeValue(raw);
                if (norm != null)
                    clean.Add(norm);
            }

            return clean;
        }

        public static object NormalizeValue(object value)
        {
            if (value == null)
                return null;

            if (value is JToken token)
                return NormalizeToken(token);

            if (value is string || value is bool || value is char || value is Guid || value is DateTime || value is DateTimeOffset)
                return value;

            Type t = value.GetType();
            if (t.IsPrimitive || t.IsEnum || value is decimal)
                return value;

            if (value is IDictionary dict)
            {
                Dictionary<string, object> clean = new();
                foreach (DictionaryEntry entry in dict)
                {
                    string key = entry.Key?.ToString() ?? string.Empty;
                    if (string.IsNullOrEmpty(key))
                        continue;

                    clean[key] = NormalizeValue(entry.Value);
                }

                return clean;
            }

            if (value is IEnumerable enumerable)
            {
                List<object> clean = new();
                foreach (object item in enumerable)
                    clean.Add(NormalizeValue(item));

                return clean;
            }

            return value?.ToString();
        }

        private static object NormalizeToken(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.None)
                return null;

            switch (token.Type)
            {
                case JTokenType.Object:
                {
                    Dictionary<string, object> clean = new();
                    foreach (JProperty prop in ((JObject)token).Properties())
                        clean[prop.Name] = NormalizeToken(prop.Value);

                    return clean;
                }

                case JTokenType.Array:
                {
                    List<object> clean = new();
                    foreach (JToken item in (JArray)token)
                        clean.Add(NormalizeToken(item));

                    return clean;
                }

                case JTokenType.Integer:
                    return token.Value<long>();

                case JTokenType.Float:
                    return token.Value<double>();

                case JTokenType.Boolean:
                    return token.Value<bool>();

                case JTokenType.String:
                    return token.Value<string>();

                case JTokenType.Date:
                    return token.Value<DateTime>().ToString("o");

                case JTokenType.Guid:
                    return token.Value<Guid>().ToString();

                default:
                    return token.ToString();
            }
        }

        public static bool SameContent(CodeExportPayload a, CodeExportPayload b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if (a == null || b == null)
                return false;

            return string.Equals(a.Xml ?? string.Empty, b.Xml ?? string.Empty, StringComparison.Ordinal) && string.Equals(a.Code ?? string.Empty, b.Code ?? string.Empty, StringComparison.Ordinal);
        }

        public static bool ContainsContent(IEnumerable<CodeExportPayload> list, CodeExportPayload payload)
        {
            if (list == null || payload == null)
                return false;

            foreach (CodeExportPayload entry in list)
            {
                if (entry != null && SameContent(entry, payload))
                    return true;
            }

            return false;
        }
    }
}
