using JetBrains.Annotations;

namespace Content.Shared.Temperature.HeatContainer;

public static partial class HeatContainerHelpers
{
    /// <summary>
    /// Divides a <see cref="HeatContainer"/> into two.
    /// </summary>
    /// <param name="c">The <see cref="HeatContainer"/> to divide. This will be modified to contain the remaining heat capacity.</param>
    /// <param name="fraction">The fraction of the heat capacity to move to the new container. Clamped between 0 and 1.</param>
    /// <returns>A new <see cref="HeatContainer"/> containing the specified fraction of the original container's heat capacity and the same temperature.</returns>
    [PublicAPI]
    public static HeatContainer Divide(this HeatContainer c, float fraction)
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
}
