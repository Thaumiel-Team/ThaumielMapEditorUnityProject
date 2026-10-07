using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Assets.Scripts.Networking.Blocky.Definitions
{
    public abstract class DefBase
    {
        public abstract void Register();

        public static (string label, string value)[] EnumOptions<T>() where T : struct, Enum
        {
            if (typeof(T).GetCustomAttribute<FlagsAttribute>() != null)
            {
                return Enum.GetValues(typeof(T))
                    .Cast<T>()
                    .Where(v =>
                    {
                        ulong numeric = Convert.ToUInt64(v);
                        return numeric == 0 || (numeric & (numeric - 1)) == 0;
                    })
                    .OrderBy(v => Convert.ToUInt64(v))
                    .Select(v => (v.ToString(), v.ToString()))
                    .ToArray();
            }

            return Enum.GetNames(typeof(T)).Select(n => (n, n)).ToArray();
        }
    }
}