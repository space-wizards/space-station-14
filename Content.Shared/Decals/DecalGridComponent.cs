namespace Content.Shared.Decals
{
    [RegisterComponent]
    [Friend(typeof(SharedDecalSystem))]
    public sealed class DecalGridComponent : Component
    {
        [DataField("chunkCollection", serverOnly: true)]
        public DecalGridChunkCollection ChunkCollection = new(new ());

        public record DecalGridChunkCollection(Dictionary<Vector2i, Dictionary<uint, Decal>> ChunkCollection)
        {
            public uint NextUid;
        }
    }
}
