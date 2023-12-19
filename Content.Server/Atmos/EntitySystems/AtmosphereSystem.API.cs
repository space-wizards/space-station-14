using System.Linq;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Reactions;
using Content.Server.NodeContainer.NodeGroups;
using Content.Shared.Atmos;
using Robust.Server.GameObjects;
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

    public bool HasAtmosphere(EntityUid gridUid)
    {
        var ev = new HasAtmosphereMethodEvent(gridUid);
        RaiseLocalEvent(gridUid, ref ev);

        return ev.Result;
    }

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

    public void InvalidateTile(EntityUid gridUid, Vector2i tile)
    {
        var ev = new InvalidateTileMethodEvent(gridUid, tile);
        RaiseLocalEvent(gridUid, ref ev);
    }

    public GasMixture?[]? GetTileMixtures(EntityUid? gridUid, EntityUid? mapUid, List<Vector2i> tiles, bool excite = false)
    {
        var ev = new GetTileMixturesMethodEvent(gridUid, mapUid, tiles, excite);

        // If we've been passed a grid, try to let it handle it.
        if (gridUid.HasValue)
        {
            DebugTools.Assert(_mapManager.IsGrid(gridUid.Value));
            RaiseLocalEvent(gridUid.Value, ref ev, false);
        }

        if (ev.Handled)
            return ev.Mixtures;

        // We either don't have a grid, or the event wasn't handled.
        // Let the map handle it instead, and also broadcast the event.
        if (mapUid.HasValue)
        {
            DebugTools.Assert(_mapManager.IsMap(mapUid.Value));
            RaiseLocalEvent(mapUid.Value, ref ev, true);
        }
        else
            RaiseLocalEvent(ref ev);

        if (ev.Handled)
            return ev.Mixtures;

        // Default to a space mixture... This is a space game, after all!
        ev.Mixtures ??= new GasMixture?[tiles.Count];
        for (var i = 0; i < tiles.Count; i++)
        {
            ev.Mixtures[i] ??= GasMixture.SpaceGas;
        }
        return ev.Mixtures;
    }

    public GasMixture? GetTileMixture (Entity<TransformComponent?> entity, MapGridComponent? grid = null, bool excite = false)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return null;

        var indices = _transformSystem.GetGridTilePositionOrDefault(entity);
        return GetTileMixture(entity.Comp.GridUid, entity.Comp.MapUid, indices, excite);
    }

    public GasMixture? GetTileMixture(EntityUid? gridUid, EntityUid? mapUid, Vector2i gridTile, bool excite = false)
    {
        var ev = new GetTileMixtureMethodEvent(gridUid, mapUid, gridTile, excite);

        // If we've been passed a grid, try to let it handle it.
        if(gridUid.HasValue)
        {
            DebugTools.Assert(_mapManager.IsGrid(gridUid.Value));
            RaiseLocalEvent(gridUid.Value, ref ev, false);
        }

        if (ev.Handled)
            return ev.Mixture;

        // We either don't have a grid, or the event wasn't handled.
        // Let the map handle it instead, and also broadcast the event.
        if(mapUid.HasValue)
        {
            DebugTools.Assert(_mapManager.IsMap(mapUid.Value));
            RaiseLocalEvent(mapUid.Value, ref ev, true);
        }
        else
            RaiseLocalEvent(ref ev);

        // Default to a space mixture... This is a space game, after all!
        return ev.Mixture ?? GasMixture.SpaceGas;
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
        var ev = new IsTileAirBlockedMethodEvent(gridUid, tile, directions, mapGridComp);
        RaiseLocalEvent(gridUid, ref ev);

        // If nothing handled the event, it'll default to true.
        return ev.Result;
    }

    public bool IsTileSpace(EntityUid? gridUid, EntityUid? mapUid, Vector2i tile, MapGridComponent? mapGridComp = null)
    {
        var ev = new IsTileSpaceMethodEvent(gridUid, mapUid, tile, mapGridComp);

        // Try to let the grid (if any) handle it...
        if (gridUid.HasValue)
            RaiseLocalEvent(gridUid.Value, ref ev, false);

        // If we didn't have a grid or the event wasn't handled
        // we let the map know, and also broadcast the event while at it!
        if (mapUid.HasValue && !ev.Handled)
            RaiseLocalEvent(mapUid.Value, ref ev, true);

        // We didn't have a map, and the event isn't handled, therefore broadcast the event.
        else if (!mapUid.HasValue && !ev.Handled)
            RaiseLocalEvent(ref ev);

        // If nothing handled the event, it'll default to true.
        // Oh well, this is a space game after all, deal with it!
        return ev.Result;
    }

    public bool IsTileMixtureProbablySafe(EntityUid? gridUid, EntityUid mapUid, Vector2i tile)
    {
        return IsMixtureProbablySafe(GetTileMixture(gridUid, mapUid, tile));
    }

    public float GetTileHeatCapacity(EntityUid? gridUid, EntityUid mapUid, Vector2i tile)
    {
        return GetHeatCapacity(GetTileMixture(gridUid, mapUid, tile) ?? GasMixture.SpaceGas);
    }

    public IEnumerable<Vector2i> GetAdjacentTiles(EntityUid gridUid, Vector2i tile)
    {
        var ev = new GetAdjacentTilesMethodEvent(gridUid, tile);
        RaiseLocalEvent(gridUid, ref ev);

        return ev.Result ?? Enumerable.Empty<Vector2i>();
    }

    public IEnumerable<GasMixture> GetAdjacentTileMixtures(EntityUid gridUid, Vector2i tile, bool includeBlocked = false, bool excite = false)
    {
        var ev = new GetAdjacentTileMixturesMethodEvent(gridUid, tile, includeBlocked, excite);
        RaiseLocalEvent(gridUid, ref ev);

        return ev.Result ?? Enumerable.Empty<GasMixture>();
    }

    public void UpdateAdjacent(EntityUid gridUid, Vector2i tile, MapGridComponent? mapGridComp = null)
    {
        var ev = new UpdateAdjacentMethodEvent(gridUid, tile, mapGridComp);
        RaiseLocalEvent(gridUid, ref ev);
    }

    public void HotspotExpose(EntityUid gridUid, Vector2i tile, float exposedTemperature, float exposedVolume,
        EntityUid? sparkSourceUid = null, bool soh = false)
    {
        var ev = new HotspotExposeMethodEvent(gridUid, sparkSourceUid, tile, exposedTemperature, exposedVolume, soh);
        RaiseLocalEvent(gridUid, ref ev);
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

    public void FixTileVacuum(EntityUid gridUid, Vector2i tile)
    {
        var ev = new FixTileVacuumMethodEvent(gridUid, tile);
        RaiseLocalEvent(gridUid, ref ev);
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

    [ByRefEvent] private record struct HasAtmosphereMethodEvent
        (EntityUid Grid, bool Result = false, bool Handled = false);

    [ByRefEvent] private record struct SetSimulatedGridMethodEvent
        (EntityUid Grid, bool Simulated, bool Handled = false);

    [ByRefEvent] private record struct IsSimulatedGridMethodEvent
        (EntityUid Grid, bool Simulated = false, bool Handled = false);

    [ByRefEvent] private record struct GetAllMixturesMethodEvent
        (EntityUid Grid, bool Excite = false, IEnumerable<GasMixture>? Mixtures = null, bool Handled = false);

    [ByRefEvent] private record struct InvalidateTileMethodEvent
        (EntityUid Grid, Vector2i Tile, bool Handled = false);

    [ByRefEvent] private record struct GetTileMixturesMethodEvent
        (EntityUid? GridUid, EntityUid? MapUid, List<Vector2i> Tiles, bool Excite = false, GasMixture?[]? Mixtures = null, bool Handled = false);

    [ByRefEvent] private record struct GetTileMixtureMethodEvent
        (EntityUid? GridUid, EntityUid? MapUid, Vector2i Tile, bool Excite = false, GasMixture? Mixture = null, bool Handled = false);

    [ByRefEvent] private record struct ReactTileMethodEvent
        (EntityUid GridId, Vector2i Tile, ReactionResult Result = default, bool Handled = false);

    [ByRefEvent] private record struct IsTileAirBlockedMethodEvent
        (EntityUid Grid, Vector2i Tile, AtmosDirection Direction = AtmosDirection.All, MapGridComponent? MapGridComponent = null, bool Result = false, bool Handled = false)
    {
        /// <summary>
        ///     True if one of the enabled blockers has <see cref="AirtightComponent.NoAirWhenFullyAirBlocked"/>. Note
        ///     that this does not actually check if all directions are blocked.
        /// </summary>
        public bool NoAir = false;
    }

    [ByRefEvent] private record struct IsTileSpaceMethodEvent
        (EntityUid? Grid, EntityUid? Map, Vector2i Tile, MapGridComponent? MapGridComponent = null, bool Result = true, bool Handled = false);

    [ByRefEvent] private record struct GetAdjacentTilesMethodEvent
        (EntityUid Grid, Vector2i Tile, IEnumerable<Vector2i>? Result = null, bool Handled = false);

    [ByRefEvent] private record struct GetAdjacentTileMixturesMethodEvent
        (EntityUid Grid, Vector2i Tile, bool IncludeBlocked, bool Excite,
            IEnumerable<GasMixture>? Result = null, bool Handled = false);

    [ByRefEvent] private record struct UpdateAdjacentMethodEvent
        (EntityUid Grid, Vector2i Tile, MapGridComponent? MapGridComponent = null, bool Handled = false);

    [ByRefEvent] private record struct HotspotExposeMethodEvent
        (EntityUid Grid, EntityUid? SparkSourceUid, Vector2i Tile, float ExposedTemperature, float ExposedVolume, bool soh, bool Handled = false);

    [ByRefEvent] private record struct HotspotExtinguishMethodEvent
        (EntityUid Grid, Vector2i Tile, bool Handled = false);

    [ByRefEvent] private record struct IsHotspotActiveMethodEvent
        (EntityUid Grid, Vector2i Tile, bool Result = false, bool Handled = false);

    [ByRefEvent] private record struct FixTileVacuumMethodEvent
        (EntityUid Grid, Vector2i Tile, bool Handled = false);

    [ByRefEvent] private record struct AddPipeNetMethodEvent
        (EntityUid Grid, PipeNet PipeNet, bool Handled = false);

    [ByRefEvent] private record struct RemovePipeNetMethodEvent
        (EntityUid Grid, PipeNet PipeNet, bool Handled = false);

    [ByRefEvent] private record struct AddAtmosDeviceMethodEvent
        (EntityUid Grid, AtmosDeviceComponent Device, bool Result = false, bool Handled = false);

    [ByRefEvent] private record struct RemoveAtmosDeviceMethodEvent
        (EntityUid Grid, AtmosDeviceComponent Device, bool Result = false, bool Handled = false);
}
