#if UNITY_EDITOR
using Assets.Scripts.Enums;
using Assets.Scripts.Lockers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Assets.Scripts.Components.Objects.Lockers
{
    public class CabinetLocker : LockerObject
    {
        private LockerType GetLockerType()
        {
            return GetSourcePrefabName(gameObject).ToLowerInvariant() switch
            {
                "medkit" => LockerType.Medkit,
                "adrenaline medkit locker" => LockerType.Adrenaline,
                _ => LockerType.None,
            };
        }

        private static string GetSourcePrefabName(GameObject instance)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(instance))
            {
                GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(instance);
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(root);
                if (source != null)
                    return source.name;
            }

            PrefabStage stage = PrefabStageUtility.GetPrefabStage(instance);
            if (stage != null)
                return stage.prefabContentsRoot.name;

            return instance.name.Replace("(Clone)", "").Trim();
        }

        public override LockerType LockerType => GetLockerType();

        protected override LockerChamber[] CreateDefaultChambers()
        {
            static LockerChamber Chamber(uint index, ItemType item, string name) => new()
            {
                Name = name,
                Index = index,
                Permissions = DoorPermissionFlags.None,
                Data = new()
                {
                    new ChamberData
                    {
                        ItemType = item,
                        SpawnPercent = 100f,
                        AmountToSpawn = 1
                    }
                }
            };

            return new[]
            {
                Chamber(0, ItemType.Medkit, "Main Chamber"),
                Chamber(1, ItemType.Painkillers, "Lower Shelf"),
                Chamber(2, ItemType.Adrenaline, "Upper Shelf"),
            };
        }
    }
}
#endif