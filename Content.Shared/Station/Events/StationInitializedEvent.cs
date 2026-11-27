using JetBrains.Annotations;

namespace Content.Shared.Station.Events;

/// <summary>
/// Broadcast event fired when a station is first set up.
/// This is the ideal point to add components to it.
/// </summary>
[PublicAPI]
public sealed class StationInitializedEvent(EntityUid station) : EntityEventArgs
{
    /// <summary>
    /// Station this event is for.
    /// </summary>
    public EntityUid Station = station;
}
