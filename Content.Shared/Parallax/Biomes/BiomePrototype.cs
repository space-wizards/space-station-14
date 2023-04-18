using Content.Shared.Parallax.Biomes.Layers;
using Robust.Shared.Noise;
using Robust.Shared.Prototypes;

namespace Content.Shared.Parallax.Biomes;

[Prototype("biome")]
public sealed class BiomePrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    [DataField("layers")]
    public List<IBiomeLayer> Layers = new();
}
