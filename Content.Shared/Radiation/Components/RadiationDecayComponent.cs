using Content.Shared.Radiation.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Radiation.Components;

/// <summary>
/// Decreases <see cref="RadiationSourceComponent"/> intensity exponentially over time.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedRadiationSystem))]
public sealed partial class RadiationDecayComponent : Component
{
    /// <summary>
    /// Rate at which the source intensity decreases, the higher the faster.
    /// Formula: intensity = e^(rate*time)
    /// To get from a half-life, do -(ln(0.5) / seconds) which should be a small number greater than 0
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("rate", required: true), AutoNetworkedField]
    public float Rate;
}
