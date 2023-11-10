using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.TextScreen.Components;

/// <summary>
/// This is an active component for tracking <see cref="TextScreenVisualsComponent"/>
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
