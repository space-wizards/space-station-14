using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Observer
{
    [Serializable, NetSerializable]
    public class GhostWarpToNameRequestMessage : ComponentMessage
    {
        public GhostWarpToNameRequestMessage(string warpTarget = default)
        {
            WarpName = warpTarget;
            Directed = true;
        }

        public string WarpName { get; }
    }
}
