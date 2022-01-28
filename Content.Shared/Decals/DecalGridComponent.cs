using System.Collections.Generic;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Decals
{
    [RegisterComponent]
    [Friend(typeof(SharedDecalSystem))]
    public class DecalGridComponent : Component
    {
        public override string Name => "DecalGrid";

        [DataField("chunkCollection", serverOnly: true)]
        public DecalGridChunkCollection ChunkCollection = new(new ());

        public record DecalGridChunkCollection(Dictionary<Vector2i, Dictionary<uint, Decal>> ChunkCollection)
        {
            public uint NextUid;
        }
    }
}
