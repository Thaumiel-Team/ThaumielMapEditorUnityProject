using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;

namespace Assets.Scripts.Tools.Editor
{
    internal static class OptionalPackageDefines
    {
        public const string Blocky = "TME_BLOCKY";
        public const string Collab = "TME_COLLAB";

        private static readonly Dictionary<string, string> DefinesByPackage = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Blocky"] = Blocky,
            ["Collab"] = Collab,
            ["ItemReferences"] = "TME_ITEMREF"
        };

        public static void SetInstalled(string packageName, bool installed)
        {
            if (string.IsNullOrEmpty(packageName) || !DefinesByPackage.TryGetValue(packageName, out string symbol))
                return;

            foreach (NamedBuildTarget target in GetTargets())
                SetSymbol(target, symbol, installed);
        }

        private static IEnumerable<NamedBuildTarget> GetTargets()
        {
            HashSet<BuildTargetGroup> groups = new()
            {
                BuildTargetGroup.Standalone,
                BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget),
            };

            foreach (BuildTargetGroup group in groups)
            {
                if (group == BuildTargetGroup.Unknown)
                    continue;

                NamedBuildTarget target;
                try
                {
                    target = NamedBuildTarget.FromBuildTargetGroup(group);
                }
                catch (Exception)
                {
                    continue;
                }

                yield return target;
            }
        }

        private static void SetSymbol(NamedBuildTarget target, string symbol, bool enabled)
        {
            string current = PlayerSettings.GetScriptingDefineSymbols(target);
            List<string> defines = current
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(define => define.Trim())
                .Where(define => define.Length > 0)
                .ToList();

            bool present = defines.Any(define => string.Equals(define, symbol, StringComparison.Ordinal));
            if (present == enabled)
                return;

            if (enabled)
            {
                defines.Add(symbol);
            }
            else
                defines.RemoveAll(define => string.Equals(define, symbol, StringComparison.Ordinal));

            PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", defines));
            AssetDatabase.SaveAssets();
        }
    }
}