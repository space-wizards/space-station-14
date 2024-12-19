using Content.Shared.Atmos;

namespace Content.Server.Temperature.Components;

/// <summary>
/// The entity will cause the surrounding air temperature to change passively without
/// any need for power or anything else.
/// </summary>
[RegisterComponent]
public sealed partial class RadiantTemperatureComponent : Component
{
    /// <summary>
    /// The temperature that the entity will try to reach
    /// </summary>
    [DataField]
    public float GoalTemperature = Atmospherics.T20C;

    /// <summary>
    /// How much energy (in joules) to add to or remove from the surrounding air per second
    /// </summary>
    [DataField]
    public float EnergyChangedPerSecond = 1f;
}
