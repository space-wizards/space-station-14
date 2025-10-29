using System.Diagnostics;
using System.Linq;
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
    public GasMixture? GetContainingMixture(Entity<TransformComponent?> ent, bool ignoreExposed = false, bool excite = false)
    {
        if (!Resolve(ent, ref ent.Comp))
            return null;

        return GetContainingMixture(ent, ent.Comp.GridUid, ent.Comp.MapUid, ignoreExposed, excite);
    }

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

    public bool HasAtmosphere(EntityUid gridUid) => _atmosQuery.HasComponent(gridUid);

    public bool SetSimulatedGrid(EntityUid gridUid, bool simulated)
    {
        var ev = new SetSimulatedGridMethodEvent(gridUid, simulated);
        RaiseLocalEvent(gridUid, ref ev);

        return ev.Handled;
    }

    public bool IsSimulatedGrid(EntityUid gridUid)
    {
        var ev = new IsSimulatedGridMethodEvent(gridUid);
        RaiseLocalEvent(gridUid, ref ev);

        return ev.Simulated;
    }

    public IEnumerable<GasMixture> GetAllMixtures(EntityUid gridUid, bool excite = false)
    {
        var ev = new GetAllMixturesMethodEvent(gridUid, excite);
        RaiseLocalEvent(gridUid, ref ev);

        if(!ev.Handled)
            return Enumerable.Empty<GasMixture>();

        DebugTools.AssertNotNull(ev.Mixtures);
        return ev.Mixtures!;
    }

    public void InvalidateTile(Entity<GridAtmosphereComponent?> entity, Vector2i tile)
    {
        if (_atmosQuery.Resolve(entity.Owner, ref entity.Comp, false))
            entity.Comp.InvalidatedCoords.Add(tile);
    }

    public GasMixture?[]? GetTileMixtures(
        Entity<GridAtmosphereComponent?, GasTileOverlayComponent?>? grid,
        Entity<MapAtmosphereComponent?>? map,
        List<Vector2i> tiles,
        bool excite = false)
    {
        GasMixture?[]? mixtures = null;
        var handled = false;

        // If we've been passed a grid, try to let it handle it.
        if (grid is {} gridEnt && Resolve(gridEnt, ref gridEnt.Comp1))
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
        if (map is {} mapEnt && _mapAtmosQuery.Resolve(mapEnt, ref mapEnt.Comp))
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

    public GasMixture? GetTileMixture (Entity<TransformComponent?> entity, bool excite = false)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return null;

        var indices = _transformSystem.GetGridTilePositionOrDefault(entity);
        return GetTileMixture(entity.Comp.GridUid, entity.Comp.MapUid, indices, excite);
    }

    public GasMixture? GetTileMixture(
        Entity<GridAtmosphereComponent?, GasTileOverlayComponent?>? grid,
        Entity<MapAtmosphereComponent?>? map,
        Vector2i gridTile,
        bool excite = false)
    {
        // If we've been passed a grid, try to let it handle it.
        if (grid is {} gridEnt
            && Resolve(gridEnt, ref gridEnt.Comp1, false)
            && gridEnt.Comp1.Tiles.TryGetValue(gridTile, out var tile))
        {
            if (excite)
            {
                AddActiveTile(gridEnt.Comp1, tile);
                InvalidateVisuals((grid.Value.Owner, grid.Value.Comp2), gridTile);
            }

            return tile.Air;
        }

        if (map is {} mapEnt && _mapAtmosQuery.Resolve(mapEnt, ref mapEnt.Comp, false))
            return mapEnt.Comp.Mixture;

        // Default to a space mixture... This is a space game, after all!
        return GasMixture.SpaceGas;
    }

    public ReactionResult ReactTile(EntityUid gridId, Vector2i tile)
    {
        var ev = new ReactTileMethodEvent(gridId, tile);
        RaiseLocalEvent(gridId, ref ev);

        ev.Handled = true;

        return ev.Result;
    }

    public bool IsTileAirBlocked(EntityUid gridUid, Vector2i tile, AtmosDirection directions = AtmosDirection.All, MapGridComponent? mapGridComp = null)
    {
        if (!Resolve(gridUid, ref mapGridComp, false))
            return false;

        var data = GetAirtightData(gridUid, mapGridComp, tile);
        return data.BlockedDirections.IsFlagSet(directions);
    }

    public bool IsTileSpace(Entity<GridAtmosphereComponent?>? grid, Entity<MapAtmosphereComponent?>? map, Vector2i tile)
    {
        if (grid is {} gridEnt && _atmosQuery.Resolve(gridEnt, ref gridEnt.Comp, false)
            && gridEnt.Comp.Tiles.TryGetValue(tile, out var tileAtmos))
        {
            return tileAtmos.Space;
        }

        if (map is {} mapEnt && _mapAtmosQuery.Resolve(mapEnt, ref mapEnt.Comp, false))
            return mapEnt.Comp.Space;

        // If nothing handled the event, it'll default to true.
        // Oh well, this is a space game after all, deal with it!
        return true;
    }

    public bool IsTileMixtureProbablySafe(Entity<GridAtmosphereComponent?>? grid, Entity<MapAtmosphereComponent?> map, Vector2i tile)
    {
        return IsMixtureProbablySafe(GetTileMixture(grid, map, tile));
    }

    public float GetTileHeatCapacity(Entity<GridAtmosphereComponent?>? grid, Entity<MapAtmosphereComponent?> map, Vector2i tile)
    {
        return GetHeatCapacity(GetTileMixture(grid, map, tile) ?? GasMixture.SpaceGas);
    }

    public TileMixtureEnumerator GetAdjacentTileMixtures(Entity<GridAtmosphereComponent?> grid, Vector2i tile, bool includeBlocked = false, bool excite = false)
    {
        if (!_atmosQuery.Resolve(grid, ref grid.Comp, false))
            return TileMixtureEnumerator.Empty;

        return !grid.Comp.Tiles.TryGetValue(tile, out var atmosTile)
            ? TileMixtureEnumerator.Empty
            : new(atmosTile.AdjacentTiles);
    }

    public void HotspotExpose(Entity<GridAtmosphereComponent?> grid, Vector2i tile, float exposedTemperature, float exposedVolume,
        EntityUid? sparkSourceUid = null, bool soh = false)
    {
        if (!_atmosQuery.Resolve(grid, ref grid.Comp, false))
            return;

        if (grid.Comp.Tiles.TryGetValue(tile, out var atmosTile))
            HotspotExpose(grid.Comp, atmosTile, exposedTemperature, exposedVolume, soh, sparkSourceUid);
    }

    public void HotspotExpose(TileAtmosphere tile, float exposedTemperature, float exposedVolume,
        EntityUid? sparkSourceUid = null, bool soh = false)
    {
        if (!_atmosQuery.TryGetComponent(tile.GridIndex, out var atmos))
            return;

        DebugTools.Assert(atmos.Tiles.TryGetValue(tile.GridIndices, out var tmp) && tmp == tile);
        HotspotExpose(atmos, tile, exposedTemperature, exposedVolume, soh, sparkSourceUid);
    }

    public void HotspotExtinguish(EntityUid gridUid, Vector2i tile)
    {
        var ev = new HotspotExtinguishMethodEvent(gridUid, tile);
        RaiseLocalEvent(gridUid, ref ev);
    }

    public bool IsHotspotActive(EntityUid gridUid, Vector2i tile)
    {
        var ev = new IsHotspotActiveMethodEvent(gridUid, tile);
        RaiseLocalEvent(gridUid, ref ev);

        // If not handled, this will be false. Just like in space!
        return ev.Result;
    }

    public bool AddPipeNet(Entity<GridAtmosphereComponent?> grid, PipeNet pipeNet)
    {
        return _atmosQuery.Resolve(grid, ref grid.Comp, false) && grid.Comp.PipeNets.Add(pipeNet);
    }

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

    [ByRefEvent] private record struct SetSimulatedGridMethodEvent
        (EntityUid Grid, bool Simulated, bool Handled = false);

    [ByRefEvent] private record struct IsSimulatedGridMethodEvent
        (EntityUid Grid, bool Simulated = false, bool Handled = false);

    [ByRefEvent] private record struct GetAllMixturesMethodEvent
        (EntityUid Grid, bool Excite = false, IEnumerable<GasMixture>? Mixtures = null, bool Handled = false);

    [ByRefEvent] private record struct ReactTileMethodEvent
        (EntityUid GridId, Vector2i Tile, ReactionResult Result = default, bool Handled = false);

    [ByRefEvent] private record struct HotspotExtinguishMethodEvent
        (EntityUid Grid, Vector2i Tile, bool Handled = false);

    [ByRefEvent] private record struct IsHotspotActiveMethodEvent
        (EntityUid Grid, Vector2i Tile, bool Result = false, bool Handled = false);
}


/// <summary>
/// Raised broadcasted when a pipe node group within a grid has been removed.
/// </summary>
/// <param name="Grid">The grid with the removed node group.</param>
/// <param name="NetId">The net id of the removed node group.</param>
[ByRefEvent]
public record struct PipeNodeGroupRemovedEvent(EntityUid Grid, int NetId);
