using Content.Server.Shuttles.Systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Shuttles.Components;

/// <summary>
/// If added to a grid gets launched when the emergency shuttle launches.
/// </summary>
[RegisterComponent]
public sealed partial class EscapePodComponent : Component
{
    [DataField("launchTime", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan? LaunchTime;

    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? Destination;
}
