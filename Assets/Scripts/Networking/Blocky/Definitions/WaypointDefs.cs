using System.Collections.Generic;

namespace Assets.Scripts.Networking.Blocky.Definitions
{
    public class WaypointDefs : DefBase
    {
        private enum Properties
        {
            Rotation,
            Scale,
            Position,
            Base,
            VisualizeBounds,
            Priority,
            BoundsSize,
            WaypointId
        }

        public override void Register()
        {
            BlocklyServer.RegisterCategory("Waypoint", "#1565C0", "");

            RegisterCreateWaypoint();
            RegisterSetVisualizeBounds();
            RegisterSetPriority();
            RegisterSetBoundsSize();
            RegisterGetProperty();
        }

        private void RegisterGetProperty()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "get_waypoint_property",
                PropertyTypes = new Dictionary<string, string>
                {
                    ["Rotation"] = "Quaternion",
                    ["Scale"] = "Vector3",
                    ["Position"] = "Vector3",
                    ["VisualizeBounds"] = "Boolean",
                    ["Priority"] = "Number",
                    ["BoundsSize"] = "Vector3",
                },
                Category = "Waypoint",
                Color = "#1565C0",
                Tooltip = "Gets a property of the Waypoint.",
                Explanation = "Reads one Property (priority, bounds, and more) from the Waypoint object.",
                Message = "Get %2 of Waypoint: %1",
                Connections = BlockConnectionType.Output,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Waypoint"),
                    BlockArg.Dropdown("Property", EnumOptions<Properties>())
                }
            });
        }

        private void RegisterCreateWaypoint()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "waypoint_create",
                OutputType = "Waypoint",
                Category = "Waypoint",
                Color = "#1565C0",
                Tooltip = "Create a new WaypointObject with a given name.",
                Explanation = "Spawns a waypoint marker and outputs it for wiring.",
                Message = "Create Waypoint  Name: %1",
                Connections = BlockConnectionType.Output,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.TextField("name", "MyWaypoint")
                }
            });
        }

        private void RegisterSetVisualizeBounds()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "waypoint_set_visualize_bounds",
                Category = "Waypoint",
                Color = "#1565C0",
                Tooltip = "Toggle whether the waypoint's bounds are visualized in the editor or at runtime.",
                Explanation = "Shows the bounds box for debugging layouts.\nTurn off for the final version to hide helpers.",
                Message = "Set Visualize Bounds of Waypoint: %1 → %2",
                Connections = BlockConnectionType.Previous | BlockConnectionType.Next,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Waypoint"),
                    BlockArg.Checkbox("visualizeBounds", false)
                }
            });
        }

        private void RegisterSetPriority()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "waypoint_set_priority",
                Category = "Waypoint",
                Color = "#1565C0",
                Tooltip = "Set the priority value of the waypoint. Higher values influence ordering or selection logic.",
                Explanation = "Higher numbers win when systems pick between waypoints.",
                Message = "Set Priority of Waypoint: %1 → %2",
                Connections = BlockConnectionType.Previous | BlockConnectionType.Next,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Waypoint"),
                    BlockArg.NumberField("priority", 0.0)
                }
            });
        }

        private void RegisterSetBoundsSize()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "waypoint_set_bounds_size",
                Category = "Waypoint",
                Color = "#1565C0",
                Tooltip = "Set the size of the waypoint bounds.",
                Explanation = "Resizes the trigger box with x, y, z extents.",
                Message = "Set Bounds Size of Waypoint: %1 →  x: %2  y: %3  z: %4",
                Connections = BlockConnectionType.Previous | BlockConnectionType.Next,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Waypoint"),
                    BlockArg.NumberField("x", 1.0),
                    BlockArg.NumberField("y", 1.0),
                    BlockArg.NumberField("z", 1.0)
                }
            });
        }
    }
}