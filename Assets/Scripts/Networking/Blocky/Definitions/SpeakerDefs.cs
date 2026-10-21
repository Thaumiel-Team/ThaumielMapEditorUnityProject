using System.Collections.Generic;

namespace Assets.Scripts.Networking.Blocky.Definitions
{
    public class SpeakerDefs : DefBase
    {
        private enum Properties
        {
            Rotation,
            Scale,
            Position,
            Player,
            Volume,
            IsSpatial,
            MinDistance,
            MaxDistance,
            Loop,
            Id,
            Path,
            IsPlaying
        }

        public override void Register()
        {
            BlocklyServer.RegisterCategory("Speaker", "#B71C1C", "");

            RegisterCreateSpeaker();
            RegisterSetVolume();
            RegisterSetIsSpatial();
            RegisterSetMinDistance();
            RegisterSetMaxDistance();
            RegisterSetLoop();
            RegisterSetPath();
            RegisterPlay();
            RegisterPause();
            RegisterUnpause();
            RegisterGetProperty();
        }

        private void RegisterGetProperty()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "get_speaker_property",
                PropertyTypes = new Dictionary<string, string>
                {
                    ["Rotation"] = "Quaternion",
                    ["Scale"] = "Vector3",
                    ["Position"] = "Vector3",
                    ["Player"] = "Player",
                    ["Volume"] = "Number",
                    ["IsSpatial"] = "Boolean",
                    ["MinDistance"] = "Number",
                    ["MaxDistance"] = "Number",
                    ["Loop"] = "Boolean",
                    ["Path"] = "String",
                    ["IsPlaying"] = "Boolean",
                },
                Category = "Speaker",
                Color = "#B71C1C",
                Tooltip = "Gets a property of the speaker.",
                Explanation = "Reads one Property (volume, path, loop state, and more) from the Speaker object.",
                Message = "Get %2 of Speaker: %1",
                Connections = BlockConnectionType.Output,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Speaker"),
                    BlockArg.Dropdown("Property", EnumOptions<Properties>())
                }
            });
        }

        private void RegisterCreateSpeaker()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "speaker_create",
                OutputType = "Speaker",
                Category = "Speaker",
                Color = "#B71C1C",
                Tooltip = "Create a new SpeakerObject with a given name.",
                Explanation = "Spawns a speaker and outputs it. Set path and volume before playing.",
                Message = "Create Speaker  Name: %1",
                Connections = BlockConnectionType.Output,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.TextField("name", "MySpeaker")
                }
            });
        }

        private void RegisterSetVolume()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "speaker_set_volume",
                Category = "Speaker",
                Color = "#B71C1C",
                Tooltip = "Set the volume of the speaker as a percentage from 0 to 100.",
                Explanation = "0 is silent, 100 is full. Values outside the range are clamped by the game.",
                Message = "Set Volume of Speaker: %1 → %2",
                Connections = BlockConnectionType.Previous | BlockConnectionType.Next,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Speaker"),
                    BlockArg.NumberField("volume", 100.0, 0.0, 100.0)
                }
            });
        }

        private void RegisterSetIsSpatial()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "speaker_set_is_spatial",
                Category = "Speaker",
                Color = "#B71C1C",
                Tooltip = "Toggle whether the speaker uses spatial audio. When enabled, volume and panning are affected by the listener's position.",
                Explanation = "Spatial sound fades with distance and pans by direction.\nDisable for global music and announcements.",
                Message = "Set Spatial of Speaker: %1 → %2",
                Connections = BlockConnectionType.Previous | BlockConnectionType.Next,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Speaker"),
                    BlockArg.Checkbox("isSpatial", false)
                }
            });
        }

        private void RegisterSetMinDistance()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "speaker_set_min_distance",
                Category = "Speaker",
                Color = "#B71C1C",
                Tooltip = "Set the minimum distance at which spatial audio begins to attenuate. Within this range audio plays at full volume.",
                Explanation = "Inside this radius there is no falloff.\nRaise it for large rooms so nearby audio stays loud.",
                Message = "Set Min Distance of Speaker: %1 → %2",
                Connections = BlockConnectionType.Previous | BlockConnectionType.Next,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Speaker"),
                    BlockArg.NumberField("minDistance", 1.0)
                }
            });
        }

        private void RegisterSetMaxDistance()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "speaker_set_max_distance",
                Category = "Speaker",
                Color = "#B71C1C",
                Tooltip = "Set the maximum distance at which the speaker can be heard. Beyond this range audio is inaudible.",
                Explanation = "Sound fades between min and max distance, then cuts out.\nLower it to contain audio to one area.",
                Message = "Set Max Distance of Speaker: %1 → %2",
                Connections = BlockConnectionType.Previous | BlockConnectionType.Next,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Speaker"),
                    BlockArg.NumberField("maxDistance", 10.0)
                }
            });
        }

        private void RegisterSetLoop()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "speaker_set_loop",
                Category = "Speaker",
                Color = "#B71C1C",
                Tooltip = "Toggle whether the speaker loops its audio playback.",
                Explanation = "Looping restarts the clip automatically for ambience and alarms.",
                Message = "Set Loop of Speaker: %1 → %2",
                Connections = BlockConnectionType.Previous | BlockConnectionType.Next,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Speaker"),
                    BlockArg.Checkbox("loop", false)
                }
            });
        }

        private void RegisterSetPath()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "speaker_set_path",
                Category = "Speaker",
                Color = "#B71C1C",
                Tooltip = "Set the file path of the audio file this speaker will play. Used as the default path when Play is called with no argument.",
                Explanation = "The default clip for this speaker. Play with an empty file uses this path.",
                Message = "Set Path of Speaker: %1 → %2",
                Connections = BlockConnectionType.Previous | BlockConnectionType.Next,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Speaker"),
                    BlockArg.TextField("path", "")
                }
            });
        }

        private void RegisterPlay()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "speaker_play",
                Category = "Speaker",
                Color = "#B71C1C",
                Tooltip = "Begin audio playback. If a filepath is provided it overrides the speaker's default Path.",
                Explanation = "Starts the clip, optionally swapping to filepath first.\nCall Pause to halt without resetting.",
                Message = "Play Speaker: %1  File: %2",
                Connections = BlockConnectionType.Previous | BlockConnectionType.Next,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Speaker"),
                    BlockArg.TextField("filepath", "")
                }
            });
        }

        private void RegisterPause()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "speaker_pause",
                Category = "Speaker",
                Color = "#B71C1C",
                Tooltip = "Pause audio playback on this speaker.",
                Explanation = "Freezes the clip in place. Resume continues from the same spot.",
                Message = "Pause Speaker: %1",
                Connections = BlockConnectionType.Previous | BlockConnectionType.Next,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Speaker")
                }
            });
        }

        private void RegisterUnpause()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "speaker_unpause",
                Category = "Speaker",
                Color = "#B71C1C",
                Tooltip = "Resume audio playback on this speaker if it was previously paused.",
                Explanation = "Continues a paused clip. Does nothing when already playing.",
                Message = "Resume Speaker: %1",
                Connections = BlockConnectionType.Previous | BlockConnectionType.Next,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Speaker")
                }
            });
        }
    }
}