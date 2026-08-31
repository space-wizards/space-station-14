using Content.Shared.TextScreen.Systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.TextScreen.Components;

/// <summary>
/// Additional visual data for text screens that support countdown timers with frame-by-frame updates.
/// Entities must have <see cref="TextScreenVisualsComponent"/> to work!
/// </summary>
[RegisterComponent, Access(typeof(TextScreenVisualizerSystem))]
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
    /// Whether or not the timer will show times with centisecond precision.
    /// If false, times will be shown with second precision at most.
    /// </summary>
    [DataField]
    public bool ShowCentiseconds = true;

    /// <summary>
    /// The last value being displayed on the screen, (hundreds):(ones).
    /// e.g. 12:34 would be a value of 1234.
    /// 0 indicates that the timer has finished, and should display FinishedText instead.
    /// Only used client-side.
    /// </summary>
    [DataField]
    public int ScreenValue;

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
/// Sprite layers for text screen timers.
/// </summary>
[Serializable]
public enum TimerVisualLayers : byte
{
    /// <summary>A light that lights up with the status of the timer.</summary>
    Light
}
