using Content.Shared.Temperature.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Temperature.Components;

/// <summary>
/// This is used for an entity that varies in speed based on current temperature.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedTemperatureSystem)), AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class TemperatureSpeedComponent : Component
{
    /// <summary>
    /// Pairs of temperature thresholds to applied slowdown values.
    /// </summary>
    [DataField]
    public Dictionary<float, float> Thresholds = new();

    /// <summary>
    /// The current speed modifier from <see cref="Thresholds"/> we reached.
    /// Stored and networked so that the client doesn't mispredict temperature
    /// </summary>
    [DataField, AutoNetworkedField]
    public float? CurrentSpeedModifier;

    /// <summary>
    /// The time at which the temperature slowdown is updated.
    /// </summary>
    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan? NextSlowdownUpdate;
}
