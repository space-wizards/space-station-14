using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.TextScreen.Components;

[RegisterComponent]
public sealed partial class ActiveTextScreenTimerComponent : Component
{
    [DataField("remaining", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan Remaining;
}
