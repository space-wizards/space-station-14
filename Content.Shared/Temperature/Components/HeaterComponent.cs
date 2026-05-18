using Robust.Shared.GameStates;

namespace Content.Shared.Temperature.Components;

/// <summary>
///     Heats entities and solutions placed on this entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HeaterComponent : Component
{
    /// <summary>
    ///     The thermal conductance of the heater.
    ///     This determines how fast heat is transferred to the target based on temperature difference.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Conductivity = 10f;

    /// <summary>
    ///     The maximum temperature the heater can heat a target to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxTemperature = 700f;

    /// <summary>
    ///     Whether this heater requires power to function.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RequiresPower = true;
}
