using Content.Shared.Parallax.Biomes.Points;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Parallax.Biomes.Markers;

[Prototype("biomeMarkerLayer")]
public sealed class BiomeMarkerLayerPrototype : IBiomeMarkerLayer
{
    [IdDataField] public string ID { get; } = default!;

    [DataField("proto", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Prototype = string.Empty;

    /// <inheritdoc />
    [DataField("radius")]
    public float Radius { get; } = 32f;

    /// <summary>
    /// How many mobs to spawn in one group.
    /// </summary>
    [DataField("groupCount")]
    public int GroupCount = 1;

    /// <inheritdoc />
    [DataField("size")]
    public int Size { get; } = 128;
}
