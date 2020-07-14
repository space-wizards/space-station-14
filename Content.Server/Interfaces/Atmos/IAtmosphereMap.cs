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
        /// Update all the ship's atmospheres, both for reactions and updating modified rooms.
        /// </summary>
        /// <param name="frameTime">The time since the last update</param>
        void Update(float frameTime);
    }
}
