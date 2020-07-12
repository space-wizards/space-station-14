using System.Collections.Generic;
using System.Linq;
using Content.Shared.Atmos;

namespace Content.Server.Interfaces.Atmos
{
    /// <summary>
    /// Represents a single gas container inside which air can mix freely.
    /// </summary>
    public interface IAtmosphere
    {
        /// <summary>The universal gas constant, in (cubic meters*pascals)/(kelvin*mols)</summary>
        /// <remarks>Note this is in pascals, NOT kilopascals - divide by 1000 to convert it</remarks>
        public const float R = 8.314462618f;

        /// <summary>
        /// All the gasses contained in this atmosphere
        /// </summary>
        IEnumerable<GasProperty> Gasses { get; }

        /// <summary>
        /// The volume enclosed by this atmosphere, in cubic meters
        /// </summary>
        float Volume { get; }

        /// <summary>
        /// The total pressure of this atmosphere, in kilopascals
        /// </summary>
        /// <remarks>
        /// Governed by the ideal gas law
        /// </remarks>
        float Pressure { get; }

        /// <summary>
        /// The combined quantity of all gasses in this atmosphere, in mols
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
        float QuantityOf(Gas gas)
        {
            return Gasses.Where(prop => prop.Gas == gas).Sum(prop => prop.Quantity);
        }

        /// <summary>
        /// The partial pressure of a specific gas
        /// </summary>
        /// <param name="gas">The gas in question</param>
        /// <returns>The partial pressure in kilopascals</returns>
        float PartialPressureOf(Gas gas)
        {
            return QuantityOf(gas) * PressureRatio;
        }

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

        /// <summary>
        /// Remove a given volume of gas from the atmosphere, getting the mixture removed.
        /// </summary>
        /// <remarks>
        /// This should not reduce the volume of the atmosphere.
        /// </remarks>
        /// <param name="volume">The volume of gas to remove.</param>
        /// <returns>A new <see cref="IAtmosphere"/> containing the removed gases.</returns>
        IAtmosphere Take(float volume);
    }
}
