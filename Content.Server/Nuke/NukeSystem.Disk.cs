using Content.Server.Administration.Logs;
using Content.Server.GameTicking;
using Content.Server.Station.Components;
using Content.Shared.Database;
using Content.Shared.Maps;
using Content.Shared.Nuke;
using Robust.Shared.Map;

namespace Content.Server.Nuke;

public sealed partial class NukeSystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;

    public void NukeDiskInitialize()
    {
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChangedDisk);
        SubscribeLocalEvent<NukeDiskComponent, MapInitEvent>(OnMapDiskInit);
        SubscribeLocalEvent<NukeDiskComponent, ComponentShutdown>(OnDiskShutdown);
    }

    private void OnRunLevelChangedDisk(GameRunLevelChangedEvent ev)
    {

        //Try to compensate for restartroundnow command
        if (ev.Old == GameRunLevel.InRound && ev.New == GameRunLevel.PreRoundLobby)
            OnRoundEnd();

        switch (ev.New)
        {
            case GameRunLevel.PostRound:
                OnRoundEnd();
                break;
        }
    }

    private void OnMapDiskInit(EntityUid uid, NukeDiskComponent component, MapInitEvent args)
    {
        var originStation = _stationSystem.GetOwningStation(uid);
        var xform = Transform(uid);

        if (originStation != null)
        {
            component.Station = originStation.Value;
            component.StationMap = (xform.MapID, xform.GridUid);
        }
    }

    private void OnDiskShutdown(EntityUid uid, NukeDiskComponent component, ComponentShutdown args)
    {
        var diskGridUid = component.StationMap.Item2;

        if (!_mapManager.TryGetGrid(diskGridUid, out var grid) || !component.Respawn || !TryComp<StationMemberComponent>(diskGridUid, out var stationMember))
            return;

        var xform = Transform(diskGridUid.Value);
        var mapPos = xform.MapPosition;
        var circle = new Circle(mapPos.Position, 2);

        foreach (var tile in grid.GetTilesIntersecting(circle))
        {
            //If it's not a station or the same station, don't spawn a disk
            if (stationMember.Station != component.Station)
                return;

            if (tile.IsSpace() || tile.IsBlockedTurf(true))
                continue;

            mapPos = tile.GridPosition().ToMap(EntityManager);
        }

        Spawn(component.Disk, mapPos);
        _adminLog.Add(LogType.Respawn, LogImpact.High, $"The nuclear disk was deleted and was respawned at {mapPos}");
    }

    private void OnRoundEnd()
    {
        var diskQuery = EntityQuery<NukeDiskComponent>();

        //Turn respawning off so the disk doesn't respawn during reset
        foreach (var disk in diskQuery)
        {
            disk.Respawn = false;
        }
    }
}
