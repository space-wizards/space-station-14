using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Observer
{
    [Serializable, NetSerializable]
    public class GhostWarpToLocationRequestMessage : ComponentMessage
    {
        public string Name { get; }

        public GhostWarpToLocationRequestMessage(string name)
        {
            Name = name;
            Directed = true;
        }
    }
}
