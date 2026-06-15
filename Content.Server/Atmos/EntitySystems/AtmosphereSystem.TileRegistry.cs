using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Maps;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Server.Atmos.EntitySystems;

// Registry of TileAtmosphere objects: creation, grid/map/space classification, and disconnected-tile trim.
public sealed partial class AtmosphereSystem
{
    /// <summary>
    /// Returns the <see cref="TileAtmosphere"/> at <paramref name="index"/>, creating and registering one if absent.
    /// </summary>
    /// <param name="grid">The grid whose tile registry to look up.</param>
    /// <param name="index">Tile coordinates on the grid.</param>
    /// <param name="invalidateNew">When a tile is newly created, queue it for revalidation. False to skip, e.g. during a full rebuild that revalidates everything anyway.</param>
    /// <returns>The existing or newly created tile.</returns>
    private TileAtmosphere GetOrNewTile(Entity<GridAtmosphereComponent> grid, Vector2i index, bool invalidateNew = true)
    {
        var tile = grid.Comp.Tiles.GetOrNew(index, out var existing);
        if (existing)
            return tile;

        if (invalidateNew)
            grid.Comp.InvalidatedCoords.Add(index);

        tile.GridIndex = grid.Owner;
        tile.GridIndices = index;
        return tile;
    }

    /// <summary>
    /// Marks a tile for the disconnected-tile trim pass, skipping it if it's already queued.
    /// </summary>
    private static void QueuePossiblyDisconnectedTile(GridAtmosphereComponent atmos, TileAtmosphere tile)
    {
        if (tile.TrimQueued)
            return;

        tile.TrimQueued = true;
        atmos.PossiblyDisconnectedTiles.Add(tile);
    }

    /// <summary>
    /// This method queues a tile and all of its neighbours up for processing by <see cref="TrimDisconnectedMapTiles"/>.
    /// </summary>
    public static void QueueTileTrim(GridAtmosphereComponent atmos, TileAtmosphere tile)
    {
        QueuePossiblyDisconnectedTile(atmos, tile);

        for (var i = 0; i < Atmospherics.Directions; i++)
        {
            var direction = (AtmosDirection) (1 << i);
            var indices = tile.GridIndices.Offset(direction);
            if (atmos.Tiles.TryGetValue(indices, out var adj)
                && adj.NoGridTile
                && !adj.TrimQueued)
            {
                QueuePossiblyDisconnectedTile(atmos, adj);
            }
        }
    }

    /// <summary>
    /// Tiles in a <see cref="GridAtmosphereComponent"/> are either grid-tiles, or tiles
    /// adjacent to grid-tiles that represent the map's atmosphere. This method trims any map-tiles that are no longer
    /// adjacent to any grid-tiles.
    /// </summary>
    private void TrimDisconnectedMapTiles(
        Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent)
    {
        var atmos = ent.Comp1;

        foreach (var tile in atmos.PossiblyDisconnectedTiles)
        {
            tile.TrimQueued = false;
            if (!tile.NoGridTile)
                continue;

            var connected = false;
            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var indices = tile.GridIndices.Offset((AtmosDirection) (1 << i));
                if (_map.TryGetTile(ent.Comp3, indices, out var gridTile) && !gridTile.IsEmpty)
                {
                    connected = true;
                    break;
                }
            }

            if (!connected)
            {
                RemoveActiveTile(atmos, tile);
                atmos.Tiles.Remove(tile.GridIndices);
            }
        }

        atmos.PossiblyDisconnectedTiles.Clear();
    }

    /// <summary>
    /// Checks whether a tile has a corresponding grid-tile, or whether it is a "map" tile. Also checks whether the
    /// tile should be considered "space"
    /// </summary>
    private void UpdateTileData(
        Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent,
        MapAtmosphereComponent? mapAtmos,
        TileAtmosphere tile)
    {
        var idx = tile.GridIndices;
        bool mapAtmosphere;
        if (_map.TryGetTile(ent.Comp3, idx, out var gTile) && !gTile.IsEmpty)
        {
            var contentDef = (ContentTileDefinition) _tileDefinitionManager[gTile.TypeId];
            mapAtmosphere = contentDef.MapAtmosphere;
            tile.ThermalConductivity = contentDef.ThermalConductivity;
            tile.HeatCapacity = contentDef.HeatCapacity;
            tile.NoGridTile = false;
        }
        else
        {
            mapAtmosphere = true;
            tile.ThermalConductivity =  0.5f;
            tile.HeatCapacity = float.PositiveInfinity;

            if (!tile.NoGridTile)
            {
                tile.NoGridTile = true;

                // This tile just became a non-grid atmos tile.
                // It, or one of its neighbours, might now be completely disconnected from the grid.
                QueueTileTrim(ent.Comp1, tile);
            }
        }

        UpdateAirtightData(ent.Owner, ent.Comp1, ent.Comp3, tile);

        if (mapAtmosphere)
        {
            if (!tile.MapAtmosphere)
            {
                (tile.Air, tile.Space) = GetDefaultMapAtmosphere(mapAtmos);
                tile.MapAtmosphere = true;
                ent.Comp1.MapTiles.Add(tile);
            }

            DebugTools.AssertNotNull(tile.Air);
            DebugTools.Assert(tile.Air?.Immutable ?? false);
            return;
        }

        if (!tile.MapAtmosphere)
            return;

        // Tile used to be exposed to the map's atmosphere, but isn't anymore.
        RemoveMapAtmos(ent.Comp1, tile);
    }

    /// <summary>
    /// Detaches a tile from the map atmosphere, clearing its immutable map air and archived state.
    /// </summary>
    private static void RemoveMapAtmos(GridAtmosphereComponent atmos, TileAtmosphere tile)
    {
        DebugTools.Assert(tile.MapAtmosphere);
        DebugTools.AssertNotNull(tile.Air);
        DebugTools.Assert(tile.Air?.Immutable ?? false);
        tile.MapAtmosphere = false;
        atmos.MapTiles.Remove(tile);
        tile.Air = null;
        tile.AirArchived = null;
        tile.ArchivedCycle = 0;
        tile.LastShare = 0f;
        tile.Space = false;
    }

    /// <summary>
    /// Ensures a grid-tile's air mixture matches its airtight state: removes air from fully-blocked
    /// tiles with <see cref="AirtightData.NoAirWhenBlocked"/> set, and assigns a fresh mixture to
    /// unblocked tiles that don't have one yet.
    /// </summary>
    private void UpdateTileAir(
        Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent,
        TileAtmosphere tile,
        float volume)
    {
        if (tile.MapAtmosphere)
        {
            DebugTools.AssertNotNull(tile.Air);
            DebugTools.Assert(tile.Air?.Immutable ?? false);
            return;
        }

        var data = tile.AirtightData;
        var fullyBlocked = data.BlockedDirections == AtmosDirection.All;

        if (fullyBlocked && data.NoAirWhenBlocked)
        {
            if (tile.Air == null)
                return;

            tile.Air = null;
            tile.AirArchived = null;
            tile.ArchivedCycle = 0;
            tile.LastShare = 0f;
            tile.Hotspot = new Hotspot();
            NotifyDeviceTileChanged((ent.Owner, ent.Comp1, ent.Comp3), tile.GridIndices);
            return;
        }

        if (tile.Air != null)
            return;

        tile.Air = new GasMixture(volume){Temperature = Atmospherics.T20C};

        if (data.FixVacuum)
            GridFixTileVacuum(tile);

        // Since we assigned the tile a new GasMixture we need to tell any devices
        // on this tile that the reference has changed.
        NotifyDeviceTileChanged((ent.Owner, ent.Comp1, ent.Comp3), tile.GridIndices);
    }
}
