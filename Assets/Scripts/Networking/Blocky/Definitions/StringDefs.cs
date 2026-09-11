using System.Collections.Generic;

namespace Assets.Scripts.Networking.Blocky.Definitions
{
    public class StringDefs : DefBase
    {
        public override void Register()
        {
            BlocklyServer.RegisterCategory("String", "#26A69A", "");

            RegisterContains();
            RegisterStartsWith();
            RegisterEndsWith();
            RegisterToCase();
            RegisterTrim();
            RegisterSubstring();
            RegisterReplace();
            RegisterIndexOf();
            RegisterCharAt();
            RegisterFormat();
            RegisterIsNullOrEmpty();
            RegisterIsNullOrWhitespace();
            RegisterPad();
            RegisterJoin();
            RegisterSplit();
        }

        private void RegisterContains()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "string_contains",
                Category = "String",
                Color = "#26A69A",
                Tooltip = "Returns true if the string contains the given value.",
                Message = "%1 .Contains( %2 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("STR", "String"),
                    BlockArg.Value("VALUE", "String")
                },
            });
        }

        private void RegisterStartsWith()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "string_starts_with",
                Category = "String",
                Color = "#26A69A",
                Tooltip = "Returns true if the string starts with the given value.",
                Message = "%1 .StartsWith( %2 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("STR", "String"),
                    BlockArg.Value("VALUE", "String")
                },
            });
        }

        private void RegisterEndsWith()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "string_ends_with",
                Category = "String",
                Color = "#26A69A",
                Tooltip = "Returns true if the string ends with the given value.",
                Message = "%1 .EndsWith( %2 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("STR", "String"),
                    BlockArg.Value("VALUE", "String")
                },
            });
        }

        private void RegisterToCase()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "string_to_upper",
                Category = "String",
                Color = "#26A69A",
                Tooltip = "Returns the string in upper case.",
                Message = "%1 .ToUpper()",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("STR", "String")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "string_to_lower",
                Category = "String",
                Color = "#26A69A",
                Tooltip = "Returns the string in lower case.",
                Message = "%1 .ToLower()",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("STR", "String")
                },
            });
        }

        private void RegisterTrim()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "string_trim",
                Category = "String",
                Color = "#26A69A",
                Tooltip = "Returns the string with leading and trailing whitespace removed.",
                Message = "%1 .Trim()",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("STR", "String")
                },
            });
        }

        private void RegisterSubstring()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "string_substring",
                Category = "String",
                Color = "#26A69A",
                Tooltip = "Returns the part of the string starting at the given index, taking the given length of characters.",
                Message = "%1 .Substring( %2 , %3 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("STR", "String"),
                    BlockArg.Value("START", "Number"),
                    BlockArg.Value("LENGTH", "Number")
                },
            });
        }

        private void RegisterReplace()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "string_replace",
                Category = "String",
                Color = "#26A69A",
                Tooltip = "Replaces every occurrence of the old value with the new value.",
                Message = "%1 .Replace( %2 , %3 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("STR", "String"),
                    BlockArg.Value("OLD", "String"),
                    BlockArg.Value("NEW", "String")
                },
            });
        }

        private void RegisterIndexOf()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "string_index_of",
                Category = "String",
                Color = "#26A69A",
                Tooltip = "Returns the index of the first occurrence of the value, or -1 if it is not found.",
                Message = "%1 .IndexOf( %2 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("STR", "String"),
                    BlockArg.Value("VALUE", "String")
                },
            });
        }

        private void RegisterCharAt()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "string_char_at",
                Category = "String",
                Color = "#26A69A",
                Tooltip = "Returns the character at the given index as a one-character string.",
                Message = "%1 [ %2 ]",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("STR", "String"),
                    BlockArg.Value("INDEX", "Number")
                },
            });
        }

        private void RegisterFormat()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "string_format",
                Category = "String",
                Color = "#26A69A",
                Tooltip = "Formats a string using placeholders such as {0}, {1} and {2}.",
                Message = "string.Format( %1 , %2 , %3 , %4 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("FMT", "String"),
                    BlockArg.Value("ARG1"),
                    BlockArg.Value("ARG2"),
                    BlockArg.Value("ARG3")
                },
            });
        }

        private void RegisterIsNullOrEmpty()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "string_is_null_or_empty",
                Category = "String",
                Color = "#26A69A",
                Tooltip = "Returns true if the string is null or empty.",
                Message = "string.IsNullOrEmpty( %1 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("STR", "String")
                },
            });
        }

        private void RegisterIsNullOrWhitespace()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "string_is_null_or_whitespace",
                Category = "String",
                Color = "#26A69A",
                Tooltip = "Returns true if the string is null, empty, or consists only of whitespace.",
                Message = "string.IsNullOrWhiteSpace( %1 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("STR", "String")
                },
            });
        }

        private void RegisterPad()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "string_pad_left",
                Category = "String",
                Color = "#26A69A",
                Tooltip = "Pads the string on the left with spaces so it is at least the given total width.",
                Message = "%1 .PadLeft( %2 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("STR", "String"),
                    BlockArg.Value("TOTAL", "Number")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "string_pad_right",
                Category = "String",
                Color = "#26A69A",
                Tooltip = "Pads the string on the right with spaces so it is at least the given total width.",
                Message = "%1 .PadRight( %2 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("STR", "String"),
                    BlockArg.Value("TOTAL", "Number")
                },
            });
        }

        private void RegisterJoin()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "string_join",
                Category = "String",
                Color = "#26A69A",
                Tooltip = "Joins the items of a list into a single string using the given separator.",
                Message = "string.Join( %1 , %2 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("SEP", "String"),
                    BlockArg.Value("LIST")
                },
            });
        }

        private void RegisterSplit()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "string_split",
                Category = "String",
                Color = "#26A69A",
                Tooltip = "Splits the string by the given separator into an array of strings.",
                Message = "%1 .Split( %2 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("STR", "String"),
                    BlockArg.Value("SEP", "String")
                },
            });
        }
    }
}
