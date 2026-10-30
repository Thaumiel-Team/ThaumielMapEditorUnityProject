#if UNITY_EDITOR
using Assets.Scripts.Enums;
using Assets.Scripts.Lockers;

namespace Assets.Scripts.Components.Objects.Lockers
{
    public class StandardLocker : LockerObject
    {
        public override LockerType LockerType { get; set; } = LockerType.Misc;

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
                Chamber(0, ItemType.KeycardJanitor, "Top Left"),
                Chamber(1, ItemType.KeycardJanitor, "Top Middle"),
                Chamber(2, ItemType.KeycardJanitor, "Top Right"),
                Chamber(3, ItemType.KeycardJanitor, "Bottom Left"),
                Chamber(4, ItemType.KeycardJanitor, "Bottom Middle"),
                Chamber(5, ItemType.KeycardJanitor, "Bottom Right"),
            };
        }
    }
}
#endif