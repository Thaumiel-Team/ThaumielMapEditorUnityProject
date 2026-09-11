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


        public string HelpUrl { get; set; }
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
}
