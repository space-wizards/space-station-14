using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Parallax.Biomes.Markers;

/// <summary>
/// Spawns entities inside of the specified area with the minimum specified radius.
/// </summary>
[Prototype("biomeMarkerLayerRadius")]
public sealed class BiomeMarkerLayerRadiusPrototype : IBiomeMarkerLayer
{
    [IdDataField] public string ID { get; } = default!;

    [DataField("proto", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Prototype { get; } = string.Empty;

    [DataField("mask", customTypeSerializer:typeof(PrototypeIdSerializer<BiomeTemplatePrototype>))]
    public string? Mask { get; }

    /// <summary>
    /// Minimum radius between 2 points
    /// </summary>
    [DataField("radius")]
    public float Radius = 32f;

    /// <summary>
    /// How many mobs to spawn in one group.
    /// </summary>
    [DataField("groupCount")]
    public int GroupCount = 1;

    /// <inheritdoc />
    [DataField("size")]
    public int Size { get; } = 128;
}
