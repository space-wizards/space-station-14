using Content.Server.Botany.Systems;

namespace Content.Server.Botany.Components;

[RegisterComponent]
[Access(typeof(SeedExtractorSystem))]
public sealed partial class SeedExtractorComponent : Component
{
    /// <summary>
    /// The minimum amount of seed packets dropped.
    /// </summary>
    [DataField("baseMinSeeds"), ViewVariables(VVAccess.ReadWrite)]
    public int BaseMinSeeds = 1;

    /// <summary>
    /// The maximum amount of seed packets dropped.
    /// </summary>
    [DataField("baseMaxSeeds"), ViewVariables(VVAccess.ReadWrite)]
    public int BaseMaxSeeds = 3;
}
