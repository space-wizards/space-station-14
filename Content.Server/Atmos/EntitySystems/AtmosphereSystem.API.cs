using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using JetBrains.Annotations;

namespace Content.Server.Atmos.EntitySystems;

public partial class AtmosphereSystem
{
    /*
     General API for interacting with AtmosphereSystem.

     If you feel like you're stepping on eggshells because you can't access things in AtmosphereSystem,
     consider adding a method here instead of making your own way to work around it.
     */

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
    /// Return speedup factor for pumped or flow-based devices that depend on MaxTransferRate.
    /// </summary>
    [PublicAPI]
    public float PumpSpeedup()
    {
        return Speedup;
    }
}
