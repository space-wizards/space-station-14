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
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;

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

        if (!component.Respawn || !TryComp<StationMemberComponent>(diskGridUid, out var stationMember) || stationMember.Station != component.Station)
            return;

        if (TryFindRandomTile(diskGridUid.Value, 100, out var coords))
        {
            Spawn(component.Disk, coords.ToMap(EntityManager));
            _adminLog.Add(LogType.Respawn, LogImpact.High, $"The nuclear disk was deleted and was respawned at {coords.ToMap(EntityManager)}");
        }

        //If the above fails, spawn at the center of the grid on the station
        else
        {
            var xform = Transform(diskGridUid.Value);
            var mapPos = xform.MapPosition;
            var circle = new Circle(mapPos.Position, 2);

            if (!_mapManager.TryGetGrid(diskGridUid.Value, out var grid))
                return;

            foreach (var tile in grid.GetTilesIntersecting(circle))
            {
                if (tile.IsSpace(_tileDefinitionManager) || tile.IsBlockedTurf(true))
                    continue;

                mapPos = tile.GridPosition().ToMap(EntityManager);
            }

            Spawn(component.Disk, mapPos);
            _adminLog.Add(LogType.Respawn, LogImpact.High, $"The nuclear disk was deleted and was respawned at {mapPos}");
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
    /// <param name="maxAttempts">The maximum amount of attempts it should try before it gives up</param>
    /// <param name="targetCoords">If successful, the coordinates of the safe tile</param>
    /// <returns></returns>
    public bool TryFindRandomTile(EntityUid targetGrid, int maxAttempts, out EntityCoordinates targetCoords)
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
            var newTileRef = tile.GetTileRef(targetGrid);

            if (newTileRef.IsSpace(_tileDefinitionManager) || newTileRef.IsBlockedTurf(true))
                continue;

            found = true;
            targetCoords = grid.GridTileToLocal(tile);
            break;
        }

        if (!found)
            return false;

        return true;
    }
}
