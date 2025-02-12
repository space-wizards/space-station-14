using Content.Server.Shuttles.Systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Shuttles.Components;

[RegisterComponent, Access(typeof(ArrivalsSystem)), AutoGenerateComponentPause]
public sealed partial class ArrivalsShuttleComponent : Component
{
    [DataField]
    public EntityUid Station;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)))]
    [AutoPausedField]
    public TimeSpan NextTransfer;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)))]
    public TimeSpan NextArrivalsTime;

    /// <summary>
    ///     the first arrivals FTL originates from nullspace instead of the station
    /// </summary>
    [DataField]
    public bool FirstRun = true;

}
