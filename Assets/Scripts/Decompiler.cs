using System;
using System.Collections.Generic;
using System.IO;
using Assets.Scripts.Components.Objects;
using Assets.Scripts.Components.Tools;
using Assets.Scripts.Enums;
using Assets.Scripts.Yaml;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using PhysicsTool = Assets.Scripts.Components.Tools.Physics;

namespace Assets.Scripts
{
    public static class Decompiler
    {
        public static event Action<YamlSchematic> OnSchematicDecompiled;

        public static BuilderPrefabRegistry _registry;

        private static readonly Dictionary<int, Transform> _instanceMap = new();

        public static void DecompileData(BuilderPrefabRegistry registry)
        {
            _instanceMap.Clear();
            _registry = registry;

            string yamlPath = EditorUtility.OpenFilePanel("Select Schematic", "", "yml");
            if (string.IsNullOrEmpty(yamlPath))
                return;

            string yaml = File.ReadAllText(yamlPath);
            YamlSchematic schematic = YamlParser.Deserializer.Deserialize<YamlSchematic>(yaml);

            GameObject root = new(schematic.FileName);
            root.transform.rotation = Quaternion.Euler(schematic.Rotation);
            root.transform.localScale = schematic.Scale;
            Builder builder = root.AddComponent<Builder>();

            Undo.RegisterCreatedObjectUndo(root, $"Decompile {schematic.FileName}");

            foreach (YamlCustomObject obj in schematic.Objects)
            {
                GameObject prefab = GetPrefabForObject(obj);
                if (prefab == null)
                {
                    Debug.LogWarning($"No prefab mapped for ObjectType '{obj.ObjectType}', skipping '{obj.Name}'.");
                    continue;
                }

                GameObject instance = UnityEngine.Object.Instantiate(prefab);
                instance.name = obj.Name;

                Transform parent = obj.ParentId == schematic.RootObjectId ? root.transform : _instanceMap.TryGetValue(obj.ParentId, out Transform p) ? p : root.transform;
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

                _instanceMap[obj.ObjectId] = instance.transform;
                Undo.RegisterCreatedObjectUndo(instance, $"Decompile {obj.Name}");
            }

            _instanceMap.Clear();
            foreach (YamlCustomObject obj in schematic.ServerSideObjects)
            {
                GameObject prefab = GetPrefabForObject(obj);
                if (prefab == null)
                {
                    Debug.LogWarning($"No prefab mapped for ObjectType '{obj.ObjectType}', skipping '{obj.Name}'.");
                    continue;
                }

                GameObject instance = UnityEngine.Object.Instantiate(prefab);
                instance.name = obj.Name;

                Transform parent;
                if (_instanceMap.TryGetValue(obj.ParentId, out Transform mappedParent))
                {
                    parent = mappedParent;
                }
                else
                    parent = builder.server.gameObject.transform;

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

                _instanceMap[obj.ObjectId] = instance.transform;
                Undo.RegisterCreatedObjectUndo(instance, $"Decompile {obj.Name}");

                if (obj.Tools != null && obj.Tools.Count > 0)
                {
                    foreach (YamlTool tool in obj.Tools)
                    {
                        if (!Enum.TryParse<ToolType>(tool.ToolName, true, out var result))
                            continue;

                        switch (result)
                        {
                            case ToolType.Health:
                                Health health = block.AddComponent<Health>();
                                health.Properties = tool.Properties;
                                health.Decompile();
                                break;

                            case ToolType.Physics:
                                PhysicsTool physics = block.AddComponent<PhysicsTool>();
                                physics.Properties = tool.Properties;
                                physics.Decompile();
                                break;

                            case ToolType.Doorlink:
                                DoorLink doorlink = block.AddComponent<DoorLink>();
                                doorlink.Properties = tool.Properties;
                                doorlink.Decompile();
                                break;

                            case ToolType.ColliderTrigger:
                                ColliderTrigger collider = block.AddComponent<ColliderTrigger>();
                                collider.Properties = tool.Properties;
                                collider.Decompile();
                                break;

                            case ToolType.InteractableTrigger:
                                InteractableTrigger interactable = block.AddComponent<InteractableTrigger>();
                                interactable.Properties = tool.Properties;
                                interactable.Decompile();
                                break;

                            case ToolType.BlockyRuntime:
                                BlockyRuntime blocky = block.AddComponent<BlockyRuntime>();
                                blocky.Properties = tool.Properties;
                                blocky.Decompile();
                                break;
                        }
                    }
                }
            }

            Selection.activeGameObject = root;
            Debug.Log($"Decompiled schematic '{schematic.FileName}' with {schematic.Objects.Count} objects.");
            OnSchematicDecompiled?.Invoke(schematic);
        }

        public static void LoadRegistry()
        {
            string[] guids = AssetDatabase.FindAssets("t:BuilderPrefabRegistry");
            if (guids.Length == 0)
                return;

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            _registry = AssetDatabase.LoadAssetAtPath<BuilderPrefabRegistry>(path);
        }

        public static GameObject GetPrefabForObject(YamlCustomObject obj)
        {
            return obj.ObjectType switch
            {
                ObjectType.Primitive => GetPrimitivePrefab(obj),
                ObjectType.Door => GetDoorPrefab(obj),
                ObjectType.Camera => GetCameraPrefab(obj),
                ObjectType.Clutter => GetClutterPrefab(obj),
                ObjectType.Locker => GetLockerPrefab(obj),
                ObjectType.Target => GetTargetPrefab(obj),
                ObjectType.TextToy => _registry.TextToyPrefab,
                ObjectType.Capybara => _registry.CapybaraPrefab,
                ObjectType.Light => _registry.LightPrefab,
                ObjectType.Workstation => _registry.WorkstationPrefab,
                ObjectType.Interactable => _registry.InteractablePrefab,
                ObjectType.Waypoint => _registry.WaypointPrefab,
                ObjectType.Pickup => _registry.PickupPrefab,
                ObjectType.Teleporter => _registry.TeleporterPrefab,
                ObjectType.GameObject => _registry.EmptyGameObjectPrefab,
                _ => null
            };
        }

        public static GameObject GetPrimitivePrefab(YamlCustomObject obj)
        {
            if (!obj.Values.TryGetValue("PrimitiveType", out object primitiveType))
                return null;

            return Enum.Parse<PrimitiveType>(Convert.ToString(primitiveType)) switch
            {
                PrimitiveType.Sphere => _registry.SpherePrefab,
                PrimitiveType.Cube => _registry.CubePrefab,
                PrimitiveType.Cylinder => _registry.CylinderPrefab,
                PrimitiveType.Capsule => _registry.CapsulePrefab,
                PrimitiveType.Plane => _registry.PlanePrefab,
                PrimitiveType.Quad => _registry.QuadPrefab,
                _ => null
            };
        }

        public static GameObject GetDoorPrefab(YamlCustomObject obj)
        {
            if (!obj.Values.TryGetValue("DoorType", out object doorType))
                return null;

            return Enum.Parse<DoorType>(Convert.ToString(doorType)) switch
            {
                DoorType.Lcz => _registry.LczDoorPrefab,
                DoorType.Hcz => _registry.HczDoorPrefab,
                DoorType.Ez => _registry.EzDoorPrefab,
                DoorType.Gate => _registry.GateDoorPrefab,
                DoorType.BulkHead => _registry.BulkHeadDoorPrefab,
                _ => null
            };
        }

        public static GameObject GetCameraPrefab(YamlCustomObject obj)
        {
            if (!obj.Values.TryGetValue("CameraType", out object cameraType))
                return null;

            return Enum.Parse<Enums.CameraType>(Convert.ToString(cameraType)) switch
            {
                Enums.CameraType.Lcz => _registry.LczCameraPrefab,
                Enums.CameraType.Hcz => _registry.HczCameraPrefab,
                Enums.CameraType.Ez => _registry.EzCameraPrefab,
                Enums.CameraType.EzArm => _registry.EzArmCameraPrefab,
                Enums.CameraType.Sz => _registry.SzCameraPrefab,
                _ => null
            };
        }

        public static GameObject GetClutterPrefab(YamlCustomObject obj)
        {
            if (!obj.Values.TryGetValue("ClutterType", out object clutterType))
                return null;

            return Enum.Parse<ClutterType>(Convert.ToString(clutterType)) switch
            {
                ClutterType.SimpleBoxes => _registry.SimpleBoxesPrefab,
                ClutterType.PipesShort => _registry.PipesShortPrefab,
                ClutterType.BoxesLadder => _registry.BoxesLadderPrefab,
                ClutterType.TankSupportedShelf => _registry.TankSupportedShelfPrefab,
                ClutterType.AngledFences => _registry.AngledFencesPrefab,
                ClutterType.HugeOrangePipes => _registry.HugeOrangePipesPrefab,
                ClutterType.PipesLongOpen => _registry.PipesLongOpenPrefab,
                ClutterType.BrokenElectricalBox => _registry.BrokenElectricalBoxPrefab,
                _ => null
            };
        }

        public static GameObject GetLockerPrefab(YamlCustomObject obj)
        {
            if (!obj.Values.TryGetValue("LockerType", out object lockerType))
                return null;

            return Enum.Parse<LockerType>(Convert.ToString(lockerType)) switch
            {
                LockerType.Pedestal => _registry.PedestalPrefab,
                LockerType.LargeGun => _registry.LargeGunPrefab,
                LockerType.RifleRack => _registry.RifleRackPrefab,
                LockerType.Misc => _registry.MiscLockerPrefab,
                LockerType.Medkit => _registry.MedkitPrefab,
                LockerType.Adrenaline => _registry.AdrenalinePrefab,
                LockerType.ExperimentalWeapon => _registry.ExperimentalWeaponPrefab,
                _ => null
            };
        }

        public static GameObject GetTargetPrefab(YamlCustomObject obj)
        {
            if (!obj.Values.TryGetValue("TargetType", out object targetType))
                return null;

            return Enum.Parse<TargetType>(Convert.ToString(targetType)) switch
            {
                TargetType.Binary => _registry.BinaryTargetPrefab,
                TargetType.ClassD => _registry.ClassDTargetPrefab,
                TargetType.Sport => _registry.SportTargetPrefab,
                _ => null
            };
        }
    }
}