using System.Runtime.CompilerServices;
using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping.Components;
using Robust.Shared.Map.Components;

namespace Content.Server.Atmos.EntitySystems;

public partial class AtmosphereSystem
{
    /*
    Partial class that stores miscellaneous utility methods for Atmospherics.
    */

    /// <summary>
    /// <para>Marks a tile's visual overlay as needing to be redetermined.</para>
    ///
    /// <para>A tile's overlay (how it looks like, ex. water vapor's texture)
    /// is determined via determining how much gas there is on the tile.
    /// This is expensive to do for every tile/gas that may have a custom overlay,
    /// so its done once and only updated when it needs to be updated.</para>
    /// </summary>
    /// <param name="ent">The grid the tile is on.</param>
    /// <param name="tile">The tile to invalidate.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void InvalidateVisuals(
        Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent,
        TileAtmosphere tile)
    {
        _gasTileOverlaySystem.Invalidate((ent.Owner, ent.Comp2), tile.GridIndices);
    }

    /// <summary>
    /// Gets the volume in liters for a number of tiles, on a specific grid.
    /// </summary>
    /// <param name="mapGrid">The grid in question.</param>
    /// <param name="tiles">The amount of tiles.</param>
    /// <returns>The volume in liters that the tiles occupy.</returns>
    private float GetVolumeForTiles(MapGridComponent mapGrid, int tiles = 1)
    {
        return Atmospherics.CellVolume * mapGrid.TileSize * tiles;
    }

    /// <summary>
    /// Updates the <see cref="AirtightData"/> for a <see cref="TileAtmosphere"/>
    /// immediately.
    /// </summary>
    /// <remarks>This method is extremely important if you are doing something in Atmospherics
    /// that is time-sensitive! <see cref="AirtightData"/> is cached and invalidated on
    /// a cycle, so airtight changes performed during or after an invalidation will
    /// not take effect until the next Atmospherics tick!</remarks>
    /// <param name="uid">The entity the grid is on.</param>
    /// <param name="atmos">The <see cref="GridAtmosphereComponent"/> the tile is on.</param>
    /// <param name="grid">The <see cref="MapGridComponent"/> the tile is on.</param>
    /// <param name="tile">The <see cref="TileAtmosphere"/> to update.</param>
    private void UpdateAirtightData(EntityUid uid, GridAtmosphereComponent atmos, MapGridComponent grid, TileAtmosphere tile)
    {
        var oldBlocked = tile.AirtightData.BlockedDirections;

        tile.AirtightData = tile.NoGridTile
            ? default
            : GetAirtightData(uid, grid, tile.GridIndices);

        if (tile.AirtightData.BlockedDirections != oldBlocked && tile.ExcitedGroup != null)
            ExcitedGroupDispose(atmos, tile.ExcitedGroup);
    }

    /// <summary>
    /// Retrieves current <see cref="AirtightData"/> for a tile on a grid.
    /// This is determined on-the-fly, not from cached data, so it will reflect
    /// changes done in the current Atmospherics tick.
    /// </summary>
    /// <param name="uid">The entity the grid is on.</param>
    /// <param name="grid">The <see cref="MapGridComponent"/> the tile is on.</param>
    /// <param name="tile">The indices of the tile.</param>
    /// <returns>The current <see cref="AirtightData"/> for the tile.</returns>
    private AirtightData GetAirtightData(EntityUid uid, MapGridComponent grid, Vector2i tile)
    {
        var blockedDirs = AtmosDirection.Invalid;
        var noAirWhenBlocked = false;
        var fixVacuum = false;

        foreach (var ent in _map.GetAnchoredEntities(uid, grid, tile))
        {
            if (!_airtightQuery.TryGetComponent(ent, out var airtight))
                continue;

            fixVacuum |= airtight.FixVacuum;

            if (!airtight.AirBlocked)
                continue;

            blockedDirs |= airtight.AirBlockedDirection;
            noAirWhenBlocked |= airtight.NoAirWhenFullyAirBlocked;

            if (blockedDirs == AtmosDirection.All && noAirWhenBlocked && fixVacuum)
                break;
        }

        return new AirtightData(blockedDirs, noAirWhenBlocked, fixVacuum);
    }

    /// <summary>
    /// Pries a tile in a grid.
    /// </summary>
    /// <param name="mapGrid">The grid in question.</param>
    /// <param name="tile">The indices of the tile.</param>
    private void PryTile(Entity<MapGridComponent> mapGrid, Vector2i tile)
    {
        if (!_mapSystem.TryGetTileRef(mapGrid.Owner, mapGrid.Comp, tile, out var tileRef))
            return;

        _tile.PryTile(tileRef);
    }

    /// <summary>
    /// Notifies all subscribing entities on a particular tile that the tile has changed.
    /// Atmos devices may store references to tiles, so this is used to properly resync devices
    /// after a significant atmos change on that tile, for example a tile getting a new <see cref="GasMixture"/>.
    /// </summary>
    /// <param name="ent">The grid atmosphere entity.</param>
    /// <param name="tile">The tile to check for devices on.</param>
    private void NotifyDeviceTileChanged(Entity<GridAtmosphereComponent, MapGridComponent> ent, Vector2i tile)
    {
        var inTile = _mapSystem.GetAnchoredEntities(ent.Owner, ent.Comp2, tile);
        var ev = new AtmosDeviceTileChangedEvent();
        foreach (var uid in inTile)
        {
            RaiseLocalEvent(uid, ref ev);
        }
    }
}
