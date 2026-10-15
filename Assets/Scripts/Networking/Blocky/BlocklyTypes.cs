using System.Collections.Generic;

namespace Assets.Scripts.Networking.Blocky
{
    /// <summary>
    /// Describes a custom Blockly block to register in the browser.
    /// Build arg entries with the <see cref="BlockArg"/> helpers.
    /// </summary>
    public class BlockDefinition
    {
        /// <summary>
        /// Unique block type id, e.g. "myplugin_jump".
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Blockly message string. Use %1, %2 … as placeholders for args.
        /// Example: "Jump with force %1"
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Category name. Auto-created if it doesn't exist yet.
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Hue 0–360 or hex colour string e.g. "#E65100".
        /// </summary>
        public object Color { get; set; } = 200;

        /// <summary>
        /// Blockly args0 array. Use <see cref="BlockArg"/> to build entries.
        /// </summary>
        public List<Dictionary<string, object>> Args { get; set; }

        public List<BlockConnectionType> Connections { get; set; } = new();

        /// <summary>
        /// JavaScript generator body string.
        ///
        /// Available variables:
        ///   block – the Blockly block instance
        ///   Blockly – the Blockly global
        ///   cs_val(block, inputName, fallback) – returns the C# code string for a value input
        ///   cs_stmts(block, inputName) – returns concatenated C# for a statement input
        ///
        /// Statement block – return a string ending in \n:
        ///   return 'MyFunc(' + cs_val(block,"ARG","0") + ');\n';
        ///
        /// Value block – return [codeString, order]:
        ///   return ['gameObject.GetHealth()', 0];
        /// </summary>
        public string Generator { get; set; }


        public string Tooltip { get; set; }

        /// <summary>
        /// Longer explanation of what the block does, shown in the browser help viewer when the block is selected. Displayed on hover when <see cref="Tooltip"/> is empty.
        /// </summary>
        public string Explanation { get; set; }

        public string HelpUrl { get; set; }

        /// <summary>
        /// Blockly output check type, e.g. "Number", "String", "Boolean",
        /// "Vector3", "Array", "Door", "Player". Null means untyped.
        /// Only sent to the browser, never exported to Unity YAML.
        /// </summary>
        public string OutputType { get; set; }

        /// <summary>
        /// For property getters: maps Property dropdown values to output
        /// types. The browser updates the output check when the selection
        /// changes. Properties without a map entry stay untyped.
        /// Only sent to the browser, never exported to Unity YAML.
        /// </summary>
        public Dictionary<string, string> PropertyTypes { get; set; }
    }

    /// <summary>
    /// A validation issue reported to the browser, shown in its issue list, counters, and block badges. Send via <see cref="BlocklyServer.ReportIssues"/>.
    /// </summary>
    public class BlockIssue
    {
        /// <summary>
        /// Stable id for this issue. Reporting again with the same id replaces it.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// "err" or "warn". Anything else is shown as a warning.
        /// </summary>
        public string Level { get; set; } = "warn";

        /// <summary>
        /// Human readable message shown in the issue list and block popups.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Optional block type ids this issue applies to. Matching blocks get
        /// warning badges and click-to-locate. Omit for workspace-wide issues.
        /// </summary>
        public List<string> BlockTypes { get; set; }
    }

    /// <summary>
    /// A conditional validation rule evaluated live in the browser. Unlike <see cref="BlockIssue"/> (shown immediately), a check only produces an issue when its condition matches a block. Send via <see cref="BlocklyServer.RegisterCheck"/>.
    /// </summary>
    public class BlockCheck
    {
        /// <summary>
        /// Stable id for this check. Registering again with the same id replaces it.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// "err" or "warn" for produced issues. Anything else is shown as a warning.
        /// </summary>
        public string Level { get; set; } = "warn";

        /// <summary>
        /// Message shown when the condition matches. Supports placeholders:
        /// {InputName} for inferred input types, plus {type}, {input} and {expected} for mismatch checks. Example: "Cannot compare {A} with {B}."
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Block type ids this check applies to. Empty means all blocks.
        /// </summary>
        public List<string> Blocks { get; set; }

        /// <summary>
        /// Optional pair of value input names, e.g. ["A", "B"].
        /// Fires when both inputs have known, incompatible types (e.g. Boolean vs String).
        /// Unknown types (variables, function results) never fire.
        /// </summary>
        public List<string> InputTypesDiffer { get; set; }

        /// <summary>
        /// Optional value input names that must all carry this type, e.g.
        /// ["IF0"] with "Boolean". Fires on the first known mismatch.
        /// </summary>
        public List<string> MismatchInputs { get; set; }

        /// <summary>
        /// Expected type for <see cref="MismatchInputs"/>, e.g. "Boolean".
        /// </summary>
        public string MismatchExpected { get; set; }

        /// <summary>
        /// Optional value-input names that must be connected, e.g. ["POS"].
        /// Fires when one is empty (supports {input} in the message). Only
        /// real value sockets count, so dropdown fields never trigger it.
        /// </summary>
        public List<string> RequiredInputs { get; set; }
    }

    /// <summary>
    /// Fluent factory for Blockly args0 entries.
    /// </summary>
    public static class BlockArg
    {
        /// <summary>
        /// A value input socket
        /// </summary>
        /// <param name="name">Input name referenced in the generator, e.g. "FORCE".</param>
        /// <param name="check">Optional type constraint: "Number", "Boolean", "Vector3", etc.</param>
        public static Dictionary<string, object> Value(string name, string check = null)
        {
            Dictionary<string, object> d = new()
            {
                ["type"] = "input_value",
                ["name"] = name
            };

            if (check != null)
                d["check"] = check;

            return d;
        }

        /// <summary>
        /// A statement input
        /// </summary>
        /// <param name="name">Input name, e.g. "DO".</param>
        public static Dictionary<string, object> Statement(string name) => new()
        {
            ["type"] = "input_statement",
            ["name"] = name
        };

        /// <summary>
        /// A dummy input just a line break / row spacer, no socket.
        /// </summary>
        public static Dictionary<string, object> Dummy() => new()
        {
            ["type"] = "input_dummy"
        };

        /// <summary>
        /// An inline text field.
        /// </summary>
        /// <param name="name">Field name, e.g. "TAG".</param>
        /// <param name="defaultText">Initial text shown in the field.</param>
        public static Dictionary<string, object> TextField(string name, string defaultText = "") => new()
        {
            ["type"] = "field_input",
            ["name"] = name,
            ["text"] = defaultText
        };

        /// <summary>
        /// An inline number field.
        /// </summary>
        /// <param name="name">Field name, e.g. "TIMES".</param>
        /// <param name="defaultValue">Initial numeric value.</param>
        /// <param name="min">Optional minimum.</param>
        /// <param name="max">Optional maximum.</param>
        public static Dictionary<string, object> NumberField(string name, double defaultValue = 0, double? min = null, double? max = null)
        {
            Dictionary<string, object> d = new()
            {
                ["type"] = "field_number",
                ["name"] = name,
                ["value"] = defaultValue
            };

            if (min.HasValue)
                d["min"] = min.Value;

            if (max.HasValue)
                d["max"] = max.Value;

            return d;
        }

        /// <summary>
        /// A dropdown field.
        /// </summary>
        /// <param name="name">Field name, e.g. "MODE".</param>
        /// <param name="options">Pairs of (display label, code value).</param>
        public static Dictionary<string, object> Dropdown(string name, params (string label, string value)[] options)
        {
            List<List<string>> opts = new();
            foreach ((string label, string value) in options)
            {
                opts.Add(new List<string> { label, value });
            }

            return new Dictionary<string, object>
            {
                ["type"] = "field_dropdown",
                ["name"] = name,
                ["options"] = opts
            };
        }

        /// <summary>
        /// A checkbox field.
        /// </summary>
        /// <param name="name">Field name, e.g. "ENABLED".</param>
        /// <param name="checked">Initial checked state.</param>
        public static Dictionary<string, object> Checkbox(string name, bool @checked = false) => new()
        {
            ["type"] = "field_checkbox",
            ["name"] = name,
            ["checked"] = @checked
        };

        /// <summary>
        /// A colour picker field.
        /// </summary>
        /// <param name="name">Field name, e.g. "COLOR".</param>
        /// <param name="defaultColor">Hex string, e.g. "#ff0000".</param>
        public static Dictionary<string, object> ColorField(string name, string defaultColor = "#ff0000") => new()
        {
            ["type"] = "field_colour",
            ["name"] = name,
            ["colour"] = defaultColor
        };

        public static Dictionary<string, object> Variable(string name, string defaultName = "item") => new()
        {
            ["type"] = "field_variable",
            ["name"] = name,
            ["variable"] = defaultName
        };
    }

    /// <summary>
    /// One variable exposed on a custom event block, e.g. the player who
    /// triggered it. Sent via <see cref="BlocklyServer.RegisterEvent"/>.
    /// </summary>
    public class BlockEventParam
    {
        /// <summary>
        /// Default variable name shown in the block, e.g. "player".
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// C# type used in the generated method signature,
        /// e.g. "Player", "Collider", "string", "int". Defaults to "object".
        /// </summary>
        public string Type { get; set; } = "object";

        public static BlockEventParam Var(string name, string type = "object") => new()
        {
            Name = name,
            Type = type
        };
    }

    /// <summary>
    /// Describes a custom event block added to the browser Events toolbox.
    /// Behaves like the built-in events (spawned_event, trigger_enter_event…):
    /// it can lock workspaces, is validated, and generates a C# method.
    /// </summary>
    public class BlockEvent
    {
        /// <summary>
        /// Unique block type id, e.g. "my_custom_event".
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Display label on the block, e.g. "OnCustom". Also used as the
        /// generated C# method name when <see cref="Method"/> is empty.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// C# method name in generated code. Defaults to <see cref="Label"/>.
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// Variables shown in the block header. Each becomes a variable
        /// dropdown plus a same-position method parameter.
        /// </summary>
        public List<BlockEventParam> Params { get; set; }

        /// <summary>
        /// Block colour, hue 0-360 or hex string e.g. "#E65100".
        /// </summary>
        public object Color { get; set; } = "#E65100";

        public string Tooltip { get; set; }

        /// <summary>
        /// Longer explanation shown in the browser help viewer.
        /// </summary>
        public string Explanation { get; set; }
    }
}
