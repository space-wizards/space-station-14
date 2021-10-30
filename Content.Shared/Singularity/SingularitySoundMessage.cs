using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Singularity
{
    [Serializable, NetSerializable]
#pragma warning disable 618
    public class SingularitySoundMessage : ComponentMessage
#pragma warning restore 618
    {
        public bool Start { get; }

        public SingularitySoundMessage(bool start)
        {
            Directed = true;
            Start = start;
        }
    }


}
