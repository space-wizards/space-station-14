using Content.Shared.Botany.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Botany.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SeedExtractorSystem))]
public sealed partial class SeedExtractorComponent : Component
{
    /// <summary>
    /// The minimum amount of seed packets dropped.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int BaseMinSeeds = 1;

    /// <summary>
    /// The maximum amount of seed packets dropped.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int BaseMaxSeeds = 3;
}
