using System;
using Assets.Scripts.Components.Objects;
using Assets.Scripts.Enums;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Collab.Editor
{
    public static class CollabPrefabMap
    {
        public static GameObject Resolve(string prefabPath, ObjectType objectType, string hint)
        {
            if (!string.IsNullOrEmpty(prefabPath))
            {
                GameObject direct = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (direct != null)
                    return direct;
            }
            
            string mapped = GetPrefabPath(objectType, hint);
            if (!string.IsNullOrEmpty(mapped))
            {
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(mapped);
                if (go != null)
                    return go;
            }
            return null;
        }

        public static string GetPrefabPath(ObjectType objectType, string hint)
        {
            string h = (hint ?? string.Empty).Trim();
            return objectType switch
            {
                ObjectType.Primitive => h.ToLowerInvariant() switch
                {
                    "sphere" => "Assets/Parts/Primitives/Sphere.prefab",
                    "cube" => "Assets/Parts/Primitives/Cube.prefab",
                    "cylinder" => "Assets/Parts/Primitives/Cylinder.prefab",
                    "capsule" => "Assets/Parts/Primitives/Capsule.prefab",
                    "plane" => "Assets/Parts/Primitives/Plane.prefab",
                    "quad" => "Assets/Parts/Primitives/Quad.prefab",
                    _ => "Assets/Parts/Primitives/Cube.prefab",
                },
                ObjectType.Door => h.ToLowerInvariant() switch
                {
                    "lcz" => "Assets/Parts/Doors/LCZ BreakableDoor.prefab",
                    "hcz" => "Assets/Parts/Doors/HCZ BreakableDoor.prefab",
                    "ez" => "Assets/Parts/Doors/EZ BreakableDoor.prefab",
                    "gate" => "Assets/Parts/Doors/Spawnable Unsecured Pryable GateDoor.prefab",
                    "bulkhead" => "Assets/Parts/Doors/HCZ BulkDoor.prefab",
                    _ => "Assets/Parts/Doors/LCZ BreakableDoor.prefab",
                },
                ObjectType.Camera => h.ToLowerInvariant() switch
                {
                    "lcz" => "Assets/Parts/Cameras/LczCameraToy.prefab",
                    "hcz" => "Assets/Parts/Cameras/HczCameraToy.prefab",
                    "ez" => "Assets/Parts/Cameras/EzCameraToy.prefab",
                    "ezarm" => "Assets/Parts/Cameras/EzArmCameraToy.prefab",
                    "sz" => "Assets/Parts/Cameras/SzCameraToy.prefab",
                    _ => "Assets/Parts/Cameras/LczCameraToy.prefab",
                },
                ObjectType.Clutter => h.ToLowerInvariant() switch
                {
                    "simpleboxes" => "Assets/Parts/Clutter/Simple Boxes Open Connector.prefab",
                    "pipesshort" => "Assets/Parts/Clutter/Pipes Short Open Connector.prefab",
                    "boxesladder" => "Assets/Parts/Clutter/Boxes Ladder Open Connector.prefab",
                    "tanksupportedshelf" => "Assets/Parts/Clutter/Tank-Supported Shelf Open Connector.prefab",
                    "angledfences" => "Assets/Parts/Clutter/Angled Fences Open Connector.prefab",
                    "hugeorangepipes" => "Assets/Parts/Clutter/Huge Orange Pipes Open Connector.prefab",
                    "pipeslongopen" => "Assets/Parts/Clutter/Pipes Long Open Connector.prefab",
                    "brokenelectricalbox" => "Assets/Parts/Clutter/Broken Electrical Box Open Connector.prefab",
                    _ => "Assets/Parts/Clutter/Simple Boxes Open Connector.prefab",
                },
                ObjectType.Locker => h.ToLowerInvariant() switch
                {
                    "pedestal" => "Assets/Parts/Lockers/SCP Pedestal.prefab",
                    "largegun" => "Assets/Parts/Lockers/Large Locker.prefab",
                    "riflerack" or "misc" => "Assets/Parts/Lockers/Locker.prefab",
                    "medkit" => "Assets/Parts/Lockers/MedKit.prefab",
                    "adrenaline" => "Assets/Parts/Lockers/Adrenaline Medkit Locker.prefab",
                    "experimentalweapon" => "Assets/Parts/Lockers/Experimental Weapon Locker.prefab",
                    _ => "Assets/Parts/Lockers/Locker.prefab",
                },
                ObjectType.Target => h.ToLowerInvariant() switch
                {
                    "binary" => "Assets/Parts/Targets/binaryTargetPrefab.prefab",
                    "classd" => "Assets/Parts/Targets/dboyTargetPrefab.prefab",
                    "sport" => "Assets/Parts/Targets/sportTargetPrefab.prefab",
                    _ => "Assets/Parts/Targets/binaryTargetPrefab.prefab",
                },
                ObjectType.TextToy => "Assets/Parts/Text.prefab",
                ObjectType.Capybara => "Assets/Parts/Capybara.prefab",
                ObjectType.Light => "Assets/Parts/Area Light.prefab",
                ObjectType.Workstation => "Assets/Parts/Work Station.prefab",
                ObjectType.Interactable => "Assets/Parts/Interactable.prefab",
                ObjectType.Waypoint => "Assets/Parts/Waypoint.prefab",
                ObjectType.Pickup => "Assets/Parts/Pickup.prefab",
                ObjectType.Teleporter => "Assets/Parts/Teleporter.prefab",
                ObjectType.GameObject => "Assets/Parts/Empty GameObject.prefab",
                ObjectType.PlayerSpawnPoint => "Assets/Parts/Player Spawn Point.prefab",
                ObjectType.RagdollSpawner => "Assets/Parts/Ragdoll Spawner.prefab",
                ObjectType.Speaker => string.Empty,
                _ => string.Empty,
            };
        }
        
        public static Type ComponentTypeFor(ObjectType t)
        {
            return t switch
            {
                ObjectType.Primitive => typeof(PrimitiveObject),
                ObjectType.Light => typeof(LightObject),
                ObjectType.Door => typeof(DoorObject),
                ObjectType.Workstation => typeof(WorkstationObject),
                ObjectType.Interactable => typeof(InteractableObject),
                ObjectType.TextToy => typeof(TextToyObject),
                ObjectType.Capybara => typeof(CapyBaraObject),
                ObjectType.Pickup => typeof(PickupObject),
                ObjectType.Clutter => typeof(ClutterObject),
                ObjectType.Camera => typeof(CameraObject),
                ObjectType.Waypoint => typeof(WaypointObject),
                ObjectType.Locker => typeof(LockerObject),
                ObjectType.Target => typeof(TargetObject),
                ObjectType.Teleporter => typeof(TeleporterObject),
                ObjectType.GameObject => typeof(EmptyGameObject),
                ObjectType.Speaker => typeof(SpeakerObject),
                ObjectType.PlayerSpawnPoint => typeof(PlayerSpawnPointObject),
                ObjectType.RagdollSpawner => typeof(RagdollSpawner),
                _ => typeof(EmptyGameObject),
            };
        }
    }
}
