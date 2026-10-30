#if UNITY_EDITOR
using System;

namespace Assets.Scripts.Components.Tools.Helpers
{
    [Serializable]
    public class RunCommand
    {
        public enum CommandType
        {
            RemoteAdmin,
            Client,
            Console
        }

        public CommandType Type;
        public string Command;
    }
}
#endif