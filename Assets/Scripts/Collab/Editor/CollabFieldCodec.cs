using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Assets.Scripts.Collab.Editor
{
    public static class CollabFieldCodec
    {
        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            Culture = CultureInfo.InvariantCulture,
            NullValueHandling = NullValueHandling.Include,
            MissingMemberHandling = MissingMemberHandling.Ignore,
        };

        public static List<SyncField> Extract(Component comp)
        {
            List<SyncField> outList = new();
            if (comp == null)
                return outList;

            Type t = comp.GetType();
            FieldInfo[] fields = t.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (FieldInfo f in fields)
            {
                if (f.IsNotSerialized)
                    continue;

                try
                {
                    object val = f.GetValue(comp);
                    string json = EncodeValue(val, f.FieldType);
                    outList.Add(new SyncField { key = f.Name, json = json });
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Collab] Extract field {t.Name}.{f.Name} failed: {ex.Message}");
                }
            }
            return outList;
        }

        public static void Apply(Component comp, List<SyncField> fields)
        {
            if (comp == null || fields == null)
                return;

            Type t = comp.GetType();
            foreach (SyncField sf in fields)
            {
                FieldInfo f = t.GetField(sf.key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly) ?? t.GetField(sf.key, BindingFlags.Public | BindingFlags.Instance);
                if (f == null)
                    continue;

                try
                {
                    object val = DecodeValue(sf.json, f.FieldType);
                    f.SetValue(comp, val);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Collab] Apply field {t.Name}.{sf.key} failed: {ex.Message}");
                }
            }
        }

        public static string FieldsHash(List<SyncField> fields)
        {
            if (fields == null || fields.Count == 0)
                return "0";

            fields.Sort((a, b) => string.CompareOrdinal(a.key, b.key));
            int h = 17;
            foreach (SyncField f in fields)
            {
                h = h * 31 + (f.key?.GetHashCode() ?? 0);
                h = h * 31 + (f.json?.GetHashCode() ?? 0);
            }

            return h.ToString();
        }

        private static string EncodeValue(object val, Type fieldType)
        {
            if (val == null)
                return "null";

            if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
            {
                UnityEngine.Object uo = val as UnityEngine.Object;
                if (uo == null)
                    return JsonConvert.SerializeObject(string.Empty, JsonSettings);

                GameObject go = uo is GameObject g ? g : (uo is Component c ? c.gameObject : null);
                string guid = go != null ? CollabId.Get(go) : string.Empty;
                return JsonConvert.SerializeObject("guid:" + guid, JsonSettings);
            }

            if (IsUnityObjectList(fieldType, out _))
            {
                List<string> guids = new();
                if (val is IEnumerable en)
                {
                    foreach (object item in en)
                    {
                        UnityEngine.Object uo = item as UnityEngine.Object;
                        if (uo == null)
                            continue;

                        GameObject go = uo is GameObject g ? g : (uo is Component c ? c.gameObject : null);
                        if (go != null)
                            guids.Add("guid:" + CollabId.Get(go));
                    }
                }
                return JsonConvert.SerializeObject(guids, JsonSettings);
            }

            if (TryEncodeUnityStruct(val, out string structJson) && structJson != null)
                return structJson;

            try
            {
                return JsonConvert.SerializeObject(val, JsonSettings);
            }
            catch
            {
                return "null";
            }
        }

        private static object DecodeValue(string json, Type fieldType)
        {
            if (string.IsNullOrEmpty(json) || json == "null")
            {
                if (fieldType.IsValueType && Nullable.GetUnderlyingType(fieldType) == null)
                    return Activator.CreateInstance(fieldType);

                return null;
            }
            if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
            {
                string s = JsonConvert.DeserializeObject<string>(json, JsonSettings) ?? string.Empty;
                string guid = s.StartsWith("guid:") ? s.Substring(5) : s;
                if (string.IsNullOrEmpty(guid))
                    return null;

                GameObject go = CollabId.Find(guid);
                if (go == null)
                    return null;

                if (fieldType == typeof(GameObject))
                    return go;
                    
                return go.GetComponent(fieldType);
            }

            if (IsUnityObjectList(fieldType, out Type elemType))
            {
                List<string> raw = JsonConvert.DeserializeObject<List<string>>(json, JsonSettings) ?? new List<string>();
                IList list = (IList)Activator.CreateInstance(fieldType);
                foreach (string s in raw)
                {
                    string guid = s.StartsWith("guid:") ? s.Substring(5) : s;
                    if (string.IsNullOrEmpty(guid))
                        continue;

                    GameObject go = CollabId.Find(guid);
                    if (go == null)
                        continue;

                    object resolved = elemType == typeof(GameObject) ? go : go.GetComponent(elemType);
                    if (resolved != null)
                        list.Add(resolved);
                }
                return list;
            }

            if (TryDecodeUnityStruct(json, fieldType, out object structVal))
                return structVal;

            try
            {
                return JsonConvert.DeserializeObject(json, fieldType, JsonSettings);
            }
            catch
            {
                if (fieldType.IsValueType && Nullable.GetUnderlyingType(fieldType) == null)
                    return Activator.CreateInstance(fieldType);

                return null;
            }
        }

        private static bool IsUnityObjectList(Type t, out Type elem)
        {
            elem = null;
            if (!t.IsGenericType)
                return false;

            if (t.GetGenericTypeDefinition() != typeof(List<>))
                return false;

            elem = t.GetGenericArguments()[0];
            return typeof(UnityEngine.Object).IsAssignableFrom(elem);
        }
        
        private static bool TryEncodeUnityStruct(object val, out string json)
        {
            json = null;
            try
            {
                switch (val)
                {
                    case Color c:
                    {
                        JObject o = new()
                        {
                            ["r"] = c.r,
                            ["g"] = c.g,
                            ["b"] = c.b,
                            ["a"] = c.a
                        };

                        json = o.ToString(Formatting.None);
                        return true;
                    }

                    case Quaternion q:
                    {
                        JObject o = new()
                        {
                            ["x"] = q.x,
                            ["y"] = q.y,
                            ["z"] = q.z,
                            ["w"] = q.w
                        };

                        json = o.ToString(Formatting.None);
                        return true;
                    }

                    case Vector4 v4:
                    {
                        JObject o = new()
                        {
                            ["x"] = v4.x,
                            ["y"] = v4.y,
                            ["z"] = v4.z,
                            ["w"] = v4.w
                        };

                        json = o.ToString(Formatting.None);
                        return true;
                    }

                    case Vector3 v3:
                    {
                        JObject o = new()
                        {
                            ["x"] = v3.x,
                            ["y"] = v3.y,
                            ["z"] = v3.z
                        };

                        json = o.ToString(Formatting.None);
                        return true;
                    }

                    case Vector2 v2:
                    {
                        JObject o = new()
                        {
                            ["x"] = v2.x,
                            ["y"] = v2.y
                        };

                        json = o.ToString(Formatting.None);
                        return true;
                    }
                }
            }
            catch
            {
                json = null;
                return false;
            }

            return false;
        }

        private static bool TryDecodeUnityStruct(string json, Type fieldType, out object val)
        {
            val = null;
            try
            {
                if (fieldType == typeof(Color))
                {
                    JObject o = JObject.Parse(json);
                    val = new Color(StructFloat(o, "r"), StructFloat(o, "g"), StructFloat(o, "b"), StructFloat(o, "a", 1f));
                    return true;
                }

                if (fieldType == typeof(Quaternion))
                {
                    JObject o = JObject.Parse(json);
                    val = new Quaternion(StructFloat(o, "x"), StructFloat(o, "y"), StructFloat(o, "z"), StructFloat(o, "w"));
                    return true;
                }

                if (fieldType == typeof(Vector4))
                {
                    JObject o = JObject.Parse(json);
                    val = new Vector4(StructFloat(o, "x"), StructFloat(o, "y"), StructFloat(o, "z"), StructFloat(o, "w"));
                    return true;
                }

                if (fieldType == typeof(Vector3))
                {
                    JObject o = JObject.Parse(json);
                    val = new Vector3(StructFloat(o, "x"), StructFloat(o, "y"), StructFloat(o, "z"));
                    return true;
                }

                if (fieldType == typeof(Vector2))
                {
                    JObject o = JObject.Parse(json);
                    val = new Vector2(StructFloat(o, "x"), StructFloat(o, "y"));
                    return true;
                }
            }
            catch
            {
                val = null;
                return false;
            }

            return false;
        }

        private static float StructFloat(JObject o, string key, float fallback = 0f)
        {
            if (o == null)
                return fallback;

            JToken token = o[key];
            if (token == null)
                return fallback;

            try
            {
                return token.Value<float>();
            }
            catch
            {
                return fallback;
            }
        }
    }
}
