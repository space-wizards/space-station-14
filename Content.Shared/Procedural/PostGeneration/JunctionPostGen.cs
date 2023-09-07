using Content.Shared.Maps;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Procedural.PostGeneration;

/// <summary>
/// Places the specified entities at junction areas.
/// </summary>
public sealed partial class JunctionPostGen : IPostDunGen
{
    /// <summary>
    /// Width to check for junctions.
    /// </summary>
    [DataField("width")]
    public int Width = 3;

    [DataField("tile", customTypeSerializer:typeof(PrototypeIdSerializer<ContentTileDefinition>))]
    public string Tile = "FloorSteel";

    [DataField("entities", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
    public List<string?> Entities = new()
    {
        "CableApcExtension",
        "AirlockGlass"
    };
}
