using Content.Shared.Maps;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Procedural.PostGeneration;

/// <summary>
/// Selects [count] rooms and places external doors to them.
/// </summary>
public sealed class EntrancePostGen : IPostDunGen
{
    /// <summary>
    /// How many rooms we place doors on.
    /// </summary>
    [DataField("count")]
    public int Count = 1;

    [DataField("door", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Door = "AirlockGlass";

    [DataField("tile", customTypeSerializer:typeof(PrototypeIdSerializer<ContentTileDefinition>))]
    public string Tile = "FloorSteel";
}
