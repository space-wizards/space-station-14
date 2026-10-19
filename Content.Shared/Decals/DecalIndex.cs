using Robust.Shared.Serialization;

namespace Content.Shared.Decals;

[Serializable, NetSerializable]
public readonly record struct DecalIndex(Vector2i Chunk, ushort Id)
{
    public override string ToString()
    {
        return $"{Chunk.X},{Chunk.Y}:{Id}";
    }
}
