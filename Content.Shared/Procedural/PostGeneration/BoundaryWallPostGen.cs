using Content.Shared.Maps;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Procedural.PostGeneration;

/// <summary>
/// Iterates room edges and places the relevant tiles and walls on any free indices.
/// </summary>
public sealed partial class BoundaryWallPostGen : IPostDunGen
{
    [DataField]
    public ProtoId<ContentTileDefinition> Tile = "FloorSteel";

    [DataField]
    public EntProtoId Wall = "WallSolid";

    /// <summary>
    /// Walls to use in corners if applicable.
    /// </summary>
    [DataField]
    public string? CornerWall;

    [DataField]
    public BoundaryWallFlags Flags = BoundaryWallFlags.Corridors | BoundaryWallFlags.Rooms;
}

[Flags]
public enum BoundaryWallFlags : byte
{
    Rooms = 1 << 0,
    Corridors = 1 << 1,
}
