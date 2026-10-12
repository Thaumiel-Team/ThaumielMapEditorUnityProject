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
                OutputType = "Number",
                Category = "Numbers",
                Color = "#5B67A5",
                Tooltip = "Returns the smaller of the two numbers.",
                Explanation = "Outputs A or B, whichever is lower.",
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
                OutputType = "Number",
                Category = "Numbers",
                Color = "#5B67A5",
                Tooltip = "Returns the larger of the two numbers.",
                Explanation = "Outputs A or B, whichever is higher.",
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
                OutputType = "Number",
                Category = "Numbers",
                Color = "#5B67A5",
                Tooltip = "Clamps the number between 0 and 1.",
                Explanation = "Values below 0 become 0, above 1 become 1.\nPerfect for opacity, lerp amounts, and progress bars.",
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
                OutputType = "Number",
                Category = "Numbers",
                Color = "#5B67A5",
                Tooltip = "Interpolates between two numbers by the given amount (0 to 1).",
                Explanation = "T = 0 gives A, T = 1 gives B, halves give the middle.\nAnimate T over time for smooth transitions.",
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
                OutputType = "Number",
                Category = "Numbers",
                Color = "#5B67A5",
                Tooltip = "Moves the current value towards the target, never moving more than the max delta.",
                Explanation = "Steps CURRENT toward TARGET by at most MAXDELTA per call.\nCombine with deltaTime for steady speeds.",
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
                OutputType = "Number",
                Category = "Numbers",
                Color = "#5B67A5",
                Tooltip = "Returns a random integer from min (inclusive) to max (exclusive).",
                Explanation = "MIN can roll, MAX never rolls. For 1-6 dice use 1 to 7.",
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
                OutputType = "Number",
                Category = "Numbers",
                Color = "#5B67A5",
                Tooltip = "Returns a random float between min and max.",
                Explanation = "Any decimal between MIN and MAX, both ends possible.\nUse it for scatter, spread, and variation.",
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
                OutputType = "Number",
                Category = "Numbers",
                Color = "#5B67A5",
                Tooltip = "Returns 1 if the number is positive, -1 if negative, and 0 if it is zero.",
                Explanation = "Tells you the direction of a value at a glance.\nUseful for flipping sprites or choosing branches.",
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
                OutputType = "Number",
                Category = "Numbers",
                Color = "#5B67A5",
                Tooltip = "Rounds the number down to the nearest integer.",
                Explanation = "3.9 becomes 3, and -3.1 becomes -4.\nDown always means toward negative infinity.",
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
                OutputType = "Number",
                Category = "Numbers",
                Color = "#5B67A5",
                Tooltip = "Rounds the number up to the nearest integer.",
                Explanation = "3.1 becomes 4, and -3.9 becomes -3.\nUp always means toward positive infinity.",
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
                OutputType = "Number",
                Category = "Numbers",
                Color = "#5B67A5",
                Tooltip = "Rounds the number to the nearest integer.",
                Explanation = "3.5 rounds to 4. Halves round away from zero.",
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
                OutputType = "Number",
                Category = "Numbers",
                Color = "#5B67A5",
                Tooltip = "Rounds the number to the given number of decimal places.",
                Explanation = "2 decimals turns 3.14159 into 3.14.\nUse it to tidy displayed numbers.",
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
                OutputType = "Number",
                Category = "Numbers",
                Color = "#5B67A5",
                Tooltip = "Bounces a value back and forth between 0 and the given length.",
                Explanation = "Feed it rising time for patrolling or blinking effects.\nUnlike Repeat, it reverses instead of jumping.",
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
                OutputType = "Number",
                Category = "Numbers",
                Color = "#5B67A5",
                Tooltip = "Loops a value so that it is always between 0 and the given length.",
                Explanation = "Wraps around like a clock: LENGTH becomes 0 again.\nUse it for cycling indexes and angles.",
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
                OutputType = "Number",
                Category = "Numbers",
                Color = "#5B67A5",
                Tooltip = "The mathematical constant Pi (3.14159...).",
                Explanation = "Ratio of a circle circumference to its diameter.\nUse it with radians and trigonometry.",
                Message = "π (Pi)",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = null,
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "math_deg2rad",
                OutputType = "Number",
                Category = "Numbers",
                Color = "#5B67A5",
                Tooltip = "Converts a number in degrees to radians.",
                Explanation = "180 degrees becomes Pi. Needed for Sin, Cos, and rotation math.",
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
                OutputType = "Number",
                Category = "Numbers",
                Color = "#5B67A5",
                Tooltip = "Converts a number in radians to degrees.",
                Explanation = "Pi becomes 180. Use it to display angles readably.",
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
                OutputType = "Boolean",
                Category = "Numbers",
                Color = "#5B67A5",
                Tooltip = "Returns true if the number is not a number (NaN).",
                Explanation = "NaN appears after bad math like 0 divided by 0.\nCheck this to catch broken calculations.",
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
                OutputType = "Boolean",
                Category = "Numbers",
                Color = "#5B67A5",
                Tooltip = "Returns true if the number is positive or negative infinity.",
                Explanation = "Infinity appears after dividing by zero.\nCheck this to catch runaway values.",
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
