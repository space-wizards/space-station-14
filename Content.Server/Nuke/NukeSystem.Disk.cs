using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
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
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;

    public void NukeDiskInitialize()
    {
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChangedDisk);
        SubscribeLocalEvent<NukeDiskStationSetupEvent>(OnSetupDiskStation);
        SubscribeLocalEvent<NukeDiskComponent, ComponentStartup>(OnDiskStartup);
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

    private void OnSetupDiskStation(NukeDiskStationSetupEvent ev)
    {
        var originStation = _stationSystem.GetOwningStation(ev.Disk);
        var xform = Transform(ev.Disk);

        if (originStation != null)
            ev.DiskComp.Station = originStation.Value;

        if (xform.GridUid != null)
            ev.DiskComp.StationMap = (xform.MapUid, xform.GridUid);
    }

    private void OnDiskStartup(EntityUid uid, NukeDiskComponent component, ComponentStartup args)
    {
        var ev = new NukeDiskStationSetupEvent(uid, component);
        QueueLocalEvent(ev);
    }

    private void OnDiskShutdown(EntityUid uid, NukeDiskComponent component, ComponentShutdown args)
    {
        var diskMapUid = component.StationMap.Item1;
        var diskGridUid = component.StationMap.Item2;

        if (!component.Respawn || !HasComp<StationMemberComponent>(diskGridUid) || diskMapUid == null)
            return;

        if (TryFindRandomTile(diskGridUid.Value, diskMapUid.Value, 10, out var coords))
        {
            Spawn(component.Disk, coords);
            _adminLog.Add(LogType.Respawn, LogImpact.High, $"The nuclear disk was deleted and was respawned at {coords}");
        }

        //If the above fails, spawn at the center of the grid on the station
        else
        {
            var xform = Transform(diskGridUid.Value);
            var pos = xform.Coordinates;
            var mapPos = xform.MapPosition;
            var circle = new Circle(mapPos.Position, 2);

            if (!_mapManager.TryGetGrid(diskGridUid.Value, out var grid))
                return;

            var found = false;

            foreach (var tile in grid.GetTilesIntersecting(circle))
            {
                if (tile.IsSpace(_tileDefinitionManager) || tile.IsBlockedTurf(true) || !_atmosphere.IsTileMixtureProbablySafe(diskGridUid, diskMapUid.Value, grid.TileIndicesFor(mapPos)))
                    continue;

                pos = tile.GridPosition();
                found = true;

                if (found)
                    break;
            }

            Spawn(component.Disk, pos);
            _adminLog.Add(LogType.Respawn, LogImpact.High, $"The nuclear disk was deleted and was respawned at {pos}");
        }
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

    /// <summary>
    /// Try to find a random safe tile on the supplied grid
    /// </summary>
    /// <param name="targetGrid">The grid that you're looking for a safe tile on</param>
    /// <param name="targetMap">The map that you're looking for a safe tile on</param>
    /// <param name="maxAttempts">The maximum amount of attempts it should try before it gives up</param>
    /// <param name="targetCoords">If successful, the coordinates of the safe tile</param>
    /// <returns></returns>
    public bool TryFindRandomTile(EntityUid targetGrid, EntityUid targetMap, int maxAttempts, out EntityCoordinates targetCoords)
    {
        targetCoords = EntityCoordinates.Invalid;

        if (!_mapManager.TryGetGrid(targetGrid, out var grid))
            return false;

        var xform = Transform(targetGrid);

        if (!grid.TryGetTileRef(xform.Coordinates, out var tileRef))
            return false;

        var tile = tileRef.GridIndices;

        var found = false;
        var (gridPos, _, gridMatrix) = xform.GetWorldPositionRotationMatrix();
        var gridBounds = gridMatrix.TransformBox(grid.LocalAABB);

        //Obviously don't put anything ridiculous in here
        for (var i = 0; i < maxAttempts; i++)
        {
            var randomX = _random.Next((int) gridBounds.Left, (int) gridBounds.Right);
            var randomY = _random.Next((int) gridBounds.Bottom, (int) gridBounds.Top);

            tile = new Vector2i(randomX - (int) gridPos.X, randomY - (int) gridPos.Y);
            var mapPos = grid.GridTileToWorldPos(tile);
            var mapTarget = grid.WorldToTile(mapPos);
            var circle = new Circle(mapPos, 2);

            foreach (var newTileRef in grid.GetTilesIntersecting(circle))
            {
                if (newTileRef.IsSpace(_tileDefinitionManager) || newTileRef.IsBlockedTurf(true) || !_atmosphere.IsTileMixtureProbablySafe(targetGrid, targetMap, mapTarget))
                    continue;

                found = true;
                targetCoords = grid.GridTileToLocal(tile);
                break;
            }

            //Found a safe tile, no need to continue
            if (found)
                break;
        }

        if (!found)
            return false;

        return true;
    }
}
