using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Client.TextScreen;

/// <summary>
/// Added to an entity already containing a <see cref="TextScreenVisualsComponent"/> to track frame-by-frame timer updates
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ScrollingTextScreenVisualsComponent : Component
{
    /// <summary>
    /// The start time that the message was sent off.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan StartTime = TimeSpan.Zero;

    /// <summary>
    /// The next time that the sprites for the screen need to be scrolled.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdateTime = TimeSpan.MaxValue;
    public Dictionary<string, string?> LayerStatesToDraw = new();
}
