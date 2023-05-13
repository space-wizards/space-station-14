using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Parallax.Biomes.Markers;

/// <summary>
/// Spawns the count of entities inside the specified area.
/// </summary>
[Prototype("biomeMarkerLayerCount")]
public sealed class BiomeMarkerLayerCount : IBiomeMarkerLayer
{
    [IdDataField] public string ID { get; } = default!;

    [DataField("proto", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Prototype { get; } = string.Empty;

    [DataField("mask", customTypeSerializer:typeof(PrototypeIdSerializer<BiomeTemplatePrototype>))]
    public string? Mask { get; }

    /// <summary>
    /// How many groups to spawn in the area.
    /// </summary>
    [DataField("count")]
    public int Count = 1;

    /// <summary>
    /// How many entities to spawn inside of a group. Will perform a BFS out to neighbours if a mask is specified.
    /// </summary>
    [DataField("groupSize")]
    public int GroupSize = 1;

    [DataField("size")] public int Size { get; } = 128;
}
