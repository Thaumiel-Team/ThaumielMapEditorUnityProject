using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Assets.Scripts.Extensions
{
    public static class ConvertExtensions
    {
        private static bool TryParseFloat(object value, out float result)
        {
            result = 0f;
            switch (value)
            {
                case null:
                    return false;

                case float f:
                    result = f;
                    return true;

                case double d:
                    result = (float)d;
                    return true;

                case long l:
                    result = l;
                    return true;

                case int i:
                    result = i;
                    return true;

                case string s when !string.IsNullOrWhiteSpace(s):
                    s = s.Trim();
                    if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
                        return true;

                    if (s.Contains(','))
                    {
                        string normalized = s.Replace(',', '.');
                        if (float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
                            return true;
                    }

                    if (float.TryParse(s, NumberStyles.Float, CultureInfo.CurrentCulture, out result))
                        return true;

                    return float.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result) || float.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out result);
                    
                default:
                    if (value is IConvertible)
                    {
                        try
                        {
                            result = Convert.ToSingle(value, CultureInfo.InvariantCulture);
                            return true;
                        }
                        catch
                        {
                            try
                            {
                                result = Convert.ToSingle(value, CultureInfo.CurrentCulture);
                                return true;
                            }
                            catch
                            {
                                return false;
                            }
                        }
                    }
                    return false;
            }
        }

        public static Vector3? ToVector3(object obj)
        {
            if (obj is Dictionary<string, object> dict)
            {
                if (dict.TryGetValue("x", out var x) && dict.TryGetValue("y", out var y) && dict.TryGetValue("z", out var z))
                {
                    if (TryParseFloat(x, out float fx) && TryParseFloat(y, out float fy) && TryParseFloat(z, out float fz))
                        return new Vector3(fx, fy, fz);
                }
            }
            else if (obj is List<object> list && list.Count >= 3)
            {
                if (TryParseFloat(list[0], out float fx) && TryParseFloat(list[1], out float fy) && TryParseFloat(list[2], out float fz))
                    return new Vector3(fx, fy, fz);
            }

            Debug.LogWarning($"Could not convert '{obj}' to Vector3.");
            return null;
        }

        public static Vector2? ToVector2(object obj)
        {
            if (obj is Dictionary<string, object> dict)
            {
                if (dict.TryGetValue("x", out var x) && dict.TryGetValue("y", out var y))
                {
                    if (TryParseFloat(x, out float fx) && TryParseFloat(y, out float fy))
                        return new Vector2(fx, fy);
                }
            }
            else if (obj is List<object> list && list.Count >= 2)
            {
                if (TryParseFloat(list[0], out float fx) && TryParseFloat(list[1], out float fy))
                    return new Vector2(fx, fy);
            }

            Debug.LogWarning($"Could not convert '{obj}' to Vector2.");
            return null;
        }
    }
}