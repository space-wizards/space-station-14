using Content.Shared.Parallax.Biomes.Layers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Parallax.Biomes;

/// <summary>
/// A preset group of biome layers to be used for a <see cref="BiomeComponent"/>
/// </summary>
[Prototype("biomeTemplate")]
public sealed partial class BiomeTemplatePrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    [DataField("layers")]
    public List<IBiomeLayer> Layers = new();
}
