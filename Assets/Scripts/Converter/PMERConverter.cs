using System;
using System.Collections.Generic;
using System.IO;
using Assets.Scripts.Components.Objects;
using Assets.Scripts.Enums;
using Assets.Scripts.Yaml;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Assets.Scripts.Extensions;
using System.Linq;
using System.Globalization;

namespace Assets.Scripts.Converter
{
#pragma warning disable CS0618
    [InitializeOnLoad]
    public class PMERConverter : EditorWindow
    {
        public static event Action<PMERSchematic, YamlSchematic> OnSchematicConverted;
        
        public enum PMERBlockType
        {
            Empty = 0,
            Primitive = 1,
            Light = 2,
            Pickup = 3,
            Workstation = 4,
            Schematic = 5,
            Teleport = 6,
            Locker = 7,
            Text = 8,
            Interactable = 9,
        }

        private static readonly JsonSerializerSettings settings = new()
        {
            Culture = CultureInfo.InvariantCulture,
        };

        private static readonly Dictionary<int, Transform> _instanceMap = new();

        [MenuItem("Thaumiel/Tools/PMER Converter")]
        public static void Open()
        {
            string jsonPath = EditorUtility.OpenFilePanel("Select Schematic", "", "json");
            if (string.IsNullOrEmpty(jsonPath))
                return;

            GameObject root = ConvertAndSpawn(jsonPath);
            if (root != null)
                Selection.activeGameObject = root;
        }

        [MenuItem("Thaumiel/Tools/PMER Converter (Batch)")]
        public static void OpenBatch()
        {
            string folder = EditorUtility.OpenFolderPanel("Select Folder With Schematics", "", "");
            if (string.IsNullOrEmpty(folder))
                return;

            string[] files;
            try
            {
                files = Directory.GetFiles(folder, "*.json", SearchOption.AllDirectories);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to list schematics in '{folder}': {ex.Message}");
                EditorUtility.DisplayDialog("PMER Converter (Batch)", $"Failed to list schematics in:\n{folder}\n\n{ex.Message}", "OK");
                return;
            }

            if (files.Length == 0)
            {
                EditorUtility.DisplayDialog("PMER Converter (Batch)", $"No .json schematics found in:\n{folder}", "OK");
                return;
            }

            List<GameObject> roots = ConvertMultiple(files);

            if (roots.Count > 0)
                Selection.objects = roots.ToArray();
        }

        public static List<GameObject> ConvertMultiple(IEnumerable<string> jsonPaths)
        {
            List<string> paths = jsonPaths?.Where(p => !string.IsNullOrEmpty(p)).ToList() ?? new();
            List<GameObject> roots = new(paths.Count);
            List<string> failed = new();

            if (paths.Count == 0)
                return roots;

            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Batch Convert PMER Schematics");
            Undo.IncrementCurrentGroup();

            try
            {
                for (int i = 0; i < paths.Count; i++)
                {
                    string jsonPath = paths[i];
                    try
                    {
                        EditorUtility.DisplayProgressBar("PMER Converter (Batch)", $"{Path.GetFileName(jsonPath)} ({i + 1}/{paths.Count})", (float)i / paths.Count);

                        GameObject root = ConvertAndSpawn(jsonPath);
                        if (root != null)
                        {
                            roots.Add(root);
                        }
                        else
                            failed.Add(jsonPath);
                    }
                    catch (Exception ex)
                    {
                        failed.Add(jsonPath);
                        Debug.LogError($"Failed to convert '{jsonPath}': {ex}");
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Undo.CollapseUndoOperations(group);
            }

            Debug.Log($"PMER Converter (Batch): converted {roots.Count}/{paths.Count} schematics.{(failed.Count > 0 ? $" Failed: {string.Join(", ", failed.Select(Path.GetFileName))}" : string.Empty)}");

            if (failed.Count > 0)
                EditorUtility.DisplayDialog("PMER Converter (Batch)", $"Converted {roots.Count}/{paths.Count} schematics.\n\nFailed:\n{string.Join("\n", failed)}", "OK");

            return roots;
        }

        public static GameObject ConvertAndSpawn(string jsonPath)
        {
            if (string.IsNullOrEmpty(jsonPath))
                return null;

            if (!File.Exists(jsonPath))
            {
                Debug.LogError($"Schematic file not found: '{jsonPath}'.");
                return null;
            }

            _instanceMap.Clear();

            string json = File.ReadAllText(jsonPath);
            PMERSchematic pmer = JsonConvert.DeserializeObject<PMERSchematic>(json, settings);
            if (pmer?.Blocks == null)
            {
                Debug.LogError($"Failed to parse PMER schematic: '{jsonPath}'.");
                return null;
            }

            YamlSchematic tmeschematic = ConvertSchematic(pmer, Path.GetFileNameWithoutExtension(jsonPath));
            OnSchematicConverted?.Invoke(pmer, tmeschematic);

            GameObject root = new(tmeschematic.FileName);
            root.transform.rotation = Quaternion.Euler(tmeschematic.Rotation);
            root.transform.localScale = tmeschematic.Scale;
            root.AddComponent<Builder>();
            Undo.RegisterCreatedObjectUndo(root, $"Convert {tmeschematic.FileName}");

            for (int i = 0; i < tmeschematic.Objects.Count; i++)
            {
                YamlCustomObject obj = tmeschematic.Objects[i];
                PMERBlock originalBlock = pmer.Blocks[i];

                GameObject prefab = Decompiler.GetPrefabForObject(obj);
                if (prefab == null)
                {
                    Debug.LogWarning($"No prefab mapped for ObjectType '{obj.ObjectType}', skipping '{obj.Name}'.");
                    continue;
                }

                GameObject instance = Instantiate(prefab);
                instance.name = obj.Name;

                Transform parent = originalBlock.ParentId == pmer.RootObjectId ? root.transform : _instanceMap.TryGetValue(originalBlock.ParentId, out Transform p) ? p : root.transform;
                instance.transform.SetParent(parent, true);

                if (instance.TryGetComponent(out ObjectBase block))
                {
                    block.Name = obj.Name;
                    block.Static = obj.IsStatic;
                    block.MovementSmoothing = obj.MovementSmoothing;
                    block.ObjectType = obj.ObjectType;
                    block.Properties = obj.Values;

                    block.Decompile(root.transform);
                }

                instance.transform.SetLocalPositionAndRotation(obj.Position, Quaternion.Euler(obj.Rotation));
                instance.transform.localScale = obj.Scale;

                _instanceMap[originalBlock.ObjectId] = instance.transform;
                Undo.RegisterCreatedObjectUndo(instance, $"Convert {obj.Name}");
            }

            return root;
        }

        /// <summary>
        /// Converts a PMER schematic into a TME schematic
        /// </summary>
        /// <param name="root">The PMER schematic root</param>
        /// <returns></returns>
        public static YamlSchematic ConvertSchematic(PMERSchematic root, string filename)
        {
            YamlSchematic tme = new()
            {
                FileName = filename,
                Rotation = Vector3.one,
                Scale = Vector3.one,
                Objects = new()
            };

            foreach (PMERBlock block in root.Blocks)
                tme.Objects.Add(ConvertBlock(block));

            return tme;
        }

        private static YamlCustomObject ConvertBlock(PMERBlock block)
        {
            YamlCustomObject obj = new()
            {
                Name = block.Name,
                Position = block.Position,
                Rotation = block.Rotation,
                Scale = block.Scale,
                IsStatic = block.Properties != null && block.Properties.TryGetValue("Static", out object s) && Convert.ToBoolean(s),
                MovementSmoothing = 60,
                ObjectType = MapBlockType(block.BlockType),
                Values = NormalizeProperties(block)
            };

            return obj;
        }

        private static ObjectType MapBlockType(PMERBlockType blockType)
        {
            ObjectType Warn()
            {
                Debug.LogWarning($"Unsupported BlockType {blockType} was found. Setting type to None.");
                return ObjectType.None;
            }

            return blockType switch
            {
                PMERBlockType.Primitive => ObjectType.Primitive,
                PMERBlockType.Light => ObjectType.Light,
                PMERBlockType.Pickup => ObjectType.Pickup,
                PMERBlockType.Workstation => ObjectType.Workstation,
                PMERBlockType.Schematic => ObjectType.Schematic,
                PMERBlockType.Teleport => ObjectType.Teleporter,
                PMERBlockType.Locker => ObjectType.Locker,
                PMERBlockType.Text => ObjectType.TextToy,
                PMERBlockType.Interactable => ObjectType.Interactable,
                PMERBlockType.Empty => ObjectType.GameObject,
                _ => Warn(),
            };
        }

        private static Dictionary<string, object> NormalizeProperties(PMERBlock block)
        {
            Dictionary<string, object> dict = block.Properties != null ? new Dictionary<string, object>(block.Properties) : new();

            switch (block.BlockType)
            {
                case PMERBlockType.Primitive:
                    if (dict.TryGetValue("PrimitiveType", out var pt))
                        dict["PrimitiveType"] = Convert.ToInt32(pt);

                    if (dict.TryGetValue("PrimitiveFlags", out var pf))
                    {
                        dict["PrimitiveFlags"] = Convert.ToByte(pf);
                    }
                    else
                        dict["PrimitiveFlags"] = PrimitiveFlags.Visible | PrimitiveFlags.Collidable;

                    if (dict.TryGetValue("Color", out var color))
                    {
                        if (TryParseColor(color.ToString(), out Color unityColor))
                        {
                            dict["Color"] = unityColor;
                        }
                        else
                            Debug.LogWarning($"Failed to parse color value: {color}");
                    }
                    break;

                case PMERBlockType.Light:
                    if (dict.TryGetValue("LightType", out var lighttype))
                        dict["LightType"] = (LightType)Convert.ToInt32(lighttype);

                    if (dict.TryGetValue("Color", out var lightcolor))
                    {
                        if (TryParseColor(lightcolor.ToString(), out Color unitylightColor))
                            dict["LightColor"] = unitylightColor;
                        else
                            Debug.LogWarning($"Failed to parse color value: {lightcolor}");
                    }

                    if (dict.TryGetValue("Intensity", out var intensity))
                        dict["LightIntensity"] = ToSingle(intensity);

                    if (dict.TryGetValue("Range", out var range))
                        dict["LightRange"] = ToSingle(range);

                    if (dict.TryGetValue("Shape", out var shape))
                        dict["LightShape"] = (LightShape)Convert.ToInt32(shape);

                    if (dict.TryGetValue("SpotAngle", out var spotangle))
                        dict["SpotAngle"] = ToSingle(spotangle);

                    if (dict.TryGetValue("InnerSpotAngle", out var innerspotangle))
                        dict["InnerSpotAngle"] = ToSingle(innerspotangle);

                    if (dict.TryGetValue("ShadowStrength", out var shadowStrength))
                        dict["ShadowStrength"] = ToSingle(shadowStrength);

                    if (dict.TryGetValue("ShadowType", out var shadowtype))
                        dict["ShadowType"] = (LightShadows)Convert.ToInt32(shadowtype);
                    break;
    
                case PMERBlockType.Pickup:
                    if (dict.TryGetValue("ItemType", out var itemtype))
                        dict["ItemToSpawn"] = (ItemType)Convert.ToInt32(itemtype);

                    if (dict.TryGetValue("Chance", out var chance))
                        dict["SpawnPercentage"] = ToSingle(chance);

                    if (dict.TryGetValue("Uses", out var uses))
                        dict["MaxAmount"] = Convert.ToInt32(uses);
                    break;

                case PMERBlockType.Teleport:
                    if (dict.TryGetValue("Cooldown", out var cooldown))
                        dict["Cooldown"] = ToSingle(cooldown);

                    if (dict.TryGetValue("Id", out var id))
                        dict["Id"] = Guid.Parse(Convert.ToString(id));

                    if (dict.TryGetValue("TargetTeleporters", out var targets))
                    {
                        if (targets is object[] array)
                        {
#nullable enable
                            List<string?> ids = array.Select(t => t.GetType().GetField("Id")?.GetValue(t) as string).Where(id => id != null).ToList();
#nullable disable
                            if (Guid.TryParse(ids.First(), out var targetid))
                                dict["Target"] = targetid;
                        }
                    }

                    break;

                case PMERBlockType.Interactable:
                    if (dict.TryGetValue("ColliderShape", out var collidershape))
                        dict["Shape"] = (ColliderShape)Convert.ToInt32(collidershape);

                    if (dict.TryGetValue("InteractionDuration", out var duration))
                        dict["Duration"] = ToSingle(duration);

                    if (dict.TryGetValue("IsLocked", out var locked))
                        dict["Locked"] = ToSingle(locked);

                    break;

                case PMERBlockType.Text:
                    if (dict.TryGetValue("Text", out var text))
                        dict["TextFormat"] = Convert.ToString(text);

                    if (dict.TryGetValue("DisplaySize", out var displaysize))
                        dict["DisplaySize"] = ConvertExtensions.ToVector2(displaysize);
                        
                    break;
            }

            return dict;
        }

        private static bool TryParseColor(string input, out Color color)
        {
            color = Color.white;
            if (string.IsNullOrWhiteSpace(input))
                return false;

            input = input.Trim();

            if (input.Contains(':'))
            {
                string[] parts = input.Split(':');
                if (parts.Length >= 3 && TryParseFloatString(parts[0], out float r) && TryParseFloatString(parts[1], out float g) && TryParseFloatString(parts[2], out float b))
                {
                    float a = 1f;
                    if (parts.Length >= 4 && !TryParseFloatString(parts[3], out a))
                        return false;

                    color = new Color(r / 255f, g / 255f, b / 255f, a);
                    return true;
                }

                return false;
            }

            string hex = input.TrimStart('#');
            if ((hex.Length == 6 || hex.Length == 8) && IsHexString(hex))
            {
                try
                {
                    if (hex.Length == 6)
                    {
                        color = new Color(
                            Convert.ToInt32(hex.Substring(0, 2), 16) / 255f,
                            Convert.ToInt32(hex.Substring(2, 2), 16) / 255f,
                            Convert.ToInt32(hex.Substring(4, 2), 16) / 255f
                        );

                        return true;
                    }

                    color = new Color(
                        Convert.ToInt32(hex.Substring(0, 2), 16) / 255f,
                        Convert.ToInt32(hex.Substring(2, 2), 16) / 255f,
                        Convert.ToInt32(hex.Substring(4, 2), 16) / 255f,
                        Convert.ToInt32(hex.Substring(6, 2), 16) / 255f
                    );

                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Exception parsing hex color '{hex}': {ex.Message}");
                    return false;
                }
            }

            if (ColorUtility.TryParseHtmlString(input, out Color htmlColor))
            {
                color = htmlColor;
                return true;
            }

            if (!input.StartsWith("#") && ColorUtility.TryParseHtmlString("#" + input, out Color prefixedColor))
            {
                color = prefixedColor;
                return true;
            }

            return false;
        }

        private static bool TryParseHexColor(string hex, out Color color) => TryParseColor(hex, out color);

        private static bool IsHexString(string value)
        {
            foreach (char c in value)
            {
                bool isHex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
                if (!isHex)
                    return false;
            }

            return true;
        }

        private static bool TryParseFloatString(string s, out float result)
        {
            result = 0f;
            if (string.IsNullOrWhiteSpace(s))
                return false;

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

            if (float.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result))
                return true;

            if (float.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out result))
                return true;

            return false;
        }

        private static float ToSingle(object value)
        {
            switch (value)
            {
                case null:
                    return 0f;

                case float f:
                    return f;

                case double d:
                    return (float)d;

                case long l:
                    return l;

                case int i:
                    return i;

                case bool b:
                    return b ? 1f : 0f;

                case string s when TryParseFloatString(s, out float parsed):
                    return parsed;

                case string s:
                    throw new FormatException($"Unable to parse '{s}' as float.");

                default:
                    if (value is IConvertible)
                    {
                        try
                        {
                            return Convert.ToSingle(value, CultureInfo.InvariantCulture);
                        }
                        catch
                        {
                            return Convert.ToSingle(value, CultureInfo.CurrentCulture);
                        }
                    }

                    throw new InvalidCastException($"Unable to convert '{value}' ({value.GetType().Name}) to float.");
            }
        }
    }
}