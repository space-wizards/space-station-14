using Content.Shared.Station.Components;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.GameTicking;

public sealed partial class ServerGameTicker
{
    public override void GameAnnouncement(LocId announcement, SoundSpecifier? announcementSound = null, Color? color = null)
    {
        var filter = Filter.Empty().AddWhere(UserHasJoinedGame);
        Chat.DispatchFilteredAnnouncement(filter, Loc.GetString(announcement), announcementSound: announcementSound, colorOverride: color);
    }

    public override void MapAnnouncement(MapId map, LocId announcement, SoundSpecifier? announcementSound = null, Color? color = null)
    {
        var filter = Filter.Empty().AddInMap(map);
        Chat.DispatchFilteredAnnouncement(filter, Loc.GetString(announcement), announcementSound: announcementSound, colorOverride: color);
    }

    public override void StationMapAnnouncement(Entity<StationDataComponent> station, LocId announcement, SoundSpecifier? announcementSound = null, Color? color = null)
    {
        var filter = Station.GetOnMap(station.Comp);
        Chat.DispatchFilteredAnnouncement(filter, Loc.GetString(announcement), announcementSound: announcementSound, colorOverride: color);
    }

    public override void StationAnnouncement(LocId announcement, SoundSpecifier? announcementSound = null, Color? color = null)
    {
        foreach (var station in Station.GetStations())
        {
            StationAnnouncement(station, announcement, announcementSound, color);
        }
    }

    public override void StationAnnouncement(EntityUid station, LocId announcement, SoundSpecifier? announcementSound = null, Color? color = null)
    {
        _chatSystem.DispatchStationAnnouncement(station, Loc.GetString(announcement), announcementSound: announcementSound, colorOverride: color);
    }
}
