using JetBrains.Annotations;

namespace Content.Shared.Temperature.HeatContainer;

public static partial class HeatContainerHelpers
{
    /// <summary>
    /// Splits a <see cref="IHeatContainer"/> into two, modifying the original container
    /// to contain the remaining fraction of the original heat capacity and the same temperature.
    /// </summary>
    /// <param name="c">The <see cref="IHeatContainer"/> to split. This will be modified to contain the remaining heat capacity.</param>
    /// <param name="cSplit">A <see cref="IHeatContainer"/> that will be modified to contain
    /// the specified fraction of the original container's heat capacity and the same temperature. Any previous value will be overwritten.</param>
    /// <param name="fraction">The fraction of the heat capacity to move to the new container. Clamped between 0 and 1.</param>
    [PublicAPI]
    public static void SplitFrom<T1, T2>(ref T1 c, ref T2 cSplit, float fraction = 0.5f)
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
    /// Splits a <see cref="IHeatContainer"/> into two, modifying the original container
    /// to contain the remaining fraction of the original heat capacity and the same temperature,
    /// while discarding the rest.
    /// </summary>
    /// <param name="c">The <see cref="IHeatContainer"/> to split off. This will be modified to contain the remaining heat capacity.</param>
    /// <param name="fraction">The fraction of the heat capacity to remove from the original container. Clamped between 0 and 1.</param>
    /// <remarks>This discards the leftover fraction. Be very careful with using this as you may void heat unintentionally.</remarks>
    [PublicAPI]
    public static void SplitFrom<T>(ref T c, float fraction = 0.5f)
        where T : IHeatContainer
    {
        fraction = Math.Clamp(fraction, 0f, 1f);
        var newHeatCapacity = c.HeatCapacity * fraction;

        c.HeatCapacity -= newHeatCapacity;
    }

    /// <summary>
    /// Splits a source <see cref="IHeatContainer"/> into a specified number of equal parts.
    /// This means you will get N + 1 equal parts where N is the length of the given array.
    /// </summary>
    /// <param name="c">The input <see cref="IHeatContainer"/> to split. It will be modified such that it is equal to each entry in <paramref name="dividedArray"/>.</param>
    /// <param name="dividedArray">An array of <see cref="IHeatContainer"/>s equally split from the source.<see cref="IHeatContainer"/>.
    /// This will be written to.</param>
    [PublicAPI]
    public static void SplitFrom<T1, T2>(ref T1 c, T2[] dividedArray)
        where T1 : IHeatContainer
        where T2 : struct, IHeatContainer // if we allowed classes you'd just have an array reffing the same obj
    {
        var num = dividedArray.Length + 1;
        for (var i = 0; i < dividedArray.Length; i++)
        {
            dividedArray[i].Temperature = c.Temperature;
            dividedArray[i].HeatCapacity = c.HeatCapacity / num;
        }

        c.HeatCapacity /= num;
    }

    /// <summary>
    /// Splits a source <see cref="IHeatContainer"/> into a specified number of equal parts, keeping the source unmodified.
    /// This means you will get N equal parts where N is the length of the given array.
    /// </summary>
    /// <param name="c">The input <see cref="IHeatContainer"/> to split into equal parts. This container is not modified, so make sure to discard it to avoid breaking energy conservation.</param>
    /// <param name="dividedArray">An array of <see cref="IHeatContainer"/>s the source will be split into.
    /// This will be written to.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when attempting to divide the source container by zero.</exception>
    [PublicAPI]
    public static void SplitAndCopy<T1, T2>(ref T1 c, T2[] dividedArray)
        where T1 : IHeatContainer
        where T2 : struct, IHeatContainer // if we allowed classes you'd just have an array reffing the same obj
    {
        var num = dividedArray.Length;
        ArgumentOutOfRangeException.ThrowIfZero(num);
        for (var i = 0; i < num; i++)
        {
            dividedArray[i].Temperature = c.Temperature;
            dividedArray[i].HeatCapacity = c.HeatCapacity / num;
        }
    }
}
