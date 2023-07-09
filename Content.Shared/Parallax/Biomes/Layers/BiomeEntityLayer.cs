using Content.Shared.Maps;
using Robust.Shared.Noise;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Parallax.Biomes.Layers;

[Serializable, NetSerializable]
public sealed class BiomeEntityLayer : IBiomeWorldLayer
{
    /// <inheritdoc/>
    [DataField("allowedTiles", customTypeSerializer:typeof(PrototypeIdListSerializer<ContentTileDefinition>))]
    public List<string> AllowedTiles { get; } = new();

    [DataField("noise")] public FastNoiseLite Noise { get; } = new(0);

    /// <inheritdoc/>
    [DataField("threshold")]
    public float Threshold { get; } = 0.5f;

    /// <inheritdoc/>
    [DataField("invert")] public bool Invert { get; } = false;

    [DataField("entities", required: true, customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
    public List<string> Entities = new();
}
