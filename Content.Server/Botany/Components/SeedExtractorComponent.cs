using Content.Server.Botany.Systems;

namespace Content.Server.Botany.Components;

[RegisterComponent]
[Access(typeof(SeedExtractorSystem))]
public sealed class SeedExtractorComponent : Component
{
    // TODO: Upgradeable machines.
    [DataField("minSeeds")] public int MinSeeds = 1;

    [DataField("maxSeeds")] public int MaxSeeds = 4;
}
