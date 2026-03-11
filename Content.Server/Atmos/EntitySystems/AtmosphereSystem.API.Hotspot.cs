using Content.Server.Atmos.Components;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Content.Server.Atmos.EntitySystems;

public partial class AtmosphereSystem
{
    /// <summary>
    /// Exposes a tile to a hotspot of given temperature and volume, igniting it if conditions are met.
    /// </summary>
    /// <param name="grid">The grid to expose the tile on.</param>
    /// <param name="tile">The tile to expose.</param>
    /// <param name="exposedTemperature">The temperature of the hotspot to expose.
    /// You can think of this as exposing a temperature of a flame.</param>
    /// <param name="exposedVolume">The volume of the hotspot to expose.
    /// You can think of this as how big the flame is initially.
    /// Bigger flames will ramp a fire faster.</param>
    /// <param name="soh">Whether to "boost" a fire that's currently on the tile already.
    /// Does nothing if the tile isn't already a hotspot.
    /// This clamps the temperature and volume of the hotspot to the maximum
    /// of the provided parameters and whatever's on the tile.</param>
    /// <param name="sparkSourceUid">Entity that started the exposure for admin logging.</param>
    [PublicAPI]
    public void HotspotExpose(Entity<GridAtmosphereComponent?> grid,
        Vector2i tile,
        float exposedTemperature,
        float exposedVolume,
        EntityUid? sparkSourceUid = null,
        bool soh = false)
    {
        if (!_atmosQuery.Resolve(grid, ref grid.Comp, false))
            return;

        if (grid.Comp.Tiles.TryGetValue(tile, out var atmosTile))
            HotspotExpose(grid.Comp, atmosTile, exposedTemperature, exposedVolume, soh, sparkSourceUid);
    }

    /// <summary>
    /// Exposes a tile to a hotspot of given temperature and volume, igniting it if conditions are met.
    /// </summary>
    /// <param name="tile">The <see cref="TileAtmosphere"/> to expose.</param>
    /// <param name="exposedTemperature">The temperature of the hotspot to expose.
    /// You can think of this as exposing a temperature of a flame.</param>
    /// <param name="exposedVolume">The volume of the hotspot to expose.
    /// You can think of this as how big the flame is initially.
    /// Bigger flames will ramp a fire faster.</param>
    /// <param name="soh">Whether to "boost" a fire that's currently on the tile already.
    /// Does nothing if the tile isn't already a hotspot.
    /// This clamps the temperature and volume of the hotspot to the maximum
    /// of the provided parameters and whatever's on the tile.</param>
    /// <param name="sparkSourceUid">Entity that started the exposure for admin logging.</param>
    [PublicAPI]
    public void HotspotExpose(TileAtmosphere tile,
        float exposedTemperature,
        float exposedVolume,
        EntityUid? sparkSourceUid = null,
        bool soh = false)
    {
        if (!_atmosQuery.TryGetComponent(tile.GridIndex, out var atmos))
            return;

        DebugTools.Assert(atmos.Tiles.TryGetValue(tile.GridIndices, out var tmp) && tmp == tile);
        HotspotExpose(atmos, tile, exposedTemperature, exposedVolume, soh, sparkSourceUid);
    }

    /// <summary>
    /// Extinguishes a hotspot on a tile.
    /// </summary>
    /// <param name="gridUid">The grid to extinguish the hotspot on.</param>
    /// <param name="tile">The tile on the grid to extinguish the hotspot on.</param>
    [PublicAPI]
    public void HotspotExtinguish(EntityUid gridUid, Vector2i tile)
    {
        var ev = new HotspotExtinguishMethodEvent(gridUid, tile);
        RaiseLocalEvent(gridUid, ref ev);
    }

    /// <summary>
    /// Checks if a hotspot is active on a tile.
    /// </summary>
    /// <param name="gridUid">The grid to check.</param>
    /// <param name="tile">The tile on the grid to check.</param>
    /// <returns>True if a hotspot is active on the tile, false otherwise.</returns>
    [PublicAPI]
    public bool IsHotspotActive(EntityUid gridUid, Vector2i tile)
    {
        var ev = new IsHotspotActiveMethodEvent(gridUid, tile);
        RaiseLocalEvent(gridUid, ref ev);

        // If not handled, this will be false. Just like in space!
        return ev.Result;
    }
}
