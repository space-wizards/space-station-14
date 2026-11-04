using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Client.TextScreen;

/// <summary>
/// A component for text screens that support countdown timers with frame-by-frame updates.
/// </summary>
/// <remarks>
/// Entities must have <see cref="TextScreenVisualsComponent"/> to work!
/// </remarks>
[RegisterComponent, Access(typeof(TextScreenSystem))]
[AutoGenerateComponentPause]
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
    /// The string to draw onto the screen when the target time is reached.
    /// </summary>
    [DataField]
    public string FinishedText = "";

    /// <summary>
    /// The 0-indexed row to use for the timer data.
    /// </summary>
    [DataField]
    public int TimerRow;

    /// <summary>
    /// If true, the timer will show small durations with centisecond precision.
    /// If false, times will be shown with second precision at most.
    /// </summary>
    [DataField]
    public bool ShowCentiseconds = true;

    /// <summary>
    /// The last value being displayed on the screen.
    /// </summary>
    /// <remarks>
    /// A value of all zeros implies the timer is done, and <see cref="FinishedText"/> should be displayed instead.
    [DataField]
    public TimerDisplay ScreenValue;

    /// <summary>
    /// The state to use for the <see cref="TimerVisualLayers.Light"/> layer when the timer is in progress.
    /// </summary>
    [DataField]
    public string? RunningState;

    /// <summary>
    /// The state to use for the <see cref="TimerVisualLayers.Light"/> layer when the timer elapses.
    /// </summary>
    [DataField]
    public string? FinishedState;
}

/// <summary>
/// A value to display on a timer, agnostic of time unit.
/// </summary>
/// <remarks>
/// Values to be expressed as <c>HIGH:LOW</c>, both values effectively capped at 99.
/// Used to avoid string comparisons.
/// </remarks>
[DataDefinition, Serializable]
public partial record struct TimerDisplay(int HighValue, int LowValue)
{
    public override readonly string ToString()
    {
        var high = int.Clamp(HighValue, 0, 99);
        var low = int.Clamp(LowValue, 0, 99);
        return $"{high:D2}:{low:D2}";
    }
}

/// <summary>
/// Sprite layers for text screen timers.
/// </summary>
[Serializable]
public enum TimerVisualLayers : byte
{
    /// <summary>A light that turns on with the status of the timer.</summary>
    Light
}
