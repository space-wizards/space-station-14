using JetBrains.Annotations;

namespace Content.Shared.Station.Events;

/// <summary>
/// Directed event fired on a station when a grid is no longer a member of the station.
/// </summary>
[PublicAPI]
public sealed class StationGridRemovedEvent(EntityUid gridId, EntityUid station) : EntityEventArgs
{
    /// <summary>
    /// ID of the grid removed from the station.
    /// </summary>
    public EntityUid GridId = gridId;

    /// <summary>
    /// EntityUid of the station this grid was added to.
    /// </summary>
    public EntityUid Station = station;
}
