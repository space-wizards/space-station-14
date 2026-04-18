using JetBrains.Annotations;

namespace Content.Shared.Temperature.HeatContainer;

public static partial class HeatContainerHelpers
{
    /// <summary>
    /// Splits a <see cref="IHeatContainer"/> into two.
    /// </summary>
    /// <param name="c">The <see cref="IHeatContainer"/> to split. This will be modified to contain the remaining heat capacity.</param>
    /// <param name="cSplit">A <see cref="IHeatContainer"/> that will be modified to contain
    /// the specified fraction of the original container's heat capacity and the same temperature.</param>
    /// <param name="fraction">The fraction of the heat capacity to move to the new container. Clamped between 0 and 1.</param>
    [PublicAPI]
    public static void Split<T1, T2>(ref T1 c, ref T2 cSplit, float fraction = 0.5f)
        where T1 : IHeatContainer
        where T2 : IHeatContainer
    {
        fraction = Math.Clamp(fraction, 0f, 1f);
        var newHeatCapacity = c.HeatCapacity * fraction;

        cSplit.HeatCapacity = newHeatCapacity;
        cSplit.Temperature = c.Temperature;

        c.HeatCapacity -= newHeatCapacity;
    }

    /// <summary>
    /// Splits a <see cref="IHeatContainer"/> into two,
    /// modifying the original container to contain the specified fraction of the original heat capacity and the same temperature.
    /// </summary>
    /// <param name="c">A <see cref="IHeatContainer"/> that will be modified to contain
    /// the specified fraction of the original container's heat capacity and the same temperature.</param>
    /// <param name="fraction">The fraction of the heat capacity to move to the new container. Clamped between 0 and 1.</param>
    /// <remarks>This discards the leftover fraction. Be very careful with using this as you may void heat unintentionally.</remarks>
    [PublicAPI]
    public static void Split<T>(ref T c, float fraction = 0.5f)
        where T : IHeatContainer
    {
        fraction = Math.Clamp(fraction, 0f, 1f);
        var newHeatCapacity = c.HeatCapacity * fraction;
        c.HeatCapacity = newHeatCapacity;
    }

    /// <summary>
    /// Divides a source <see cref="IHeatContainer"/> into a specified number of equal parts.
    /// </summary>
    /// <param name="c">The input <see cref="IHeatContainer"/> to split.</param>
    /// <param name="dividedArray">An array of <see cref="IHeatContainer"/>s equally split from the source <see cref="IHeatContainer"/>.
    /// This will be written to. This must be the same length as num.</param>
    /// <param name="num">The number of <see cref="IHeatContainer"/>s
    /// to split the source <see cref="IHeatContainer"/> into.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when attempting to divide the source container by zero.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the length of the divided array does not match the specified number of divisions.</exception>
    [PublicAPI]
    public static void Divide<T>(this T c, T[] dividedArray, int num)
        where T : struct, IHeatContainer // if we allowed classes you'd just have an array reffing the same obj
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(num);
        ArgumentOutOfRangeException.ThrowIfNotEqual(dividedArray.Length, num);

        var fraction = 1f / num;
        Split(ref c, fraction);

        for (var i = 0; i < num; i++)
        {
            dividedArray[i] = c;
        }
    }
}
