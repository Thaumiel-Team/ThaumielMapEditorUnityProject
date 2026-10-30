#if UNITY_EDITOR
using Assets.Scripts.Enums;
using Assets.Scripts.Lockers;

namespace Assets.Scripts.Components.Objects.Lockers
{
    public class ExperimentalWeaponLocker : LockerObject
    {
        public override LockerType LockerType { get; set; } = LockerType.ExperimentalWeapon;

        protected override LockerChamber[] CreateDefaultChambers()
        {
            return new[]
            {
                new LockerChamber
                {
                    Name = "Chamber",
                    Index = 0,
                    Permissions = DoorPermissionFlags.ArmoryLevelThree,
                    Data = new()
                    {
                        new ChamberData
                        {
                            ItemType = ItemType.Jailbird,
                            SpawnPercent = 50f,
                            AmountToSpawn = 1
                        },
                        new ChamberData
                        {
                            ItemType = ItemType.ParticleDisruptor,
                            SpawnPercent = 50f,
                            AmountToSpawn = 1
                        }
                    }
                },
            };
        }
    }
}
#endif