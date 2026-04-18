using JetBrains.Annotations;

namespace Content.Shared.Temperature.HeatContainers;

public static partial class HeatContainerHelpers
{
    /// <summary>
    /// Merges two heat containers into one, conserving total internal energy.
    /// </summary>
    /// <param name="cA">The first <see cref="IHeatContainer"/> to merge. This will be modified to contain the merged result.</param>
    /// <param name="cB">The second <see cref="IHeatContainer"/> to merge.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the combined heat capacity of both containers is zero or negative.</exception>
    [PublicAPI]
    public static void Merge<T1, T2>(ref T1 cA, ref T2 cB)
        where T1 : IHeatContainer
        where T2 : IHeatContainer
    {
        var combinedHeatCapacity = cA.HeatCapacity + cB.HeatCapacity;
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(combinedHeatCapacity);

        cA.HeatCapacity = combinedHeatCapacity;
        cA.Temperature = (cA.InternalEnergy + cB.InternalEnergy) / combinedHeatCapacity;
    }


    /// <summary>
    /// Merges an array of <see cref="IHeatContainer"/>s into a single heat container, conserving total internal energy.
    /// </summary>
    /// <param name="cA">The first <see cref="IHeatContainer"/> to merge.
    /// This will be modified to contain the merged result.</param>
    /// <param name="cN">The array of <see cref="IHeatContainer"/>s to merge.</param>
    [PublicAPI]
    public static void Merge<T1, T2>(ref T1 cA, T2[] cN)
        where T1 : IHeatContainer
        where T2 : IHeatContainer
    {
        // merge the first array and then merge the result with cA to avoid alloc
        var temp = new HeatContainer();
        cN.Merge(ref temp);
        Merge(ref cA, ref temp);
    }

    /// <summary>
    /// Merges an array of <see cref="IHeatContainer"/>s into a single heat container, conserving total internal energy.
    /// </summary>
    /// <param name="cN">The array of <see cref="IHeatContainer"/>s to merge.</param>
    /// <param name="result">The modified <see cref="IHeatContainer"/> containing the merged result.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the combined heat capacity of all containers is zero or negative.</exception>
    [PublicAPI]
    public static void Merge<T1, T2>(this T1[] cN, ref T2 result)
        where T1 : IHeatContainer
        where T2 : IHeatContainer
    {
        var totalHeatCapacity = 0f;
        var totalEnergy = 0f;

        foreach (var c in cN)
        {
            totalHeatCapacity += c.HeatCapacity;
            totalEnergy += c.InternalEnergy;
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(totalHeatCapacity);

        result.HeatCapacity = totalHeatCapacity;
        result.Temperature = totalEnergy / totalHeatCapacity;
    }

    /// <summary>
    /// Determines the temperature at which two <see cref="HeatContainer"/>s equalize when combined into a single container.
    /// Order does not matter when using this method.
    /// </summary>
    /// <param name="cA">The first <see cref="HeatContainer"/>.</param>
    /// <param name="cB">The second <see cref="HeatContainer"/>.</param>
    /// <returns>The temperature at which these two heat containers are expected to equalize.</returns>
    [PublicAPI, Pure]
    public static float EqualizationTemperature(HeatContainer cA, HeatContainer cB)
    {
        return (cA.InternalEnergy + cB.InternalEnergy) / (cA.HeatCapacity + cA.HeatCapacity);
    }

    /// <summary>
    /// Determines the temperature at which a given number of <see cref="HeatContainer"/>s equalize when combined into a single container.
    /// Order does not matter when using this method.
    /// </summary>
    /// <param name="cN">An array of <see cref="HeatContainer"/>s we're equalizing the temperatures of.</param>
    [PublicAPI, Pure]
    public static float EqualizationTemperature(params HeatContainer[] cN)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(cN.Length, 2);
        var totalHeatCapacity = 0f;
        var totalEnergy = 0f;

        foreach (var c in cN)
        {
            totalHeatCapacity += c.HeatCapacity;
            totalEnergy += c.InternalEnergy;
        }

        return totalEnergy / totalHeatCapacity;
    }
}
