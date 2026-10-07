using System.Collections.Generic;
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
                Category = "Player",
                Color = "#df6717",
                Tooltip = "Gets a property of the player.",
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
                Category = "Player",
                Color = "#df6717",
                Tooltip = "Gets a player by their id.",
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
                Category = "Player",
                Color = "#df6717",
                Tooltip = "Gets a player by their user id.",
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
                Category = "Player",
                Color = "#df6717",
                Tooltip = "Gets a player by their collider.",
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
                Category = "Player",
                Color = "#df6717",
                Tooltip = "List of all players currently on the server.",
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