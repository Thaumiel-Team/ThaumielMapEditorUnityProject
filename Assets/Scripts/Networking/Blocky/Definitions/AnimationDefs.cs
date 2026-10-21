using System.Collections.Generic;

namespace Assets.Scripts.Networking.Blocky.Definitions
{
    public class AnimationDefs : DefBase
    {
        private const string CategoryName = "Animation";
        private const string CategoryColor = "#00838F";

        public override void Register()
        {
            BlocklyServer.RegisterCategory(CategoryName, CategoryColor, "");

            RegisterPlayOnSchematic();
            RegisterPlay();
            RegisterPlayByIndex();
            RegisterPauseOnSchematic();
            RegisterPause();
            RegisterPauseByIndex();
        }

        private void RegisterPlayOnSchematic()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "animation_play_schematic",
                Category = CategoryName,
                Color = CategoryColor,
                Tooltip = "Play an animation on a specific schematic.",
                Explanation = "Calls AnimationController.Play(animationName) on the connected schematic.\nThe animation name must match an Animator state exactly.",
                Message = "Play Animation on Schematic: %1 → %2",
                Connections = BlockConnectionType.Previous | BlockConnectionType.Next,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Schematic", "Schematic"),
                    BlockArg.TextField("animationName", "Idle")
                }
            });
        }

        private void RegisterPlay()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "animation_play",
                Category = CategoryName,
                Color = CategoryColor,
                Tooltip = "Play an animation on the schematic that is currently executing.",
                Explanation = "Calls AnimationController.Play(animationName) on the executor's schematic.\nThe animation name must match an Animator state exactly.",
                Message = "Play Animation → %1",
                Connections = BlockConnectionType.Previous | BlockConnectionType.Next,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.TextField("animationName", "Idle")
                }
            });
        }

        private void RegisterPlayByIndex()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "animation_play_index",
                Category = CategoryName,
                Color = CategoryColor,
                Tooltip = "Play an animation on a specific animator index of a schematic.",
                Explanation = "Calls AnimationController.Play(animationName, animatorIndex).\nUse the index to target one of the schematic's animators.",
                Message = "Play Animation on Schematic: %1 → %2  Animator Index: %3",
                Connections = BlockConnectionType.Previous | BlockConnectionType.Next,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Schematic", "Schematic"),
                    BlockArg.TextField("animationName", "Idle"),
                    BlockArg.NumberField("animatorIndex", 0, 0)
                }
            });
        }

        private void RegisterPauseOnSchematic()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "animation_pause_schematic",
                Category = CategoryName,
                Color = CategoryColor,
                Tooltip = "Pause an animation on a specific schematic.",
                Explanation = "Pauses animationName on the connected schematic's AnimationController.",
                Message = "Pause Animation on Schematic: %1 → %2",
                Connections = BlockConnectionType.Previous | BlockConnectionType.Next,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Schematic", "Schematic"),
                    BlockArg.TextField("animationName", "Idle")
                }
            });
        }

        private void RegisterPause()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "animation_pause",
                Category = CategoryName,
                Color = CategoryColor,
                Tooltip = "Pause an animation on the schematic that is currently executing.",
                Explanation = "Pauses animationName on the executor's schematic AnimationController.",
                Message = "Pause Animation → %1",
                Connections = BlockConnectionType.Previous | BlockConnectionType.Next,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.TextField("animationName", "Idle")
                }
            });
        }

        private void RegisterPauseByIndex()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "animation_pause_index",
                Category = CategoryName,
                Color = CategoryColor,
                Tooltip = "Pause an animation on a specific animator index of a schematic.",
                Explanation = "Pauses animationName at animatorIndex on the connected schematic.",
                Message = "Pause Animation on Schematic: %1 → %2  Animator Index: %3",
                Connections = BlockConnectionType.Previous | BlockConnectionType.Next,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Schematic", "Schematic"),
                    BlockArg.TextField("animationName", "Idle"),
                    BlockArg.NumberField("animatorIndex", 0, 0)
                }
            });
        }
    }
}
