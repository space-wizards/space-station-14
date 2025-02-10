using Content.Server.Temperature.Systems;

namespace Content.Server.Temperature.Components;

[RegisterComponent]
[Access(typeof(TemperatureSystem))]
public sealed partial class TemperatureProtectionComponent : Component
{
    /// <summary>
    ///     Multiplier for the transferred heat when heating up
    /// </summary>
    [DataField]
    public float HeatingCoefficient = 1.0f;

    /// <summary>
    ///     Multiplier for the transferred heat when cooling down
    /// </summary>
    [DataField]
    public float CoolingCoefficient = 1.0f;
}

/// <summary>
/// Event raised on an entity with <see cref="TemperatureProtectionComponent"/> to determine the actual value of the coefficient.
/// </summary>
[ByRefEvent]
public record struct GetTemperatureProtectionEvent(float Coefficient);
