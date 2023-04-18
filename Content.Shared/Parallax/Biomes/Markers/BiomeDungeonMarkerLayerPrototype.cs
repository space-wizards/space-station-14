using Content.Shared.Parallax.Biomes.Points;
using Content.Shared.Procedural;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Parallax.Biomes.Markers;

[Prototype("biomeDungeonMarkerLayer")]
public sealed class BiomeDungeonMarkerLayerPrototype : IBiomeMarkerLayer
{
    [IdDataField] public string ID { get; } = default!;

    [DataField("proto", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<DungeonConfigPrototype>))]
    public string Prototype = string.Empty;

    /// <inheritdoc />
    [DataField("variations")]
    public int Variations { get; } = 4;

    /// <inheritdoc />
    [DataField("radius")]
    public float Radius { get; } = 48f;

    /// <inheritdoc />
    [DataField("count")]
    public int Count { get; } = 4;

    /// <inheritdoc />
    [DataField("size")]
    public int Size { get; } = 256;
}
