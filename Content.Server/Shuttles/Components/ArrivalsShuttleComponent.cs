using Content.Server.Shuttles.Systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Shuttles.Components;

[RegisterComponent, Access(typeof(ArrivalsSystem))]
public sealed class ArrivalsShuttleComponent : Component
{
    [DataField("station")]
    public EntityUid Station;

    [DataField("nextTransfer", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextTransfer;
}
