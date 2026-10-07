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
                Category = "Date & Time",
                Color = "#00695C",
                Tooltip = "The current local date and time.",
                Message = "DateTime.Now",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = null,
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "datetime_utc_now",
                Category = "Date & Time",
                Color = "#00695C",
                Tooltip = "The current UTC date and time.",
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
                Category = "Date & Time",
                Color = "#00695C",
                Tooltip = "Gets a single component of the date/time.",
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
                Category = "Date & Time",
                Color = "#00695C",
                Tooltip = "Adds a number of time units to the date/time.",
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
                Category = "Date & Time",
                Color = "#00695C",
                Tooltip = "Subtracts one date/time from another, giving a TimeSpan.",
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
                Category = "Date & Time",
                Color = "#00695C",
                Tooltip = "Converts the date/time to its default string representation.",
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
                Category = "Date & Time",
                Color = "#00695C",
                Tooltip = "Parses a string into a DateTime.",
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
                Category = "Date & Time",
                Color = "#00695C",
                Tooltip = "The number of 100-nanosecond ticks of the date/time.",
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
                Category = "Date & Time",
                Color = "#00695C",
                Tooltip = "Creates a TimeSpan representing the given amount of time.",
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
                Category = "Date & Time",
                Color = "#00695C",
                Tooltip = "Gets a component of the TimeSpan. The Total variants include the whole span.",
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
                Category = "Date & Time",
                Color = "#00695C",
                Tooltip = "A TimeSpan of zero.",
                Message = "TimeSpan.Zero",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = null,
            });
        }
    }
}
