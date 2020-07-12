using Robust.Shared.Map;

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
    public interface IGridAtmosphereManager
    {
        /// <summary>
        /// Get the atmosphere at a position on the grid
        /// </summary>
        /// <remarks>
        /// This operation may perform expensive tasks (such as finding the outline of
        /// the room), however repeat calls must be fast.
        /// </remarks>
        /// <param name="indices">The position on the grid</param>
        /// <returns>The relevant atmosphere, or <code>null</code> if this cell
        /// is connected to space</returns>
        IAtmosphere GetAtmosphere(MapIndices indices);

        /// <summary>
        /// Notify the atmosphere system that something at a given position may have changed.
        /// </summary>
        /// <param name="indices"></param>
        void Invalidate(MapIndices indices);
    }
}
