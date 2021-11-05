using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.Decals
{
    [Serializable, NetSerializable]
    public class DecalChunkUpdateEvent : EntityEventArgs
    {
        public Dictionary<GridId, HashSet<Vector2i>> UpdatedChunks = new ();
    }
}
