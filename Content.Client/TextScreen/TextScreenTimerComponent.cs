using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Client.TextScreen;

/// <summary>
/// Added to an entity already containing a <see cref="TextScreenVisualsComponent"/> to track frame-by-frame timer updates
/// </summary>
[RegisterComponent]
public sealed partial class TextScreenTimerComponent : Component
{
    [DataField("targetTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan Target = TimeSpan.Zero;
    public Dictionary<string, string?> LayerStatesToDraw = new();
}
