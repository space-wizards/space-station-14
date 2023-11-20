using System.Linq;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.Reactions;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Server.Atmos.EntitySystems;

public sealed partial class AtmosphereSystem
{
    private void InitializeGridAtmosphere()
    {
        SubscribeLocalEvent<GridAtmosphereComponent, ComponentInit>(OnGridAtmosphereInit);
        SubscribeLocalEvent<GridAtmosphereComponent, GridSplitEvent>(OnGridSplit);

        #region Atmos API Subscriptions

        SubscribeLocalEvent<GridAtmosphereComponent, HasAtmosphereMethodEvent>(GridHasAtmosphere);
        SubscribeLocalEvent<GridAtmosphereComponent, IsSimulatedGridMethodEvent>(GridIsSimulated);
        SubscribeLocalEvent<GridAtmosphereComponent, GetAllMixturesMethodEvent>(GridGetAllMixtures);
        SubscribeLocalEvent<GridAtmosphereComponent, GetTileMixtureMethodEvent>(GridGetTileMixture);
        SubscribeLocalEvent<GridAtmosphereComponent, GetTileMixturesMethodEvent>(GridGetTileMixtures);
        SubscribeLocalEvent<GridAtmosphereComponent, ReactTileMethodEvent>(GridReactTile);
        SubscribeLocalEvent<GridAtmosphereComponent, IsTileAirBlockedMethodEvent>(GridIsTileAirBlocked);
        SubscribeLocalEvent<GridAtmosphereComponent, IsTileSpaceMethodEvent>(GridIsTileSpace);
        SubscribeLocalEvent<GridAtmosphereComponent, GetAdjacentTilesMethodEvent>(GridGetAdjacentTiles);
        SubscribeLocalEvent<GridAtmosphereComponent, GetAdjacentTileMixturesMethodEvent>(GridGetAdjacentTileMixtures);
        SubscribeLocalEvent<GridAtmosphereComponent, HotspotExposeMethodEvent>(GridHotspotExpose);
        SubscribeLocalEvent<GridAtmosphereComponent, HotspotExtinguishMethodEvent>(GridHotspotExtinguish);
        SubscribeLocalEvent<GridAtmosphereComponent, IsHotspotActiveMethodEvent>(GridIsHotspotActive);
        SubscribeLocalEvent<GridAtmosphereComponent, FixTileVacuumMethodEvent>(GridFixTileVacuum);
        SubscribeLocalEvent<GridAtmosphereComponent, AddPipeNetMethodEvent>(GridAddPipeNet);
        SubscribeLocalEvent<GridAtmosphereComponent, RemovePipeNetMethodEvent>(GridRemovePipeNet);
        SubscribeLocalEvent<GridAtmosphereComponent, AddAtmosDeviceMethodEvent>(GridAddAtmosDevice);
        SubscribeLocalEvent<GridAtmosphereComponent, RemoveAtmosDeviceMethodEvent>(GridRemoveAtmosDevice);

        #endregion
    }

    private void OnGridAtmosphereInit(EntityUid uid, GridAtmosphereComponent gridAtmosphere, ComponentInit args)
    {
        base.Initialize();

        if (!TryComp(uid, out MapGridComponent? mapGrid))
            return;

        EnsureComp<GasTileOverlayComponent>(uid);

        foreach (var (indices, tile) in gridAtmosphere.Tiles)
        {
            gridAtmosphere.InvalidatedCoords.Add(indices);
            tile.GridIndex = uid;
        }

        GridRepopulateTiles((uid, mapGrid, gridAtmosphere));
    }

    private void OnGridSplit(EntityUid uid, GridAtmosphereComponent originalGridAtmos, ref GridSplitEvent args)
    {
        foreach (var newGrid in args.NewGrids)
        {
            // Make extra sure this is a valid grid.
            if (!TryComp(newGrid, out MapGridComponent? mapGrid))
                continue;

            // If the new split grid has an atmosphere already somehow, use that. Otherwise, add a new one.
            if (!TryComp(newGrid, out GridAtmosphereComponent? newGridAtmos))
                newGridAtmos = AddComp<GridAtmosphereComponent>(newGrid);

            // We assume the tiles on the new grid have the same coordinates as they did on the old grid...
            var enumerator = mapGrid.GetAllTilesEnumerator();

            while (enumerator.MoveNext(out var tile))
            {
                var indices = tile.Value.GridIndices;

                // This split event happens *before* the spaced tiles have been invalidated, therefore we can still
                // access their gas data. On the next atmos update tick, these tiles will be spaced. Poof!
                if (!originalGridAtmos.Tiles.TryGetValue(indices, out var tileAtmosphere))
                    continue;

                // The new grid atmosphere has been initialized, meaning it has all the needed TileAtmospheres...
                if (!newGridAtmos.Tiles.TryGetValue(indices, out var newTileAtmosphere))
                    // Let's be honest, this is really not gonna happen, but just in case...!
                    continue;

                // Copy a bunch of data over... Not great, maybe put this in TileAtmosphere?
                newTileAtmosphere.Air = tileAtmosphere.Air?.Clone() ?? null;
                newTileAtmosphere.MolesArchived = newTileAtmosphere.Air == null ? null : new float[Atmospherics.AdjustedNumberOfGases];
                newTileAtmosphere.Hotspot = tileAtmosphere.Hotspot;
                newTileAtmosphere.HeatCapacity = tileAtmosphere.HeatCapacity;
                newTileAtmosphere.Temperature = tileAtmosphere.Temperature;
                newTileAtmosphere.PressureDifference = tileAtmosphere.PressureDifference;
                newTileAtmosphere.PressureDirection = tileAtmosphere.PressureDirection;

                // TODO ATMOS: Somehow force GasTileOverlaySystem to perform an update *right now, right here.*
                // The reason why is that right now, gas will flicker until the next GasTileOverlay update.
                // That looks bad, of course. We want to avoid that! Anyway that's a bit more complicated so out of scope.

                // Invalidate the tile, it's redundant but redundancy is good! Also HashSet so really, no duplicates.
                originalGridAtmos.InvalidatedCoords.Add(indices);
                newGridAtmos.InvalidatedCoords.Add(indices);
            }
        }
    }

    private void GridHasAtmosphere(EntityUid uid, GridAtmosphereComponent component, ref HasAtmosphereMethodEvent args)
    {
        if (args.Handled)
            return;

        args.Result = true;
        args.Handled = true;
    }

    private void GridIsSimulated(EntityUid uid, GridAtmosphereComponent component, ref IsSimulatedGridMethodEvent args)
    {
        if (args.Handled)
            return;

        args.Simulated = component.Simulated;
        args.Handled = true;
    }

    private void GridGetAllMixtures(EntityUid uid, GridAtmosphereComponent component,
        ref GetAllMixturesMethodEvent args)
    {
        if (args.Handled)
            return;

        IEnumerable<GasMixture> EnumerateMixtures(EntityUid gridUid, GridAtmosphereComponent grid, bool invalidate)
        {
            foreach (var (indices, tile) in grid.Tiles)
            {
                if (tile.Air == null)
                    continue;

                if (invalidate)
                {
                    //var ev = new InvalidateTileMethodEvent(gridUid, indices);
                    //GridInvalidateTile(gridUid, grid, ref ev);
                    AddActiveTile(grid, tile);
                }

                yield return tile.Air;
            }
        }

        // Return the enumeration over all the tiles in the atmosphere.
        args.Mixtures = EnumerateMixtures(uid, component, args.Excite);
        args.Handled = true;
    }

    private void GridGetTileMixture(EntityUid uid, GridAtmosphereComponent component,
        ref GetTileMixtureMethodEvent args)
    {
        if (args.Handled)
            return;

        if (!component.Tiles.TryGetValue(args.Tile, out var tile))
            return; // Do NOT handle the event if we don't have that tile, the map will handle it instead.

        if (args.Excite)
            component.InvalidatedCoords.Add(args.Tile);

        args.Mixture = tile.Air;
        args.Handled = true;
    }

    private void GridGetTileMixtures(EntityUid uid, GridAtmosphereComponent component,
        ref GetTileMixturesMethodEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        args.Mixtures = new GasMixture?[args.Tiles.Count];

        for (var i = 0; i < args.Tiles.Count; i++)
        {
            var tile = args.Tiles[i];
            if (!component.Tiles.TryGetValue(tile, out var atmosTile))
            {
                // need to get map atmosphere
                args.Handled = false;
                continue;
            }

            if (args.Excite)
                component.InvalidatedCoords.Add(tile);

            args.Mixtures[i] = atmosTile.Air;
        }
    }

    private void GridReactTile(EntityUid uid, GridAtmosphereComponent component, ref ReactTileMethodEvent args)
    {
        if (args.Handled)
            return;

        if (!component.Tiles.TryGetValue(args.Tile, out var tile))
            return;

        args.Result = tile.Air is { } air ? React(air, tile) : ReactionResult.NoReaction;
        args.Handled = true;
    }

    private void GridIsTileAirBlocked(EntityUid uid, GridAtmosphereComponent component,
        ref IsTileAirBlockedMethodEvent args)
    {
        if (args.Handled)
            return;

        var mapGridComp = args.MapGridComponent;

        if (!Resolve(uid, ref mapGridComp))
            return;

        var data = GetAirtightData((uid, mapGridComp), args.Tile);
        args.NoAir = data.NoAir;
        args.Result = data.Blocked.IsFlagSet(args.Direction);
        args.Handled = true;
    }

    private void GridIsTileSpace(EntityUid uid, GridAtmosphereComponent component, ref IsTileSpaceMethodEvent args)
    {
        if (args.Handled)
            return;

        // We don't have that tile, so let the map handle it.
        if (!component.Tiles.TryGetValue(args.Tile, out var tile))
            return;

        args.Result = tile.Space;
        args.Handled = true;
    }

    private void GridGetAdjacentTiles(EntityUid uid, GridAtmosphereComponent component,
        ref GetAdjacentTilesMethodEvent args)
    {
        if (args.Handled)
            return;

        if (!component.Tiles.TryGetValue(args.Tile, out var tile))
            return;

        IEnumerable<Vector2i> EnumerateAdjacent(GridAtmosphereComponent grid, TileAtmosphere t)
        {
            foreach (var adj in t.AdjacentTiles)
            {
                if (adj == null)
                    continue;

                yield return adj.GridIndices;
            }
        }

        args.Result = EnumerateAdjacent(component, tile);
        args.Handled = true;
    }

    private void GridGetAdjacentTileMixtures(EntityUid uid, GridAtmosphereComponent component,
        ref GetAdjacentTileMixturesMethodEvent args)
    {
        if (args.Handled)
            return;

        if (!component.Tiles.TryGetValue(args.Tile, out var tile))
            return;

        IEnumerable<GasMixture> EnumerateAdjacent(GridAtmosphereComponent grid, TileAtmosphere t)
        {
            foreach (var adj in t.AdjacentTiles)
            {
                if (adj?.Air == null)
                    continue;

                yield return adj.Air;
            }
        }

        args.Result = EnumerateAdjacent(component, tile);
        args.Handled = true;
    }

    private void GridUpdateAdjacent(
        Entity<GridAtmosphereComponent, MapGridComponent, TransformComponent> entity,
        Vector2i index)
    {
        if (!entity.Comp1.Tiles.TryGetValue(index, out var tile))
            return;

        tile.BlockedAirflow = GetAirtightData((entity, entity), tile.GridIndices).Blocked;
        GridUpdateAdjacent(entity, tile);
    }

    private void GridUpdateAdjacent(
        Entity<GridAtmosphereComponent, MapGridComponent, TransformComponent> entity,
        TileAtmosphere tile)
    {
        var (uid, atmos, grid, xform) = entity;
        var mapUid = xform.MapUid;

        tile.AdjacentBits = AtmosDirection.Invalid;

        for (var i = 0; i < Atmospherics.Directions; i++)
        {
            var direction = (AtmosDirection) (1 << i);

            var otherIndices = tile.GridIndices.Offset(direction);

            if (!atmos.Tiles.TryGetValue(otherIndices, out var adjacent))
            {
                adjacent = new TileAtmosphere(tile.GridIndex, otherIndices,
                    GetTileMixture(null, mapUid, otherIndices),
                    space: IsTileSpace(null, mapUid, otherIndices, grid));
            }

            var oppositeDirection = direction.GetOpposite();

            adjacent.BlockedAirflow = GetAirtightData((uid, grid), adjacent.GridIndices).Blocked;

            if (!adjacent.BlockedAirflow.IsFlagSet(oppositeDirection) && !tile.BlockedAirflow.IsFlagSet(direction))
            {
                adjacent.AdjacentBits |= oppositeDirection;
                tile.AdjacentBits |= direction;
                adjacent.AdjacentTiles[oppositeDirection.ToIndex()] = tile;
                tile.AdjacentTiles[i] = adjacent;
            }
            else
            {
                adjacent.AdjacentBits &= ~oppositeDirection;
                tile.AdjacentBits &= ~direction;
                adjacent.AdjacentTiles[oppositeDirection.ToIndex()] = null;
                tile.AdjacentTiles[i] = null;
            }

            DebugTools.Assert(!(tile.AdjacentBits.IsFlagSet(direction) ^
                                adjacent.AdjacentBits.IsFlagSet(oppositeDirection)));

            if (!adjacent.AdjacentBits.IsFlagSet(adjacent.MonstermosInfo.CurrentTransferDirection))
                adjacent.MonstermosInfo.CurrentTransferDirection = AtmosDirection.Invalid;
        }

        if (!tile.AdjacentBits.IsFlagSet(tile.MonstermosInfo.CurrentTransferDirection))
            tile.MonstermosInfo.CurrentTransferDirection = AtmosDirection.Invalid;
    }

    private void GridHotspotExpose(EntityUid uid, GridAtmosphereComponent component, ref HotspotExposeMethodEvent args)
    {
        if (args.Handled)
            return;

        if (!component.Tiles.TryGetValue(args.Tile, out var tile))
            return;

        HotspotExpose(component, tile, args.ExposedTemperature, args.ExposedVolume, args.soh, args.SparkSourceUid);
        args.Handled = true;
    }

    private void GridHotspotExtinguish(EntityUid uid, GridAtmosphereComponent component,
        ref HotspotExtinguishMethodEvent args)
    {
        if (args.Handled)
            return;

        if (!component.Tiles.TryGetValue(args.Tile, out var tile))
            return;

        tile.Hotspot = new Hotspot();
        args.Handled = true;

        //var ev = new InvalidateTileMethodEvent(uid, args.Tile);
        //GridInvalidateTile(uid, component, ref ev);
        AddActiveTile(component, tile);
    }

    private void GridIsHotspotActive(EntityUid uid, GridAtmosphereComponent component,
        ref IsHotspotActiveMethodEvent args)
    {
        if (args.Handled)
            return;

        if (!component.Tiles.TryGetValue(args.Tile, out var tile))
            return;

        args.Result = tile.Hotspot.Valid;
        args.Handled = true;
    }

    private void GridFixTileVacuum(EntityUid uid, GridAtmosphereComponent component, ref FixTileVacuumMethodEvent args)
    {
        if (args.Handled)
            return;

        var adjEv = new GetAdjacentTileMixturesMethodEvent(uid, args.Tile, false, true);
        GridGetAdjacentTileMixtures(uid, component, ref adjEv);

        if (!adjEv.Handled || !component.Tiles.TryGetValue(args.Tile, out var tile))
            return;

        if (!TryComp<MapGridComponent>(uid, out var mapGridComp))
            return;

        var adjacent = adjEv.Result!.ToArray();

        // Return early, let's not cause any funny NaNs or needless vacuums.
        if (adjacent.Length == 0)
            return;

        tile.Air = new GasMixture
        {
            Volume = GetVolumeForTiles(mapGridComp, 1),
            Temperature = Atmospherics.T20C
        };

        tile.MolesArchived = new float[Atmospherics.AdjustedNumberOfGases];
        tile.ArchivedCycle = 0;

        var ratio = 1f / adjacent.Length;
        var totalTemperature = 0f;

        foreach (var adj in adjacent)
        {
            totalTemperature += adj.Temperature;

            // Remove a bit of gas from the adjacent ratio...
            var mix = adj.RemoveRatio(ratio);

            // And merge it to the new tile air.
            Merge(tile.Air, mix);

            // Return removed gas to its original mixture.
            Merge(adj, mix);
        }

        // New temperature is the arithmetic mean of the sum of the adjacent temperatures...
        tile.Air.Temperature = totalTemperature / adjacent.Length;
    }

    private void GridAddPipeNet(EntityUid uid, GridAtmosphereComponent component, ref AddPipeNetMethodEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = component.PipeNets.Add(args.PipeNet);
    }

    private void GridRemovePipeNet(EntityUid uid, GridAtmosphereComponent component, ref RemovePipeNetMethodEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = component.PipeNets.Remove(args.PipeNet);
    }

    private void GridAddAtmosDevice(Entity<GridAtmosphereComponent> grid, ref AddAtmosDeviceMethodEvent args)
    {
        if (args.Handled)
            return;

        if (!grid.Comp.AtmosDevices.Add((args.Device.Owner, args.Device)))
            return;

        args.Device.JoinedGrid = grid;
        args.Handled = true;
        args.Result = true;
    }

    private void GridRemoveAtmosDevice(EntityUid uid, GridAtmosphereComponent component,
        ref RemoveAtmosDeviceMethodEvent args)
    {
        if (args.Handled)
            return;

        if (!component.AtmosDevices.Remove((args.Device.Owner, args.Device)))
            return;

        args.Device.JoinedGrid = null;
        args.Handled = true;
        args.Result = true;
    }

    /// <summary>
    ///     Repopulates all tiles on a grid atmosphere.
    /// </summary>
    /// <param name="mapGrid">The grid where to get all valid tiles from.</param>
    /// <param name="gridAtmosphere">The grid atmosphere where the tiles will be repopulated.</param>
    private void GridRepopulateTiles(Entity<MapGridComponent, GridAtmosphereComponent> grid)
    {
        var (uid, mapGrid, gridAtmosphere) = grid;
        var volume = GetVolumeForTiles(mapGrid, 1);

        foreach (var tile in mapGrid.GetAllTiles())
        {
            if (!gridAtmosphere.Tiles.ContainsKey(tile.GridIndices))
                gridAtmosphere.Tiles[tile.GridIndices] = new TileAtmosphere(tile.GridUid, tile.GridIndices,
                    new GasMixture(volume) { Temperature = Atmospherics.T20C });

            gridAtmosphere.InvalidatedCoords.Add(tile.GridIndices);
        }

        TryComp(uid, out GasTileOverlayComponent? overlay);

        var xform = Transform(grid.Owner);

        // Gotta do this afterwards so we can properly update adjacent tiles.
        foreach (var (position, _) in gridAtmosphere.Tiles.ToArray())
        {
            GridUpdateAdjacent((uid, gridAtmosphere, mapGrid, xform), position);
            InvalidateVisuals(uid, position, overlay);
        }
    }
}
