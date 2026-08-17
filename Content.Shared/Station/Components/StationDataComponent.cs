using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Station.Components;

/// <summary>
/// Stores core information about a station, namely its config and associated grids.
/// All station entities will have this component.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedStationSystem))]
[AutoGenerateComponentState, AutoGenerateEntityRelations(shutdownEvent: false)]
public sealed partial class StationDataComponent : Component
{
    /// <summary>
    /// The game map prototype, if any, associated with this station.
    /// </summary>
    [DataField]
    public StationConfig? StationConfig;

    /// <summary>
    /// The map-specific profile used to order jobs on this station.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<JobWeightPrototype>? JobWeights;

    /// <summary>
    /// List of all grids this station is part of.
    /// </summary>
    [DataField, AutoNetworkedField, AutoRelationField]
    public HashSet<EntityRelation> Grids = new();
}
