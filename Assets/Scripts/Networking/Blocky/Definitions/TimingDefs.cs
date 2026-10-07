using System.Collections.Generic;

namespace Assets.Scripts.Networking.Blocky.Definitions
{
    public class TimingDefs : DefBase
    {
        public override void Register()
        {
            BlocklyServer.RegisterCategory("Timing", "#1f38f5", "");

            RegisterWait();
        }

        private void RegisterWait()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "timing_wait_for_frames",
                Category = "Timing",
                Color = "#1f38f5",
                Tooltip = "Waits for the specified number of frames, then runs the body.",
                Message = "Wait → %1 frames { %2 %3 }",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Next, BlockConnectionType.Previous },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.NumberField("FRAMES", 5),
                    BlockArg.Dummy(),
                    BlockArg.Statement("DO")
                }
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "timing_wait_for_seconds",
                Category = "Timing",
                Color = "#1f38f5",
                Tooltip = "Waits for the specified number of seconds, then runs the body.",
                Message = "Wait → %1 seconds { %2 %3 }",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Next, BlockConnectionType.Previous },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.NumberField("SECONDS", 5),
                    BlockArg.Dummy(),
                    BlockArg.Statement("DO")
                }
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "timing_wait_until_true",
                Category = "Timing",
                Color = "#1f38f5",
                Tooltip = "Waits until the input is true, then runs the body.",
                Message = "Wait Until → %1 { %2 %3 }",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Next, BlockConnectionType.Previous },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("WaitingInput"),
                    BlockArg.Dummy(),
                    BlockArg.Statement("DO")
                }
            });
        }
    }
}