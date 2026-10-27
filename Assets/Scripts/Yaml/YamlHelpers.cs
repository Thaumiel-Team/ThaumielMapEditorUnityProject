using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Enums;
using Assets.Scripts.Lockers;
using UnityEngine;

namespace Assets.Scripts.Yaml
{
    public class YamlHelpers
    {
        public static T ParseObject<T>(object value) where T : new()
        {
            if (value is T typed)
                return typed;

            if (value is not Dictionary<string, object> dict)
            {
                Debug.LogWarning($"[YamlHelpers] ParseObject<{typeof(T).Name}>: expected Dictionary<string, object>, got {value?.GetType().Name ?? "null"}");
                return new T();
            }

            T instance = new();
            PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo property in properties)
            {
                if (!property.CanWrite)
                    continue;

                if (!dict.TryGetValue(property.Name, out object rawValue))
                    continue;

                try
                {
                    object parsed = ParseValueForType(property.PropertyType, rawValue);
                    property.SetValue(instance, parsed);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[YamlHelpers] Failed to parse property '{property.Name}' on {typeof(T).Name}: {ex.Message}");
                }
            }

            Debug.Log($"[YamlHelpers] ParseObject<{typeof(T).Name}> completed.");
            return instance;
        }

        private static object ParseValueForType(Type targetType, object rawValue)
        {
            if (targetType == typeof(Vector2))
                return ParseVector2(rawValue);

            if (targetType == typeof(Vector3))
                return ParseVector3(rawValue);

            if (targetType == typeof(Color))
                return ParseColor(rawValue);

            if (targetType.IsEnum)
                return ParseEnumByType(targetType, rawValue);

            if (IsGenericList(targetType))
                return ParseListByElementType(targetType, rawValue);

            if (targetType == typeof(string))
                return rawValue?.ToString();

            if (targetType.IsValueType || targetType.IsPrimitive)
                return Convert.ChangeType(rawValue, targetType);

            if (targetType.IsClass)
                return ParseNestedObject(targetType, rawValue);

            return rawValue;
        }

        private static bool IsGenericList(Type type)
            => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
            
        private static object ParseListByElementType(Type listType, object rawValue)
        {
            if (rawValue is not List<object> rawList)
                return Activator.CreateInstance(listType);

            Type elementType = listType.GetGenericArguments()[0];
            IList resultList = (IList)Activator.CreateInstance(listType);

            foreach (object item in rawList)
            {
                object parsed = ParseValueForType(elementType, item);
                resultList.Add(parsed);
            }

            return resultList;
        }

        private static object ParseEnumByType(Type enumType, object rawValue)
        {
            if (rawValue == null)
                return Enum.ToObject(enumType, 0);

            if (Enum.TryParse(enumType, rawValue.ToString(), ignoreCase: true, out object result))
                return result;

            Debug.LogWarning($"[YamlHelpers] Could not parse '{rawValue}' as enum {enumType.Name}. Using default.");
            return Enum.ToObject(enumType, 0);
        }

        private static object ParseNestedObject(Type targetType, object rawValue)
        {
            MethodInfo genericMethod = typeof(YamlHelpers).GetMethod(nameof(ParseObject), BindingFlags.Public | BindingFlags.Static)?.MakeGenericMethod(targetType);

            if (genericMethod == null)
            {
                Debug.LogWarning($"[YamlHelpers] Could not find ParseObject method for type {targetType.Name}.");
                return Activator.CreateInstance(targetType);
            }

            return genericMethod.Invoke(null, new object[] { rawValue });
        }

        public static Vector2 ParseVector2(object value)
        {
            if (value is Vector2 v)
                return v;

            if (value is Dictionary<string, object> dict)
            {
                float x = dict.TryGetValue("x", out object xVal) ? Convert.ToSingle(xVal) : 0f;
                float y = dict.TryGetValue("y", out object yVal) ? Convert.ToSingle(yVal) : 0f;
                return new Vector2(x, y);
            }

            return default;
        }

        public static Vector3 ParseVector3(object value)
        {
            if (value is Vector3 v)
                return v;

            if (value is Dictionary<string, object> dict)
            {
                float x = dict.TryGetValue("x", out object xVal) ? Convert.ToSingle(xVal) : 0f;
                float y = dict.TryGetValue("y", out object yVal) ? Convert.ToSingle(yVal) : 0f;
                float z = dict.TryGetValue("z", out object zVal) ? Convert.ToSingle(zVal) : 0f;
                return new Vector3(x, y, z);
            }

            return default;
        }

        public static Color ParseColor(object value)
        {
            if (value is Color c)
                return c;

            if (value is Dictionary<object, object> dict)
            {
                float r = dict.TryGetValue("r", out object rVal) ? Convert.ToSingle(rVal) : 0f;
                float g = dict.TryGetValue("g", out object gVal) ? Convert.ToSingle(gVal) : 0f;
                float b = dict.TryGetValue("b", out object bVal) ? Convert.ToSingle(bVal) : 1f;
                float a = dict.TryGetValue("a", out object aVal) ? Convert.ToSingle(aVal) : 1f;

                return new Color(r, g, b, a);
            }

            return Color.white;
        }

        public static LockerChamber ParseChamber(object value)
        {
            if (value is LockerChamber chamber)
                return chamber;

            if (value is not Dictionary<string, object> dict)
                return new LockerChamber();

            return new LockerChamber()
            {
                Name = dict.TryGetValue("Name", out object name) ? Convert.ToString(name) : string.Empty,
                Index = (uint)(dict.TryGetValue("Index", out object index) ? Convert.ToInt32(index) : default),
                Permissions = dict.TryGetValue("Permissions", out object permissions) ? (DoorPermissionFlags)Enum.Parse(typeof(DoorPermissionFlags), permissions.ToString()) : default,
                Data = dict.TryGetValue("Data", out object data) && data is List<object> dataList ? dataList.Select(d => ParseLockerData(d)).ToList() : new List<ChamberData>()
            };
        }

        public static ChamberData ParseLockerData(object value)
        {
            if (value is ChamberData chamberData)
                return chamberData;

            if (value is not Dictionary<string, object> dict)
                return new ChamberData();

            return new ChamberData()
            {
                ItemType = dict.TryGetValue("ItemType", out object itemType) ? (ItemType)Enum.Parse(typeof(ItemType), itemType.ToString()) : default,
                SpawnPercent = dict.TryGetValue("SpawnPercent", out object spawnPercent) ? Convert.ToSingle(spawnPercent) : default,
                AmountToSpawn = dict.TryGetValue("AmountToSpawn", out object amountToSpawn) ? Convert.ToInt32(amountToSpawn) : default
            };
        }

        public static List<T> ParseList<T>(object value, Func<object, T> parser)
        {
            List<T> result = new();
            if (value is null || value is string)
                return result;

            if (value is IEnumerable enumerable)
            {
                foreach (object item in enumerable)
                    result.Add(parser(item));
            }

            return result;
        }

        public static List<T> ParseList<T>(object value)
        {
            List<T> result = new();
            if (value is null || value is string)
                return result;

            if (value is IEnumerable enumerable)
            {
                foreach (object item in enumerable)
                {
                    if (item is T typed)
                    {
                        result.Add(typed);
                    }
                    else if (item is not null)
                    {
                        if (typeof(T) == typeof(string))
                        {
                            result.Add((T)(object)Convert.ToString(item));
                        }
                        else if (typeof(T).IsEnum)
                        {
                            result.Add((T)Enum.Parse(typeof(T), Convert.ToString(item), true));
                        }
                        else
                            result.Add((T)Convert.ChangeType(item, typeof(T)));
                    }
                }
            }

            return result;
        }

        public static T ParseEnum<T>(object value) where T : struct, Enum
        {
            if (value is T direct)
                return direct;

            if (Enum.TryParse(value.ToString(), ignoreCase: true, out T result))
                return result;

            return default;
        }

        public static List<T> ParseEnumList<T>(object value) where T : struct, Enum
            => ParseList(value, ParseEnum<T>);
    }
}