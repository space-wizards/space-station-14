using JetBrains.Annotations;

namespace Content.Shared.Temperature.HeatContainer;

/// <summary>
/// Class containing helper methods for working with <see cref="HeatContainer"/>s.
/// Use these classes instead of implementing your own heat transfer logic.
/// </summary>
public static partial class HeatContainerHelpers
{
    /// <summary>
    /// Adds or removes heat energy from the container.
    /// Positive values add heat, negative values remove heat.
    /// </summary>
    /// <param name="c">The <see cref="HeatContainer"/> to add or remove energy.</param>
    /// <param name="dQ">The energy in joules to remove.</param>
    [PublicAPI]
    public static void ChangeHeat(this HeatContainer c, float dQ)
    {
        c.Temperature = c.ChangeHeatQuery(dQ);
    }

    /// <summary>
    /// Calculates the resulting temperature of the container after adding or removing heat energy.
    /// Positive values add heat, negative values remove heat. This method doesn't change the container's state.
    /// </summary>
    /// <param name="c">The <see cref="HeatContainer"/> to query.</param>
    /// <param name="dQ">The energy in joules to add or remove.</param>
    /// <returns>The resulting temperature after the heat change.</returns>
    [PublicAPI]
    public static float ChangeHeatQuery(this HeatContainer c, float dQ)
    {
        return c.Temperature + dQ / c.HeatCapacity;
    }

    /// <summary>
    /// Changes the heat capacity of a <see cref="HeatContainer"/> without altering its thermal energy.
    /// Adjusts the temperature accordingly to maintain the same internal energy.
    /// </summary>
    /// <param name="c">The <see cref="HeatContainer"/> to modify.</param>
    /// <param name="newHeatCapacity">The new heat capacity to set.</param>
    [PublicAPI]
    public static void ChangeHeatCapacity(this HeatContainer c, float newHeatCapacity)
    {
        var currentEnergy = c.InternalEnergy;
        c.HeatCapacity = newHeatCapacity;
        c.Temperature = currentEnergy / c.HeatCapacity;
    }

    /// <summary>
    /// Changes the heat capacity of a <see cref="HeatContainer"/> while keeping its temperature unchanged.
    /// Adjusts the internal energy accordingly.
    /// </summary>
    /// <param name="c">The <see cref="HeatContainer"/> to modify.</param>
    /// <param name="newHeatCapacity">The new heat capacity to set.</param>
    /// <returns>The energy in joules added to (positive) or removed from (negative) the container.</returns>
    [PublicAPI]
    public static float ChangeHeatCapacityKeepTemperature(this HeatContainer c, float newHeatCapacity)
    {
        var currentTemperature = c.Temperature;
        c.HeatCapacity = newHeatCapacity;
        return c.ConductHeatToTemp(currentTemperature);
    }
}
