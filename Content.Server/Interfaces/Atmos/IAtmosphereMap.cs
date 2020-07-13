using System.Collections.Generic;
using System.Linq;
using Content.Server.Atmos;
using Content.Shared.Atmos;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Map;

namespace Content.Server.Interfaces.Atmos
{
    /// <summary>
    /// The server-wide atmosphere manager. This encapsulates the atmospherics backend, which
    /// divides the world up into different atmospheres enclosed in their own rooms.
    /// </summary>
    public interface IAtmosphereMap
    {
        /// <summary>
        /// Get the atmosphere manager for a grid, by it's ID
        /// </summary>
        /// <remarks>
        /// Must throw an ArgumentException if the grid does not exist in the Map Manager.
        /// </remarks>
        /// <param name="grid">The ID of the grid in question</param>
        /// <returns>The atmosphere manager for the selected grid</returns>
        IGridAtmosphereManager GetGridAtmosphereManager(GridId grid);

        /// <summary>
        /// Get the atmosphere for a given transform.
        /// </summary>
        /// <remarks>
        /// This must be functionally equivalent to:
        /// <code>
        /// GetGridAtmosphereManager(position.GridID).GetAtmosphere(position.GridPosition);
        /// </code>
        /// This method is provided since looking up the atmosphere of an entity is a very
        /// common operation, and the implementation may wish to apply optimisations for this
        /// use-case.
        /// </remarks>
        /// <param name="position">The position at which to find the atmosphere</param>
        /// <returns>The atmosphere at the given point, or <code>null</code> in the vacuum of space</returns>
        IAtmosphere GetAtmosphere(ITransformComponent position);

        /// <summary>
        /// Update all the ship's atmospheres, both for reactions and updating modified rooms.
        /// </summary>
        /// <param name="frameTime">The time since the last update</param>
        void Update(float frameTime);
    }
}
