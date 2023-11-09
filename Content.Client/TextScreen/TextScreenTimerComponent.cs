using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;


namespace Content.Client.TextScreen;

/// <summary>
/// This is an active component for tracking <see cref="TextScreenVisualsComponent"/>
/// </summary>
[RegisterComponent]
public sealed partial class TextScreenTimerComponent : Component
{
    [DataField("targetTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan Target = TimeSpan.Zero;
    public int Row;
}
