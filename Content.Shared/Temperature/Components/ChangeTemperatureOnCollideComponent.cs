using Content.Shared.Temperature.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Temperature.Components;

/// <summary>
/// Put this component on a projectile that you would like to change the temperature on whatever it hits.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedTemperatureSystem))]
public sealed partial class ChangeTemperatureOnCollideComponent : Component
{
    /// <summary>
    /// The amount it changes the target's temperature by. In Joules.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Heat = 0f;

    /// <summary>
    /// If this heat change ignores heat resistance or not.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IgnoreHeatResistance = true;
}
