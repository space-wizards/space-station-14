using System.Diagnostics;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer.NodeGroups;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Server.Atmos.EntitySystems;

public partial class AtmosphereSystem
{
    /*
     General API for interacting with AtmosphereSystem.

     If you feel like you're stepping on eggshells because you can't access things in AtmosphereSystem,
     consider adding a method here instead of making your own way to work around it.
     */

    /// <summary>
    /// Gets the <see cref="GasMixture"/> that an entity is contained within.
    /// </summary>
    /// <param name="ent">The entity to get the mixture for.</param>
    /// <param name="ignoreExposed">If true, will ignore mixtures that the entity is contained in
    /// (ex. lockers and cryopods) and just get the tile mixture.</param>
    /// <param name="excite">If true, will mark the tile as active for atmosphere processing.</param>
    /// <returns>A <see cref="GasMixture"/> if one could be found, null otherwise.</returns>
    [PublicAPI]
    public GasMixture? GetContainingMixture(Entity<TransformComponent?> ent, bool ignoreExposed = false, bool excite = false)
    {
        if (!Resolve(ent, ref ent.Comp))
            return null;

        return GetContainingMixture(ent, ent.Comp.GridUid, ent.Comp.MapUid, ignoreExposed, excite);
    }

    /// <summary>
    /// Gets the <see cref="GasMixture"/> that an entity is contained within.
    /// </summary>
    /// <param name="ent">The entity to get the mixture for.</param>
    /// <param name="grid">The grid that the entity may be on.</param>
    /// <param name="map">The map that the entity may be on.</param>
    /// <param name="ignoreExposed">If true, will ignore mixtures that the entity is contained in
    /// (ex. lockers and cryopods) and just get the tile mixture.</param>
    /// <param name="excite">If true, will mark the tile as active for atmosphere processing.</param>
    /// <returns>A <see cref="GasMixture"/> if one could be found, null otherwise.</returns>
    [PublicAPI]
    public GasMixture? GetContainingMixture(
        Entity<TransformComponent?> ent,
        Entity<GridAtmosphereComponent?, GasTileOverlayComponent?>? grid,
        Entity<MapAtmosphereComponent?>? map,
        bool ignoreExposed = false,
        bool excite = false)
    {
        if (!Resolve(ent, ref ent.Comp))
            return null;

        if (!ignoreExposed && !ent.Comp.Anchored)
        {
            // Used for things like disposals/cryo to change which air people are exposed to.
            var ev = new AtmosExposedGetAirEvent((ent, ent.Comp), excite);
            RaiseLocalEvent(ent, ref ev);
            if (ev.Handled)
                return ev.Gas;

            // TODO ATMOS: recursively iterate up through parents
            // This really needs recursive InContainer metadata flag for performance
            // And ideally some fast way to get the innermost airtight container.
        }

        var position = _transformSystem.GetGridTilePositionOrDefault((ent, ent.Comp));
        return GetTileMixture(grid, map, position, excite);
    }

    /// <summary>
    /// Checks if a grid has an atmosphere.
    /// </summary>
    /// <param name="gridUid">The grid to check.</param>
    /// <returns>True if the grid has an atmosphere, false otherwise.</returns>
    [PublicAPI]
    public bool HasAtmosphere(EntityUid gridUid)
    {
        return _atmosQuery.HasComponent(gridUid);
    }

    /// <summary>
    /// Sets whether a grid is simulated by Atmospherics.
    /// </summary>
    /// <param name="gridUid">The grid to set.</param>
    /// <param name="simulated">Whether the grid should be simulated.</param>
    /// <returns>>True if the grid's simulated state was changed, false otherwise.</returns>
    [PublicAPI]
    public bool SetSimulatedGrid(EntityUid gridUid, bool simulated)
    {
        // TODO ATMOS this event literally has no subscribers. Did this just get silently refactored out?
        var ev = new SetSimulatedGridMethodEvent(gridUid, simulated);
        RaiseLocalEvent(gridUid, ref ev);

        return ev.Handled;
    }

    /// <summary>
    /// Checks whether a grid is simulated by Atmospherics.
    /// </summary>
    /// <param name="gridUid">The grid to check.</param>
    /// <returns>>True if the grid is simulated, false otherwise.</returns>
    public bool IsSimulatedGrid(EntityUid gridUid)
    {
        var ev = new IsSimulatedGridMethodEvent(gridUid);
        RaiseLocalEvent(gridUid, ref ev);

        return ev.Simulated;
    }

    /// <summary>
    /// Gets all <see cref="TileAtmosphere"/> <see cref="GasMixture"/>s on a grid.
    /// </summary>
    /// <param name="gridUid">The grid to get mixtures for.</param>
    /// <param name="excite">Whether to mark all tiles as active for atmosphere processing.</param>
    /// <returns>An enumerable of all gas mixtures on the grid.</returns>
    [PublicAPI]
    public IEnumerable<GasMixture> GetAllMixtures(EntityUid gridUid, bool excite = false)
    {
        var ev = new GetAllMixturesMethodEvent(gridUid, excite);
        RaiseLocalEvent(gridUid, ref ev);

        if (!ev.Handled)
            return [];

        DebugTools.AssertNotNull(ev.Mixtures);
        return ev.Mixtures!;
    }

    /// <summary>
    /// <para>Invalidates a tile on a grid, marking it for revalidation.</para>
    ///
    /// <para>Frequently used tile data like <see cref="AirtightData"/> are determined once and cached.
    /// If this tile's state changes, ex. being added or removed, then this position in the map needs to
    /// be updated.</para>
    ///
    /// <para>Tiles that need to be updated are marked as invalid and revalidated before all other
    /// processing stages.</para>
    /// </summary>
    /// <param name="entity">The grid entity.</param>
    /// <param name="tile">The tile to invalidate.</param>
    [PublicAPI]
    public void InvalidateTile(Entity<GridAtmosphereComponent?> entity, Vector2i tile)
    {
        if (_atmosQuery.Resolve(entity.Owner, ref entity.Comp, false))
            entity.Comp.InvalidatedCoords.Add(tile);
    }

    /// <summary>
    /// Gets the gas mixtures for a list of tiles on a grid or map.
    /// </summary>
    /// <param name="grid">The grid to get mixtures from.</param>
    /// <param name="map">The map to get mixtures from.</param>
    /// <param name="tiles">The list of tiles to get mixtures for.</param>
    /// <param name="excite">Whether to mark the tiles as active for atmosphere processing.</param>
    /// <returns>>An array of gas mixtures corresponding to the input tiles.</returns>
    [PublicAPI]
    public GasMixture?[]? GetTileMixtures(
        Entity<GridAtmosphereComponent?, GasTileOverlayComponent?>? grid,
        Entity<MapAtmosphereComponent?>? map,
        List<Vector2i> tiles,
        bool excite = false)
    {
        GasMixture?[]? mixtures = null;
        var handled = false;

        // If we've been passed a grid, try to let it handle it.
        if (grid is { } gridEnt && _atmosQuery.Resolve(gridEnt, ref gridEnt.Comp1))
        {
            if (excite)
                Resolve(gridEnt, ref gridEnt.Comp2);

            handled = true;
            mixtures = new GasMixture?[tiles.Count];

            for (var i = 0; i < tiles.Count; i++)
            {
                var tile = tiles[i];
                if (!gridEnt.Comp1.Tiles.TryGetValue(tile, out var atmosTile))
                {
                    // need to get map atmosphere
                    handled = false;
                    continue;
                }

                mixtures[i] = atmosTile.Air;

                if (excite)
                {
                    AddActiveTile(gridEnt.Comp1, atmosTile);
                    InvalidateVisuals((gridEnt.Owner, gridEnt.Comp2), tile);
                }
            }
        }

        if (handled)
            return mixtures;

        // We either don't have a grid, or the event wasn't handled.
        // Let the map handle it instead, and also broadcast the event.
        if (map is { } mapEnt && _mapAtmosQuery.Resolve(mapEnt, ref mapEnt.Comp))
        {
            mixtures ??= new GasMixture?[tiles.Count];
            for (var i = 0; i < tiles.Count; i++)
            {
                mixtures[i] ??= mapEnt.Comp.Mixture;
            }

            return mixtures;
        }

        // Default to a space mixture... This is a space game, after all!
        mixtures ??= new GasMixture?[tiles.Count];
        for (var i = 0; i < tiles.Count; i++)
        {
            mixtures[i] ??= GasMixture.SpaceGas;
        }

        return mixtures;
    }

    /// <summary>
    /// Gets the gas mixture for a specific tile that an entity is on.
    /// </summary>
    /// <param name="entity">The entity to get the tile mixture for.</param>
    /// <param name="excite">Whether to mark the tile as active for atmosphere processing.</param>
    /// <returns>A <see cref="GasMixture"/> if one could be found, null otherwise.</returns>
    /// <remarks>This does not return the <see cref="GasMixture"/> that the entity
    /// may be contained in, ex. if the entity is currently in a locker/crate with its own
    /// <see cref="GasMixture"/>.</remarks>
    [PublicAPI]
    public GasMixture? GetTileMixture(Entity<TransformComponent?> entity, bool excite = false)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return null;

        var indices = _transformSystem.GetGridTilePositionOrDefault(entity);
        return GetTileMixture(entity.Comp.GridUid, entity.Comp.MapUid, indices, excite);
    }

    /// <summary>
    /// Gets the gas mixture for a specific tile on a grid or map.
    /// </summary>
    /// <param name="grid">The grid to get the mixture from.</param>
    /// <param name="map">The map to get the mixture from.</param>
    /// <param name="gridTile">The tile to get the mixture from.</param>
    /// <param name="excite">Whether to mark the tile as active for atmosphere processing.</param>
    /// <returns>>A <see cref="GasMixture"/> if one could be found, null otherwise.</returns>
    [PublicAPI]
    public GasMixture? GetTileMixture(
        Entity<GridAtmosphereComponent?, GasTileOverlayComponent?>? grid,
        Entity<MapAtmosphereComponent?>? map,
        Vector2i gridTile,
        bool excite = false)
    {
        // If we've been passed a grid, try to let it handle it.
        if (grid is { } gridEnt
            && _atmosQuery.Resolve(gridEnt, ref gridEnt.Comp1, false)
            && gridEnt.Comp1.Tiles.TryGetValue(gridTile, out var tile))
        {
            if (excite)
            {
                AddActiveTile(gridEnt.Comp1, tile);
                InvalidateVisuals((grid.Value.Owner, grid.Value.Comp2), gridTile);
            }

            return tile.Air;
        }

        if (map is { } mapEnt && _mapAtmosQuery.Resolve(mapEnt, ref mapEnt.Comp, false))
            return mapEnt.Comp.Mixture;

        // Default to a space mixture... This is a space game, after all!
        return GasMixture.SpaceGas;
    }

    /// <summary>
    /// Triggers a tile's <see cref="GasMixture"/> to react.
    /// </summary>
    /// <param name="gridId">The grid to react the tile on.</param>
    /// <param name="tile">The tile to react.</param>
    /// <returns>The result of the reaction.</returns>
    [PublicAPI]
    public ReactionResult ReactTile(EntityUid gridId, Vector2i tile)
    {
        var ev = new ReactTileMethodEvent(gridId, tile);
        RaiseLocalEvent(gridId, ref ev);

        ev.Handled = true;

        return ev.Result;
    }

    /// <summary>
    /// Checks if a tile on a grid is air-blocked in the specified directions.
    /// </summary>
    /// <param name="gridUid">The grid to check.</param>
    /// <param name="tile">The tile on the grid to check.</param>
    /// <param name="directions">The directions to check for air-blockage.</param>
    /// <param name="mapGridComp">Optional map grid component associated with the grid.</param>
    /// <returns>True if the tile is air-blocked in the specified directions, false otherwise.</returns>
    [PublicAPI]
    public bool IsTileAirBlocked(EntityUid gridUid,
        Vector2i tile,
        AtmosDirection directions = AtmosDirection.All,
        MapGridComponent? mapGridComp = null)
    {
        if (!Resolve(gridUid, ref mapGridComp, false))
            return false;

        // TODO ATMOS: This reconstructs the data instead of getting the cached version. Might want to include a method to get the cached version later.
        var data = GetAirtightData(gridUid, mapGridComp, tile);
        return data.BlockedDirections.IsFlagSet(directions);
    }

    /// <summary>
    /// Checks if a tile on a grid or map is space as defined by a tile's definition of space.
    /// Some tiles can hold back space and others cannot - for example, plating can hold
    /// back space, whereas scaffolding cannot, exposing the map atmosphere beneath.
    /// </summary>
    /// <remarks>This does not check if the <see cref="GasMixture"/> on the tile is space,
    /// it only checks the current tile's ability to hold back space.</remarks>
    /// <param name="grid">The grid to check.</param>
    /// <param name="map">The map to check.</param>
    /// <param name="tile">The tile to check.</param>
    /// <returns>True if the tile is space, false otherwise.</returns>
    [PublicAPI]
    public bool IsTileSpace(Entity<GridAtmosphereComponent?>? grid, Entity<MapAtmosphereComponent?>? map, Vector2i tile)
    {
        if (grid is { } gridEnt && _atmosQuery.Resolve(gridEnt, ref gridEnt.Comp, false)
                                && gridEnt.Comp.Tiles.TryGetValue(tile, out var tileAtmos))
        {
            return tileAtmos.Space;
        }

        if (map is { } mapEnt && _mapAtmosQuery.Resolve(mapEnt, ref mapEnt.Comp, false))
            return mapEnt.Comp.Space;

        // If nothing handled the event, it'll default to true.
        // Oh well, this is a space game after all, deal with it!
        return true;
    }

    /// <summary>
    /// Checks if the gas mixture on a tile is "probably safe".
    /// Probably safe is defined as having at least air alarm-grade safe pressure and temperature.
    /// (more than 260K, less than 360K, and between safe low and high pressure as defined in
    /// <see cref="Atmospherics.WarningLowPressure"/> and <see cref="Atmospherics.WarningHighPressure"/>)
    /// </summary>
    /// <param name="grid">The grid to check.</param>
    /// <param name="map">The map to check.</param>
    /// <param name="tile">The tile to check.</param>
    /// <returns>True if the tile's mixture is probably safe, false otherwise.</returns>
    [PublicAPI]
    public bool IsTileMixtureProbablySafe(Entity<GridAtmosphereComponent?>? grid, Entity<MapAtmosphereComponent?> map, Vector2i tile)
    {
        return IsMixtureProbablySafe(GetTileMixture(grid, map, tile));
    }

    /// <summary>
    /// Gets the heat capacity of the gas mixture on a tile.
    /// </summary>
    /// <param name="grid">The grid to check.</param>
    /// <param name="map">The map to check.</param>
    /// <param name="tile">The tile on the grid/map to check.</param>
    /// <returns>>The heat capacity of the tile's mixture, or the heat capacity of space if a mixture could not be found.</returns>
    [PublicAPI]
    public float GetTileHeatCapacity(Entity<GridAtmosphereComponent?>? grid, Entity<MapAtmosphereComponent?> map, Vector2i tile)
    {
        return GetHeatCapacity(GetTileMixture(grid, map, tile) ?? GasMixture.SpaceGas);
    }

    /// <summary>
    /// Gets an enumerator for the adjacent tile mixtures of a tile on a grid.
    /// </summary>
    /// <param name="grid">The grid to get adjacent tile mixtures from.</param>
    /// <param name="tile">The tile to get adjacent mixtures for.</param>
    /// <param name="includeBlocked">Whether to include blocked adjacent tiles.</param>
    /// <param name="excite">Whether to mark the adjacent tiles as active for atmosphere processing.</param>
    /// <returns>An enumerator for the adjacent tile mixtures.</returns>
    [PublicAPI]
    public TileMixtureEnumerator GetAdjacentTileMixtures(Entity<GridAtmosphereComponent?> grid, Vector2i tile, bool includeBlocked = false, bool excite = false)
    {
        // TODO ATMOS includeBlocked and excite parameters are unhandled currently.
        if (!_atmosQuery.Resolve(grid, ref grid.Comp, false))
            return TileMixtureEnumerator.Empty;

        return !grid.Comp.Tiles.TryGetValue(tile, out var atmosTile)
            ? TileMixtureEnumerator.Empty
            : new TileMixtureEnumerator(atmosTile.AdjacentTiles);
    }

    /// <summary>
    /// Exposes a tile to a hotspot of given temperature and volume, igniting it if conditions are met.
    /// </summary>
    /// <param name="grid">The grid to expose the tile on.</param>
    /// <param name="tile">The tile to expose.</param>
    /// <param name="exposedTemperature">The temperature of the hotspot to expose.
    /// You can think of this as exposing a temperature of a flame.</param>
    /// <param name="exposedVolume">The volume of the hotspot to expose.
    /// You can think of this as how big the flame is initially.
    /// Bigger flames will ramp a fire faster.</param>
    /// <param name="soh">Whether to "boost" a fire that's currently on the tile already.
    /// Does nothing if the tile isn't already a hotspot.
    /// This clamps the temperature and volume of the hotspot to the maximum
    /// of the provided parameters and whatever's on the tile.</param>
    /// <param name="sparkSourceUid">Entity that started the exposure for admin logging.</param>
    [PublicAPI]
    public void HotspotExpose(Entity<GridAtmosphereComponent?> grid,
        Vector2i tile,
        float exposedTemperature,
        float exposedVolume,
        EntityUid? sparkSourceUid = null,
        bool soh = false)
    {
        if (!_atmosQuery.Resolve(grid, ref grid.Comp, false))
            return;

        if (grid.Comp.Tiles.TryGetValue(tile, out var atmosTile))
            HotspotExpose(grid.Comp, atmosTile, exposedTemperature, exposedVolume, soh, sparkSourceUid);
    }

    /// <summary>
    /// Exposes a tile to a hotspot of given temperature and volume, igniting it if conditions are met.
    /// </summary>
    /// <param name="tile">The <see cref="TileAtmosphere"/> to expose.</param>
    /// <param name="exposedTemperature">The temperature of the hotspot to expose.
    /// You can think of this as exposing a temperature of a flame.</param>
    /// <param name="exposedVolume">The volume of the hotspot to expose.
    /// You can think of this as how big the flame is initially.
    /// Bigger flames will ramp a fire faster.</param>
    /// <param name="soh">Whether to "boost" a fire that's currently on the tile already.
    /// Does nothing if the tile isn't already a hotspot.
    /// This clamps the temperature and volume of the hotspot to the maximum
    /// of the provided parameters and whatever's on the tile.</param>
    /// <param name="sparkSourceUid">Entity that started the exposure for admin logging.</param>
    [PublicAPI]
    public void HotspotExpose(TileAtmosphere tile,
        float exposedTemperature,
        float exposedVolume,
        EntityUid? sparkSourceUid = null,
        bool soh = false)
    {
        if (!_atmosQuery.TryGetComponent(tile.GridIndex, out var atmos))
            return;

        DebugTools.Assert(atmos.Tiles.TryGetValue(tile.GridIndices, out var tmp) && tmp == tile);
        HotspotExpose(atmos, tile, exposedTemperature, exposedVolume, soh, sparkSourceUid);
    }

    /// <summary>
    /// Extinguishes a hotspot on a tile.
    /// </summary>
    /// <param name="gridUid">The grid to extinguish the hotspot on.</param>
    /// <param name="tile">The tile on the grid to extinguish the hotspot on.</param>
    [PublicAPI]
    public void HotspotExtinguish(EntityUid gridUid, Vector2i tile)
    {
        var ev = new HotspotExtinguishMethodEvent(gridUid, tile);
        RaiseLocalEvent(gridUid, ref ev);
    }

    /// <summary>
    /// Checks if a hotspot is active on a tile.
    /// </summary>
    /// <param name="gridUid">The grid to check.</param>
    /// <param name="tile">The tile on the grid to check.</param>
    /// <returns>True if a hotspot is active on the tile, false otherwise.</returns>
    [PublicAPI]
    public bool IsHotspotActive(EntityUid gridUid, Vector2i tile)
    {
        var ev = new IsHotspotActiveMethodEvent(gridUid, tile);
        RaiseLocalEvent(gridUid, ref ev);

        // If not handled, this will be false. Just like in space!
        return ev.Result;
    }

    /// <summary>
    /// Adds a <see cref="PipeNet"/> to a grid.
    /// </summary>
    /// <param name="grid">The grid to add the pipe net to.</param>
    /// <param name="pipeNet">The pipe net to add.</param>
    /// <returns>True if the pipe net was added, false otherwise.</returns>
    [PublicAPI]
    public bool AddPipeNet(Entity<GridAtmosphereComponent?> grid, PipeNet pipeNet)
    {
        return _atmosQuery.Resolve(grid, ref grid.Comp, false) && grid.Comp.PipeNets.Add(pipeNet);
    }

    /// <summary>
    /// Removes a <see cref="PipeNet"/> from a grid.
    /// </summary>
    /// <param name="grid">The grid to remove the pipe net from.</param>
    /// <param name="pipeNet">The pipe net to remove.</param>
    /// <returns>True if the pipe net was removed, false otherwise.</returns>
    [PublicAPI]
    public bool RemovePipeNet(Entity<GridAtmosphereComponent?> grid, PipeNet pipeNet)
    {
        // Technically this event can be fired even on grids that don't
        // actually have grid atmospheres.
        if (pipeNet.Grid is not null)
        {
            var ev = new PipeNodeGroupRemovedEvent(grid, pipeNet.NetId);
            RaiseLocalEvent(ref ev);
        }

        return _atmosQuery.Resolve(grid, ref grid.Comp, false) && grid.Comp.PipeNets.Remove(pipeNet);
    }

    /// <summary>
    /// Adds an entity with an <see cref="AtmosDeviceComponent"/> to a grid's list of atmos devices.
    /// </summary>
    /// <param name="grid">The grid to add the device to.</param>
    /// <param name="device">The device to add.</param>
    /// <returns>True if the device was added, false otherwise.</returns>
    [PublicAPI]
    public bool AddAtmosDevice(Entity<GridAtmosphereComponent?> grid, Entity<AtmosDeviceComponent> device)
    {
        DebugTools.Assert(device.Comp.JoinedGrid == null);
        DebugTools.Assert(Transform(device).GridUid == grid);

        if (!_atmosQuery.Resolve(grid, ref grid.Comp, false))
            return false;

        if (!grid.Comp.AtmosDevices.Add(device))
            return false;

        device.Comp.JoinedGrid = grid;
        return true;
    }

    /// <summary>
    /// Removes an entity with an <see cref="AtmosDeviceComponent"/> from a grid's list of atmos devices.
    /// </summary>
    /// <param name="grid">The grid to remove the device from.</param>
    /// <param name="device">The device to remove.</param>
    /// <returns>True if the device was removed, false otherwise.</returns>
    public bool RemoveAtmosDevice(Entity<GridAtmosphereComponent?> grid, Entity<AtmosDeviceComponent> device)
    {
        DebugTools.Assert(device.Comp.JoinedGrid == grid);

        if (!_atmosQuery.Resolve(grid, ref grid.Comp, false))
            return false;

        if (!grid.Comp.AtmosDevices.Remove(device))
            return false;

        device.Comp.JoinedGrid = null;
        return true;
    }

    /// <summary>
    /// Adds an entity with a DeltaPressureComponent to the DeltaPressure processing list.
    /// Also fills in important information on the component itself.
    /// </summary>
    /// <param name="grid">The grid to add the entity to.</param>
    /// <param name="ent">The entity to add.</param>
    /// <returns>True if the entity was added to the list, false if it could not be added or
    /// if the entity was already present in the list.</returns>
    [PublicAPI]
    public bool TryAddDeltaPressureEntity(Entity<GridAtmosphereComponent?> grid, Entity<DeltaPressureComponent> ent)
    {
        // The entity needs to be part of a grid, and it should be the right one :)
        var xform = Transform(ent);

        // The entity is not on a grid, so it cannot possibly have an atmosphere that affects it.
        if (xform.GridUid == null)
        {
            return false;
        }

        // Entity should be on the grid it's being added to.
        Debug.Assert(xform.GridUid == grid.Owner);

        if (!_atmosQuery.Resolve(grid, ref grid.Comp, false))
            return false;

        if (grid.Comp.DeltaPressureEntityLookup.ContainsKey(ent.Owner))
        {
            return false;
        }

        grid.Comp.DeltaPressureEntityLookup[ent.Owner] = grid.Comp.DeltaPressureEntities.Count;
        grid.Comp.DeltaPressureEntities.Add(ent);

        ent.Comp.GridUid = grid.Owner;
        ent.Comp.InProcessingList = true;

        return true;
    }

    /// <summary>
    /// Removes an entity with a DeltaPressureComponent from the DeltaPressure processing list.
    /// </summary>
    /// <param name="grid">The grid to remove the entity from.</param>
    /// <param name="ent">The entity to remove.</param>
    /// <returns>True if the entity was removed from the list, false if it could not be removed or
    /// if the entity was not present in the list.</returns>
    [PublicAPI]
    public bool TryRemoveDeltaPressureEntity(Entity<GridAtmosphereComponent?> grid, Entity<DeltaPressureComponent> ent)
    {
        if (!_atmosQuery.Resolve(grid, ref grid.Comp, false))
            return false;

        if (!grid.Comp.DeltaPressureEntityLookup.TryGetValue(ent.Owner, out var index))
            return false;

        var lastIndex = grid.Comp.DeltaPressureEntities.Count - 1;
        if (lastIndex < 0)
            return false;

        if (index != lastIndex)
        {
            var lastEnt = grid.Comp.DeltaPressureEntities[lastIndex];
            grid.Comp.DeltaPressureEntities[index] = lastEnt;
            grid.Comp.DeltaPressureEntityLookup[lastEnt.Owner] = index;
        }

        grid.Comp.DeltaPressureEntities.RemoveAt(lastIndex);
        grid.Comp.DeltaPressureEntityLookup.Remove(ent.Owner);

        if (grid.Comp.DeltaPressureCursor > grid.Comp.DeltaPressureEntities.Count)
            grid.Comp.DeltaPressureCursor = grid.Comp.DeltaPressureEntities.Count;

        ent.Comp.InProcessingList = false;
        ent.Comp.GridUid = null;
        return true;
    }

    /// <summary>
    /// Checks if a DeltaPressureComponent is currently considered for processing on a grid.
    /// </summary>
    /// <param name="grid">The grid that the entity may belong to.</param>
    /// <param name="ent">The entity to check.</param>
    /// <returns>True if the entity is part of the processing list, false otherwise.</returns>
    [PublicAPI]
    public bool IsDeltaPressureEntityInList(Entity<GridAtmosphereComponent?> grid, Entity<DeltaPressureComponent> ent)
    {
        // Dict and list must be in sync - deep-fried if we aren't.
        if (!_atmosQuery.Resolve(grid, ref grid.Comp, false))
            return false;

        var contains = grid.Comp.DeltaPressureEntityLookup.ContainsKey(ent.Owner);
        Debug.Assert(contains == grid.Comp.DeltaPressureEntities.Contains(ent));

        return contains;
    }

    [ByRefEvent]
    private record struct SetSimulatedGridMethodEvent(
        EntityUid Grid,
        bool Simulated,
        bool Handled = false);

    [ByRefEvent]
    private record struct IsSimulatedGridMethodEvent(
        EntityUid Grid,
        bool Simulated = false,
        bool Handled = false);

    [ByRefEvent]
    private record struct GetAllMixturesMethodEvent(
        EntityUid Grid,
        bool Excite = false,
        IEnumerable<GasMixture>? Mixtures = null,
        bool Handled = false);

    [ByRefEvent]
    private record struct ReactTileMethodEvent(
        EntityUid GridId,
        Vector2i Tile,
        ReactionResult Result = default,
        bool Handled = false);

    [ByRefEvent]
    private record struct HotspotExtinguishMethodEvent(
        EntityUid Grid,
        Vector2i Tile,
        bool Handled = false);

    [ByRefEvent]
    private record struct IsHotspotActiveMethodEvent(
        EntityUid Grid,
        Vector2i Tile,
        bool Result = false,
        bool Handled = false);
}


/// <summary>
/// Raised broadcasted when a pipe node group within a grid has been removed.
/// </summary>
/// <param name="Grid">The grid with the removed node group.</param>
/// <param name="NetId">The net id of the removed node group.</param>
[ByRefEvent]
public record struct PipeNodeGroupRemovedEvent(EntityUid Grid, int NetId);
