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
}
