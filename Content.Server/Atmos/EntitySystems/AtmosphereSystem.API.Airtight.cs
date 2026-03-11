using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using JetBrains.Annotations;
using Robust.Shared.Map.Components;

namespace Content.Server.Atmos.EntitySystems;

public partial class AtmosphereSystem
{
    /// <summary>
    /// Checks if a tile on a grid is air-blocked in the specified directions.
    /// This only checks for if the current tile, and only the current tile, is blocking
    /// air.
    /// </summary>
    /// <param name="gridUid">The grid to check.</param>
    /// <param name="tile">The tile on the grid to check.</param>
    /// <param name="directions">The directions to check for air-blockage.</param>
    /// <param name="mapGridComp">Optional map grid component associated with the grid.</param>
    /// <returns>True if the tile is air-blocked in the specified directions, false otherwise.</returns>
    /// <remarks>This rebuilds airtight data on-the-fly. You should only use this if you've just
    /// invalidated airtight data, and you cannot wait one atmostick to revalidate it.</remarks>
    [PublicAPI]
    public bool IsTileAirBlocked(EntityUid gridUid,
        Vector2i tile,
        AtmosDirection directions = AtmosDirection.All,
        MapGridComponent? mapGridComp = null)
    {
        if (!Resolve(gridUid, ref mapGridComp, false))
            return false;

        var data = GetAirtightData(gridUid, mapGridComp, tile);
        return data.BlockedDirections.IsFlagSet(directions);
    }

    /// <summary>
    /// Checks if a tile on a grid is air-blocked in the specified directions, using cached data.
    /// This only checks for if the current tile, and only the current tile, is blocking
    /// air.
    /// </summary>
    /// <param name="grid">The grid to check.</param>
    /// <param name="tile">The tile on the grid to check.</param>
    /// <param name="directions">The directions to check for air-blockage.</param>
    /// <returns>True if the tile is air-blocked in the specified directions, false otherwise.</returns>
    /// <remarks>Returns data that is currently cached by Atmospherics.
    /// You should always use this method over <see cref="IsTileAirBlocked"/> as it's more performant.
    /// If you need to get up-to-date data because you've just invalidated airtight data,
    /// use <see cref="IsTileAirBlocked"/>.</remarks>
    [PublicAPI]
    public bool IsTileAirBlockedCached(Entity<GridAtmosphereComponent?> grid,
        Vector2i tile,
        AtmosDirection directions = AtmosDirection.All)
    {
        if (!_atmosQuery.Resolve(grid, ref grid.Comp, false))
            return false;

        if (!grid.Comp.Tiles.TryGetValue(tile, out var atmosTile))
            return false;

        return atmosTile.AirtightData.BlockedDirections.IsFlagSet(directions);
    }

    /// <summary>
    /// Returns the <see cref="TileAtmosphere.AdjacentBits"/> for a tile on a grid.
    /// This represents the directions that the air can currently flow to.
    /// </summary>
    /// <param name="grid">The grid entity that the tile belongs to.</param>
    /// <param name="tile">The <see cref="Vector2i"/> coordinates to check.</param>
    /// <returns>The <see cref="TileAtmosphere.AdjacentBits"/> of the tile,
    /// <see cref="AtmosDirection.Invalid"/> if the grid or tile couldn't be found.</returns>
    /// <remarks>Note that this data is cached and is updated at the beginning of every atmostick.
    /// As such, any airtight changes that were made may not be reflected in this value until
    /// the cache is refreshed in the next processing tick.</remarks>
    [PublicAPI]
    public AtmosDirection GetAirflowDirections(Entity<GridAtmosphereComponent?> grid, Vector2i tile)
    {
        if (!_atmosQuery.Resolve(grid, ref grid.Comp, false))
            return AtmosDirection.Invalid;

        if (!grid.Comp.Tiles.TryGetValue(tile, out var atmosTile))
            return AtmosDirection.Invalid;

        return atmosTile.AdjacentBits;
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
}
