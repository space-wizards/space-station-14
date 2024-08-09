using Content.Shared.Temperature.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Temperature.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedTemperatureSystem))]
public sealed partial class TemperatureProtectionComponent : Component
{
    /// <summary>
    ///     How much to multiply temperature deltas by.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Coefficient = 1.0f;
}

/// <summary>
/// Event raised on an entity with <see cref="TemperatureProtectionComponent"/> to determine the actual value of the coefficient.
/// </summary>
[ByRefEvent]
public record struct GetTemperatureProtectionEvent(float Coefficient);
