using System.Collections.Generic;

namespace Assets.Scripts.Networking.Blocky.Definitions
{
    public class LogicDefs : DefBase
    {
        public override void Register()
        {
            BlocklyServer.RegisterCategory("Logic", "#f51fbc", "");

            RegisterLoops();
            RegisterList();
        }

        private void RegisterLoops()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "foreach_loop",
                Category = "Logic",
                Color = "#f51fbc",
                Tooltip = "Loops through every element in the list, exposing each as a variable.",
                Message = "foreach %1 in %2 { %3 %4 }",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Next, BlockConnectionType.Previous },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Variable("VAR", "item"),
                    BlockArg.Value("LIST"),
                    BlockArg.Dummy(),
                    BlockArg.Statement("DO")
                }
            });
        }

        private void RegisterList()
        {
            // new List<T>()
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "list_create",
                Category = "Logic",
                Color = "#f51fbc",
                Tooltip = "Creates a new empty list.",
                Message = "new List",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = null
            });

            // list.Add(item)
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "list_add",
                Category = "Logic",
                Color = "#f51fbc",
                Tooltip = "Adds an item to the list.",
                Message = "%1 .Add( %2 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Next, BlockConnectionType.Previous },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("LIST"),
                    BlockArg.Value("ITEM")
                }
            });

            // list.Remove(item)
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "list_remove",
                Category = "Logic",
                Color = "#f51fbc",
                Tooltip = "Removes an item from the list.",
                Message = "%1 .Remove( %2 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Next, BlockConnectionType.Previous },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("LIST"),
                    BlockArg.Value("ITEM")
                }
            });

            // list.RemoveAt(index)
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "list_remove_at",
                Category = "Logic",
                Color = "#f51fbc",
                Tooltip = "Removes the item at the given index.",
                Message = "%1 .RemoveAt( %2 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Next, BlockConnectionType.Previous },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("LIST"),
                    BlockArg.NumberField("INDEX", 0, 0)
                }
            });

            // list[index]
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "list_get",
                Category = "Logic",
                Color = "#f51fbc",
                Tooltip = "Gets the item at the given index.",
                Message = "%1 [ %2 ]",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("LIST"),
                    BlockArg.NumberField("INDEX", 0, 0)
                }
            });

            // list.Count
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "list_count",
                Category = "Logic",
                Color = "#f51fbc",
                Tooltip = "Returns the number of items in the list.",
                Message = "%1 .Count",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("LIST")
                }
            });

            // list.Contains(item)
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "list_contains",
                Category = "Logic",
                Color = "#f51fbc",
                Tooltip = "Returns true if the list contains the item.",
                Message = "%1 .Contains( %2 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("LIST"),
                    BlockArg.Value("ITEM")
                }
            });

            // list.Clear()
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "list_clear",
                Category = "Logic",
                Color = "#f51fbc",
                Tooltip = "Removes all items from the list.",
                Message = "%1 .Clear()",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Next, BlockConnectionType.Previous },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("LIST")
                }
            });

            // list.First()
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "list_first",
                Category = "Logic",
                Color = "#f51fbc",
                Tooltip = "Returns the first item in the list.",
                Message = "%1 .First()",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("LIST")
                }
            });

            // list.Last()
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "list_last",
                Category = "Logic",
                Color = "#f51fbc",
                Tooltip = "Returns the last item in the list.",
                Message = "%1 .Last()",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("LIST")
                }
            });
        }
    }
}