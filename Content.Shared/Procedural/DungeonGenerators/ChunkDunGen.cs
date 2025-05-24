namespace Content.Shared.Procedural.DungeonGenerators;

/// <summary>
/// Turns a chunked area into a dungeon for layer purposes. Assumes the position is the BL origin.
/// </summary>
public sealed partial class ChunkDunGen : IDunGenLayer
{
    [DataField]
    public int Size = 16;
}
