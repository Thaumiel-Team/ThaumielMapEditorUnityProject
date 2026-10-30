#if UNITY_EDITOR
using Assets.Scripts.Enums;
using Assets.Scripts.Lockers;

namespace Assets.Scripts.Components.Objects.Lockers
{
    public class RifleRack : LockerObject
    {
        public override LockerType LockerType { get; set; } = LockerType.RifleRack;

        protected override LockerChamber[] CreateDefaultChambers()
        {
            static LockerChamber Chamber(uint index, ItemType item, string name) => new()
            {
                Name = name,
                Index = index,
                Permissions = DoorPermissionFlags.ArmoryLevelTwo,
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
                Chamber(0, ItemType.GunE11SR, "Main Chamber"),
                Chamber(1, ItemType.Ammo556x45, "Bullet 1"),
                Chamber(2, ItemType.Ammo556x45, "Bullet 2"),
                Chamber(3, ItemType.Ammo556x45, "Bullet 3"),
                Chamber(4, ItemType.Ammo556x45, "Bullet 4"),
                Chamber(5, ItemType.GrenadeHE, "Grenade 1"),
                Chamber(6, ItemType.GrenadeHE, "Grenade 2"),
            };
        }
    }
}
#endif