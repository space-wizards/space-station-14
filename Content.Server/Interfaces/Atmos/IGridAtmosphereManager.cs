using System;
using System.Collections.Generic;
using Content.Server.Atmos;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.Interfaces.Atmos
{
    /// <summary>
    /// Manages the various separate atmospheres inside a single grid.
    /// </summary>
    /// <remarks>
    /// Airlocks separate
    /// atmospheres (as the name implies) and thus the station will have many different
    /// atmospheres.
    ///
    /// Every cell in an atmosphere has a path to every other cell
    /// without passing through any solid walls, or passing through airlocks
    /// and similar objects which can block gas flow.
    /// </remarks>
    public interface IGridAtmosphereManager : IDisposable
    {
        /// <summary>
        /// Get the zone at a position on the grid or null
        /// </summary>
        /// <param name="indices">The position on the grid</param>
        /// <returns>The relevant zone, or <code>null</code> if there's no zone there.</returns>
        ZoneAtmosphere GetZone(MapIndices indices);

        /// <summary>
        /// Get the tile at a position on the grid or null
        /// </summary>
        /// <param name="indices">The position on the grid</param>
        /// <returns>The relevant tile, or <code>null</code> if there's no tile there.</returns>
        TileAtmosphere GetTile(MapIndices indices);

        /// <summary>
        /// Notify the atmosphere system that something at a given position may have changed.
        /// </summary>
        /// <param name="indices">Position</param>
        void Invalidate(MapIndices indices);

        /// <summary>
        ///     Gets the volume in liters for a number of cells.
        /// </summary>
        /// <param name="cellCount">Number of cells</param>
        /// <returns>Volume in liters</returns>
        float GetVolumeForCells(int cellCount);

        /// <summary>
        ///     Returns whether the tile is zone blocked (meaning it can separate two zones)
        /// </summary>
        /// <param name="indices">Position</param>
        /// <returns></returns>
        bool IsZoneBlocked(MapIndices indices);

        /// <summary>
        ///     Returns whether the tile is air blocked (meaning no air can flow through it)
        /// </summary>
        /// <param name="indices">Position</param>
        /// <returns></returns>
        bool IsAirBlocked(MapIndices indices);

        /// <summary>
        ///     Returns whether the tile is space.
        /// </summary>
        /// <param name="indices">Position</param>
        /// <returns></returns>
        bool IsSpace(MapIndices indices);

        /// <summary>
        ///     Returns a dictionary with adjacent zones to the specified indices.
        /// </summary>
        /// <param name="indices"></param>
        /// <returns></returns>
        Dictionary<Direction, ZoneAtmosphere> GetAdjacentZones(MapIndices indices);

        /// <summary>
        ///     Returns a dictionary with adjacent tiles to the specified indices.
        /// </summary>
        /// <param name="indices"></param>
        /// <returns></returns>
        Dictionary<Direction, TileAtmosphere> GetAdjacentTiles(MapIndices indices);

        void Update(float frameTime);
    }
}
