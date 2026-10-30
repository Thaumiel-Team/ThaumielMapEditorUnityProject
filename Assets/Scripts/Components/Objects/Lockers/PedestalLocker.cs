#if UNITY_EDITOR
using Assets.Scripts.Enums;
using Assets.Scripts.Lockers;

namespace Assets.Scripts.Components.Objects.Lockers
{
    public class PedestalLocker : LockerObject
    {
        public override LockerType LockerType { get; set; } = LockerType.Pedestal;

        protected override LockerChamber[] CreateDefaultChambers()
        {
            return new[]
            {
                new LockerChamber
                {
                    Name = "Chamber",
                    Index = 0,
                    Permissions = DoorPermissionFlags.ContainmentLevelTwo,
                    Data = new()
                    {
                        new ChamberData
                        {
                            ItemType = ItemType.SCP1509,
                            SpawnPercent = 16.6f,
                            AmountToSpawn = 1
                        },
                        new ChamberData
                        {
                            ItemType = ItemType.SCP207,
                            SpawnPercent = 16.6f,
                            AmountToSpawn = 1
                        },
                        new ChamberData
                        {
                            ItemType = ItemType.SCP018,
                            SpawnPercent = 16.6f,
                            AmountToSpawn = 1
                        },
                        new ChamberData
                        {
                            ItemType = ItemType.SCP1576,
                            SpawnPercent = 16.6f,
                            AmountToSpawn = 1
                        },
                        new ChamberData
                        {
                            ItemType = ItemType.SCP244b,
                            SpawnPercent = 16.6f,
                            AmountToSpawn = 1
                        },
                        new ChamberData
                        {
                            ItemType = ItemType.SCP500,
                            SpawnPercent = 16.6f,
                            AmountToSpawn = 1
                        }
                    }
                },
            };
        }
    }
}
#endif