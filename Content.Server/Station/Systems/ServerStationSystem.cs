using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Server.GameTicking;
using Content.Shared.GameTicking;
using Content.Shared.Station;
using Content.Shared.Station.Components;
using Content.Shared.Station.Events;
using JetBrains.Annotations;
using Robust.Server.GameStates;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Station.Systems;

/// <inheritdoc/>
[PublicAPI]
public sealed partial class ServerStationSystem : Shared.Station.Systems.StationSystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private AtmosphereSystem _atmos = default!;
    [Dependency] private PvsOverrideSystem _pvsOverride = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRoundEnd);
        SubscribeLocalEvent<PostGameMapLoad>(OnPostGameMapLoad);
        SubscribeLocalEvent<StationDataComponent, ComponentStartup>(OnStationAdd);
        SubscribeLocalEvent<StationDataComponent, ComponentShutdown>(OnStationDeleted);
        SubscribeLocalEvent<StationMemberComponent, ComponentShutdown>(OnStationGridDeleted);
        SubscribeLocalEvent<StationMemberComponent, PostGridSplitEvent>(OnStationSplitEvent);

        SubscribeLocalEvent<StationGridAddedEvent>(OnStationGridAdded);
        SubscribeLocalEvent<StationGridRemovedEvent>(OnStationGridRemoved);

        _player.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    private void OnStationSplitEvent(EntityUid uid, StationMemberComponent component, ref PostGridSplitEvent args)
    {
        AddGridToStation(component.Station, args.Grid); // Add the new grid as a member.
    }

    private void OnStationGridDeleted(EntityUid uid, StationMemberComponent component, ref ComponentShutdown args)
    {
        if (!TryComp<StationDataComponent>(component.Station, out var stationData))
            return;

        stationData.Grids.Remove(uid);
        Dirty(component.Station, stationData);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _player.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.Connected)
        {
            RaiseNetworkEvent(new StationsUpdatedEvent(GetStationNames()), e.Session);
        }
    }

    private void UpdateTrackersOnGrid(EntityUid gridId, EntityUid? station)
    {
        var query = EntityQueryEnumerator<StationTrackerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var tracker, out var xform))
        {
            if (xform.GridUid == gridId)
            {
                SetStation((uid, tracker), station);
            }
        }
    }

    #region Event handlers

    private void OnStationAdd(EntityUid uid, StationDataComponent component, ComponentStartup args)
    {
        RaiseNetworkEvent(new StationsUpdatedEvent(GetStationNames()), Filter.Broadcast());

        var metaData = MetaData(uid);
        RaiseLocalEvent(new StationInitializedEvent(uid));
        Sawmill.Info($"Set up station {metaData.EntityName} ({uid}).");
        _pvsOverride.AddGlobalOverride(uid);
    }

    private void OnStationDeleted(EntityUid uid, StationDataComponent component, ref ComponentShutdown args)
    {
        foreach (var grid in component.Grids)
        {
            RemComp<StationMemberComponent>(grid);

            // If the station gets deleted, we raise the event for every grid that was a part of it
            RaiseLocalEvent(new StationGridRemovedEvent(grid, uid));
        }

        RaiseNetworkEvent(new StationsUpdatedEvent(GetStationNames()), Filter.Broadcast());
    }

    private void OnPostGameMapLoad(PostGameMapLoad ev)
    {
        var dict = new Dictionary<string, List<EntityUid>>();

        // Iterate over all BecomesStation
        foreach (var grid in ev.Grids)
        {
            // We still setup the grid
            if (TryComp<BecomesStationComponent>(grid, out var becomesStation))
                dict.GetOrNew(becomesStation.Id).Add(grid);
        }

        if (!dict.Any())
        {
            // Oh jeez, no stations got loaded.
            // We'll yell about it, but the thing this used to do with creating a dummy is kinda pointless now.
            Sawmill.Error($"There were no station grids for {ev.GameMap.ID}!");
        }

        foreach (var (id, gridIds) in dict)
        {
            StationConfig stationConfig;

            if (ev.GameMap.Stations.ContainsKey(id))
                stationConfig = ev.GameMap.Stations[id];
            else
            {
                Sawmill.Error($"The station {id} in map {ev.GameMap.ID} does not have an associated station config!");
                continue;
            }

            InitializeNewStation(stationConfig, gridIds, ev.StationName, ev.GameMap);
        }
    }

    private void OnRoundEnd(GameRunLevelChangedEvent eventArgs)
    {
        if (eventArgs.New != GameRunLevel.PreRoundLobby)
            return;

        var query = EntityQueryEnumerator<StationDataComponent>();
        while (query.MoveNext(out var station, out _))
        {
            QueueDel(station);
        }
    }

    private void OnStationGridAdded(StationGridAddedEvent ev)
    {
        // When a grid is added to a station, update all trackers on that grid
        UpdateTrackersOnGrid(ev.GridId, ev.Station);
    }

    private void OnStationGridRemoved(StationGridRemovedEvent ev)
    {
        // When a grid is removed from a station, update all trackers on that grid to null
        UpdateTrackersOnGrid(ev.GridId, null);
    }

    #endregion Event handlers

    /// <inheritdoc/>
    public override bool TryFindRandomTileOnStation(Entity<StationDataComponent> station,
        out Vector2i tile,
        [NotNullWhen(true)] out Entity<MapGridComponent>? targetGrid,
        out EntityCoordinates targetCoords,
        int numAttempts = 10)
    {
        tile = default;
        targetGrid = null;
        targetCoords = EntityCoordinates.Invalid;

        var targetGridMaybe = GetLargestGrid(station.Owner);
        if (targetGridMaybe is null)
            return false;

        if (!TryComp<MapGridComponent>(targetGridMaybe.Value, out var comp))
            return false;

        targetGrid = (targetGridMaybe.Value, comp);
        var grid = targetGrid.Value;

        var gridTiles = Map.GetAllTiles(grid, grid.Comp).ToList();
        var totalTiles = gridTiles.Count;
        if (totalTiles == 0)
            return false;

        for (var i = 0; i < numAttempts && totalTiles > 0; i++)
        {
            // Find random tile within list.
            var nextTileIndex = Random.Next(totalTiles);
            var tileRef = gridTiles[nextTileIndex];
            gridTiles.RemoveSwap(nextTileIndex);
            totalTiles--;

            // Invalid tile, try again.
            if (_atmos.IsTileSpace(grid.Owner, Transform(grid).MapUid, tileRef.GridIndices)
                || _atmos.IsTileAirBlockedCached(grid.Owner, tileRef.GridIndices))
            {
                continue;
            }

            targetCoords = Map.GridTileToLocal(grid, grid.Comp, tileRef.GridIndices);
            tile = tileRef.GridIndices;
            return true;
        }

        return false;
    }
}

