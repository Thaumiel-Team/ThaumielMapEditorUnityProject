using System;
using System.Collections.Generic;

namespace Assets.Scripts.Networking.Blocky
{
    [Flags]
    public enum BlockConnectionType
    {
        /// <summary>
        /// No connections.
        /// </summary>
        None = 0,

        /// <summary>
        /// The small peg on the right plugs into an Input connection.
        /// Used for value blocks that return a result (e.g. a number, Vector3).
        /// Sets the block's Output in Blockly.
        /// </summary>
        Output = 1 << 0,

        /// <summary>
        /// The socket on the left accepts an Output peg.
        /// Defined as input_value in the args list.
        /// </summary>
        Input = 1 << 1,

        /// <summary>
        /// The notch on the top connects to a Next connection below it.
        /// Used for statement blocks that stack vertically.
        /// </summary>
        Previous = 1 << 2,

        /// <summary>
        /// The bump on the bottom connects to a Previous connection above it.
        /// Used for statement blocks that stack vertically.
        /// </summary>
        Next = 1 << 3,
    }

    public static class BlockConnectionTypeExtensions
    {
        private static readonly BlockConnectionType[] All =
        {
            BlockConnectionType.Output,
            BlockConnectionType.Input,
            BlockConnectionType.Previous,
            BlockConnectionType.Next,
        };

        /// <summary>
        /// Splits a combined flags value into the individual connection types.
        /// </summary>
        public static List<BlockConnectionType> ToList(this BlockConnectionType connections)
        {
            List<BlockConnectionType> result = new();
            foreach (BlockConnectionType connection in All)
            {
                if ((connections & connection) == connection)
                    result.Add(connection);
            }

            return result;
        }
    }
}
