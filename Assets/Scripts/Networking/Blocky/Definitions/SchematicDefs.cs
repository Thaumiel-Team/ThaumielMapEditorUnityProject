using System.Collections.Generic;

namespace Assets.Scripts.Networking.Blocky.Definitions
{
    public class SchematicDefs : DefBase
    {
        private const string CategoryName = "Schematic";
        private const string CategoryColor = "#37474F";

        public override void Register()
        {
            BlocklyServer.RegisterCategory(CategoryName, CategoryColor, "");

            RegisterSpawn();
            RegisterSpawnRotated();
            RegisterSpawnScaled();
            RegisterSpawnFromPath();
            RegisterSpawnFromPathRotated();
            RegisterSpawnFromPathScaled();
            RegisterSpawnFromDirectory();
            RegisterSpawnFromDirectoryRotated();
            RegisterSpawnFromDirectoryScaled();
            RegisterSpawnSerialized();
            RegisterSpawnSerializedRotated();
            RegisterSpawnSerializedScaled();
        }

        private void RegisterSpawn()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "schematic_spawn",
                OutputType = "Schematic",
                Category = CategoryName,
                Color = CategoryColor,
                Tooltip = "Spawn a loaded schematic by name at a position.",
                Explanation = "Uses Loader.SpawnSchematic(schematicName, position).\nThe name must match a loaded schematic file.",
                Message = "Spawn Schematic  Name: %1  Position: %2",
                Connections = BlockConnectionType.Output,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.TextField("schematicName", ""),
                    BlockArg.Value("Position", "Vector3")
                }
            });
        }

        private void RegisterSpawnRotated()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "schematic_spawn_rotated",
                OutputType = "Schematic",
                Category = CategoryName,
                Color = CategoryColor,
                Tooltip = "Spawn a loaded schematic by name at a position with a rotation.",
                Explanation = "Uses Loader.SpawnSchematic(schematicName, position, rotation).\nLeave Rotation empty for the default rotation.",
                Message = "Spawn Schematic  Name: %1  Position: %2  Rotation: %3",
                Connections = BlockConnectionType.Output,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.TextField("schematicName", ""),
                    BlockArg.Value("Position", "Vector3"),
                    BlockArg.Value("Rotation", "Quaternion")
                }
            });
        }

        private void RegisterSpawnScaled()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "schematic_spawn_scaled",
                OutputType = "Schematic",
                Category = CategoryName,
                Color = CategoryColor,
                Tooltip = "Spawn a loaded schematic by name at a position, rotation, and scale.",
                Explanation = "Uses Loader.SpawnSchematic(schematicName, position, rotation, scale).\nLeave Rotation or Scale empty for their defaults.",
                Message = "Spawn Schematic  Name: %1  Position: %2  Rotation: %3  Scale: %4",
                Connections = BlockConnectionType.Output,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.TextField("schematicName", ""),
                    BlockArg.Value("Position", "Vector3"),
                    BlockArg.Value("Rotation", "Quaternion"),
                    BlockArg.Value("Scale", "Vector3")
                }
            });
        }

        private void RegisterSpawnFromPath()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "schematic_spawn_from_path",
                OutputType = "Schematic",
                Category = CategoryName,
                Color = CategoryColor,
                Tooltip = "Spawn a schematic from a YAML file path at a position.",
                Explanation = "Uses Loader.SpawnSchematicFromPath(path, position).\nThe file is loaded on demand.",
                Message = "Spawn Schematic from Path  Path: %1  Position: %2",
                Connections = BlockConnectionType.Output,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.TextField("path", ""),
                    BlockArg.Value("Position", "Vector3")
                }
            });
        }

        private void RegisterSpawnFromPathRotated()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "schematic_spawn_from_path_rotated",
                OutputType = "Schematic",
                Category = CategoryName,
                Color = CategoryColor,
                Tooltip = "Spawn a schematic from a YAML file path at a position with a rotation.",
                Explanation = "Uses Loader.SpawnSchematicFromPath(path, position, rotation).\nLeave Rotation empty for the default rotation.",
                Message = "Spawn Schematic from Path  Path: %1  Position: %2  Rotation: %3",
                Connections = BlockConnectionType.Output,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.TextField("path", ""),
                    BlockArg.Value("Position", "Vector3"),
                    BlockArg.Value("Rotation", "Quaternion")
                }
            });
        }

        private void RegisterSpawnFromPathScaled()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "schematic_spawn_from_path_scaled",
                OutputType = "Schematic",
                Category = CategoryName,
                Color = CategoryColor,
                Tooltip = "Spawn a schematic from a YAML file path at a position, rotation, and scale.",
                Explanation = "Uses Loader.SpawnSchematicFromPath(path, position, rotation, scale).\nLeave Rotation or Scale empty for their defaults.",
                Message = "Spawn Schematic from Path  Path: %1  Position: %2  Rotation: %3  Scale: %4",
                Connections = BlockConnectionType.Output,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.TextField("path", ""),
                    BlockArg.Value("Position", "Vector3"),
                    BlockArg.Value("Rotation", "Quaternion"),
                    BlockArg.Value("Scale", "Vector3")
                }
            });
        }

        private void RegisterSpawnFromDirectory()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "schematic_spawn_from_directory",
                OutputType = "Schematic",
                Category = CategoryName,
                Color = CategoryColor,
                Tooltip = "Spawn a schematic by name from a custom directory at a position.",
                Explanation = "Uses Loader.SpawnSchematicFromDirectory(directory, schematicName, position).\nThe schematic is loaded from that directory on demand.",
                Message = "Spawn Schematic from Directory  Directory: %1  Name: %2  Position: %3",
                Connections = BlockConnectionType.Output,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.TextField("directory", ""),
                    BlockArg.TextField("schematicName", ""),
                    BlockArg.Value("Position", "Vector3")
                }
            });
        }

        private void RegisterSpawnFromDirectoryRotated()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "schematic_spawn_from_directory_rotated",
                OutputType = "Schematic",
                Category = CategoryName,
                Color = CategoryColor,
                Tooltip = "Spawn a schematic by name from a custom directory at a position with a rotation.",
                Explanation = "Uses Loader.SpawnSchematicFromDirectory(directory, schematicName, position, rotation).\nLeave Rotation empty for the default rotation.",
                Message = "Spawn Schematic from Directory  Directory: %1  Name: %2  Position: %3  Rotation: %4",
                Connections = BlockConnectionType.Output,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.TextField("directory", ""),
                    BlockArg.TextField("schematicName", ""),
                    BlockArg.Value("Position", "Vector3"),
                    BlockArg.Value("Rotation", "Quaternion")
                }
            });
        }

        private void RegisterSpawnFromDirectoryScaled()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "schematic_spawn_from_directory_scaled",
                OutputType = "Schematic",
                Category = CategoryName,
                Color = CategoryColor,
                Tooltip = "Spawn a schematic by name from a custom directory at a position, rotation, and scale.",
                Explanation = "Uses Loader.SpawnSchematicFromDirectory(directory, schematicName, position, rotation, scale).\nLeave Rotation or Scale empty for their defaults.",
                Message = "Spawn Schematic from Directory  Directory: %1  Name: %2  Position: %3  Rotation: %4  Scale: %5",
                Connections = BlockConnectionType.Output,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.TextField("directory", ""),
                    BlockArg.TextField("schematicName", ""),
                    BlockArg.Value("Position", "Vector3"),
                    BlockArg.Value("Rotation", "Quaternion"),
                    BlockArg.Value("Scale", "Vector3")
                }
            });
        }

        private void RegisterSpawnSerialized()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "schematic_spawn_serialized",
                OutputType = "Schematic",
                Category = CategoryName,
                Color = CategoryColor,
                Tooltip = "Spawn an already loaded SerializableSchematic at a position.",
                Explanation = "Uses Loader.SpawnSchematic(schematic, position).\nConnect a SerializableSchematic value, not a spawned Schematic.",
                Message = "Spawn Serializable Schematic  Schematic: %1  Position: %2",
                Connections = BlockConnectionType.Output,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("SerializableSchematic", "SerializableSchematic"),
                    BlockArg.Value("Position", "Vector3")
                }
            });
        }

        private void RegisterSpawnSerializedRotated()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "schematic_spawn_serialized_rotated",
                OutputType = "Schematic",
                Category = CategoryName,
                Color = CategoryColor,
                Tooltip = "Spawn an already loaded SerializableSchematic at a position with a rotation.",
                Explanation = "Uses Loader.SpawnSchematic(schematic, position, rotation).\nLeave Rotation empty for the default rotation.",
                Message = "Spawn Serializable Schematic  Schematic: %1  Position: %2  Rotation: %3",
                Connections = BlockConnectionType.Output,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("SerializableSchematic", "SerializableSchematic"),
                    BlockArg.Value("Position", "Vector3"),
                    BlockArg.Value("Rotation", "Quaternion")
                }
            });
        }

        private void RegisterSpawnSerializedScaled()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "schematic_spawn_serialized_scaled",
                OutputType = "Schematic",
                Category = CategoryName,
                Color = CategoryColor,
                Tooltip = "Spawn an already loaded SerializableSchematic at a position, rotation, and scale.",
                Explanation = "Uses Loader.SpawnSchematic(schematic, position, rotation, scale).\nLeave Rotation or Scale empty for their defaults.",
                Message = "Spawn Serializable Schematic  Schematic: %1  Position: %2  Rotation: %3  Scale: %4",
                Connections = BlockConnectionType.Output,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("SerializableSchematic", "SerializableSchematic"),
                    BlockArg.Value("Position", "Vector3"),
                    BlockArg.Value("Rotation", "Quaternion"),
                    BlockArg.Value("Scale", "Vector3")
                }
            });
        }
    }
}
