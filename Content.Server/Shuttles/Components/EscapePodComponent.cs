using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Shuttles.Components;

/// <summary>
/// If added to a grid gets launched when the emergency shuttle launches.
/// </summary>
[RegisterComponent]
public sealed class EscapePodComponent : Component
{
    [DataField("launchTime", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan? LaunchTime;
}
