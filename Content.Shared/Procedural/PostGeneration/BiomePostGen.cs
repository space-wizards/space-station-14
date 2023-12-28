using Content.Shared.Parallax.Biomes;
using Content.Shared.Procedural.PostGeneration;
using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural.PostGeneration;

/// <summary>
/// Generates a biome on top of valid tiles, then removes the biome when done.
/// Only works if no existing biome is present.
/// </summary>
public sealed partial class BiomePostGen : IPostDunGen
{
    [DataField(required: true)]
    public ProtoId<BiomeTemplatePrototype> BiomeTemplate;
}
