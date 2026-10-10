using Content.Shared.FixedPoint;
using Content.Shared.Temperature.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Temperature.Components;

/// <summary>
///     Specifies welder-specific heating values for <see cref="HeaterToolComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedTemperatureSystem))]
public sealed partial class WelderHeaterComponent : Component
{
    /// <summary>
    ///     Amount of fuel consumed per heat application cycle.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 FuelConsumptionPerHeat = FixedPoint2.New(1);
}
