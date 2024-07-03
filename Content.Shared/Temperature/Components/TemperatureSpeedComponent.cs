using Content.Shared.Temperature.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Temperature.Components;

/// <summary>
/// This is used for an entity that varies in speed based on current temperature.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedTemperatureSystem))]
public sealed partial class TemperatureSpeedComponent : Component
{
    /// <summary>
    /// Pairs of temperature thresholds to applied slowdown values.
    /// </summary>
    [DataField]
    public Dictionary<float, float> Thresholds;

    /// <summary>
    /// The time at which the temperature slowdown is updated.
    /// </summary>
    [DataField]
    public TimeSpan? NextSlowdownUpdate;
}
