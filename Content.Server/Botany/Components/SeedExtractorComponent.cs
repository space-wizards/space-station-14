using Content.Server.Botany.Systems;

namespace Content.Server.Botany.Components;

[RegisterComponent]
[Access(typeof(SeedExtractorSystem))]
public sealed partial class SeedExtractorComponent : Component
{
    /// <summary>
    /// The minimum amount of seed packets dropped.
    /// </summary>
    [DataField]
    public int BaseMinSeeds = 1;

    /// <summary>
    /// The maximum amount of seed packets dropped.
    /// </summary>
    [DataField]
    public int BaseMaxSeeds = 3;
}
