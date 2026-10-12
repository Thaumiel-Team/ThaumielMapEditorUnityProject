using System.Collections.Generic;

namespace Assets.Scripts.Networking.Blocky.Definitions
{
    public class DateTimeDefs : DefBase
    {
        public override void Register()
        {
            BlocklyServer.RegisterCategory("Date & Time", "#00695C", "");

            RegisterNow();
            RegisterPart();
            RegisterAdd();
            RegisterSubtract();
            RegisterToString();
            RegisterParse();
            RegisterTicks();
            RegisterTimeSpanFrom();
            RegisterTimeSpanPart();
            RegisterTimeSpanZero();
        }

        private void RegisterNow()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "datetime_now",
                OutputType = "DateTime",
                Category = "Date & Time",
                Color = "#00695C",
                Tooltip = "The current local date and time.",
                Explanation = "Reads the server clock in local time.\nUse UtcNow instead when comparing across time zones.",
                Message = "DateTime.Now",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = null,
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "datetime_utc_now",
                OutputType = "DateTime",
                Category = "Date & Time",
                Color = "#00695C",
                Tooltip = "The current UTC date and time.",
                Explanation = "Reads the server clock in universal time. Best choice for cooldowns and timestamps.",
                Message = "DateTime.UtcNow",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = null,
            });
        }

        private void RegisterPart()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "datetime_part",
                OutputType = "Number",
                Category = "Date & Time",
                Color = "#00695C",
                Tooltip = "Gets a single component of the date/time.",
                Explanation = "Extracts one PART (year, month, day, hour, and more) from DT.",
                Message = "Get %2 of %1",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("DT"),
                    BlockArg.Dropdown("PART",
                        ("Year", "Year"),
                        ("Month", "Month"),
                        ("Day", "Day"),
                        ("Hour", "Hour"),
                        ("Minute", "Minute"),
                        ("Second", "Second"),
                        ("Millisecond", "Millisecond"),
                        ("Day of Year", "DayOfYear"))
                },
            });
        }

        private void RegisterAdd()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "datetime_add",
                OutputType = "DateTime",
                Category = "Date & Time",
                Color = "#00695C",
                Tooltip = "Adds a number of time units to the date/time.",
                Explanation = "Returns a new date shifted by VALUE units of UNIT. DT itself is unchanged.",
                Message = "%1 .Add%2 ( %3 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("DT"),
                    BlockArg.Dropdown("UNIT",
                        ("Seconds", "Seconds"),
                        ("Minutes", "Minutes"),
                        ("Hours", "Hours"),
                        ("Days", "Days"),
                        ("Milliseconds", "Milliseconds")),
                    BlockArg.Value("VALUE", "Number")
                },
            });
        }

        private void RegisterSubtract()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "datetime_subtract",
                OutputType = "TimeSpan",
                Category = "Date & Time",
                Color = "#00695C",
                Tooltip = "Subtracts one date/time from another, giving a TimeSpan.",
                Explanation = "Outputs how much time passes between A and B.\nUse a TimeSpan part block to read the result.",
                Message = "%1 - %2",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("A"),
                    BlockArg.Value("B")
                },
            });
        }

        private void RegisterToString()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "datetime_to_string",
                OutputType = "String",
                Category = "Date & Time",
                Color = "#00695C",
                Tooltip = "Converts the date/time to its default string representation.",
                Explanation = "Turns DT into readable text for logs and displays.\nFor custom layouts use the format version.",
                Message = "%1 .ToString()",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("DT")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "datetime_to_string_format",
                OutputType = "String",
                Explanation = "Formats DT with the FMT pattern. \"yyyy-MM-dd HH:mm\" gives sortable stamps like 2026-10-07 14:30.",
                Category = "Date & Time",
                Color = "#00695C",
                Tooltip = "Converts the date/time to a string using a format, e.g. \"yyyy-MM-dd HH:mm\".",
                Message = "%1 .ToString( %2 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("DT"),
                    BlockArg.Value("FMT", "String")
                },
            });
        }

        private void RegisterParse()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "datetime_parse",
                OutputType = "DateTime",
                Category = "Date & Time",
                Color = "#00695C",
                Tooltip = "Parses a string into a DateTime.",
                Explanation = "Reads a date from STR. Accepts most common date layouts.\nFails on text that is not a date.",
                Message = "DateTime.Parse( %1 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("STR", "String")
                },
            });
        }

        private void RegisterTicks()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "datetime_ticks",
                OutputType = "Number",
                Category = "Date & Time",
                Color = "#00695C",
                Tooltip = "The number of 100-nanosecond ticks of the date/time.",
                Explanation = "A huge precise number, ideal for exact elapsed-time math.\nCompare two tick values to measure durations.",
                Message = "%1 .Ticks",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("DT")
                },
            });
        }

        private void RegisterTimeSpanFrom()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "timespan_from",
                OutputType = "TimeSpan",
                Category = "Date & Time",
                Color = "#00695C",
                Tooltip = "Creates a TimeSpan representing the given amount of time.",
                Explanation = "Builds a duration of VALUE UNITs, like 90 seconds or 2 hours.\nAdd it to Now for expiry times.",
                Message = "TimeSpan.From%2 ( %1 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("VALUE", "Number"),
                    BlockArg.Dropdown("UNIT",
                        ("Seconds", "Seconds"),
                        ("Minutes", "Minutes"),
                        ("Hours", "Hours"),
                        ("Days", "Days"),
                        ("Milliseconds", "Milliseconds"))
                },
            });
        }

        private void RegisterTimeSpanPart()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "timespan_part",
                OutputType = "Number",
                Category = "Date & Time",
                Color = "#00695C",
                Tooltip = "Gets a component of the TimeSpan. The Total variants include the whole span.",
                Explanation = "Reads one PART from TS. Days gives whole days, while TotalDays includes fractions.\nPick Total variants for exact math.",
                Message = "Get %2 of %1",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("TS"),
                    BlockArg.Dropdown("PART",
                        ("Total Days", "TotalDays"),
                        ("Total Hours", "TotalHours"),
                        ("Total Minutes", "TotalMinutes"),
                        ("Total Seconds", "TotalSeconds"),
                        ("Total Milliseconds", "TotalMilliseconds"),
                        ("Days", "Days"),
                        ("Hours", "Hours"),
                        ("Minutes", "Minutes"),
                        ("Seconds", "Seconds"),
                        ("Milliseconds", "Milliseconds"))
                },
            });
        }

        private void RegisterTimeSpanZero()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "timespan_zero",
                OutputType = "TimeSpan",
                Category = "Date & Time",
                Color = "#00695C",
                Tooltip = "A TimeSpan of zero.",
                Explanation = "A duration of nothing. Useful as a default or for resetting timers.",
                Message = "TimeSpan.Zero",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = null,
            });
        }
    }
}
