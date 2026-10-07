using System.Collections.Generic;
using Assets.Scripts.Enums;

namespace Assets.Scripts.Networking.Blocky.Definitions
{
    public class ObjectDefs : DefBase
    {
        public override void Register()
        {
            BlocklyServer.RegisterCategory("Actions", "#1565C0", "");

            RegisterRunMethod();
            RegisterPlayAnimation();
            RegisterPlayAudio();
            RegisterSendCassie();
            RegisterRunCommand();
            RegisterGiveEffect();
            RegisterRemoveEffect();
            RegisterGiveItem();
            RegisterRemoveItem();
            RegisterWarhead();
        }

        private void RegisterRunMethod()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "run_method",
                Category = "Actions",
                Color = "#1565C0",
                Tooltip = "Runs a method with reflection",
                Message = "Call Method: %1  arg1: %2  arg2: %3  arg3: %4  arg4: %5",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.TextField("METHOD", "Namespace.Class.Method"),
                    BlockArg.Value("ARG1"),
                    BlockArg.Value("ARG2"),
                    BlockArg.Value("ARG3"),
                    BlockArg.Value("ARG4"),
                }
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "run_method_instance",
                Category = "Actions",
                Color = "#1565C0",
                Tooltip = "Runs an instance method with reflection",
                Message = "on: %1 Call Method: %2 arg1: %3  arg2: %4  arg3: %5  arg4: %6",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("INSTANCE"),
                    BlockArg.TextField("METHOD", "Namespace.Class.Method"),
                    BlockArg.Value("ARG1"),
                    BlockArg.Value("ARG2"),
                    BlockArg.Value("ARG3"),
                    BlockArg.Value("ARG4"),
                }
            });
        }

        private void RegisterPlayAnimation()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "play_animation",
                Category = "Actions",
                Color = "#1565C0",
                Tooltip = "Play an Animator animation by name",
                Message = "Play Animation → %1",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.TextField("animationName", "Idle")
                }
            });
        }

        private void RegisterPlayAudio()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "play_audio",
                Category = "Actions",
                Color = "#1565C0",
                Tooltip = "Play an audio clip",
                Message = "Play Audio  File: %1  Volume: %2  Min Dist: %3  Max Dist: %4  Spatial: %5",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.TextField("path", ""),
                    BlockArg.NumberField("volume",   1.0,  0.0, 1.0),
                    BlockArg.NumberField("minDistance", 1.0,  0.0),
                    BlockArg.NumberField("maxDistance", 20.0, 0.0),
                    BlockArg.Checkbox("isSpatial", false)
                }
            });
        }

        private void RegisterSendCassie()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "send_cassie",
                Category = "Actions",
                Color = "#1565C0",
                Tooltip = "Broadcast a CASSIE announcement",
                Message = "Send CASSIE  Message: %1  Subtitles: %2  Background: %3  Priority: %4  Glitch: %5",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.TextField("message", ""),
                    BlockArg.TextField("customSubtitles", ""),
                    BlockArg.Checkbox("playBackground", false),
                    BlockArg.NumberField("priority", 0.0),
                    BlockArg.NumberField("glitchScale", 0.0, 0.0, 1.0)
                }
            });
        }

        private void RegisterRunCommand()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "run_command",
                Category = "Actions",
                Color = "#1565C0",
                Tooltip = "Execute a server / client / console command",
                Message = "Run %1 Command → %2",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Dropdown("commandType", ("Remote Admin", "RemoteAdmin"), ("Client", "Client"), ("Console", "Console")),
                    BlockArg.TextField("command", "")
                }
            });
        }

        private void RegisterGiveEffect()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "give_effect",
                Category = "Actions",
                Color = "#1565C0",
                Tooltip = "Apply a status effect to the player",
                Message = "Give Effect: %1  Intensity: %2  Duration: %3",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Dropdown("effect", EnumOptions<EffectType>()),
                    BlockArg.NumberField("intensity", 1, 1, 255),
                    BlockArg.NumberField("duration", 5, 0)
                }
            });
        }

        private void RegisterRemoveEffect()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "remove_effect",
                Category = "Actions",
                Color = "#1565C0",
                Tooltip = "Remove a status effect from the player",
                Message = "Remove Effect: %1  Intensity: %2",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Dropdown("effect", EnumOptions<EffectType>()),
                    BlockArg.NumberField("intensity", 1, 1, 255)
                }
            });
        }

        private void RegisterGiveItem()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "give_item",
                Category = "Actions",
                Color = "#1565C0",
                Tooltip = "Give the player one or more items",
                Message = "Give Item: %1  ×%2",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Dropdown("item", EnumOptions<ItemType>()),
                    BlockArg.NumberField("count", 1, 1)
                }
            });
        }

        private void RegisterRemoveItem()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "remove_item",
                Category = "Actions",
                Color = "#1565C0",
                Tooltip = "Remove one or more items from the player",
                Message = "Remove Item: %1  ×%2",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Dropdown("item", EnumOptions<ItemType>()),
                    BlockArg.NumberField("count", 1, 1)
                }
            });
        }

        private void RegisterWarhead()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "warhead",
                Category = "Actions",
                Color = "#1565C0",
                Tooltip = "Control the Alpha Warhead",
                Message = "Warhead: %1  Suppress Subtitles: %2",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Dropdown("action", EnumOptions<WarheadAction>()),
                    BlockArg.Checkbox("suppressSubtitles", false)
                }
            });
        }
    }
}