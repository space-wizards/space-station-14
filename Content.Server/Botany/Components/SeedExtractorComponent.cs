using Content.Server.Botany.Systems;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Botany.Components;

[RegisterComponent]
[Friend(typeof(SeedExtractorSystem))]
public sealed class SeedExtractorComponent : Component
{
    // TODO: Upgradeable machines.
    [DataField("minSeeds")] public int MinSeeds = 1;

    [DataField("maxSeeds")] public int MaxSeeds = 4;
}
