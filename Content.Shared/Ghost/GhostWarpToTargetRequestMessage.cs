using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Ghost
{
    [Serializable, NetSerializable]
    public class GhostWarpToTargetRequestMessage : ComponentMessage
    {
        public EntityUid Target { get; }

        public GhostWarpToTargetRequestMessage(EntityUid target)
        {
            Target = target;
            Directed = true;
        }
    }
}
