using System.Collections.Generic;

namespace Assets.Scripts.Networking.Blocky.Definitions
{
    public class CollectionDefs : DefBase
    {
        public override void Register()
        {
            BlocklyServer.RegisterCategory("Collections", "#745BA5", "");

            RegisterDictionary();
            RegisterHashSet();
            RegisterStack();
            RegisterQueue();
            RegisterArray();
        }

        private void RegisterDictionary()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "dict_create",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Creates a new empty Dictionary.",
                Message = "new Dictionary",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = null,
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "dict_add",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Adds a key/value pair to the dictionary.",
                Message = "%1 .Add( %2 , %3 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("DICT"),
                    BlockArg.Value("KEY"),
                    BlockArg.Value("VALUE")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "dict_remove",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Removes the entry with the given key from the dictionary.",
                Message = "%1 .Remove( %2 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("DICT"),
                    BlockArg.Value("KEY")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "dict_contains_key",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Returns true if the dictionary contains the given key.",
                Message = "%1 .ContainsKey( %2 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("DICT"),
                    BlockArg.Value("KEY")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "dict_contains_value",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Returns true if the dictionary contains the given value.",
                Message = "%1 .ContainsValue( %2 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("DICT"),
                    BlockArg.Value("VALUE")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "dict_get",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Gets the value stored under the given key.",
                Message = "%1 [ %2 ]",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("DICT"),
                    BlockArg.Value("KEY")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "dict_set",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Sets the value stored under the given key.",
                Message = "%1 [ %2 ] = %3",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("DICT"),
                    BlockArg.Value("KEY"),
                    BlockArg.Value("VALUE")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "dict_count",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Returns the number of entries in the dictionary.",
                Message = "%1 .Count",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("DICT")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "dict_keys",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Returns the collection of keys in the dictionary. Usable in a foreach loop.",
                Message = "%1 .Keys",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("DICT")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "dict_values",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Returns the collection of values in the dictionary. Usable in a foreach loop.",
                Message = "%1 .Values",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("DICT")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "dict_clear",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Removes all entries from the dictionary.",
                Message = "%1 .Clear()",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("DICT")
                },
            });
        }

        private void RegisterHashSet()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "hashset_create",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Creates a new empty HashSet.",
                Message = "new HashSet",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = null,
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "hashset_add",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Adds an item to the set if it is not already present.",
                Message = "%1 .Add( %2 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("SET"),
                    BlockArg.Value("ITEM")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "hashset_remove",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Removes an item from the set if it is present.",
                Message = "%1 .Remove( %2 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("SET"),
                    BlockArg.Value("ITEM")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "hashset_contains",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Returns true if the set contains the item.",
                Message = "%1 .Contains( %2 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("SET"),
                    BlockArg.Value("ITEM")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "hashset_count",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Returns the number of items in the set.",
                Message = "%1 .Count",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("SET")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "hashset_clear",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Removes all items from the set.",
                Message = "%1 .Clear()",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("SET")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "hashset_union_with",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Adds all items from the other set into this set.",
                Message = "%1 .UnionWith( %2 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("SET"),
                    BlockArg.Value("OTHER")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "hashset_intersect_with",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Keeps only the items that are present in both sets.",
                Message = "%1 .IntersectWith( %2 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("SET"),
                    BlockArg.Value("OTHER")
                },
            });
        }

        private void RegisterStack()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "stack_create",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Creates a new empty Stack (last in, first out).",
                Message = "new Stack",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = null,
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "stack_push",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Pushes an item onto the top of the stack.",
                Message = "%1 .Push( %2 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("STACK"),
                    BlockArg.Value("ITEM")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "stack_pop",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Removes and returns the top item of the stack.",
                Message = "%1 .Pop()",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("STACK")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "stack_peek",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Returns the top item of the stack without removing it.",
                Message = "%1 .Peek()",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("STACK")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "stack_count",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Returns the number of items in the stack.",
                Message = "%1 .Count",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("STACK")
                },
            });
        }

        private void RegisterQueue()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "queue_create",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Creates a new empty Queue (first in, first out).",
                Message = "new Queue",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = null,
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "queue_enqueue",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Adds an item to the back of the queue.",
                Message = "%1 .Enqueue( %2 )",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("QUEUE"),
                    BlockArg.Value("ITEM")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "queue_dequeue",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Removes and returns the item at the front of the queue.",
                Message = "%1 .Dequeue()",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("QUEUE")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "queue_peek",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Returns the item at the front of the queue without removing it.",
                Message = "%1 .Peek()",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("QUEUE")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "queue_count",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Returns the number of items in the queue.",
                Message = "%1 .Count",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("QUEUE")
                },
            });
        }

        private void RegisterArray()
        {
            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "array_create",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Creates a new array with the given number of elements.",
                Message = "new object[ %1 ]",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("SIZE", "Number")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "array_length",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Returns the number of elements in the array.",
                Message = "%1 .Length",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("ARR")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "array_get",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Gets the element at the given index of the array.",
                Message = "%1 [ %2 ]",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Output },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("ARR"),
                    BlockArg.Value("INDEX", "Number")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "array_set",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Sets the element at the given index of the array.",
                Message = "%1 [ %2 ] = %3",
                Connections = new List<BlockConnectionType> { BlockConnectionType.Previous, BlockConnectionType.Next },
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("ARR"),
                    BlockArg.Value("INDEX", "Number"),
                    BlockArg.Value("VALUE")
                },
            });
        }
    }
}
