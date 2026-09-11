using System.Collections.Generic;
using Assets.Scripts.Networking.Blocky.Definitions;
using UnityEditor;

namespace Assets.Scripts.Networking.Blocky
{
    [InitializeOnLoad]
    public static class BlocklyDefinitionRegistry
    {
        private static readonly List<DefBase> _defs = new()
        {
            new ObjectDefs(),
            new DoorDefs(),
            new PrimitiveDefs(),
            new WaypointDefs(),
            new TextToyDefs(),
            new SpeakerDefs(),
            new PlayerDefs(),
            new TimingDefs(),
            new EnumDefs(),
            new LogicDefs(),
            new StringDefs(),
            new MathDefs(),
            new CollectionDefs(),
            new DateTimeDefs(),
            new ConvertDefs()
        };

        static BlocklyDefinitionRegistry()
        {
            BlocklyServer.OnClientConnected += RegisterAll;
        }

        private static void RegisterAll()
        {
            foreach (DefBase def in _defs)
            {
                def.Register();
            }
        }
    }
}