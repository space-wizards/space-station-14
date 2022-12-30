using Content.Shared.Maps;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.Utility;

namespace Content.Shared.Parallax.Biomes;

[Prototype("biome")]
public sealed class Biome : IPrototype
{
    [IdDataFieldAttribute] public string ID { get; } = default!;

    [DataField("layers")]
    public List<IBiomeLayer> Layers = new();
}

[ImplicitDataDefinitionForInheritors]
public interface IBiomeLayer {}

public sealed class BiomeTileLayer : IBiomeLayer
{
    /// <summary>
    /// Frequency for noise: lower values create larger blobs.
    /// </summary>
    [DataField("frequency")]
    public float Frequency = 0.1f;

    [DataField("tiles", required: true, customTypeSerializer: typeof(PrototypeIdListSerializer<ContentTileDefinition>))]
    public List<string> Tiles = new();
}

public sealed class BiomeDecalLayer : IBiomeLayer
{
    /// <summary>
    /// Frequency for noise: lower values create larger blobs.
    /// </summary>
    [DataField("frequency")]
    public float Frequency = 0.25f;

    /// <summary>
    /// Decals will only be drawn if the noise is above this threshold.
    /// </summary>
    [DataField("threshold")]
    public float Threshold = 0.8f;

    [DataField("decals", required: true)]
    public List<SpriteSpecifier> Decals = new();
}
