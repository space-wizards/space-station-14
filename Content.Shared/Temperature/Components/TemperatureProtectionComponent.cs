using Content.Shared.Temperature.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Temperature.Components;

/// <summary>
/// Modifies amount of temperature change on entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedTemperatureSystem))]
public sealed partial class TemperatureProtectionComponent : Component
{
    /// <summary>
    /// Multiplier for the transferred heat when heating up.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HeatingCoefficient = 1.0f;

    /// <summary>
    /// Multiplier for the transferred heat when cooling down.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float CoolingCoefficient = 1.0f;
}

/// <summary>
/// Event raised on an entity with <see cref="TemperatureProtectionComponent"/> to determine the actual value of the coefficient.
/// </summary>
[ByRefEvent]
public record struct GetTemperatureProtectionEvent(float Coefficient);
