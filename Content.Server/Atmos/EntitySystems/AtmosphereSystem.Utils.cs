using System.Runtime.CompilerServices;
using Content.Server.Atmos.Components;
using Content.Server.Maps;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Atmos.EntitySystems;

public partial class AtmosphereSystem
{
    /*
    Partial class that stores miscellaneous utility methods for Atmospherics.
    */

    /// <summary>
    /// Gets the particular price of a <see cref="GasMixture"/>.
    /// </summary>
    /// <param name="mixture">The <see cref="GasMixture"/> to get the price of.</param>
    /// <returns>The price of the gas mixture.</returns>
    public double GetPrice(GasMixture mixture)
    {
        float basePrice = 0; // moles of gas * price/mole
        float totalMoles = 0; // total number of moles in can
        float maxComponent = 0; // moles of the dominant gas
        for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
        {
            basePrice += mixture.Moles[i] * GetGas(i).PricePerMole;
            totalMoles += mixture.Moles[i];
            maxComponent = Math.Max(maxComponent, mixture.Moles[i]);
        }

        // Pay more for gas canisters that are purer
        float purity = 1;
        if (totalMoles > 0)
        {
            purity = maxComponent / totalMoles;
        }

        return basePrice * purity;
    }

    /// <summary>
    /// <para>Marks a tile's visual overlay as needing to be redetermined.</para>
    ///
    /// <para>A tile's overlay (how it looks like, ex. water vapor's texture)
    /// is determined via determining how much gas there is on the tile.
    /// This is expensive to do for every tile/gas that may have a custom overlay,
    /// so its done once and only updated when it needs to be updated.</para>
    /// </summary>
    /// <param name="grid">The grid the tile is on.</param>
    /// <param name="tile">The tile to invalidate.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void InvalidateVisuals(Entity<GasTileOverlayComponent?> grid, Vector2i tile)
    {
        _gasTileOverlaySystem.Invalidate(grid, tile);
    }

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
    /// Data on the airtightness of a <see cref="TileAtmosphere"/>.
    /// Cached on the <see cref="TileAtmosphere"/> and updated during
    /// <see cref="AtmosphereSystem.ProcessRevalidate"/> if it was invalidated.
    /// </summary>
    /// <param name="BlockedDirections">The current directions blocked on this tile.
    /// This is where air cannot flow to.</param>
    /// <param name="NoAirWhenBlocked">Whether the tile can have air when blocking directions.
    /// Common for entities like thin windows which only block one face but can still have air in the residing tile.</param>
    /// <param name="FixVacuum">If true, Atmospherics will generate air (yes, creating matter from nothing)
    /// using the adjacent tiles as a seed if the airtightness is removed and the tile has no air.
    /// This allows stuff like airlocks that void air when becoming airtight to keep opening/closing without
    /// draining a room by continuously voiding air.</param>
    public readonly record struct AirtightData(
        AtmosDirection BlockedDirections,
        bool NoAirWhenBlocked,
        bool FixVacuum);

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
}
