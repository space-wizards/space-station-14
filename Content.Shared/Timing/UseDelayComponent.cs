using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Timing;

/// <summary>
/// Timer that creates a cooldown each time an object is activated/used.
/// Can support additional, separate cooldown timers on the object by passing a unique ID with the system methods.
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(UseDelaySystem))]
public sealed partial class UseDelayComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<string, UseDelayInfo> Delays = [];

    /// <summary>
    /// Default delay time.
    /// </summary>
    /// <remarks>
    /// This is only used at MapInit and should not be expected
    /// to reflect the length of the default delay after that.
    /// Use <see cref="UseDelaySystem.TryGetDelayInfo"/> instead.
    /// </remarks>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);
}

[Serializable, NetSerializable]
public sealed class UseDelayInfo(TimeSpan length, TimeSpan startTime = default, TimeSpan endTime = default)
{
    [ViewVariables]
    public TimeSpan Length { get; set; } = length;
    [ViewVariables]
    public TimeSpan StartTime { get; set; } = startTime;
    [ViewVariables]
    public TimeSpan EndTime { get; set; } = endTime;
}
