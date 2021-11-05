using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Decals
{
    [Serializable, NetSerializable]
    public class DecalIndexCheckEvent : EntityEventArgs
    {
        public HashSet<uint> SeenIndices = new();
    }
}
