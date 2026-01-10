using Content.Shared.Atmos;
using Content.Shared.Botany.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Botany.Components;

/// <summary>
/// Data for gas to consume/exude on plant growth.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(SharedConsumeExudeGasGrowthSystem))]
public sealed partial class ConsumeExudeGasGrowthComponent : Component
{
    /// <summary>
    /// Dictionary of gases and their consumption rates per growth tick.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<Gas, float> ConsumeGasses = new();

    /// <summary>
    /// Dictionary of gases and their exude rates per growth tick.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<Gas, float> ExudeGasses = new();
}
