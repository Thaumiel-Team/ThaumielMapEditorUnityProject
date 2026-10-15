using Assets.Scripts.Components.Tools.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Assets.Scripts.Networking.Blocky
{
    public static class BlockNodeHelper
    {
        public static Dictionary<object, object> CoerceNode(object raw)
        {
            if (raw == null)
                return null;

            if (raw is Dictionary<object, object> direct)
                return direct;

            object norm = BlockyPayloadCodec.NormalizeValue(raw);
            if (norm is Dictionary<string, object> strDict)
            {
                Dictionary<object, object> coerced = new();
                foreach (KeyValuePair<string, object> kv in strDict)
                    coerced[kv.Key] = kv.Value;

                return coerced;
            }

            return null;
        }

        /// <summary>
        /// Get the "type" field of a block node.
        /// </summary>
        public static string GetType(Dictionary<object, object> node) => GetString(node, "type");

        /// <summary>
        /// Read a string field from a block node.
        /// </summary>
        public static string GetString(Dictionary<object, object> node, string key)
        {
            if (node == null || !node.TryGetValue(key, out object value))
                return null;

            return value?.ToString();
        }

        /// <summary>
        /// Read an int field from a block node.
        /// </summary>
        public static int GetInt(Dictionary<object, object> node, string key, int fallback = 0) => int.TryParse(GetString(node, key), out int v) ? v : fallback;

        /// <summary>
        /// Read a float field from a block node.
        /// </summary>
        public static float GetFloat(Dictionary<object, object> node, string key, float fallback = 0f) => float.TryParse(GetString(node, key), NumberStyles.Any, CultureInfo.InvariantCulture, out float v) ? v : fallback;

        /// <summary>
        /// Read a bool field from a block node.
        /// </summary>
        public static bool GetBool(Dictionary<object, object> node, string key, bool fallback = false)
        {
            if (node == null || !node.TryGetValue(key, out object value))
                return fallback;

            if (value is bool b)
                return b;

            return bool.TryParse(value?.ToString(), out bool v) ? v : fallback;
        }

        /// <summary>
        /// Read an enum field from a block node.
        /// </summary>
        public static T GetEnum<T>(Dictionary<object, object> node, string key, T fallback = default) where T : struct, Enum
        {
            string raw = GetString(node, key);
            if (raw == null)
                return fallback;

            return Enum.TryParse(raw, ignoreCase: true, out T v) ? v : fallback;
        }


        /// <summary>
        /// Get a nested value-input block (e.g. the expression connected to "M" in a Logger.Info block). Returns null if not present.
        /// </summary>
        public static Dictionary<object, object> GetChild(Dictionary<object, object> node, string inputName)
        {
            if (node == null || !node.TryGetValue(inputName, out object value))
                return null;

            return CoerceNode(value);
        }

        /// <summary>
        /// Get the statement body (e.g. "DO" or "STACK") as an ordered list of block nodes. Returns an empty list if not present.
        /// </summary>
        public static List<Dictionary<object, object>> GetStatements(Dictionary<object, object> node, string stmtName)
        {
            if (node == null || !node.TryGetValue(stmtName, out object value))
                return new List<Dictionary<object, object>>();

            if (value is string)
                return new List<Dictionary<object, object>>();

            if (value is IEnumerable enumerable)
            {
                List<Dictionary<object, object>> stmts = new();
                foreach (object item in enumerable)
                {
                    Dictionary<object, object> entry = CoerceNode(item);
                    if (entry != null)
                        stmts.Add(entry);
                }

                return stmts;
            }

            return new List<Dictionary<object, object>>();
        }


        /// <summary>
        /// Cast a raw List&lt;object&gt; entry (from CodeExportPayload.Blocks) to a typed block node dictionary.
        /// </summary>
        public static Dictionary<object, object> AsNode(object raw) => CoerceNode(raw);
        /// <summary>
        /// Enumerate the top-level block list, yielding only entries whose "type" field matches <paramref name="blockType"/>.
        /// </summary>
        public static IEnumerable<Dictionary<object, object>> OfType(IEnumerable<object> blocks, string blockType)
        {
            if (blocks == null)
                yield break;

            foreach (object raw in blocks)
            {
                Dictionary<object, object> node = CoerceNode(raw);
                if (node == null)
                    continue;

                if (GetType(node) == blockType)
                    yield return node;
            }
        }

        /// <summary>
        /// Attempt to hydrate a strongly-typed helper object from a block node.
        /// Supported types are automatically detected from the block's "type" field.
        /// Returns null if the node type is not recognised.
        /// </summary>
        public static object Hydrate(Dictionary<object, object> node)
        {
            if (node == null)
                return null;

            return HydrateByType(GetType(node), node);
        }

        public static object Hydrate(object raw) => Hydrate(CoerceNode(raw));

        private static object HydrateByType(string blockType, Dictionary<object, object> node)
        {
            return blockType switch
            {
                "play_animation" => HydratePlayAnimation(node),
                "play_audio" => HydratePlayAudio(node),
                "send_cassie" => HydrateSendCassieMessage(node),
                "run_command" => HydrateRunCommand(node),
                "give_effect" => HydrateGiveEffect(node),
                "remove_effect" => HydrateRemoveEffect(node),
                "give_item" => HydrateGiveItem(node),
                "remove_item" => HydrateRemoveItem(node),
                "warhead" => HydrateWarhead(node),
                _ => null,
            };
        }

        public static PlayAnimation HydratePlayAnimation(Dictionary<object, object> node)
        {
            return new PlayAnimation
            {
                AnimationName = GetString(node, "animationName") ?? ""
            };
        }

        public static PlayAudio HydratePlayAudio(Dictionary<object, object> node)
        {
            return new PlayAudio
            {
                Path = GetString(node, "path") ?? "",
                Volume = GetFloat(node, "volume", 1f),
                MinDistance = GetFloat(node, "minDistance", 1f),
                MaxDistance = GetFloat(node, "maxDistance", 20f),
                IsSpatial = GetBool(node, "isSpatial", false)
            };
        }

        public static SendCassieMessage HydrateSendCassieMessage(Dictionary<object, object> node)
        {
            return new SendCassieMessage
            {
                Message = GetString(node, "message") ?? "",
                CustomSubtitles = GetString(node, "customSubtitles") ?? "",
                PlayBackground = GetBool(node, "playBackground"),
                Priority = GetFloat(node, "priority"),
                GlitchScale = GetFloat(node, "glitchScale")
            };
        }

        public static RunCommand HydrateRunCommand(Dictionary<object, object> node)
        {
            return new RunCommand
            {
                Type = GetEnum<RunCommand.CommandType>(node, "commandType"),
                Command = GetString(node, "command") ?? ""
            };
        }

        public static GiveEffect HydrateGiveEffect(Dictionary<object, object> node)
        {
            return new GiveEffect
            {
                Effect = GetEnum<Enums.EffectType>(node, "effect"),
                Intensity = GetInt(node, "intensity", 1),
                Duration = GetFloat(node, "duration", 5f)
            };
        }

        public static RemoveEffect HydrateRemoveEffect(Dictionary<object, object> node)
        {
            return new RemoveEffect
            {
                Effect = GetEnum<Enums.EffectType>(node, "effect"),
                Intensity = GetInt(node, "intensity", 1)
            };
        }

        public static GiveItem HydrateGiveItem(Dictionary<object, object> node)
        {
            return new GiveItem
            {
                Item = GetEnum<Enums.ItemType>(node, "item"),
                Count = (uint)GetInt(node, "count", 1)
            };
        }

        public static RemoveItem HydrateRemoveItem(Dictionary<object, object> node)
        {
            return new RemoveItem
            {
                Item = GetEnum<Enums.ItemType>(node, "item"),
                Count = (uint)GetInt(node, "count", 1)
            };
        }

        public static Warhead HydrateWarhead(Dictionary<object, object> node)
        {
            return new Warhead
            {
                Action = GetEnum<Enums.WarheadAction>(node, "action"),
                SuppressSubtitles = GetBool(node, "suppressSubtitles")
            };
        }
    }
}