using Content.Shared.TextScreen.Systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.TextScreen.Components;

/// <summary>
/// Additional visual data for text screens that support countdown timers with frame-by-frame updates.
/// Entities must have <see cref="TextScreenVisualsComponent"/> to work!
/// </summary>
[RegisterComponent, Access(typeof(TextScreenVisualizerSystem))]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class TextScreenTimerVisualsComponent : Component
{
    /// <summary>
    /// The time that the timer is counting down to.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
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
    /// The 0-indexed row to use for the timer data.
    /// </summary>
    [DataField]
    public int TimerRow;

    /// <summary>
    /// The last value being displayed on the screen, (hundreds):(ones).
    /// e.g. 12:34 would be a value of 1234.
    /// 0 indicates that the timer has finished, and should display FinishedText instead.
    /// Only used client-side.
    /// </summary>
    [DataField]
    public int ScreenValue;
}
