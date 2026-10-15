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
                OutputType = "String",
                Category = "Convert",
                Color = "#AD1457",
                Tooltip = "Converts a value to a string.",
                Explanation = "Turns numbers, booleans, and other values into text.\nUse it before joining values into messages.",
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
                OutputType = "Number",
                Category = "Convert",
                Color = "#AD1457",
                Tooltip = "Converts a value to a 32-bit integer.",
                Explanation = "Truncates toward zero, so 3.9 becomes 3.\nUse Parse when the input is text.",
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
                OutputType = "Number",
                Category = "Convert",
                Color = "#AD1457",
                Tooltip = "Converts a value to a single-precision float.",
                Explanation = "Turns integers and other numbers into decimals for math that needs fractions.",
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
                OutputType = "Number",
                Category = "Convert",
                Color = "#AD1457",
                Tooltip = "Converts a value to a double-precision float.",
                Explanation = "Like ToSingle but with more precision for exact calculations.",
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
                OutputType = "Boolean",
                Category = "Convert",
                Color = "#AD1457",
                Tooltip = "Converts a value to a boolean.",
                Explanation = "Zero, empty, and null-ish values become false, most other values become true.",
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
                OutputType = "Number",
                Category = "Convert",
                Color = "#AD1457",
                Tooltip = "Parses a string into a 32-bit integer.",
                Explanation = "Reads a whole number from STR, like 42.\nFails when the text is not a valid integer.",
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
                OutputType = "Number",
                Category = "Convert",
                Color = "#AD1457",
                Tooltip = "Parses a string into a single-precision float.",
                Explanation = "Reads a decimal from STR, like 3.5.\nFails when the text is not a valid number.",
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
                OutputType = "Number",
                Category = "Convert",
                Color = "#AD1457",
                Tooltip = "Parses a string into a double-precision float.",
                Explanation = "Like float Parse but keeps full precision.",
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
                OutputType = "Boolean",
                Category = "Convert",
                Color = "#AD1457",
                Tooltip = "Parses a string into a boolean.",
                Explanation = "Reads true or false text of any capitalisation into a boolean.",
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
                OutputType = "String",
                Explanation = "Formats VALUE with the FMT pattern. \"0.00\" forces two decimals, \"N2\" adds thousand separators.",
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
