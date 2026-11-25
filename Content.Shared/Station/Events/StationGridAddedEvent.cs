using JetBrains.Annotations;

namespace Content.Shared.Station.Events;

/// <summary>
/// Directed event fired on a station when a grid becomes a member of the station.
/// </summary>
[PublicAPI]
public sealed class StationGridAddedEvent(EntityUid gridId, EntityUid station, bool isSetup) : EntityEventArgs
{
    /// <summary>
    /// ID of the grid added to the station.
    /// </summary>
    public EntityUid GridId = gridId;

    /// <summary>
    /// EntityUid of the station this grid was added to.
    /// </summary>
    public EntityUid Station = station;

    /// <summary>
    /// Indicates that the event was fired during station setup,
    /// so that it can be ignored if StationInitializedEvent was already handled.
    /// </summary>
    public bool IsSetup = isSetup;
}
