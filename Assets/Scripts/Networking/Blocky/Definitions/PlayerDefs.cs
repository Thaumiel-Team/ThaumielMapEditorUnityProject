using System.Collections.Generic;
using System.Numerics;
using Assets.Scripts.Enums;

namespace Assets.Scripts.Networking.Blocky.Definitions
{
    public class PlayerDefs : DefBase
    {
        private enum Properties
        {
            Rotation,
            LookRotation,
            Scale,
            Position,
            Role,
            RoleBase,
            PlayerId,
            Name,
            DisplayName,
            LogName,
            UserID,
            IsAlive,
            GameObject,
            ReferenceHub,
            ReadyList,
            IsNpc,
            IsHost,
            IsPlayer,
            IsDummy,
            NetworkId,
            IsDestroyed,
            IsReady,
            LifeId,
            CustomInfo,
            InfoArea,
            Health,
            MaxHealth,
            ArtificialHealth,
            MaxArtificialHealth,
            HumeShield,
            MaxHumeShield,
            HumeShieldRegenRate,
            HumeShieldRegenCooldown,
            Gravity,
            RemoteAdminAccess,
            DoNotTrack,
            IsOverwatchEnabled,
            CurrentlySpectating,
            CurrentSpectators,
            IsSpectatable,
            CurrentItem,
            ActiveEffects,
            Room,
            Zone,
            Items,
            Ammo,
            GroupColor,
            GroupName,
            UserGroup,
            PermissionsGroupName,
            UnitId,
            HasReservedSlot,
            Velocity,
            IsInventoryFull,
            IsWithoutItems,
            IsOutOfAmmo,
            IsDisarmed,
            IsMuted,
            IsIntercomMuted,
            IsUsingRadio,
            IsSpeaking,
            IsGlobalModerator,
            IsNorthwoodStaff,
            IsExiledContributer,
            IsTMEContributer,
            IsBypassEnabled,
            IsGodModeEnabled,
            IsNoclipEnabled,
            DisarmedBy,
            Team,
            Faction,
            IsSCP,
            IsHuman,
            IsNTF,
            IsChaos,
            IsTutorial,
            StaminaRemaining,
            Emotion,
        }
        
        public override void Register()
        {
            BlocklyServer.RegisterCategory("Player", "#df6717", "");

            RegisterGetProperty();
            RegisterGravity();
            RegisterGetPlayer();
            RegisterSetRole();
            RegisterItems();
            RegisterHealth();
            RegisterGroup();
            RegisterMessages();
            RegisterLists();
            RegisterEffects();
        }

        private void RegisterGetProperty()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "get_player_property",
                PropertyTypes = new Dictionary<string, string>
                {
                    ["Rotation"] = "Quaternion",
                    ["LookRotation"] = "Quaternion",
                    ["Scale"] = "Vector3",
                    ["Position"] = "Vector3",
                    ["Gravity"] = "Vector3",
                    ["Velocity"] = "Vector3",
                    ["Name"] = "String",
                    ["DisplayName"] = "String",
                    ["LogName"] = "String",
                    ["UserID"] = "String",
                    ["CustomInfo"] = "String",
                    ["GroupName"] = "String",
                    ["PermissionsGroupName"] = "String",
                    ["PlayerId"] = "Number",
                    ["NetworkId"] = "Number",
                    ["LifeId"] = "Number",
                    ["UnitId"] = "Number",
                    ["Health"] = "Number",
                    ["MaxHealth"] = "Number",
                    ["ArtificialHealth"] = "Number",
                    ["MaxArtificialHealth"] = "Number",
                    ["HumeShield"] = "Number",
                    ["MaxHumeShield"] = "Number",
                    ["HumeShieldRegenRate"] = "Number",
                    ["HumeShieldRegenCooldown"] = "Number",
                    ["StaminaRemaining"] = "Number",
                    ["IsAlive"] = "Boolean",
                    ["IsNpc"] = "Boolean",
                    ["IsHost"] = "Boolean",
                    ["IsPlayer"] = "Boolean",
                    ["IsDummy"] = "Boolean",
                    ["IsDestroyed"] = "Boolean",
                    ["IsReady"] = "Boolean",
                    ["RemoteAdminAccess"] = "Boolean",
                    ["DoNotTrack"] = "Boolean",
                    ["IsOverwatchEnabled"] = "Boolean",
                    ["IsSpectatable"] = "Boolean",
                    ["IsInventoryFull"] = "Boolean",
                    ["IsWithoutItems"] = "Boolean",
                    ["IsOutOfAmmo"] = "Boolean",
                    ["IsDisarmed"] = "Boolean",
                    ["IsMuted"] = "Boolean",
                    ["IsIntercomMuted"] = "Boolean",
                    ["IsUsingRadio"] = "Boolean",
                    ["IsSpeaking"] = "Boolean",
                    ["IsGlobalModerator"] = "Boolean",
                    ["IsNorthwoodStaff"] = "Boolean",
                    ["IsExiledContributer"] = "Boolean",
                    ["IsTMEContributer"] = "Boolean",
                    ["IsBypassEnabled"] = "Boolean",
                    ["IsGodModeEnabled"] = "Boolean",
                    ["IsNoclipEnabled"] = "Boolean",
                    ["HasReservedSlot"] = "Boolean",
                    ["IsSCP"] = "Boolean",
                    ["IsHuman"] = "Boolean",
                    ["IsNTF"] = "Boolean",
                    ["IsChaos"] = "Boolean",
                    ["IsTutorial"] = "Boolean",
                    ["DisarmedBy"] = "Player",
                    ["CurrentlySpectating"] = "Player",
                    ["Items"] = "Array",
                    ["Ammo"] = "Array",
                    ["ActiveEffects"] = "Array",
                    ["CurrentSpectators"] = "Array",
                },
                Category = "Player",
                Color = "#df6717",
                Tooltip = "Gets a property of the player.",
                Explanation = "Reads a Property from the Player object.",
                Message = "Get %2 of Player: %1",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Player"),
                    BlockArg.Dropdown("Property", EnumOptions<Properties>())
                }
            });
        }

        private void RegisterGravity()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "set_player_gravity",
                Category = "Player",
                Color = "#df6717",
                Tooltip = "Sets the players gravity. Default: (0, -19.86, 0)",
                Explanation = "Overwrites gravity for this player only. x, y, z are the pull direction and strength.\nRestore the default to fix floaty or heavy movement.\nDefault is 0, -19.6, 0",
                Message = "Set Gravity of Player: %1 → x: %2  y: %3  z: %4",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Player"),
                    BlockArg.NumberField("x", 0),
                    BlockArg.NumberField("y", -19.86),
                    BlockArg.NumberField("z", 0)
                }
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "set_player_scale",
                Category = "Player",
                Color = "#df6717",
                Tooltip = "Sets the players scale.",
                Explanation = "Resizes the player model by x, y, z multipliers. (1, 1, 1) is normal size.",
                Message = "Set Scale of Player: %1 → x: %2  y: %3  z: %4",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Player"),
                    BlockArg.NumberField("x", 1),
                    BlockArg.NumberField("y", 1),
                    BlockArg.NumberField("z", 1)
                }
            });
        }

        private void RegisterGetPlayer()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "get_player_by_id",
                OutputType = "Player",
                Category = "Player",
                Color = "#df6717",
                Tooltip = "Gets a player by their id.",
                Explanation = "Finds the online player with this numeric id.\nOutputs nothing when they are offline.",
                Message = "Get Player by ID: %1",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.NumberField("PLAYER_ID")
                }
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "get_player_by_userid",
                OutputType = "Player",
                Category = "Player",
                Color = "#df6717",
                Tooltip = "Gets a player by their user id.",
                Explanation = "Finds the online player with this user id string.",
                Message = "Get Player by User ID: %1",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.TextField("USER_ID")
                }
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "get_player_by_collider",
                OutputType = "Player",
                Category = "Player",
                Color = "#df6717",
                Tooltip = "Gets a player by their collider.",
                Explanation = "Turns a trigger collider into its owning player.\nUse it inside trigger events with the PARAM variable.",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Message = "Get Player by Collider: %1",
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Collider")
                }
            });
        }

        private void RegisterSetRole()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "set_player_role",
                Category = "Player",
                Color = "#df6717",
                Tooltip = "Sets a players role.",
                Explanation = "Changes the player to ROLE, keeping position when KEEP_POSITION is true.",
                Message = "Set Role of Player: %1 → %2  Keep Position: %3",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Player"),
                    BlockArg.Dropdown("ROLE", EnumOptions<RoleTypeId>()),
                    BlockArg.Checkbox("KEEP_POSITION", true)
                }
            });
        }

        private void RegisterItems()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "give_player_item",
                Category = "Player",
                Color = "#df6717",
                Tooltip = "Gives the player a item.",
                Explanation = "Adds one ITEM to the player. When the inventory is full and DROP_IF_FULL is true, it drops at their feet instead.",
                Message = "Give Player: %1  Item: %2  Drop if Full: %3",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Player"),
                    BlockArg.Dropdown("ITEM", EnumOptions<ItemType>()),
                    BlockArg.Checkbox("DROP_IF_FULL", true)
                }
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "give_player_items",
                Category = "Player",
                Color = "#df6717",
                Tooltip = "Gives the player multiple items.",
                Explanation = "Adds COUNT copies of ITEM. DROP_IF_FULL controls overflow like the single version.",
                Message = "Give Player: %1  Item: %2  ×%3  Drop if Full: %4",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Player"),
                    BlockArg.Dropdown("ITEM", EnumOptions<ItemType>()),
                    BlockArg.NumberField("COUNT", 1),
                    BlockArg.Checkbox("DROP_IF_FULL", true)
                }
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "remove_player_item",
                Category = "Player",
                Color = "#df6717",
                Tooltip = "Removes the item from a player.",
                Explanation = "Takes ITEM away when held. Does nothing when the player lacks it.",
                Message = "Remove Item: %2 from Player: %1",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Player"),
                    BlockArg.Dropdown("ITEM", EnumOptions<ItemType>()),
                }
            });
        }

        private void RegisterHealth()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "set_player_health",
                Category = "Player",
                Color = "#df6717",
                Tooltip = "Sets the players health.",
                Explanation = "Overwrites current hit points. Values above max are capped by the game.",
                Message = "Set Health of Player: %1 → %2",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Player"),
                    BlockArg.NumberField("HEALTH"),
                }
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "set_player_max_health",
                Category = "Player",
                Color = "#df6717",
                Tooltip = "Sets the players max health.",
                Explanation = "Changes the health ceiling. Current health stays unless it now exceeds the max.",
                Message = "Set Max Health of Player: %1 → %2",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Player"),
                    BlockArg.NumberField("MAX_HEALTH"),
                }
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "set_player_artificial_health",
                Category = "Player",
                Color = "#df6717",
                Tooltip = "Sets the players artificial health.",
                Explanation = "Overwrites the AHP shield pool, the extra bar above health.",
                Message = "Set Artificial Health of Player: %1 → %2",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Player"),
                    BlockArg.NumberField("ARTIFICIAL_HEALTH"),
                }
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "set_player_max_artificial_health",
                Category = "Player",
                Color = "#df6717",
                Tooltip = "Sets the players max artificial health.",
                Explanation = "Changes the AHP ceiling without touching the current amount.",
                Message = "Set Max Artificial Health of Player: %1 → %2",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Player"),
                    BlockArg.NumberField("MAX_ARTIFICIAL_HEALTH"),
                }
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "set_player_hume_shield",
                Category = "Player",
                Color = "#df6717",
                Tooltip = "Sets the players hume shield.",
                Explanation = "Overwrites the Hume shield energy value.",
                Message = "Set Hume Shield of Player: %1 → %2",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Player"),
                    BlockArg.NumberField("HUME_SHIELD"),
                }
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "set_player_max_hume_shield",
                Category = "Player",
                Color = "#df6717",
                Tooltip = "Sets the players max hume shield.",
                Explanation = "Changes the Hume shield ceiling.",
                Message = "Set Max Hume Shield of Player: %1 → %2",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Player"),
                    BlockArg.NumberField("MAX_HUME_SHIELD"),
                }
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "set_player_hume_shield_regen_rate",
                Category = "Player",
                Color = "#df6717",
                Tooltip = "Sets the players hume shield regen rate.",
                Explanation = "Controls how fast the shield refills per second.\nZero disables regeneration.",
                Message = "Set Hume Shield Regen Rate of Player: %1 → %2",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Player"),
                    BlockArg.NumberField("HUME_SHIELD_REGEN_RATE"),
                }
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "set_player_hume_shield_regen_cooldown",
                Category = "Player",
                Color = "#df6717",
                Tooltip = "Sets the players hume shield regen cooldown.",
                Explanation = "Delay in seconds after damage before refilling starts.",
                Message = "Set Hume Shield Regen Cooldown of Player: %1 → %2",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Player"),
                    BlockArg.NumberField("HUME_SHIELD_REGEN_COOLDOWN"),
                }
            });
        }

        private void RegisterGroup()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "set_player_group_name",
                Category = "Player",
                Color = "#df6717",
                Tooltip = "Sets the players group name.",
                Explanation = "Changes the permission group label shown for the player.",
                Message = "Set Group Name of Player: %1 → %2",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Player"),
                    BlockArg.TextField("GROUP_NAME")
                }
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "set_player_group_color",
                Category = "Player",
                Color = "#df6717",
                Tooltip = "Sets the players group color.",
                Explanation = "Recolors the group tag. Use a color block or hex value.",
                Message = "Set Group Color of Player: %1 → %2",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Player"),
                    BlockArg.TextField("GROUP_COLOR")
                }
            });
        }

        private void RegisterMessages()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "send_player_broadcast",
                Category = "Player",
                Color = "#df6717",
                Tooltip = "Sends a broadcast message to the player.",
                Explanation = "Shows MESSAGE center-screen for DURATION seconds, visible to this player only.",
                Message = "Send Broadcast to Player: %1 → Broadcast Message: %2 Duration: %3",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Player"),
                    BlockArg.TextField("MESSAGE"),
                    BlockArg.NumberField("DURATION", 5)
                }
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "send_player_hint",
                Category = "Player",
                Color = "#df6717",
                Tooltip = "Sends a hint message to the player.",
                Explanation = "Shows a small MESSAGE hint for DURATION seconds, visible to this player only.",
                Message = "Send Hint to Player: %1 → Hint Message: %2 Duration: %3",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Player"),
                    BlockArg.TextField("MESSAGE"),
                    BlockArg.NumberField("DURATION", 5)
                }
            });
        }

        private void RegisterLists()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "player_list",
                OutputType = "Array",
                Category = "Player",
                Color = "#df6717",
                Tooltip = "List of all players currently on the server.",
                Explanation = "Outputs every online player at once. Plug it into foreach to affect everyone.",
                Message = "Player List",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = null
            });
        }

        private void RegisterEffects()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "give_player_effect",
                Category = "Player",
                Color = "#df6717",
                Tooltip = "Gives the player a status effect.",
                Explanation = "Applies effect at intensity for duration seconds. When addDuration is true, repeats extend instead of replacing.",
                Message = "Give Player: %1  Effect: %2  Intensity: %3  Duration: %4  Add Duration: %5",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Player"),
                    BlockArg.Dropdown("effect", EnumOptions<PlayerEffects>()),
                    BlockArg.NumberField("intensity", 1, 1, 255),
                    BlockArg.NumberField("duration", 5, 0),
                    BlockArg.Checkbox("addDuration", false)
                }
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "remove_player_effect",
                Category = "Player",
                Color = "#df6717",
                Tooltip = "Removes a status effect from the player.",
                Explanation = "Clears effect early, ending it immediately.",
                Message = "Remove Effect: %2 from Player: %1",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Player"),
                    BlockArg.Dropdown("effect", EnumOptions<PlayerEffects>())
                }
            });
        }
    }
}