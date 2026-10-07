using System.Collections.Generic;
using Assets.Scripts.Enums;

namespace Assets.Scripts.Networking.Blocky.Definitions
{
    public class DoorDefs : DefBase
    {
        private enum Properties
        {
            DoorType,
            Permissions,
            IsOpen,
            IsLocked,
            RequireAllPermissions,
            Bypass2176,
            Name,
            Position,
            Rotation,
            Scale,
            ExactState,
        }

        public override void Register()
        {
            BlocklyServer.RegisterCategory("Door", "#f14f2a", "");

            RegisterFindDoorByName();
            RegisterCreateDoor();
            RegisterSetDoorPerms();
            RegisterOpenDoor();
            RegisterCloseDoor();
            RegisterLockDoor();
            RegisterUnlockDoor();
            RegisterDoorIsOpen();
            RegisterDoorIsClosed();
            RegisterDoorIsOpening();
            RegisterGetProperty();
        }

        private void RegisterGetProperty()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "get_door_property",
                Category = "Door",
                Color = "#f14f2a",
                Tooltip = "Gets a property of the door.",
                Message = "Get %2 of Door: %1",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Door"),
                    BlockArg.Dropdown("Property", EnumOptions<Properties>())
                }
            });
        }

        private void RegisterFindDoorByName()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "door_find_by_name",
                Category = "Door",
                Color = "#f14f2a",
                Tooltip = "Finds a door in the schematic by its name. The search is case-insensitive and the result is cached.",
                Message = "Find Door  Name: %1",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.TextField("name", "MyDoor")
                }
            });
        }

        private void RegisterCreateDoor()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "door_create",
                Category = "Door",
                Color = "#f14f2a",
                Tooltip = "Creates a new door and spawns it into the schematic.",
                Message = "Create Door  Type: %1  Permissions: %2  Open: %3  Locked: %4  Require All Perms: %5  Bypass 2176: %6",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Dropdown("doorType", EnumOptions<DoorType>()),
                    BlockArg.Dropdown("doorPermissionFlags", EnumOptions<DoorPermissionFlags>()),
                    BlockArg.Checkbox("isOpen", false),
                    BlockArg.Checkbox("isLocked", false),
                    BlockArg.Checkbox("requireAllPermissions", false),
                    BlockArg.Checkbox("bypass2176", false)
                }
            });
        }

        private void RegisterSetDoorPerms()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "door_set_permissions",
                Category = "Door",
                Color = "#f14f2a",
                Tooltip = "Set the keycard permission flags required to interact with this door.",
                Message = "Set Permissions of Door: %1 → %2",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Door"),
                    BlockArg.Dropdown("perms", EnumOptions<DoorPermissionFlags>())
                }
            });
        }

        private void RegisterOpenDoor()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "door_open",
                Category = "Door",
                Color = "#f14f2a",
                Tooltip = "Opens this door.",
                Message = "Open Door: %1",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Door")
                }
            });
        }

        private void RegisterCloseDoor()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "door_close",
                Category = "Door",
                Color = "#f14f2a",
                Tooltip = "Closes this door.",
                Message = "Close Door: %1",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Door")
                }
            });
        }

        private void RegisterLockDoor()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "door_lock",
                Category = "Door",
                Color = "#f14f2a",
                Tooltip = "Locks this door, preventing it from being opened.",
                Message = "Lock Door: %1",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Door")
                }
            });
        }

        private void RegisterUnlockDoor()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "door_unlock",
                Category = "Door",
                Color = "#f14f2a",
                Tooltip = "Unlocks this door, allowing it to be opened.",
                Message = "Unlock Door: %1",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Door")
                }
            });
        }

        private void RegisterDoorIsOpen()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "door_is_open",
                Category = "Door",
                Color = "#f14f2a",
                Tooltip = "Returns true if the door is currently open.",
                Message = "Is Door Open: %1",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Door")
                }
            });
        }

        private void RegisterDoorIsClosed()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "door_is_closed",
                Category = "Door",
                Color = "#f14f2a",
                Tooltip = "Returns true if the door is currently closed (not open).",
                Message = "Is Door Closed: %1",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Door")
                }
            });
        }

        private void RegisterDoorIsOpening()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "door_is_opening",
                Category = "Door",
                Color = "#f14f2a",
                Tooltip = "Returns true while the door is animating between open and closed (opening or closing).",
                Message = "Is Door Opening: %1",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Door")
                }
            });
        }
    }
}