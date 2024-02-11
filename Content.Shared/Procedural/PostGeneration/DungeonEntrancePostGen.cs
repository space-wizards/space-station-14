using Content.Shared.Maps;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Procedural.PostGeneration;

/// <summary>
/// Selects [count] rooms and places external doors to them.
/// </summary>
public sealed partial class DungeonEntrancePostGen : IPostDunGen
{
    /// <summary>
    /// How many rooms we place doors on.
    /// </summary>
    [DataField("count")]
    public int Count = 1;

    [DataField("entities", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
    public List<string?> Entities = new()
    {
        "CableApcExtension",
        "AirlockGlass",
    };

    [DataField("tile", customTypeSerializer:typeof(PrototypeIdSerializer<ContentTileDefinition>))]
    public string Tile = "FloorSteel";
}
