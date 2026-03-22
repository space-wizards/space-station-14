using JetBrains.Annotations;

namespace Content.Shared.Temperature.HeatContainers;

public static partial class HeatContainerHelpers
{
    /// <summary>
    /// Merges two heat containers into one, conserving total internal energy.
    /// </summary>
    /// <param name="cA">The first <see cref="HeatContainer"/> to merge. This will be modified to contain the merged result.</param>
    /// <param name="cB">The second <see cref="HeatContainer"/> to merge.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the combined heat capacity of both containers is zero or negative.</exception>
    [PublicAPI]
    public static void Merge(this ref HeatContainer cA, HeatContainer cB)
    {
        var combinedHeatCapacity = cA.HeatCapacity + cB.HeatCapacity;
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(combinedHeatCapacity);
        var merged = new HeatContainer
        {
            HeatCapacity = combinedHeatCapacity,
            Temperature = (cA.InternalEnergy + cB.InternalEnergy) / combinedHeatCapacity,
        };

        cA = merged;
    }


    /// <summary>
    /// Merges an array of <see cref="HeatContainer"/>s into a single heat container, conserving total internal energy.
    /// </summary>
    /// <param name="cA">The first <see cref="HeatContainer"/> to merge.
    /// This will be modified to contain the merged result.</param>
    /// <param name="cN">The array of <see cref="HeatContainer"/>s to merge.</param>
    [PublicAPI]
    public static void Merge(this ref HeatContainer cA, HeatContainer[] cN)
    {
        var cAcN = new HeatContainer[cN.Length + 1];
        cAcN[0] = cA;
        cN.CopyTo(cAcN, 1);

        cA = cAcN.Merge();
    }

    /// <summary>
    /// Merges an array of <see cref="HeatContainer"/>s into a single heat container, conserving total internal energy.
    /// </summary>
    /// <param name="cN">The array of <see cref="HeatContainer"/>s to merge.</param>
    /// <returns>A new <see cref="HeatContainer"/> representing the merged result.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the combined heat capacity of all containers is zero or negative.</exception>
    [PublicAPI]
    public static HeatContainer Merge(this HeatContainer[] cN)
    {
        var totalHeatCapacity = 0f;
        var totalEnergy = 0f;

        foreach (var c in cN)
        {
            totalHeatCapacity += c.HeatCapacity;
            totalEnergy += c.InternalEnergy;
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(totalHeatCapacity);

        var result = new HeatContainer
        {
            HeatCapacity = totalHeatCapacity,
            Temperature = totalEnergy / totalHeatCapacity,
        };

        return result;
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
