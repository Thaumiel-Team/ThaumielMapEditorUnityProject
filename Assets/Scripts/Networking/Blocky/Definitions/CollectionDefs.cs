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
                Explanation = "Makes an empty key/value store. Add entries with Add, then look them up by key.",
                Message = "new Dictionary",
                Connections = BlockConnectionType.Output,
                Args = null,
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "dict_add",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Adds a key/value pair to the dictionary.",
                Explanation = "Stores VALUE under KEY in DICT.\nKeys must be unique, use Set to overwrite an existing key.",
                Message = "%1 .Add( %2 , %3 )",
                Connections = BlockConnectionType.Previous | BlockConnectionType.Next,
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
                Explanation = "Deletes the KEY entry from DICT. Does nothing if the key is missing.",
                Message = "%1 .Remove( %2 )",
                Connections = BlockConnectionType.Previous | BlockConnectionType.Next,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("DICT"),
                    BlockArg.Value("KEY")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "dict_contains_key",
                OutputType = "Boolean",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Returns true if the dictionary contains the given key.",
                Explanation = "Checks whether KEY exists in DICT.\nUse it before Get to avoid missing-key errors.",
                Message = "%1 .ContainsKey( %2 )",
                Connections = BlockConnectionType.Output,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("DICT"),
                    BlockArg.Value("KEY")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "dict_contains_value",
                OutputType = "Boolean",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Returns true if the dictionary contains the given value.",
                Explanation = "Scans every entry and returns true when VALUE is found.\nSlower than a key check on big dictionaries.",
                Message = "%1 .ContainsValue( %2 )",
                Connections = BlockConnectionType.Output,
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
                Explanation = "Reads the VALUE filed under KEY. The key must exist, so check ContainsKey first.",
                Message = "%1 [ %2 ]",
                Connections = BlockConnectionType.Output,
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
                Explanation = "Writes VALUE under KEY, adding the entry when the key is new.\nUnlike Add, this never fails on existing keys.",
                Message = "%1 [ %2 ] = %3",
                Connections = BlockConnectionType.Previous | BlockConnectionType.Next,
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
                OutputType = "Number",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Returns the number of entries in the dictionary.",
                Explanation = "Outputs how many key/value pairs DICT holds.",
                Message = "%1 .Count",
                Connections = BlockConnectionType.Output,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("DICT")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "dict_keys",
                OutputType = "Array",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Returns the collection of keys in the dictionary. Usable in a foreach loop.",
                Explanation = "Outputs all keys at once. Plug it into foreach to visit every entry.",
                Message = "%1 .Keys",
                Connections = BlockConnectionType.Output,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("DICT")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "dict_values",
                OutputType = "Array",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Returns the collection of values in the dictionary. Usable in a foreach loop.",
                Explanation = "Outputs all stored values at once. Plug it into foreach to process each one.",
                Message = "%1 .Values",
                Connections = BlockConnectionType.Output,
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
                Explanation = "Empties DICT completely while keeping the dictionary itself usable.",
                Message = "%1 .Clear()",
                Connections = BlockConnectionType.Previous | BlockConnectionType.Next,
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
                Explanation = "Makes an empty set: a collection where every item appears at most once.",
                Message = "new HashSet",
                Connections = BlockConnectionType.Output,
                Args = null,
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "hashset_add",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Adds an item to the set if it is not already present.",
                Explanation = "Inserts ITEM into SET. Adding a duplicate changes nothing, so sets are ideal for tracking seen things.",
                Message = "%1 .Add( %2 )",
                Connections = BlockConnectionType.Previous | BlockConnectionType.Next,
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
                Explanation = "Deletes ITEM from SET. Does nothing when the item is missing.",
                Message = "%1 .Remove( %2 )",
                Connections = BlockConnectionType.Previous | BlockConnectionType.Next,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("SET"),
                    BlockArg.Value("ITEM")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "hashset_contains",
                OutputType = "Boolean",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Returns true if the set contains the item.",
                Explanation = "Very fast membership test, even for large sets.\nUse it instead of list Contains when order does not matter.",
                Message = "%1 .Contains( %2 )",
                Connections = BlockConnectionType.Output,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("SET"),
                    BlockArg.Value("ITEM")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "hashset_count",
                OutputType = "Number",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Returns the number of items in the set.",
                Explanation = "Outputs how many unique items SET holds.",
                Message = "%1 .Count",
                Connections = BlockConnectionType.Output,
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
                Explanation = "Empties SET while keeping it usable.",
                Message = "%1 .Clear()",
                Connections = BlockConnectionType.Previous | BlockConnectionType.Next,
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
                Explanation = "Merges OTHER into SET. After this, SET holds every item from both.",
                Message = "%1 .UnionWith( %2 )",
                Connections = BlockConnectionType.Previous | BlockConnectionType.Next,
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
                Explanation = "Shrinks SET down to the items it shares with OTHER.\nUseful for finding common tags or permissions.",
                Message = "%1 .IntersectWith( %2 )",
                Connections = BlockConnectionType.Previous | BlockConnectionType.Next,
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
                Explanation = "Makes an empty LIFO stack: the last item pushed is the first one popped.",
                Message = "new Stack",
                Connections = BlockConnectionType.Output,
                Args = null,
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "stack_push",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Pushes an item onto the top of the stack.",
                Explanation = "Places ITEM on top of STACK.",
                Message = "%1 .Push( %2 )",
                Connections = BlockConnectionType.Previous | BlockConnectionType.Next,
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
                Explanation = "Takes the top item off STACK and outputs it.\nUse it for undo history or nested tasks.",
                Message = "%1 .Pop()",
                Connections = BlockConnectionType.Output,
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
                Explanation = "Looks at the top item while leaving STACK unchanged.",
                Message = "%1 .Peek()",
                Connections = BlockConnectionType.Output,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("STACK")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "stack_count",
                OutputType = "Number",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Returns the number of items in the stack.",
                Explanation = "Outputs how many items are stacked up.",
                Message = "%1 .Count",
                Connections = BlockConnectionType.Output,
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
                Explanation = "Makes an empty FIFO queue: items leave in the order they arrived.",
                Message = "new Queue",
                Connections = BlockConnectionType.Output,
                Args = null,
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "queue_enqueue",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Adds an item to the back of the queue.",
                Explanation = "Lines ITEM up at the back of QUEUE.",
                Message = "%1 .Enqueue( %2 )",
                Connections = BlockConnectionType.Previous | BlockConnectionType.Next,
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
                Explanation = "Takes the longest-waiting item and outputs it.\nUse it to process tasks in order.",
                Message = "%1 .Dequeue()",
                Connections = BlockConnectionType.Output,
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
                Explanation = "Looks at the next item while leaving QUEUE unchanged.",
                Message = "%1 .Peek()",
                Connections = BlockConnectionType.Output,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("QUEUE")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "queue_count",
                OutputType = "Number",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Returns the number of items in the queue.",
                Explanation = "Outputs how many items are waiting in line.",
                Message = "%1 .Count",
                Connections = BlockConnectionType.Output,
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
                OutputType = "Array",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Creates a new array with the given number of elements.",
                Explanation = "Makes a fixed-size array with SIZE slots, indexed from 0.\nUnlike lists, an array cannot grow later.",
                Message = "new object[ %1 ]",
                Connections = BlockConnectionType.Output,
                Args = new List<Dictionary<string, object>>
                {
                    BlockArg.Value("SIZE", "Number")
                },
            });

            BlocklyServer.RegisterBlock(new BlockDefinition
            {
                Id = "array_length",
                OutputType = "Number",
                Category = "Collections",
                Color = "#745BA5",
                Tooltip = "Returns the number of elements in the array.",
                Explanation = "Outputs the fixed size of ARR.",
                Message = "%1 .Length",
                Connections = BlockConnectionType.Output,
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
                Explanation = "Reads slot INDEX without changing ARR. Valid indexes run from 0 to Length - 1.",
                Message = "%1 [ %2 ]",
                Connections = BlockConnectionType.Output,
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
                Explanation = "Overwrites slot INDEX with VALUE.\nThe index must already exist, arrays cannot grow.",
                Message = "%1 [ %2 ] = %3",
                Connections = BlockConnectionType.Previous | BlockConnectionType.Next,
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
