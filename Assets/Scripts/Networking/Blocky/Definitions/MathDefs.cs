using System.Collections.Generic;

namespace Assets.Scripts.Networking.Blocky.Definitions
{
    public class MathDefs : DefBase
    {
        public override void Register()
        {
            BlocklyServer.RegisterCategory("Numbers", "#5B67A5", "");

            RegisterMinMax();
            RegisterClamp();
            RegisterLerp();
            RegisterMoveTowards();
            RegisterRandomRange();
            RegisterSign();
            RegisterRoundToInt();
            RegisterRoundDigits();
            RegisterPingPong();
            RegisterConstants();
            RegisterChecks();
        }

        private void RegisterMinMax()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "math_min",
                Category = "Numbers",
                Color = "#5B67A5",
                Tooltip = "Returns the smaller of the two numbers.",
                Message = "min( %1 , %2 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("A", "Number"),
                    BlockArg.Value("B", "Number")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "math_max",
                Category = "Numbers",
                Color = "#5B67A5",
                Tooltip = "Returns the larger of the two numbers.",
                Message = "max( %1 , %2 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("A", "Number"),
                    BlockArg.Value("B", "Number")
                },
            });
        }

        private void RegisterClamp()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "math_clamp01",
                Category = "Numbers",
                Color = "#5B67A5",
                Tooltip = "Clamps the number between 0 and 1.",
                Message = "clamp01( %1 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("VALUE", "Number")
                },
            });
        }

        private void RegisterLerp()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "math_lerp_float",
                Category = "Numbers",
                Color = "#5B67A5",
                Tooltip = "Interpolates between two numbers by the given amount (0 to 1).",
                Message = "lerp( %1 , %2 , %3 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("A", "Number"),
                    BlockArg.Value("B", "Number"),
                    BlockArg.Value("T", "Number")
                },
            });
        }

        private void RegisterMoveTowards()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "math_move_towards",
                Category = "Numbers",
                Color = "#5B67A5",
                Tooltip = "Moves the current value towards the target, never moving more than the max delta.",
                Message = "moveTowards( %1 , %2 , %3 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("CURRENT", "Number"),
                    BlockArg.Value("TARGET", "Number"),
                    BlockArg.Value("MAXDELTA", "Number")
                },
            });
        }

        private void RegisterRandomRange()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "math_random_int_range",
                Category = "Numbers",
                Color = "#5B67A5",
                Tooltip = "Returns a random integer from min (inclusive) to max (exclusive).",
                Message = "random integer from %1 to %2",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("MIN", "Number"),
                    BlockArg.Value("MAX", "Number")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "math_random_float_range",
                Category = "Numbers",
                Color = "#5B67A5",
                Tooltip = "Returns a random float between min and max.",
                Message = "random float from %1 to %2",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("MIN", "Number"),
                    BlockArg.Value("MAX", "Number")
                },
            });
        }

        private void RegisterSign()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "math_sign",
                Category = "Numbers",
                Color = "#5B67A5",
                Tooltip = "Returns 1 if the number is positive, -1 if negative, and 0 if it is zero.",
                Message = "sign( %1 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("VALUE", "Number")
                },
            });
        }

        private void RegisterRoundToInt()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "math_floor_to_int",
                Category = "Numbers",
                Color = "#5B67A5",
                Tooltip = "Rounds the number down to the nearest integer.",
                Message = "floor to int( %1 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("VALUE", "Number")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "math_ceil_to_int",
                Category = "Numbers",
                Color = "#5B67A5",
                Tooltip = "Rounds the number up to the nearest integer.",
                Message = "ceil to int( %1 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("VALUE", "Number")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "math_round_to_int",
                Category = "Numbers",
                Color = "#5B67A5",
                Tooltip = "Rounds the number to the nearest integer.",
                Message = "round to int( %1 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("VALUE", "Number")
                },
            });
        }

        private void RegisterRoundDigits()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "math_round_digits",
                Category = "Numbers",
                Color = "#5B67A5",
                Tooltip = "Rounds the number to the given number of decimal places.",
                Message = "round %1 to %2 decimal places",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("VALUE", "Number"),
                    BlockArg.Value("DIGITS", "Number")
                },
            });
        }

        private void RegisterPingPong()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "math_pingpong",
                Category = "Numbers",
                Color = "#5B67A5",
                Tooltip = "Bounces a value back and forth between 0 and the given length.",
                Message = "pingpong( %1 , %2 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("VALUE", "Number"),
                    BlockArg.Value("LENGTH", "Number")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "math_repeat",
                Category = "Numbers",
                Color = "#5B67A5",
                Tooltip = "Loops a value so that it is always between 0 and the given length.",
                Message = "repeat( %1 , %2 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("VALUE", "Number"),
                    BlockArg.Value("LENGTH", "Number")
                },
            });
        }

        private void RegisterConstants()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "math_pi",
                Category = "Numbers",
                Color = "#5B67A5",
                Tooltip = "The mathematical constant Pi (3.14159...).",
                Message = "π (Pi)",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = null,
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "math_deg2rad",
                Category = "Numbers",
                Color = "#5B67A5",
                Tooltip = "Converts a number in degrees to radians.",
                Message = "%1 in radians",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("VALUE", "Number")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "math_rad2deg",
                Category = "Numbers",
                Color = "#5B67A5",
                Tooltip = "Converts a number in radians to degrees.",
                Message = "%1 in degrees",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("VALUE", "Number")
                },
            });
        }

        private void RegisterChecks()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "math_is_nan",
                Category = "Numbers",
                Color = "#5B67A5",
                Tooltip = "Returns true if the number is not a number (NaN).",
                Message = "is NaN( %1 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("VALUE", "Number")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "math_is_infinity",
                Category = "Numbers",
                Color = "#5B67A5",
                Tooltip = "Returns true if the number is positive or negative infinity.",
                Message = "is infinity( %1 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("VALUE", "Number")
                },
            });
        }
    }
}
