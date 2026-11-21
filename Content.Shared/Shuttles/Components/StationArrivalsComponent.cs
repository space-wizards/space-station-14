using Content.Shared.Shuttles.Systems;
using Robust.Shared.Utility;

namespace Content.Shared.Shuttles.Components;

/// <summary>
/// Added to a station that is available for arrivals shuttles.
/// </summary>
[RegisterComponent, Access(typeof(ArrivalsSystem)), AutoGenerateEntityRelations]
public sealed partial class StationArrivalsComponent : Component
{
    [DataField, AutoRelationField]
    public EntityRelation Shuttle;

    [DataField]
    public ResPath ShuttlePath = new("/Maps/Shuttles/arrivals.yml");
}
