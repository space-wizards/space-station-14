using System.Linq;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Reactions;
using Content.Server.NodeContainer.NodeGroups;
using Content.Shared.Atmos;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Server.Atmos.EntitySystems;

public partial class AtmosphereSystem
{
    public GasMixture? GetContainingMixture(EntityUid uid, bool ignoreExposed = false, bool excite = false, TransformComponent? transform = null)
    {
        if (!ignoreExposed)
        {
            // Used for things like disposals/cryo to change which air people are exposed to.
            var ev = new AtmosExposedGetAirEvent(uid, excite);

            // Give the entity itself a chance to handle this.
            RaiseLocalEvent(uid, ref ev, false);

            if (ev.Handled)
                return ev.Gas;

            // We need to get the parent now, so we need the transform... If the parent is invalid, we can't do much else.
            if(!Resolve(uid, ref transform) || !transform.ParentUid.IsValid() || transform.MapUid == null)
                return GetTileMixture(null, null, Vector2i.Zero, excite);

            // Give the parent entity a chance to handle the event...
            RaiseLocalEvent(transform.ParentUid, ref ev, false);

            if (ev.Handled)
                return ev.Gas;
        }
        // Oops, we did a little bit of code duplication...
        else if(!Resolve(uid, ref transform))
        {
            return GetTileMixture(null, null, Vector2i.Zero, excite);
        }


        var gridUid = transform.GridUid;
        var mapUid = transform.MapUid;
        var position = _transformSystem.GetGridOrMapTilePosition(uid, transform);

        return GetTileMixture(gridUid, mapUid, position, excite);
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

    public GasMixture?[]? GetTileMixtures(Entity<GridAtmosphereComponent?>? grid, Entity<MapAtmosphereComponent?>? map, List<Vector2i> tiles, bool excite = false)
    {
        GasMixture?[]? mixtures = null;
        var handled = false;

        // If we've been passed a grid, try to let it handle it.
        if (grid is {} gridEnt && Resolve(gridEnt, ref gridEnt.Comp))
        {
            handled = true;
            mixtures = new GasMixture?[tiles.Count];

            for (var i = 0; i < tiles.Count; i++)
            {
                var tile = tiles[i];
                if (!gridEnt.Comp.Tiles.TryGetValue(tile, out var atmosTile))
                {
                    // need to get map atmosphere
                    handled = false;
                    continue;
                }

                mixtures[i] = atmosTile.Air;

                if (excite)
                    gridEnt.Comp.InvalidatedCoords.Add(tile);
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

    public GasMixture? GetTileMixture(Entity<GridAtmosphereComponent?>? grid, Entity<MapAtmosphereComponent?>? map, Vector2i gridTile, bool excite = false)
    {
        // If we've been passed a grid, try to let it handle it.
        if (grid is {} gridEnt
            && Resolve(gridEnt, ref gridEnt.Comp, false)
            && gridEnt.Comp.Tiles.TryGetValue(gridTile, out var tile))
        {
            if (excite)
                gridEnt.Comp.InvalidatedCoords.Add(gridTile);

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
