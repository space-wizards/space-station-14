using Content.Shared.Maps;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Procedural.PostGeneration;

/// <summary>
/// Places the specified entities on the middle connections between rooms
/// </summary>
public sealed partial class MiddleConnectionPostGen : IPostDunGen
{
    /// <summary>
    /// How much overlap there needs to be between 2 rooms exactly.
    /// </summary>
    [DataField("overlapCount")]
    public int OverlapCount = -1;

    /// <summary>
    /// How many connections to spawn between rooms.
    /// </summary>
    [DataField("count")]
    public int Count = 1;

    [DataField("tile", customTypeSerializer:typeof(PrototypeIdSerializer<ContentTileDefinition>))]
    public string Tile = "FloorSteel";

    [DataField("entities", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
    public List<string?> Entities = new()
    {
        "CableApcExtension",
        "AirlockGlass"
    };

    /// <summary>
    /// If overlap > 1 then what should spawn on the edges.
    /// </summary>
    [DataField("edgeEntities")] public List<string?> EdgeEntities = new();
}
