using Content.Shared.Shuttles.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Shuttles.Components;

[Access(typeof(ArrivalsSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause, AutoGenerateEntityRelations]
public sealed partial class ArrivalsShuttleComponent : Component
{
    [DataField, AutoRelationField]
    public EntityRelation Station;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextTransfer;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextArrivalsTime;

    /// <summary>
    /// The first arrivals FTL originates from nullspace instead of the station
    /// </summary>
    [DataField]
    public bool FirstRun = true;
}
