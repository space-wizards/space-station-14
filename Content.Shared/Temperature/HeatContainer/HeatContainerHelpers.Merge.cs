using JetBrains.Annotations;

namespace Content.Shared.Temperature.HeatContainer;

public static partial class HeatContainerHelpers
{
    /// <summary>
    /// Merges two heat containers into one, conserving total internal energy.
    /// </summary>
    /// <param name="cA">The first <see cref="IHeatContainer"/> to merge. This will be modified to contain the merged result.</param>
    /// <param name="cB">The second <see cref="IHeatContainer"/> to merge.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the combined heat capacity of both containers is zero or negative.</exception>
    [PublicAPI]
    public static void Merge<T>(ref T cA, T cB) where T : IHeatContainer, new()
    {
        var combinedHeatCapacity = cA.HeatCapacity + cB.HeatCapacity;
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(combinedHeatCapacity);
        var merged = new T
        {
            HeatCapacity = combinedHeatCapacity,
            Temperature = (cA.InternalEnergy + cB.InternalEnergy) / combinedHeatCapacity,
        };

        cA = merged;
    }


    /// <summary>
    /// Merges an array of <see cref="IHeatContainer"/>s into a single heat container, conserving total internal energy.
    /// </summary>
    /// <param name="cA">The first <see cref="IHeatContainer"/> to merge.
    /// This will be modified to contain the merged result.</param>
    /// <param name="cN">The array of <see cref="IHeatContainer"/>s to merge.</param>
    [PublicAPI]
    public static void Merge<T>(ref T cA, T[] cN) where T : IHeatContainer, new()
    {
        var cAcN = new T[cN.Length + 1];
        cAcN[0] = cA;
        cN.CopyTo(cAcN, 1);

        cA = cAcN.Merge();
    }

    /// <summary>
    /// Merges an array of <see cref="IHeatContainer"/>s into a single heat container, conserving total internal energy.
    /// </summary>
    /// <param name="cN">The array of <see cref="IHeatContainer"/>s to merge.</param>
    /// <returns>A new <see cref="IHeatContainer"/> representing the merged result.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the combined heat capacity of all containers is zero or negative.</exception>
    [PublicAPI]
    public static T Merge<T>(this T[] cN) where T : IHeatContainer, new()
    {
        var totalHeatCapacity = 0f;
        var totalEnergy = 0f;

        foreach (var c in cN)
        {
            totalHeatCapacity += c.HeatCapacity;
            totalEnergy += c.InternalEnergy;
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(totalHeatCapacity);

        var result = new T
        {
            HeatCapacity = totalHeatCapacity,
            Temperature = totalEnergy / totalHeatCapacity,
        };

        return result;
    }
}
