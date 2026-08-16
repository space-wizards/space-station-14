using Content.Shared.TextScreen.Systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.TextScreen.Components;

/// <summary>
/// Added to an entity already containing a <see cref="TextScreenVisualsComponent"/> to track frame-by-frame timer updates
/// </summary>
[RegisterComponent, AutoGenerateComponentPause, Access(typeof(TextScreenSystem))]
public sealed partial class TextScreenTimerComponent : Component
{
    /// <summary>
    /// The time that the timer is counting down to.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? TargetTime;

    /// <summary>
    /// The text to render onto the screen while the timer is running.
    /// </summary>
    [DataField]
    public string RunningText = "";

    /// <summary>
    /// The string to draw onto the screen when the timer has elapsed.
    /// </summary>
    [DataField]
    public string FinishedText = "";

    /// <summary>
    /// The row to use for the timer data.
    /// </summary>
    [DataField]
    public int TimerRow;

    /// <summary>
    /// The format string to use for displaying hours on the timer.
    /// </summary>
    [DataField]
    public string HoursFormat = "D2";

    /// <summary>
    /// The format string to use for displaying minutes on the timer.
    /// </summary>
    [DataField]
    public string MinutesFormat = "D2";

    /// <summary>
    /// The format string to use for displaying seconds on the timer.
    /// </summary>
    [DataField]
    public string SecondsFormat = "D2";

    /// <summary>
    /// The format string to use for displaying centiseconds on the timer.
    /// </summary>
    [DataField]
    public string CentisecondsFormat = "D2";
}
