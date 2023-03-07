using Content.Server.GameTicking;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Components;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._Craft.Utils;

public static class ShuttleUtils
{
    public static (MapId, EntityUid) CreateShuttleOnNewMap(IMapManager mapManager, MapLoaderSystem mapSystem, IEntityManager entityManager, string shuttlePath)
    {
        MapId mapId = MapId.Nullspace;
        EntityUid shuttleUid = EntityUid.Invalid;

        mapId = mapManager.CreateMap();
        if (mapId == MapId.Nullspace)
        {
            return (mapId, shuttleUid);
        }

        mapManager.SetMapPaused(mapId, false);

        if (!mapSystem.TryLoad(mapId, shuttlePath, out var gridList, null) || gridList == null)
        {
            return (mapId, shuttleUid);
        }

        shuttleUid = gridList[0];

        if (!entityManager.HasComponent<ShuttleComponent>(shuttleUid))
        {
            entityManager.AddComponent<ShuttleComponent>(shuttleUid);
        }

        return (mapId, shuttleUid);
    }

    public static EntityUid GetTargetStation(GameTicker ticker, IMapManager mapManager, IEntityManager entityManager)
    {
        var _targetmap = ticker.DefaultMap;
        EntityUid targetStation = EntityUid.Invalid;
        foreach (var grid in mapManager.GetAllMapGrids(_targetmap))
        {
            if (!entityManager.TryGetComponent<StationMemberComponent>(grid.Owner, out var stationMember)) continue;
            if (!entityManager.TryGetComponent<StationDataComponent>(stationMember.Station, out var stationData)) continue;
            targetStation = stationMember.Station;
            break;
        }

        return targetStation;
    }
}
