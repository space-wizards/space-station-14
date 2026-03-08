using JetBrains.Annotations;

namespace Content.Shared.Temperature.HeatContainer;

public static partial class HeatContainerHelpers
{
    /// <summary>
    /// Splits a <see cref="HeatContainer"/> into two.
    /// </summary>
    /// <param name="c">The <see cref="HeatContainer"/> to split. This will be modified to contain the remaining heat capacity.</param>
    /// <param name="fraction">The fraction of the heat capacity to move to the new container. Clamped between 0 and 1.</param>
    /// <returns>A new <see cref="HeatContainer"/> containing the specified fraction of the original container's heat capacity and the same temperature.</returns>
    [PublicAPI]
    public static HeatContainer Split(this ref HeatContainer c, float fraction = 0.5f)
    {
        fraction = Math.Clamp(fraction, 0f, 1f);
        var newHeatCapacity = c.HeatCapacity * fraction;

        var newContainer = new HeatContainer
        {
            HeatCapacity = newHeatCapacity,
            Temperature = c.Temperature,
        };

        c.HeatCapacity -= newHeatCapacity;

        return newContainer;
    }

    /// <summary>
    /// Divides a source <see cref="HeatContainer"/> into a specified number of equal parts.
    /// </summary>
    /// <param name="c">The input <see cref="HeatContainer"/> to split.</param>
    /// <param name="num">The number of <see cref="HeatContainer"/>s
    /// to split the source <see cref="HeatContainer"/> into.</param>
    /// <exception cref="ArgumentException">Thrown when attempting to divide the source container by zero.</exception>
    /// <returns>An array of <see cref="HeatContainer"/>s equally split from the source <see cref="HeatContainer"/>.</returns>
    [PublicAPI]
    public static HeatContainer[] Divide(this HeatContainer c, uint num)
    {
        ArgumentOutOfRangeException.ThrowIfZero(num);

        var fraction = 1f / num;
        var cFrac = c.Split(fraction);
        var containers = new HeatContainer[num];

        for (var i = 0; i < num; i++)
        {
            containers[i] = cFrac;
        }

        return containers;
    }
}
