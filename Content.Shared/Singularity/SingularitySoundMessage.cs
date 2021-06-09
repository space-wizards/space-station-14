#nullable enable
using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.EntitySystemMessages
{
    [Serializable, NetSerializable]
    public class SingularitySoundMessage : ComponentMessage
    {
        public bool Start { get; }

        public SingularitySoundMessage(bool start)
        {
            Directed = true;
            Start = start;
        }
    }


}
