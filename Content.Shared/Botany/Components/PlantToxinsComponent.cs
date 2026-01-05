using Content.Shared.Botany.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Botany.Components;

/// <summary>
/// Data for plant resistance to toxins.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(PlantToxinsSystem))]
public sealed partial class PlantToxinsComponent : Component
{
    /// <summary>
    /// Maximum toxin level the plant can tolerate before taking damage.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ToxinsTolerance = 4f;

    /// <summary>
    /// Divisor for calculating toxin uptake rate. Higher values mean slower toxin processing.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ToxinUptakeDivisor = 10f;
}
