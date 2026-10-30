#if UNITY_EDITOR
using Assets.Scripts.Enums;
using Assets.Scripts.Lockers;

namespace Assets.Scripts.Components.Objects.Lockers
{
    public class LargeLocker : LockerObject
    {
        public override LockerType LockerType { get; set; } = LockerType.LargeGun;

        protected override LockerChamber[] CreateDefaultChambers()
        {
            static LockerChamber Chamber(uint index, ItemType item, string name, DoorPermissionFlags perms) => new()
            {
                Name = name,
                Index = index,
                Permissions = perms,
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
                Chamber(0, ItemType.GunCrossvec, "Top Left", DoorPermissionFlags.ArmoryLevelTwo), // Large
                Chamber(1, ItemType.GunCrossvec, "Top Middle", DoorPermissionFlags.ArmoryLevelTwo), // Large
                Chamber(2, ItemType.GunCrossvec, "Top Right", DoorPermissionFlags.ArmoryLevelTwo), // Large
                Chamber(3, ItemType.GunCrossvec, "Bottom Left", DoorPermissionFlags.ArmoryLevelTwo), // Large
                Chamber(4, ItemType.GunCrossvec, "Bottom Right", DoorPermissionFlags.ArmoryLevelTwo), // Large
                Chamber(5, ItemType.Ammo9x19, "Middle Left", DoorPermissionFlags.ArmoryLevelOne), // Small
                Chamber(6, ItemType.Ammo9x19, "Center", DoorPermissionFlags.ArmoryLevelOne), // Small
                Chamber(7, ItemType.Ammo9x19, "Middle Right", DoorPermissionFlags.ArmoryLevelOne), // Small
                Chamber(8, ItemType.Ammo9x19, "Bottom Middle", DoorPermissionFlags.ArmoryLevelOne), // Small
                Chamber(9, ItemType.Coin, "Drawer", DoorPermissionFlags.None)
            };
        }
    }
}
#endif