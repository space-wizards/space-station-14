using JetBrains.Annotations;

namespace Content.Shared.Temperature.HeatContainer;

public static partial class HeatContainerHelpers
{
    /// <summary>
    /// Splits a <see cref="IHeatContainer"/> into two.
    /// </summary>
    /// <param name="c">The <see cref="IHeatContainer"/> to split. This will be modified to contain the remaining heat capacity.</param>
    /// <param name="fraction">The fraction of the heat capacity to move to the new container. Clamped between 0 and 1.</param>
    /// <returns>A new <see cref="IHeatContainer"/> containing the specified fraction of the original container's heat capacity and the same temperature.</returns>
    [PublicAPI]
    public static T Split<T>(ref T c, float fraction = 0.5f) where T : IHeatContainer, new()
    {
        fraction = Math.Clamp(fraction, 0f, 1f);
        var newHeatCapacity = c.HeatCapacity * fraction;

        var newContainer = new T
        {
            HeatCapacity = newHeatCapacity,
            Temperature = c.Temperature,
        };

        c.HeatCapacity -= newHeatCapacity;

        return newContainer;
    }

    /// <summary>
    /// Divides a source <see cref="IHeatContainer"/> into a specified number of equal parts.
    /// </summary>
    /// <param name="c">The input <see cref="IHeatContainer"/> to split.</param>
    /// <param name="num">The number of <see cref="IHeatContainer"/>s
    /// to split the source <see cref="IHeatContainer"/> into.</param>
    /// <exception cref="ArgumentException">Thrown when attempting to divide the source container by zero.</exception>
    /// <returns>An array of <see cref="IHeatContainer"/>s equally split from the source <see cref="IHeatContainer"/>.</returns>
    [PublicAPI]
    public static T[] Divide<T>(this T c, uint num) where T : IHeatContainer, new()
    {
        ArgumentOutOfRangeException.ThrowIfZero(num);

        var fraction = 1f / num;
        var cFrac = Split(ref c, fraction);
        var containers = new T[num];

        for (var i = 0; i < num; i++)
        {
            containers[i] = cFrac;
        }

        return containers;
    }
}
