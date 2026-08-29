using Content.Shared.TextScreen.Systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.TextScreen.Components;

/// <summary>
/// Added to an entity already containing a <see cref="TextScreenVisualsComponent"/> to track frame-by-frame timer updates
/// </summary>
[RegisterComponent, AutoGenerateComponentState(true, fieldDeltas: true), AutoGenerateComponentPause, Access(typeof(TextScreenSystem))]
public sealed partial class TextScreenTimerComponent : Component
{
    /// <summary>
    /// The time that the timer is counting down to.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan? TargetTime;

    /// <summary>
    /// The text to render onto the screen while the timer is running.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string RunningText = "";

    /// <summary>
    /// The string to draw onto the screen when the timer has elapsed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string FinishedText = "";

    /// <summary>
    /// The 0-indexed row to use for the timer data.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int TimerRow;

    /// <summary>
    /// The last value being displayed on the screen, (hundreds):(ones).
    /// e.g. 12:34 would be a value of 1234.
    /// 0 indicates that the timer has finished, and should display FinishedText instead.
    /// Only used client-side.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int ScreenValue;
}
