using Content.Shared.Botany.Systems;
using Content.Shared.Destructible.Thresholds;

using Robust.Shared.GameStates;

namespace Content.Shared.Botany.Components;

/// <summary>
/// Component for a machine for extracting seeds from plant produce.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(SeedExtractorSystem))]
public sealed partial class SeedExtractorComponent : Component
{
    /// <summary>
    /// The base amount of seed packets dropped.
    /// </summary>
    [DataField, AutoNetworkedField]
    public MinMax BaseSeeds = new(1, 3);
}
