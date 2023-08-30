using Content.Shared.Maps;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Procedural.PostGeneration;

/// <summary>
/// Iterates room edges and places the relevant tiles and walls on any free indices.
/// </summary>
public sealed partial class BoundaryWallPostGen : IPostDunGen
{
    [DataField("tile", customTypeSerializer:typeof(PrototypeIdSerializer<ContentTileDefinition>))]
    public string Tile = "FloorSteel";

    [DataField("wall", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Wall = "WallSolid";

    /// <summary>
    /// Walls to use in corners if applicable.
    /// </summary>
    [DataField("cornerWall", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? CornerWall;
}
