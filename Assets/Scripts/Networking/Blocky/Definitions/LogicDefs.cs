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
                Explanation = "Runs the DO body once per item. VAR holds the current item and LIST is the collection to walk.\nWorks with lists, arrays, dictionary keys, and dictionary values.",
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
                OutputType = "Array",
                Category = "Logic",
                Color = "#f51fbc",
                Tooltip = "Creates a new empty list.",
                Explanation = "Makes an empty list you can fill with Add. Store it in a variable to use it across blocks.",
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
                Explanation = "Appends ITEM to the end of LIST.\nThe list grows automatically, no size setup needed.",
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
                Explanation = "Removes the first matching ITEM found in LIST. Does nothing if the item is not in the list.",
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
                Explanation = "Removes the entry at INDEX, starting at 0 for the first item.\nLater items shift down to fill the gap.",
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
                Explanation = "Reads the entry at INDEX without changing the list. Index 0 is the first item.",
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
                OutputType = "Number",
                Category = "Logic",
                Color = "#f51fbc",
                Tooltip = "Returns the number of items in the list.",
                Explanation = "Outputs how many items LIST holds. An empty list gives 0.",
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
                OutputType = "Boolean",
                Category = "Logic",
                Color = "#f51fbc",
                Tooltip = "Returns true if the list contains the item.",
                Explanation = "Checks whether ITEM appears anywhere in LIST.\nUse it before Add to avoid duplicates.",
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
                Explanation = "Empties LIST completely. The list still exists, it just holds zero items.",
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
                Explanation = "Reads the item at index 0 without removing it.",
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
                Explanation = "Reads the final item without removing it. Handy for history and undo stacks.",
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