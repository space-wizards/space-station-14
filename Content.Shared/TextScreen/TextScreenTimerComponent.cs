using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.TextScreen.Components;

/// <summary>
/// Added to a <see cref="TextScreenVisualsComponent"/> entity to display frame-by-frame timer updates
/// </summary>
[RegisterComponent]
public sealed partial class TextScreenTimerComponent : Component
{
    [DataField("targetTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan Target = TimeSpan.Zero;
    public int Row;
    public Dictionary<string, string?> LayerStatesToDraw = new();
    public string HourFormat = "D2";
    public string MinuteFormat = "D2";
    public string SecondFormat = "D2";
}
