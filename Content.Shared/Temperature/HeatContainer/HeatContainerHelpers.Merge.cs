using JetBrains.Annotations;

namespace Content.Shared.Temperature.HeatContainer;

public static partial class HeatContainerHelpers
{
    /// <summary>
    /// Merges one heat container into another.
    /// </summary>
    /// <param name="cA">The first <see cref="IHeatContainer"/> to merge. This will be modified to contain the merged result.</param>
    /// <param name="cB">The second <see cref="IHeatContainer"/> to merge. This will remain unmodified.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the combined heat capacity of both containers is zero or negative.</exception>
    [PublicAPI]
    public static void MergeInto<T1, T2>(ref T1 cA, ref T2 cB)
        where T1 : IHeatContainer
        where T2 : IHeatContainer
    {
        var combinedHeatCapacity = cA.HeatCapacity + cB.HeatCapacity;
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(combinedHeatCapacity);

        cA.Temperature = (cA.InternalEnergy + cB.InternalEnergy) / combinedHeatCapacity;
        cA.HeatCapacity = combinedHeatCapacity;
    }


    /// <summary>
    /// Merges an array of <see cref="IHeatContainer"/>s into a single heat container.
    /// This means you combine N+1 containers into 1.
    /// </summary>
    /// <param name="cA">The first <see cref="IHeatContainer"/> to merge. This will be modified to contain the merged result.</param>
    /// <param name="cN">The array of <see cref="IHeatContainer"/>s to merge. These will remain unmodified.</param>
    [PublicAPI]
    public static void MergeInto<T1, T2>(ref T1 cA, T2[] cN)
        where T1 : IHeatContainer
        where T2 : IHeatContainer
    {
        var totalEnergy = cA.InternalEnergy;
        var totalHeatCapacity = cA.HeatCapacity;
        for (var i = 0; i < cN.Length; i++)
        {
            totalEnergy += cN[i].InternalEnergy;
            totalHeatCapacity += cN[i].HeatCapacity;
        }
        cA.Temperature = totalEnergy / totalHeatCapacity;
        cA.HeatCapacity = totalHeatCapacity;
    }

    /// <summary>
    /// Merges an array of <see cref="IHeatContainer"/>s into a single new output heat container.
    /// This means you combine N containers into 1.
    /// </summary>
    /// <param name="cA">The <see cref="IHeatContainer"/> to write the result to.</param>
    /// <param name="cN">The array of <see cref="IHeatContainer"/>s to merge. These will remain unmodified.</param>
    [PublicAPI]
    public static void MergeAndCopy<T1, T2>(ref T1 cA, T2[] cN)
        where T1 : IHeatContainer
        where T2 : IHeatContainer
    {
        var totalEnergy = 0f;
        var totalHeatCapacity = 0f;
        for (var i = 0; i < cN.Length; i++)
        {
            totalEnergy += cN[i].InternalEnergy;
            totalHeatCapacity += cN[i].HeatCapacity;
        }
        cA.Temperature = totalEnergy / totalHeatCapacity;
        cA.HeatCapacity = totalHeatCapacity;
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
