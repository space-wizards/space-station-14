using System.Collections.Generic;
using Content.Server.Atmos;
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

    /// <summary>
    /// Manages the various separate atmospheres inside a single grid.
    /// </summary>
    /// <remarks>
    /// Airlocks separate
    /// atmospheres (as the name implies) and thus the station will have many different
    /// atmospheres.
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
        /// <param name="coordinates">The position on the grid</param>
        /// <returns>The relevant atmosphere, or <code>null</code> if this cell
        /// is connected to space</returns>
        IAtmosphere GetAtmosphere(GridCoordinates coordinates);

        /// <summary>
        /// Notify the atmosphere system that something at a given position may have changed.
        /// </summary>
        /// <param name="coordinates"></param>
        void Invalidate(GridCoordinates coordinates);
    }

    /// <summary>
    /// Represents a single 'region' of the station, inside which air can mix freely.
    /// </summary>
    /// <remarks>
    /// Every cell in a region has a path to every other cell
    /// without passing through any solid walls, or passing through airlocks
    /// and similar objects which can block gas flow.
    /// </remarks>
    public interface IAtmosphere
    {
        /// <summary>
        /// All the gasses contained in this atmosphere
        /// </summary>
        IEnumerable<GasProperty> Gasses { get; }

        /// <summary>
        /// The volume of the room enclosed by this atmosphere, in cubic meters
        /// </summary>
        float Volume { get; }

        /// <summary>
        /// The total pressure of this room, in kilopascals
        /// </summary>
        /// <remarks>
        /// Governed by the ideal gas law
        /// </remarks>
        float Pressure { get; }

        /// <summary>
        /// The combined quantity of all gasses in this room, in mols
        /// </summary>
        /// <remarks>
        /// This function must be constant time with respect to the contents of the atmosphere.
        /// </remarks>
        float Quantity { get; }

        /// <summary>
        /// The mass of the contents of this atmosphere
        /// </summary>
        float Mass { get; }

        /// <summary>
        /// The temperature of this atmosphere, in degrees kelvin.
        /// </summary>
        float Temperature { get; set; }

        /// <summary>
        /// Under the ideal gas law, this returns a coefficent to convert a quantity (in
        /// mols) into a pressure (in kilopascals).
        /// </summary>
        /// <remarks>
        /// <code>PV=nRT</code>
        /// For any given gas in this atmosphere:
        /// <code>partialPressure = gasVolume * atmos.PressureRatio</code>
        /// This function <b>must</b> be constant time with respect to the number of gasses present, as
        /// it will be called extremely frequently. The use of a backing field is encouraged.
        /// </remarks>
        float PressureRatio { get; }

        /// <summary>
        /// Gets the quantity of a given gas
        /// </summary>
        /// <param name="gas">The type of gas to look for</param>
        /// <returns>The quantity of the gas in mols</returns>
        float QuantityOf(Gas gas);

        /// <summary>
        /// The partial pressure of a specific gas
        /// </summary>
        /// <param name="gas">The gas in question</param>
        /// <returns>The partial pressure in kilopascals</returns>
        float PartialPressureOf(Gas gas);

        /// <summary>
        /// Adds a quantity of gas to this room
        /// </summary>
        /// <remarks>
        /// This attempts to add the specified quantity of gas to the room. The
        /// amount successfully added is returned.
        ///
        /// It is legal to pass a negative quantity, which will be removed from the atmosphere. The
        /// return value will be the change in quantity, thus a negative value.
        /// </remarks>
        /// <param name="gas">The type of gas to add</param>
        /// <param name="quantity">The quantity, in mols, to add</param>
        /// <param name="temperature">The temperature of the incoming gas, in kelvin</param>
        /// <returns>The quantity of gas actually added</returns>
        float Add(Gas gas, float quantity, float temperature);

        /// <summary>
        /// Sets the quantity of a type of gas in the atmosphere.
        /// </summary>
        /// <remarks>
        /// Some gas containers may not be able to hold more than a certain amount of gas, and thus
        /// may return a value lower than the passed volume.
        ///
        /// <paramref name="volume"/> must not be negative.
        /// </remarks>
        /// <param name="gas">The type of gas to affect</param>
        /// <param name="volume">The desired volume</param>
        /// <returns>The actual volume</returns>
        float SetQuantity(Gas gas, float quantity);
    }
}
