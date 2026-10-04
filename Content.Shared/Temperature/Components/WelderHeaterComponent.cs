using Robust.Shared.GameStates;

namespace Content.Shared.Temperature.Components;

/// <summary>
///     Specifies welder-specific heating values for <see cref="HeaterToolComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WelderHeaterComponent : Component
{
    /// <summary>
    ///     Amount of fuel consumed per heat application cycle.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float FuelConsumptionPerHeat = 1.0f;
}
