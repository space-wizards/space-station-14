using Content.Shared.Station.Components;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Map;

namespace Content.Shared.GameTicking;

public abstract partial class GameTicker
{
    /// <summary>
    /// Dispatches an announcement to all players in game, aka not in the lobby.
    /// </summary>
    /// <param name="announcement">LocId of the announcement we're sending.</param>
    /// <param name="announcementSound">Sound that plays with the announcement.</param>
    /// <param name="color">Optional color override for the announcement.</param>
    [PublicAPI]
    public virtual void GameAnnouncement(LocId announcement, SoundSpecifier? announcementSound = null, Color? color = null) { }

    /// <summary>
    /// Dispatches an announcement to all players on a given map.
    /// </summary>
    /// <param name="map">Map we are dispatching the announcement to.</param>
    /// <param name="announcement">LocId of the announcement we're sending.</param>
    /// <param name="announcementSound">Sound that plays with the announcement.</param>
    /// <param name="color">Optional color override for the announcement.</param>
    [PublicAPI]
    public virtual void MapAnnouncement(MapId map, LocId announcement, SoundSpecifier? announcementSound = null, Color? color = null) { }

    /// <summary>
    /// Dispatches an announcement to all players on the same map as the given station.
    /// </summary>
    /// <param name="station">Station m</param>
    /// <param name="announcement">LocId of the announcement we're sending.</param>
    /// <param name="announcementSound">Sound that plays with the announcement.</param>
    /// <param name="color">Optional color override for the announcement.</param>
    [PublicAPI]
    public virtual void StationMapAnnouncement(Entity<StationDataComponent> station, LocId announcement, SoundSpecifier? announcementSound = null, Color? color = null) { }

    /// <summary>
    /// Dispatches an announcement to all players on all station grids.
    /// </summary>
    /// <param name="announcement">LocId of the announcement we're sending.</param>
    /// <param name="announcementSound">Sound that plays with the announcement.</param>
    /// <param name="color">Optional color override for the announcement.</param>
    [PublicAPI]
    public virtual void StationAnnouncement(LocId announcement, SoundSpecifier? announcementSound = null, Color? color = null) { }

    /// <summary>
    /// Dispatches an announcement to all players on a given station.
    /// </summary>
    /// <param name="station">Station we are dispatching the announcement to.</param>
    /// <param name="announcement">LocId of the announcement we're sending.</param>
    /// <param name="announcementSound">Sound that plays with the announcement.</param>
    /// <param name="color">Optional color override for the announcement.</param>
    [PublicAPI]
    public virtual void StationAnnouncement(EntityUid station, LocId announcement, SoundSpecifier? announcementSound = null, Color? color = null) { }
}
