using JetBrains.Annotations;

namespace Content.Shared.Temperature.HeatContainer;

public static partial class HeatContainerHelpers
{
    /// <summary>
    /// Merges two heat containers into one, conserving total internal energy.
    /// </summary>
    /// <param name="cA">The first <see cref="HeatContainer"/> to merge. This will be modified to contain the merged result.</param>
    /// <param name="cB">The second <see cref="HeatContainer"/> to merge.</param>
    [PublicAPI]
    public static void Merge(this ref HeatContainer cA, HeatContainer cB)
    {
        var merged = new HeatContainer
        {
            HeatCapacity = cA.HeatCapacity + cB.HeatCapacity,
            Temperature = (cA.InternalEnergy + cB.InternalEnergy) / (cA.HeatCapacity + cB.HeatCapacity)
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

        var result = new HeatContainer
        {
            HeatCapacity = totalHeatCapacity,
            Temperature = totalEnergy / totalHeatCapacity,
        };

        return result;
    }
}
