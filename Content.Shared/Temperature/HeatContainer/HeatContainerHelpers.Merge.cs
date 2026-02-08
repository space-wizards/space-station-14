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
    public static void Merge<T>(ref T cA, ref T cB) where T : IHeatContainer
    {
        var combinedHeatCapacity = cA.HeatCapacity + cB.HeatCapacity;
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(combinedHeatCapacity);

        var temp = (cA.InternalEnergy + cB.InternalEnergy) / combinedHeatCapacity;
        cA.HeatCapacity = combinedHeatCapacity;
        cA.Temperature = temp;
    }


    /// <summary>
    /// Merges an array of <see cref="IHeatContainer"/>s into a single heat container, conserving total internal energy.
    /// </summary>
    /// <param name="cA">The first <see cref="IHeatContainer"/> to merge.
    /// This will be modified to contain the merged result.</param>
    /// <param name="cN">The array of <see cref="IHeatContainer"/>s to merge.</param>
    /// <param name="temp">A temporary <see cref="IHeatContainer"/> used to perform the merge.</param>
    [PublicAPI]
    public static void Merge<T>(ref T cA, T[] cN, ref T temp) where T : IHeatContainer
    {
        // merge the first array and then merge the result with cA to avoid alloc
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
    public static void Merge<T>(this T[] cN, ref T result) where T : IHeatContainer
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
}
