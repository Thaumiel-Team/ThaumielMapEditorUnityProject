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

        private const string PartsFolder = "Assets/Parts";

        private static readonly Dictionary<string, GameObject> _prefabCache = new();

        private static readonly Dictionary<int, Transform> _instanceMap = new();

        public static void DecompileData()
        {
            _instanceMap.Clear();

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

        private static GameObject LoadPartPrefab(string relativePath)
        {
            string path = $"{PartsFolder}/{relativePath}";
            if (_prefabCache.TryGetValue(path, out GameObject cached))
                return cached;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                Debug.LogWarning($"Decompiler: prefab not found at '{path}'.");

            _prefabCache[path] = prefab;
            return prefab;
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
                ObjectType.TextToy => LoadPartPrefab("Text.prefab"),
                ObjectType.Capybara => LoadPartPrefab("Capybara.prefab"),
                ObjectType.Light => LoadPartPrefab("Area Light.prefab"),
                ObjectType.Workstation => LoadPartPrefab("Work Station.prefab"),
                ObjectType.Interactable => LoadPartPrefab("Interactable.prefab"),
                ObjectType.Waypoint => LoadPartPrefab("Waypoint.prefab"),
                ObjectType.Pickup => LoadPartPrefab("Pickup.prefab"),
                ObjectType.Teleporter => LoadPartPrefab("Teleporter.prefab"),
                ObjectType.GameObject => LoadPartPrefab("Empty GameObject.prefab"),
                ObjectType.PlayerSpawnPoint => LoadPartPrefab("Player Spawn Point.prefab"),
                ObjectType.RagdollSpawner => LoadPartPrefab("Ragdoll Spawner.prefab"),
                _ => null
            };
        }

        public static GameObject GetPrimitivePrefab(YamlCustomObject obj)
        {
            if (!obj.Values.TryGetValue("PrimitiveType", out object primitiveType))
                return null;

            return Enum.Parse<PrimitiveType>(Convert.ToString(primitiveType)) switch
            {
                PrimitiveType.Sphere => LoadPartPrefab("Primitives/Sphere.prefab"),
                PrimitiveType.Cube => LoadPartPrefab("Primitives/Cube.prefab"),
                PrimitiveType.Cylinder => LoadPartPrefab("Primitives/Cylinder.prefab"),
                PrimitiveType.Capsule => LoadPartPrefab("Primitives/Capsule.prefab"),
                PrimitiveType.Plane => LoadPartPrefab("Primitives/Plane.prefab"),
                PrimitiveType.Quad => LoadPartPrefab("Primitives/Quad.prefab"),
                _ => null
            };
        }

        public static GameObject GetDoorPrefab(YamlCustomObject obj)
        {
            if (!obj.Values.TryGetValue("DoorType", out object doorType))
                return null;

            return Enum.Parse<DoorType>(Convert.ToString(doorType)) switch
            {
                DoorType.Lcz => LoadPartPrefab("Doors/LCZ BreakableDoor.prefab"),
                DoorType.Hcz => LoadPartPrefab("Doors/HCZ BreakableDoor.prefab"),
                DoorType.Ez => LoadPartPrefab("Doors/EZ BreakableDoor.prefab"),
                DoorType.Gate => LoadPartPrefab("Doors/Spawnable Unsecured Pryable GateDoor.prefab"),
                DoorType.BulkHead => LoadPartPrefab("Doors/HCZ BulkDoor.prefab"),
                _ => null
            };
        }

        public static GameObject GetCameraPrefab(YamlCustomObject obj)
        {
            if (!obj.Values.TryGetValue("CameraType", out object cameraType))
                return null;

            return Enum.Parse<Enums.CameraType>(Convert.ToString(cameraType)) switch
            {
                Enums.CameraType.Lcz => LoadPartPrefab("Cameras/LczCameraToy.prefab"),
                Enums.CameraType.Hcz => LoadPartPrefab("Cameras/HczCameraToy.prefab"),
                Enums.CameraType.Ez => LoadPartPrefab("Cameras/EzCameraToy.prefab"),
                Enums.CameraType.EzArm => LoadPartPrefab("Cameras/EzArmCameraToy.prefab"),
                Enums.CameraType.Sz => LoadPartPrefab("Cameras/SzCameraToy.prefab"),
                _ => null
            };
        }

        public static GameObject GetClutterPrefab(YamlCustomObject obj)
        {
            if (!obj.Values.TryGetValue("ClutterType", out object clutterType))
                return null;

            return Enum.Parse<ClutterType>(Convert.ToString(clutterType)) switch
            {
                ClutterType.SimpleBoxes => LoadPartPrefab("Clutter/Simple Boxes Open Connector.prefab"),
                ClutterType.PipesShort => LoadPartPrefab("Clutter/Pipes Short Open Connector.prefab"),
                ClutterType.BoxesLadder => LoadPartPrefab("Clutter/Boxes Ladder Open Connector.prefab"),
                ClutterType.TankSupportedShelf => LoadPartPrefab("Clutter/Tank-Supported Shelf Open Connector.prefab"),
                ClutterType.AngledFences => LoadPartPrefab("Clutter/Angled Fences Open Connector.prefab"),
                ClutterType.HugeOrangePipes => LoadPartPrefab("Clutter/Huge Orange Pipes Open Connector.prefab"),
                ClutterType.PipesLongOpen => LoadPartPrefab("Clutter/Pipes Long Open Connector.prefab"),
                ClutterType.BrokenElectricalBox => LoadPartPrefab("Clutter/Broken Electrical Box Open Connector.prefab"),
                _ => null
            };
        }

        public static GameObject GetLockerPrefab(YamlCustomObject obj)
        {
            if (!obj.Values.TryGetValue("LockerType", out object lockerType))
                return null;

            return Enum.Parse<LockerType>(Convert.ToString(lockerType)) switch
            {
                LockerType.Pedestal => LoadPartPrefab("Lockers/SCP Pedestal.prefab"),
                LockerType.LargeGun => LoadPartPrefab("Lockers/Large Locker.prefab"),
                LockerType.RifleRack => LoadPartPrefab("Lockers/Locker.prefab"),
                LockerType.Misc => LoadPartPrefab("Lockers/Locker.prefab"),
                LockerType.Medkit => LoadPartPrefab("Lockers/MedKit.prefab"),
                LockerType.Adrenaline => LoadPartPrefab("Lockers/Adrenaline Medkit Locker.prefab"),
                LockerType.ExperimentalWeapon => LoadPartPrefab("Lockers/Experimental Weapon Locker.prefab"),
                _ => null
            };
        }

        public static GameObject GetTargetPrefab(YamlCustomObject obj)
        {
            if (!obj.Values.TryGetValue("TargetType", out object targetType))
                return null;

            return Enum.Parse<TargetType>(Convert.ToString(targetType)) switch
            {
                TargetType.Binary => LoadPartPrefab("Targets/binaryTargetPrefab.prefab"),
                TargetType.ClassD => LoadPartPrefab("Targets/dboyTargetPrefab.prefab"),
                TargetType.Sport => LoadPartPrefab("Targets/sportTargetPrefab.prefab"),
                _ => null
            };
        }
    }
}