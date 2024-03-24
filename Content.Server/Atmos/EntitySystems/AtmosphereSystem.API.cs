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

    public GasMixture?[]? GetTileMixtures(EntityUid? gridUid, EntityUid? mapUid, List<Vector2i> tiles, bool excite = false)
    {
        GasMixture?[]? mixtures = null;
        var handled = false;

        // If we've been passed a grid, try to let it handle it.
        if (_atmosQuery.TryGetComponent(gridUid, out var gridAtmos))
        {
            handled = true;
            mixtures = new GasMixture?[tiles.Count];

            for (var i = 0; i < tiles.Count; i++)
            {
                var tile = tiles[i];
                if (!gridAtmos.Tiles.TryGetValue(tile, out var atmosTile))
                {
                    // need to get map atmosphere
                    handled = false;
                    continue;
                }

                mixtures[i] = atmosTile.Air;

                if (excite)
                    gridAtmos.InvalidatedCoords.Add(tile);
            }
        }

        if (handled)
            return mixtures;

        // We either don't have a grid, or the event wasn't handled.
        // Let the map handle it instead, and also broadcast the event.
        if (_mapAtmosQuery.TryGetComponent(mapUid, out var mapAtmos))
        {
            mixtures ??= new GasMixture?[tiles.Count];

            for (var i = 0; i < tiles.Count; i++)
            {
                mixtures[i] ??= mapAtmos.Mixture;
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

    public GasMixture? GetTileMixture(EntityUid? gridUid, EntityUid? mapUid, Vector2i gridTile, bool excite = false)
    {
        // If we've been passed a grid, try to let it handle it.
        if (_atmosQuery.TryGetComponent(gridUid, out var gridAtmos)
            && gridAtmos.Tiles.TryGetValue(gridTile, out var tile))
        {
            if (excite)
                gridAtmos.InvalidatedCoords.Add(gridTile);

            return tile.Air;
        }

        if (_mapAtmosQuery.TryGetComponent(mapUid, out var mapAtmos))
            return mapAtmos.Mixture;

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
        if (!Resolve(gridUid, ref mapGridComp))
            return false;

        var data = GetAirtightData(gridUid, mapGridComp, tile);
        return data.BlockedDirections.IsFlagSet(directions);
    }

    public bool IsTileSpace(EntityUid? gridUid, EntityUid? mapUid, Vector2i tile, MapGridComponent? mapGridComp = null)
    {
        if (_atmosQuery.TryGetComponent(gridUid, out var gridAtmos) &&
            gridAtmos.Tiles.TryGetValue(tile, out var tileAtmos))
        {
            return tileAtmos.Space;
        }

        if (_mapAtmosQuery.TryGetComponent(mapUid, out var mapAtmos))
            return mapAtmos.Space;

        // If nothing handled the event, it'll default to true.
        // Oh well, this is a space game after all, deal with it!
        return true;
    }

    public bool IsTileMixtureProbablySafe(EntityUid? gridUid, EntityUid mapUid, Vector2i tile)
    {
        return IsMixtureProbablySafe(GetTileMixture(gridUid, mapUid, tile));
    }

    public float GetTileHeatCapacity(EntityUid? gridUid, EntityUid mapUid, Vector2i tile)
    {
        return GetHeatCapacity(GetTileMixture(gridUid, mapUid, tile) ?? GasMixture.SpaceGas);
    }

    public TileMixtureEnumerator GetAdjacentTileMixtures(EntityUid gridUid, Vector2i tile, bool includeBlocked = false, bool excite = false)
    {
        if (!_atmosQuery.TryGetComponent(gridUid, out var atmos))
            return TileMixtureEnumerator.Empty;

        return !atmos.Tiles.TryGetValue(tile, out var atmosTile)
            ? TileMixtureEnumerator.Empty
            : new(atmosTile.AdjacentTiles);
    }

    public void HotspotExpose(EntityUid gridUid, Vector2i tile, float exposedTemperature, float exposedVolume,
        EntityUid? sparkSourceUid = null, bool soh = false)
    {
        if (!_atmosQuery.TryGetComponent(gridUid, out var atmos))
            return;

        if (atmos.Tiles.TryGetValue(tile, out var atmosTile))
            HotspotExpose(atmos, atmosTile, exposedTemperature, exposedVolume, soh, sparkSourceUid);
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

    public void AddPipeNet(EntityUid gridUid, PipeNet pipeNet)
    {
        var ev = new AddPipeNetMethodEvent(gridUid, pipeNet);
        RaiseLocalEvent(gridUid, ref ev);
    }

    public void RemovePipeNet(EntityUid gridUid, PipeNet pipeNet)
    {
        var ev = new RemovePipeNetMethodEvent(gridUid, pipeNet);
        RaiseLocalEvent(gridUid, ref ev);
    }

    public bool AddAtmosDevice(EntityUid gridUid, AtmosDeviceComponent device)
    {
        // TODO: check device is on grid

        var ev = new AddAtmosDeviceMethodEvent(gridUid, device);
        RaiseLocalEvent(gridUid, ref ev);
        return ev.Result;
    }

    public bool RemoveAtmosDevice(EntityUid gridUid, AtmosDeviceComponent device)
    {
        // TODO: check device is on grid

        var ev = new RemoveAtmosDeviceMethodEvent(gridUid, device);
        RaiseLocalEvent(gridUid, ref ev);
        return ev.Result;
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

    [ByRefEvent] private record struct AddPipeNetMethodEvent
        (EntityUid Grid, PipeNet PipeNet, bool Handled = false);

    [ByRefEvent] private record struct RemovePipeNetMethodEvent
        (EntityUid Grid, PipeNet PipeNet, bool Handled = false);

    [ByRefEvent] private record struct AddAtmosDeviceMethodEvent
        (EntityUid Grid, AtmosDeviceComponent Device, bool Result = false, bool Handled = false);

    [ByRefEvent] private record struct RemoveAtmosDeviceMethodEvent
        (EntityUid Grid, AtmosDeviceComponent Device, bool Result = false, bool Handled = false);
}
