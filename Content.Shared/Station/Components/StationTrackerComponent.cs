using Robust.Shared.GameStates;

namespace Content.Shared.Station.Components;

/// <summary>
/// Component that tracks which station an entity is currently on.
/// Mainly used for UI purposes on the client to easily get station-specific data like alert levels.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedStationSystem))]
public sealed partial class StationTrackerComponent : Component
{
    /// <summary>
    /// The station this entity is currently on, if any.
    /// Null when in space or not on any grid.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Station;
}
