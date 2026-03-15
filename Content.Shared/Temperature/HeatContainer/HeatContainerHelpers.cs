using JetBrains.Annotations;

namespace Content.Shared.Temperature.HeatContainer;

/// <summary>
/// Class containing helper methods for working with <see cref="IHeatContainer"/>s.
/// Use these classes instead of implementing your own heat transfer logic.
/// </summary>
public static partial class HeatContainerHelpers
{
    /// <summary>
    /// Adds or removes heat energy from the <see cref="IHeatContainer"/>.
    /// Positive values add heat, negative values remove heat.
    /// The temperature can never become lower than 0K even if more heat is removed.
    /// </summary>
    /// <param name="c">The <see cref="IHeatContainer"/> to add or remove energy.</param>
    /// <param name="dQ">The energy in joules to add or remove.</param>
    [PublicAPI]
    public static void AddHeat<T>(ref T c, float dQ) where T : IHeatContainer
    {
        c.Temperature = AddHeatQuery(ref c, dQ);
    }

    /// <summary>
    /// Calculates the resulting temperature of the container after adding or removing heat energy.
    /// Positive values add heat, negative values remove heat. This method doesn't change the container's state.
    /// The temperature can never become lower than 0K even if more heat is removed.
    /// </summary>
    /// <param name="c">The <see cref="IHeatContainer"/> to query.</param>
    /// <param name="dQ">The energy in joules to add or remove.</param>
    /// <returns>The resulting temperature in kelvin after the heat change.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the heat capacity of the container is zero or negative.</exception>
    [PublicAPI]
    public static float AddHeatQuery<T>(ref T c, float dQ) where T : IHeatContainer
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(c.HeatCapacity);
        // Don't allow the temperature to go below the absolute minimum.
        return Math.Max(0f, c.Temperature + dQ / c.HeatCapacity);
    }

    /// <summary>
    /// Sets the heat capacity of a <see cref="IHeatContainer"/> without altering its thermal energy.
    /// Adjusts the temperature accordingly to maintain the same internal energy.
    /// </summary>
    /// <param name="c">The <see cref="IHeatContainer"/> to modify.</param>
    /// <param name="newHeatCapacity">The new heat capacity to set.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the new heat capacity is zero or negative.</exception>
    [PublicAPI]
    public static void SetHeatCapacity<T>(ref T c, float newHeatCapacity) where T : IHeatContainer
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(c.HeatCapacity);
        var currentEnergy = c.InternalEnergy;
        c.HeatCapacity = newHeatCapacity;
        c.Temperature = currentEnergy / c.HeatCapacity;
    }
}
