using System.Collections.Generic;

namespace Assets.Scripts.Networking.Blocky.Definitions
{
    public class ConvertDefs : DefBase
    {
        public override void Register()
        {
            BlocklyServer.RegisterCategory("Convert", "#AD1457", "");

            RegisterConvert();
            RegisterParse();
            RegisterToStringFormat();
        }

        private void RegisterConvert()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "convert_to_string",
                Category = "Convert",
                Color = "#AD1457",
                Tooltip = "Converts a value to a string.",
                Message = "Convert.ToString( %1 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("VALUE")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "convert_to_int",
                Category = "Convert",
                Color = "#AD1457",
                Tooltip = "Converts a value to a 32-bit integer.",
                Message = "Convert.ToInt32( %1 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("VALUE")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "convert_to_float",
                Category = "Convert",
                Color = "#AD1457",
                Tooltip = "Converts a value to a single-precision float.",
                Message = "Convert.ToSingle( %1 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("VALUE")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "convert_to_double",
                Category = "Convert",
                Color = "#AD1457",
                Tooltip = "Converts a value to a double-precision float.",
                Message = "Convert.ToDouble( %1 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("VALUE")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "convert_to_bool",
                Category = "Convert",
                Color = "#AD1457",
                Tooltip = "Converts a value to a boolean.",
                Message = "Convert.ToBoolean( %1 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("VALUE")
                },
            });
        }

        private void RegisterParse()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "convert_parse_int",
                Category = "Convert",
                Color = "#AD1457",
                Tooltip = "Parses a string into a 32-bit integer.",
                Message = "int.Parse( %1 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("STR", "String")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "convert_parse_float",
                Category = "Convert",
                Color = "#AD1457",
                Tooltip = "Parses a string into a single-precision float.",
                Message = "float.Parse( %1 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("STR", "String")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "convert_parse_double",
                Category = "Convert",
                Color = "#AD1457",
                Tooltip = "Parses a string into a double-precision float.",
                Message = "double.Parse( %1 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("STR", "String")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "convert_parse_bool",
                Category = "Convert",
                Color = "#AD1457",
                Tooltip = "Parses a string into a boolean.",
                Message = "bool.Parse( %1 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("STR", "String")
                },
            });
        }

        private void RegisterToStringFormat()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "convert_to_string_format",
                Category = "Convert",
                Color = "#AD1457",
                Tooltip = "Converts a number to a string using a format, e.g. \"0.00\" or \"N2\".",
                Message = "%1 .ToString( %2 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("VALUE"),
                    BlockArg.Value("FMT", "String")
                },
            });
        }
    }
}
