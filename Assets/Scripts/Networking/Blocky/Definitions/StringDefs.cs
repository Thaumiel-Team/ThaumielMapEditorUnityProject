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
                OutputType = "Boolean",
                Category = "String",
                Color = "#26A69A",
                Tooltip = "Returns true if the string contains the given value.",
                Explanation = "Case sensitive search. Use ToLower on both sides for case-insensitive checks.",
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
                OutputType = "Boolean",
                Category = "String",
                Color = "#26A69A",
                Tooltip = "Returns true if the string starts with the given value.",
                Explanation = "Checks the beginning only. Useful for prefixes and commands.",
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
                OutputType = "Boolean",
                Category = "String",
                Color = "#26A69A",
                Tooltip = "Returns true if the string ends with the given value.",
                Explanation = "Checks the ending only. Useful for file types and suffixes.",
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
                OutputType = "String",
                Category = "String",
                Color = "#26A69A",
                Tooltip = "Returns the string in upper case.",
                Explanation = "Use it to normalise input before comparing.",
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
                OutputType = "String",
                Category = "String",
                Color = "#26A69A",
                Tooltip = "Returns the string in lower case.",
                Explanation = "Use it on both sides of Contains or Equals for case-insensitive matching.",
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
                OutputType = "String",
                Category = "String",
                Color = "#26A69A",
                Tooltip = "Returns the string with leading and trailing whitespace removed.",
                Explanation = "Cleans up player-typed input. Inner spaces are untouched.",
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
                OutputType = "String",
                Category = "String",
                Color = "#26A69A",
                Tooltip = "Returns the part of the string starting at the given index, taking the given length of characters.",
                Explanation = "Index 0 is the first character. START + LENGTH must stay inside the string.",
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
                OutputType = "String",
                Category = "String",
                Color = "#26A69A",
                Tooltip = "Replaces every occurrence of the old value with the new value.",
                Explanation = "Swaps all matches at once, not just the first.\nUse an empty NEW value to delete matches.",
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
                OutputType = "Number",
                Category = "String",
                Color = "#26A69A",
                Tooltip = "Returns the index of the first occurrence of the value, or -1 if it is not found.",
                Explanation = "Always compare against -1 to test for absence.\nAdd the match length to jump past it.",
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
                OutputType = "String",
                Category = "String",
                Color = "#26A69A",
                Tooltip = "Returns the character at the given index as a one-character string.",
                Explanation = "Index 0 is the first character.",
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
                OutputType = "String",
                Category = "String",
                Color = "#26A69A",
                Tooltip = "Formats a string using placeholders such as {0}, {1} and {2}.",
                Explanation = "FMT holds {0}, {1}, {2} slots filled by the arg blocks in order.\nUse it to build messages with values inside.",
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
                OutputType = "Boolean",
                Category = "String",
                Color = "#26A69A",
                Tooltip = "Returns true if the string is null or empty.",
                Explanation = "Catches missing and blank text. Whitespace-only strings count as non-empty, so use the WhiteSpace version for typed input.",
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
                OutputType = "Boolean",
                Category = "String",
                Color = "#26A69A",
                Tooltip = "Returns true if the string is null, empty, or consists only of whitespace.",
                Explanation = "The strictest blank check. Best for validating typed input.",
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
                OutputType = "String",
                Category = "String",
                Color = "#26A69A",
                Tooltip = "Pads the string on the left with spaces so it is at least the given total width.",
                Explanation = "Aligns short strings to the right for tidy columns.\nLonger strings are never cut.",
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
                OutputType = "String",
                Category = "String",
                Color = "#26A69A",
                Tooltip = "Pads the string on the right with spaces so it is at least the given total width.",
                Explanation = "Aligns short strings to the left for tidy columns.",
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
                OutputType = "String",
                Category = "String",
                Color = "#26A69A",
                Tooltip = "Joins the items of a list into a single string using the given separator.",
                Explanation = "Glues LIST with SEP between items, like commas or newlines.\nPair with Split to round-trip text.",
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
                OutputType = "Array",
                Category = "String",
                Color = "#26A69A",
                Tooltip = "Splits the string by the given separator into an array of strings.",
                Explanation = "Breaks STR everywhere SEP appears.\nPair with Join or foreach to process the pieces.",
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
