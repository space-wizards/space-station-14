using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Screen.Components;

/// <summary>
/// Added to an entity already containing a <see cref="ScreenVisualsComponent"/> to track frame-by-frame timer updates
/// </summary>
[RegisterComponent]
public sealed partial class ScreenTimerComponent : Component
{
    [DataField("targetTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan Target = TimeSpan.Zero;
    public Dictionary<string, string?> LayerStatesToDraw = new();
}
