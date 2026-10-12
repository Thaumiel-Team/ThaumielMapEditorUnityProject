using System.Collections.Generic;

namespace Assets.Scripts.Networking.Blocky.Definitions
{
    public static class CheckDefs
    {
        public static void Register()
        {
            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "compare-mismatch",
                Level = "err",
                Message = "Cannot compare {A} with {B}.",
                Blocks = new List<string> { "logic_compare" },
                InputTypesDiffer = new List<string> { "A", "B" },
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "if-needs-bool",
                Level = "warn",
                Message = "If condition should be Boolean, got {type}.",
                Blocks = new List<string> { "controls_if" },
                MismatchInputs = new List<string> { "IF0" },
                MismatchExpected = "Boolean",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "while-needs-bool",
                Level = "warn",
                Message = "Loop condition should be Boolean, got {type}.",
                Blocks = new List<string> { "controls_whileUntil" },
                MismatchInputs = new List<string> { "BOOL" },
                MismatchExpected = "Boolean",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "logic-needs-bool",
                Level = "warn",
                Message = "Logic operands must be Boolean, got {type}.",
                Blocks = new List<string> { "logic_operation" },
                MismatchInputs = new List<string> { "A", "B" },
                MismatchExpected = "Boolean",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "math-ab",
                Level = "err",
                Message = "Math needs Numbers, got {type}.",
                Blocks = new List<string> { "math_min", "math_max" },
                MismatchInputs = new List<string> { "A", "B" },
                MismatchExpected = "Number",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "math-lerp",
                Level = "err",
                Message = "Lerp needs Numbers, got {type}.",
                Blocks = new List<string> { "math_lerp_float" },
                MismatchInputs = new List<string> { "A", "B", "T" },
                MismatchExpected = "Number",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "math-move",
                Level = "err",
                Message = "MoveTowards needs Numbers, got {type}.",
                Blocks = new List<string> { "math_move_towards" },
                MismatchInputs = new List<string> { "CURRENT", "TARGET", "MAXDELTA" },
                MismatchExpected = "Number",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "math-value",
                Level = "err",
                Message = "Expected a Number, got {type}.",
                Blocks = new List<string> { "math_clamp01", "math_sign", "math_floor_to_int", "math_ceil_to_int", "math_round_to_int", "math_deg2rad", "math_rad2deg", "math_is_nan", "math_is_infinity" },
                MismatchInputs = new List<string> { "VALUE" },
                MismatchExpected = "Number",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "math-round-digits",
                Level = "err",
                Message = "Rounding needs Numbers, got {type}.",
                Blocks = new List<string> { "math_round_digits" },
                MismatchInputs = new List<string> { "VALUE", "DIGITS" },
                MismatchExpected = "Number",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "math-pingpong",
                Level = "err",
                Message = "Expected Numbers, got {type}.",
                Blocks = new List<string> { "math_pingpong", "math_repeat" },
                MismatchInputs = new List<string> { "VALUE", "LENGTH" },
                MismatchExpected = "Number",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "math-random",
                Level = "err",
                Message = "Random range needs Numbers, got {type}.",
                Blocks = new List<string> { "math_random_int_range", "math_random_float_range" },
                MismatchInputs = new List<string> { "MIN", "MAX" },
                MismatchExpected = "Number",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "vec3-components",
                Level = "err",
                Message = "Vector3 components must be Numbers, got {type}.",
                Blocks = new List<string> { "unity_vec3" },
                MismatchInputs = new List<string> { "X", "Y", "Z" },
                MismatchExpected = "Number",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "vec3-set-pos",
                Level = "err",
                Message = "Position must be a Vector3, got {type}.",
                Blocks = new List<string> { "unity_set_pos" },
                MismatchInputs = new List<string> { "POS" },
                MismatchExpected = "Vector3",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "vec3-move",
                Level = "err",
                Message = "Expected a Vector3, got {type}.",
                Blocks = new List<string> { "unity_translate", "unity_rotate", "unity_set_vel", "unity_normalize" },
                MismatchInputs = new List<string> { "V" },
                MismatchExpected = "Vector3",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "vec3-scale",
                Level = "err",
                Message = "Scale must be a Vector3, got {type}.",
                Blocks = new List<string> { "unity_set_scale" },
                MismatchInputs = new List<string> { "S" },
                MismatchExpected = "Vector3",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "vec3-force",
                Level = "err",
                Message = "Force must be a Vector3, got {type}.",
                Blocks = new List<string> { "unity_add_force" },
                MismatchInputs = new List<string> { "F" },
                MismatchExpected = "Vector3",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "vec3-torque",
                Level = "err",
                Message = "Torque must be a Vector3, got {type}.",
                Blocks = new List<string> { "unity_add_torque" },
                MismatchInputs = new List<string> { "T" },
                MismatchExpected = "Vector3",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "vec3-ray",
                Level = "err",
                Message = "Ray origin and direction must be Vector3, got {type}.",
                Blocks = new List<string> { "unity_raycast" },
                MismatchInputs = new List<string> { "O", "D" },
                MismatchExpected = "Vector3",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "vec3-dist",
                Level = "err",
                Message = "Distance needs Vector3 points, got {type}.",
                Blocks = new List<string> { "unity_distance" },
                MismatchInputs = new List<string> { "A", "B" },
                MismatchExpected = "Vector3",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "vec3-lerp-vec",
                Level = "err",
                Message = "Lerp points must be Vector3, got {type}.",
                Blocks = new List<string> { "unity_lerp" },
                MismatchInputs = new List<string> { "A", "B" },
                MismatchExpected = "Vector3",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "vec3-lerp-t",
                Level = "err",
                Message = "Lerp amount must be a Number, got {type}.",
                Blocks = new List<string> { "unity_lerp" },
                MismatchInputs = new List<string> { "T" },
                MismatchExpected = "Number",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "wait-num",
                Level = "err",
                Message = "Wait time must be a Number, got {type}.",
                Blocks = new List<string> { "unity_wait" },
                MismatchInputs = new List<string> { "T" },
                MismatchExpected = "Number",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "wait-until-bool",
                Level = "warn",
                Message = "Wait condition should be Boolean, got {type}.",
                Blocks = new List<string> { "timing_wait_until_true" },
                MismatchInputs = new List<string> { "WaitingInput" },
                MismatchExpected = "Boolean",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "time-amount",
                Level = "err",
                Message = "Time amount must be a Number, got {type}.",
                Blocks = new List<string> { "datetime_add", "timespan_from" },
                MismatchInputs = new List<string> { "VALUE" },
                MismatchExpected = "Number",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "array-index",
                Level = "err",
                Message = "Index must be a Number, got {type}.",
                Blocks = new List<string> { "array_get", "array_set" },
                MismatchInputs = new List<string> { "INDEX" },
                MismatchExpected = "Number",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "str-pair",
                Level = "warn",
                Message = "Text compare needs text, got {type}.",
                Blocks = new List<string> { "string_contains", "string_starts_with", "string_ends_with" },
                MismatchInputs = new List<string> { "STR", "VALUE" },
                MismatchExpected = "String",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "str-one",
                Level = "warn",
                Message = "Expected text, got {type}.",
                Blocks = new List<string> { "string_to_upper", "string_to_lower", "string_trim", "string_char_at", "string_pad_left", "string_pad_right" },
                MismatchInputs = new List<string> { "STR" },
                MismatchExpected = "String",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "str-sub",
                Level = "warn",
                Message = "Substring needs text, got {type}.",
                Blocks = new List<string> { "string_substring" },
                MismatchInputs = new List<string> { "STR" },
                MismatchExpected = "String",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "str-sub-idx",
                Level = "err",
                Message = "Substring positions must be Numbers, got {type}.",
                Blocks = new List<string> { "string_substring" },
                MismatchInputs = new List<string> { "START", "LENGTH" },
                MismatchExpected = "Number",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "str-replace",
                Level = "warn",
                Message = "Replace needs text, got {type}.",
                Blocks = new List<string> { "string_replace" },
                MismatchInputs = new List<string> { "STR", "OLD", "NEW" },
                MismatchExpected = "String",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "str-index",
                Level = "warn",
                Message = "Expected text, got {type}.",
                Blocks = new List<string> { "string_index_of" },
                MismatchInputs = new List<string> { "STR", "VALUE" },
                MismatchExpected = "String",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "str-char-idx",
                Level = "err",
                Message = "Character index must be a Number, got {type}.",
                Blocks = new List<string> { "string_char_at" },
                MismatchInputs = new List<string> { "INDEX" },
                MismatchExpected = "Number",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "str-format",
                Level = "warn",
                Message = "Format pattern must be text, got {type}.",
                Blocks = new List<string> { "string_format" },
                MismatchInputs = new List<string> { "FMT" },
                MismatchExpected = "String",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "str-join-split",
                Level = "warn",
                Message = "Separator must be text, got {type}.",
                Blocks = new List<string> { "string_join", "string_split" },
                MismatchInputs = new List<string> { "SEP", "STR" },
                MismatchExpected = "String",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "str-parse",
                Level = "warn",
                Message = "Can only parse text, got {type}.",
                Blocks = new List<string> { "convert_parse_int", "convert_parse_float", "convert_parse_double", "convert_parse_bool", "datetime_parse" },
                MismatchInputs = new List<string> { "STR" },
                MismatchExpected = "String",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "num-format",
                Level = "warn",
                Message = "Format pattern must be text, got {type}.",
                Blocks = new List<string> { "convert_to_string_format", "datetime_to_string_format" },
                MismatchInputs = new List<string> { "FMT" },
                MismatchExpected = "String",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "fx-intensity",
                Level = "err",
                Message = "Intensity and duration must be Numbers, got {type}.",
                Blocks = new List<string> { "give_effect", "give_player_effect" },
                MismatchInputs = new List<string> { "intensity", "duration" },
                MismatchExpected = "Number",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "item-count",
                Level = "err",
                Message = "Item count must be a Number, got {type}.",
                Blocks = new List<string> { "give_item", "remove_item" },
                MismatchInputs = new List<string> { "count" },
                MismatchExpected = "Number",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "player-item-count",
                Level = "err",
                Message = "Item count must be a Number, got {type}.",
                Blocks = new List<string> { "give_player_items" },
                MismatchInputs = new List<string> { "COUNT" },
                MismatchExpected = "Number",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "player-vitals",
                Level = "err",
                Message = "Health and shield values must be Numbers, got {type}.",
                Blocks = new List<string> { "set_player_health", "set_player_max_health", "set_player_artificial_health", "set_player_max_artificial_health", "set_player_hume_shield", "set_player_max_hume_shield", "set_player_hume_shield_regen_rate", "set_player_hume_shield_regen_cooldown" },
                MismatchInputs = new List<string> { "HEALTH", "MAX_HEALTH", "ARTIFICIAL_HEALTH", "MAX_ARTIFICIAL_HEALTH", "HUME_SHIELD", "MAX_HUME_SHIELD", "HUME_SHIELD_REGEN_RATE", "HUME_SHIELD_REGEN_COOLDOWN" },
                MismatchExpected = "Number",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "player-xyz",
                Level = "err",
                Message = "Coordinates must be Numbers, got {type}.",
                Blocks = new List<string> { "set_player_gravity", "set_player_scale" },
                MismatchInputs = new List<string> { "x", "y", "z" },
                MismatchExpected = "Number",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "player-message",
                Level = "warn",
                Message = "Message must be text, got {type}.",
                Blocks = new List<string> { "send_player_broadcast", "send_player_hint" },
                MismatchInputs = new List<string> { "MESSAGE" },
                MismatchExpected = "String",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "effect-command-text",
                Level = "warn",
                Message = "Expected text, got {type}.",
                Blocks = new List<string> { "send_cassie", "run_command" },
                MismatchInputs = new List<string> { "message", "customSubtitles", "command" },
                MismatchExpected = "String",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "method-name",
                Level = "warn",
                Message = "Method name must be text, got {type}.",
                Blocks = new List<string> { "run_method", "run_method_instance" },
                MismatchInputs = new List<string> { "METHOD" },
                MismatchExpected = "String",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "audio-numbers",
                Level = "err",
                Message = "Audio settings must be Numbers, got {type}.",
                Blocks = new List<string> { "play_audio" },
                MismatchInputs = new List<string> { "volume", "minDistance", "maxDistance" },
                MismatchExpected = "Number",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "audio-path",
                Level = "warn",
                Message = "Audio path must be text, got {type}.",
                Blocks = new List<string> { "play_audio", "speaker_set_path", "speaker_play" },
                MismatchInputs = new List<string> { "path", "filepath" },
                MismatchExpected = "String",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "audio-spatial",
                Level = "warn",
                Message = "Expected a Boolean, got {type}.",
                Blocks = new List<string> { "play_audio", "speaker_set_is_spatial" },
                MismatchInputs = new List<string> { "isSpatial" },
                MismatchExpected = "Boolean",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "speaker-numbers",
                Level = "err",
                Message = "Speaker settings must be Numbers, got {type}.",
                Blocks = new List<string> { "speaker_set_volume", "speaker_set_min_distance", "speaker_set_max_distance" },
                MismatchInputs = new List<string> { "volume", "minDistance", "maxDistance" },
                MismatchExpected = "Number",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "speaker-loop",
                Level = "warn",
                Message = "Expected a Boolean, got {type}.",
                Blocks = new List<string> { "speaker_set_loop" },
                MismatchInputs = new List<string> { "loop" },
                MismatchExpected = "Boolean",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "prim-xyz",
                Level = "err",
                Message = "Coordinates must be Numbers, got {type}.",
                Blocks = new List<string> { "primitive_set_position", "primitive_set_rotation", "primitive_set_scale" },
                MismatchInputs = new List<string> { "x", "y", "z" },
                MismatchExpected = "Number",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "prim-rgba",
                Level = "err",
                Message = "Color channels must be Numbers from 0 to 1, got {type}.",
                Blocks = new List<string> { "primitive_set_color" },
                MismatchInputs = new List<string> { "r", "g", "b", "a" },
                MismatchExpected = "Number",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "prim-smoothing",
                Level = "err",
                Message = "Smoothing must be a Number, got {type}.",
                Blocks = new List<string> { "primitive_set_smoothing" },
                MismatchInputs = new List<string> { "movementSmoothing" },
                MismatchExpected = "Number",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "prim-static",
                Level = "warn",
                Message = "Expected a Boolean, got {type}.",
                Blocks = new List<string> { "primitive_set_static" },
                MismatchInputs = new List<string> { "isStatic" },
                MismatchExpected = "Boolean",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "waypoint-numbers",
                Level = "err",
                Message = "Waypoint values must be Numbers, got {type}.",
                Blocks = new List<string> { "waypoint_set_priority", "waypoint_set_bounds_size" },
                MismatchInputs = new List<string> { "priority", "x", "y", "z" },
                MismatchExpected = "Number",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "waypoint-visualize",
                Level = "warn",
                Message = "Expected a Boolean, got {type}.",
                Blocks = new List<string> { "waypoint_set_visualize_bounds" },
                MismatchInputs = new List<string> { "visualizeBounds" },
                MismatchExpected = "Boolean",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "object-names",
                Level = "warn",
                Message = "Name must be text, got {type}.",
                Blocks = new List<string> { "waypoint_create", "texttoy_create", "speaker_create", "primitive_create", "door_find_by_name" },
                MismatchInputs = new List<string> { "name" },
                MismatchExpected = "String",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "texttoy-text",
                Level = "warn",
                Message = "Text must be text, got {type}.",
                Blocks = new List<string> { "texttoy_set_text" },
                MismatchInputs = new List<string> { "text" },
                MismatchExpected = "String",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "texttoy-size",
                Level = "err",
                Message = "Display size must be Numbers, got {type}.",
                Blocks = new List<string> { "texttoy_set_display_size" },
                MismatchInputs = new List<string> { "width", "height" },
                MismatchExpected = "Number",
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "req-set-pos",
                Level = "warn",
                Message = "Position {input} is empty - nothing to move to.",
                Blocks = new List<string> { "unity_set_pos" },
                RequiredInputs = new List<string> { "POS" },
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "req-move-vec",
                Level = "warn",
                Message = "Vector {input} is empty - nothing happens.",
                Blocks = new List<string> { "unity_translate", "unity_rotate" },
                RequiredInputs = new List<string> { "V" },
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "req-force",
                Level = "warn",
                Message = "Force {input} is empty - nothing is applied.",
                Blocks = new List<string> { "unity_add_force" },
                RequiredInputs = new List<string> { "F" },
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "req-torque",
                Level = "warn",
                Message = "Torque {input} is empty - nothing is applied.",
                Blocks = new List<string> { "unity_add_torque" },
                RequiredInputs = new List<string> { "T" },
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "req-velocity",
                Level = "warn",
                Message = "Velocity {input} is empty.",
                Blocks = new List<string> { "unity_set_vel" },
                RequiredInputs = new List<string> { "V" },
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "req-normalize",
                Level = "warn",
                Message = "Vector {input} is empty.",
                Blocks = new List<string> { "unity_normalize" },
                RequiredInputs = new List<string> { "V" },
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "req-ray",
                Level = "warn",
                Message = "Ray {input} is empty - the check always misses.",
                Blocks = new List<string> { "unity_raycast" },
                RequiredInputs = new List<string> { "O", "D" },
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "req-distance",
                Level = "warn",
                Message = "Point {input} is empty.",
                Blocks = new List<string> { "unity_distance" },
                RequiredInputs = new List<string> { "A", "B" },
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "req-lerp-ab",
                Level = "warn",
                Message = "Lerp point {input} is empty.",
                Blocks = new List<string> { "unity_lerp" },
                RequiredInputs = new List<string> { "A", "B" },
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "req-lerp-t",
                Level = "warn",
                Message = "Lerp amount is empty.",
                Blocks = new List<string> { "unity_lerp" },
                RequiredInputs = new List<string> { "T" },
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "req-vec3-xyz",
                Level = "warn",
                Message = "Component {input} is empty.",
                Blocks = new List<string> { "unity_vec3" },
                RequiredInputs = new List<string> { "X", "Y", "Z" },
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "req-wait-t",
                Level = "warn",
                Message = "Wait time is empty.",
                Blocks = new List<string> { "unity_wait" },
                RequiredInputs = new List<string> { "T" },
            });

            BlocklyServer.RegisterCheck(new BlockCheck
            {
                Id = "req-format",
                Level = "warn",
                Message = "Format pattern is empty.",
                Blocks = new List<string> { "string_format" },
                RequiredInputs = new List<string> { "FMT" },
            });
        }
    }
}
