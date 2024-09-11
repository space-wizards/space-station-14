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
    /// How much the temperature of the surrounding air will change in Kelvin per tick
    /// Positive means it will only heat, negative means it will only cool
    /// </summary>
    [DataField]
    public float TemperatureChangePerTick = 1f;
}
