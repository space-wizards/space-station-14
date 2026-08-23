using Content.Shared.TextScreen.Systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.TextScreen.Components;

/// <summary>
/// Added to an entity already containing a <see cref="TextScreenVisualsComponent"/> to track frame-by-frame timer updates
/// </summary>
[RegisterComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause, Access(typeof(TextScreenSystem))]
public sealed partial class TextScreenTimerComponent : Component
{
    /// <summary>
    /// The time that the timer is counting down to.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan? TargetTime;

    /// <summary>
    /// The last received time being displayed.
    /// Only used client-side!
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan? DisplayTime;

    /// <summary>
    /// Whether or not the finish text has been displayed.
    /// </summary>
    [DataField]
    public bool FinishDisplayed;

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
    /// The row to use for the timer data.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int TimerRow;

    /// <summary>
    /// The value being displayed on the screen, (hundreds):(ones)
    /// e.g. 12:34 would be a value of 1234.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int ScreenValue;
}
