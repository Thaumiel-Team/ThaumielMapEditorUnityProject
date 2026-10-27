using System;
using System.Collections.Generic;

#if TME_BLOCKY
using Assets.Scripts.Networking.Blocky;
#endif

namespace Assets.Scripts.Components.Tools.Helpers
{
    [Serializable]
    public class InteractableClasses
    {
        public List<PlayAudio> PlayAudio;
        public List<RunCommand> RunCommand;
        public List<GiveEffect> GiveEffect;
        public List<RemoveEffect> RemoveEffect;
        public List<PlayAnimation> PlayAnimation;
        public List<GiveItem> GiveItem;
        public List<RemoveItem> RemoveItem;
        public List<Warhead> Warhead;
        public List<SendCassieMessage> SendCassieMessage;
#if TME_BLOCKY
        public List<CodeExportPayload> Blocky;
#endif
    }
}