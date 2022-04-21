using System.Runtime.CompilerServices;
using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using Content.Shared.Maps;
using Robust.Shared.Map;

namespace Content.Server.Atmos.EntitySystems;

public partial class AtmosphereSystem
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void InvalidateVisuals(GridId grid, Vector2i tile)
    {
        _gasTileOverlaySystem.Invalidate(grid, tile);
    }

    public bool NeedsVacuumFixing(IMapGrid mapGrid, Vector2i indices)
    {
        var value = false;

        foreach (var airtightComponent in GetObstructingComponents(mapGrid, indices))
        {
            value |= airtightComponent.FixVacuum;
        }

        return value;
    }

    /// <summary>
    ///     Gets the volume in liters for a number of tiles, on a specific grid.
    /// </summary>
    /// <param name="mapGrid">The grid in question.</param>
    /// <param name="tiles">The amount of tiles.</param>
    /// <returns>The volume in liters that the tiles occupy.</returns>
    private float GetVolumeForTiles(IMapGrid mapGrid, int tiles = 1)
    {
        return Atmospherics.CellVolume * mapGrid.TileSize * tiles;
    }

    /// <summary>
    ///     Gets all obstructing AirtightComponent instances in a specific tile.
    /// </summary>
    /// <param name="mapGrid">The grid where to get the tile.</param>
    /// <param name="tile">The indices of the tile.</param>
    /// <returns></returns>
    public IEnumerable<AirtightComponent> GetObstructingComponents(IMapGrid mapGrid, Vector2i tile)
    {
        var airQuery = GetEntityQuery<AirtightComponent>();
        var enumerator = mapGrid.GetAnchoredEntitiesEnumerator(tile);

        while (enumerator.MoveNext(out var uid))
        {
            if (!airQuery.TryGetComponent(uid.Value, out var airtight)) continue;
            yield return airtight;
        }
    }

    public AtmosObstructionEnumerator GetObstructingComponentsEnumerator(IMapGrid mapGrid, Vector2i tile)
    {
        var ancEnumerator = mapGrid.GetAnchoredEntitiesEnumerator(tile);
        var airQuery = GetEntityQuery<AirtightComponent>();

        var enumerator = new AtmosObstructionEnumerator(ancEnumerator, airQuery);
        return enumerator;
    }

    private AtmosDirection GetBlockedDirections(IMapGrid mapGrid, Vector2i indices)
    {
        var value = AtmosDirection.Invalid;

        foreach (var airtightComponent in GetObstructingComponents(mapGrid, indices))
        {
            if(airtightComponent.AirBlocked)
                value |= airtightComponent.AirBlockedDirection;
        }

        return value;
    }

    /// <summary>
    ///     Pries a tile in a grid.
    /// </summary>
    /// <param name="mapGrid">The grid in question.</param>
    /// <param name="tile">The indices of the tile.</param>
    private void PryTile(IMapGrid mapGrid, Vector2i tile)
    {
        if (!mapGrid.TryGetTileRef(tile, out var tileRef))
            return;

        tileRef.PryTile(_mapManager, _tileDefinitionManager, EntityManager, _robustRandom);
    }
}
