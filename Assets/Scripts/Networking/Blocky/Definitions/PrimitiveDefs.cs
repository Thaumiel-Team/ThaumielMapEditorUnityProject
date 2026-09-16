using Assets.Scripts.Enums;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Networking.Blocky.Definitions
{
    public class PrimitiveDefs : DefBase
    {
        private enum Properties
        {
            Rotation,
            Scale,
            Position,
            Base,
            Color,
            PrimitiveType,
            PrimitiveFlags,
            NetId,
            IsStatic,
            MovementSmoothing,
            ServerCollider,
            Schematic
        }

        public override void Register()
        {
            BlocklyServer.RegisterCategory("Primitive", "#6A1B9A", "");

            RegisterCreatePrimitive();
            RegisterSetPosition();
            RegisterSetRotation();
            RegisterSetScale();
            RegisterSetColor();
            RegisterSetPrimitiveType();
            RegisterSetPrimitiveFlags();
            RegisterSetIsStatic();
            RegisterSetMovementSmoothing();
            RegisterGetProperty();
        }

        private void RegisterGetProperty()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "get_primitive_property",
                Category = "Primitive",
                Color = "#6A1B9A",
                Tooltip = "Gets a property of the primitive.",
                Message = "Get %2 of Primitive: %1",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Primitive"),
                    BlockArg.Dropdown("Property", EnumOptions<Properties>())
                }
            });
        }

        private void RegisterCreatePrimitive()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "primitive_create",
                Category = "Primitive",
                Color = "#6A1B9A",
                Tooltip = "Create a new PrimitiveObject with a given name. If IsServerSide is true this is spawned on the server instead of only the client.",
                Message = "Create Primitive  Name: %1  Server Side: %2",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.TextField("name", "MyPrimitive"),
                    BlockArg.Checkbox("isServerSide", false)
                }
            });
        }

        private void RegisterSetPosition()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "primitive_set_position",
                Category = "Primitive",
                Color = "#6A1B9A",
                Tooltip = "Set the world position of the PrimitiveObject. Syncs to all spawned players.",
                Message = "Set Position of Primitive: %1 →  x: %2  y: %3  z: %4",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Primitive"),
                    BlockArg.NumberField("x", 0.0),
                    BlockArg.NumberField("y", 0.0),
                    BlockArg.NumberField("z", 0.0)
                }
            });
        }

        private void RegisterSetRotation()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "primitive_set_rotation",
                Category = "Primitive",
                Color = "#6A1B9A",
                Tooltip = "Set the rotation (Euler angles) of the PrimitiveObject. Syncs to all spawned players.",
                Message = "Set Rotation of Primitive: %1 →  x: %2  y: %3  z: %4",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Primitive"),
                    BlockArg.NumberField("x", 0.0),
                    BlockArg.NumberField("y", 0.0),
                    BlockArg.NumberField("z", 0.0)
                }
            });
        }

        private void RegisterSetScale()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "primitive_set_scale",
                Category = "Primitive",
                Color = "#6A1B9A",
                Tooltip = "Set the scale of the PrimitiveObject. Syncs to all spawned players.",
                Message = "Set Scale of Primitive: %1 →  x: %2  y: %3  z: %4",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Primitive"),
                    BlockArg.NumberField("x", 1.0),
                    BlockArg.NumberField("y", 1.0),
                    BlockArg.NumberField("z", 1.0)
                }
            });
        }

        private void RegisterSetColor()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "primitive_set_color",
                Category = "Primitive",
                Color = "#6A1B9A",
                Tooltip = "Set the RGBA color of the PrimitiveObject. Values are 0.0–1.0. Syncs to all spawned players.",
                Message = "Set Color of Primitive: %1 →  r: %2  g: %3  b: %4  a: %5",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Primitive"),
                    BlockArg.NumberField("r", 1.0, 0.0, 1.0),
                    BlockArg.NumberField("g", 1.0, 0.0, 1.0),
                    BlockArg.NumberField("b", 1.0, 0.0, 1.0),
                    BlockArg.NumberField("a", 1.0, 0.0, 1.0)
                }
            });
        }

        private void RegisterSetPrimitiveType()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "primitive_set_type",
                Category = "Primitive",
                Color = "#6A1B9A",
                Tooltip = "Set the mesh shape of the PrimitiveObject. Syncs to all spawned players.",
                Message = "Set Shape of Primitive: %1 → %2",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Primitive"),
                    BlockArg.Dropdown("primitiveType", EnumOptions<PrimitiveType>())
                }
            });
        }

        private void RegisterSetPrimitiveFlags()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "primitive_set_flags",
                Category = "Primitive",
                Color = "#6A1B9A",
                Tooltip = "Set the PrimitiveFlags on the PrimitiveObject. Controls visibility and collision behaviour.",
                Message = "Set Flags of Primitive: %1 → %2",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Primitive"),
                    BlockArg.Dropdown("primitiveFlags", EnumOptions<PrimitiveFlags>())
                }
            });
        }

        private void RegisterSetIsStatic()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "primitive_set_static",
                Category = "Primitive",
                Color = "#6A1B9A",
                Tooltip = "Toggle whether the PrimitiveObject is treated as a static (non-moving) object. Syncs to all spawned players.",
                Message = "Set Static of Primitive: %1 → %2",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Primitive"),
                    BlockArg.Checkbox("isStatic", false)
                }
            });
        }

        private void RegisterSetMovementSmoothing()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "primitive_set_smoothing",
                Category = "Primitive",
                Color = "#6A1B9A",
                Tooltip = "Set the movement smoothing byte (0–255) for interpolation on clients.",
                Message = "Set Movement Smoothing of Primitive: %1 → %2",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("Primitive"),
                    BlockArg.NumberField("movementSmoothing", 0, 0, 255)
                }
            });
        }
    }
}