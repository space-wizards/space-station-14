using JetBrains.Annotations;

namespace Content.Shared.Temperature.HeatContainer;

public static partial class HeatContainerHelpers
{
    /// <summary>
    /// Conducts heat between a <see cref="HeatContainer"/> and some body with a different temperature,
    /// given some conductivity constant k and a time delta.
    /// </summary>
    /// <param name="c">The <see cref="HeatContainer"/> to conduct heat to.</param>
    /// <param name="temp">The temperature of the second object that we are conducting heat with, in Kelvin.</param>
    /// <param name="deltaTime">The amount of time that the heat is allowed to conduct, in seconds.</param>
    /// <param name="k">The conductivity constant. This describes how well heat flows between the bodies.</param>
    /// <returns>The amount of heat in joules that is exchanged between the bodies.</returns>
    /// <example>A positive value indicates heat transfer from a hot body to a cold c.</example>
    [PublicAPI]
    public static float ConductHeat(this HeatContainer c, float temp, float deltaTime, float k)
    {
        var dQ = c.ConductHeatQuery(temp, deltaTime, k);
        c.ChangeHeat(dQ);
        return dQ;
    }

    /// <summary>
    /// Conducts heat between a <see cref="HeatContainer"/> and another <see cref="HeatContainer"/>,
    /// given some conductivity constant k and a time delta.
    /// </summary>
    /// <param name="cA">The first <see cref="HeatContainer"/> to conduct heat to.</param>
    /// <param name="cB">The second <see cref="HeatContainer"/> to conduct heat to.</param>
    /// <param name="deltaTime">The amount of time that the heat is allowed to conduct, in seconds.</param>
    /// <param name="k">The conductivity constant. This describes how well heat flows between the bodies.</param>
    /// <returns>The amount of heat in joules that is exchanged between the bodies.</returns>
    /// <example>A positive value indicates heat transfer from a hot cB to a cold cA.</example>
    [PublicAPI]
    public static float ConductHeat(this HeatContainer cA, HeatContainer cB, float deltaTime, float k)
    {
        var dQ = ConductHeatQuery(cA, cB.Temperature, deltaTime, k);
        cA.ChangeHeat(dQ);
        cB.ChangeHeat(-dQ);
        return dQ;
    }

    /// <summary>
    /// Calculates the amount of heat that would be conducted from a <see cref="HeatContainer"/> to
    /// some body with a different temperature,
    /// given some conductivity constant k and a time delta. Does not modify the container.
    /// </summary>
    /// <param name="c">The <see cref="HeatContainer"/> to conduct heat to.</param>
    /// <param name="temp">The temperature of the second object that we are conducting heat with, in Kelvin..</param>
    /// <param name="deltaTime">The amount of time that the heat is allowed to conduct, in seconds.</param>
    /// <param name="k">The conductivity constant. This describes how well heat flows between the bodies.</param>
    /// <returns>The amount of heat in joules that would be exchanged between the bodies.</returns>
    /// <example>A positive value indicates heat transfer from a hot body to a cold c.</example>
    [PublicAPI]
    public static float ConductHeatQuery(this HeatContainer c, float temp, float deltaTime, float k)
    {
        var dQ = k * (temp - c.Temperature) * deltaTime;
        var q = ConductHeatToTempQuery(c, temp);

        return Math.Min(Math.Abs(dQ), Math.Abs(q)) * Math.Sign(q);
    }

    /// <summary>
    /// Calculates the amount of heat that would be conducted between two <see cref="HeatContainer"/>s,
    /// given some conductivity constant k and a time delta. Does not modify the containers.
    /// </summary>
    /// <param name="c1">The first <see cref="HeatContainer"/> to conduct heat to.</param>
    /// <param name="c2">The second <see cref="HeatContainer"/> to conduct heat to.</param>
    /// <param name="deltaTime">The amount of time that the heat is allowed to conduct, in seconds.</param>
    /// <param name="k">The conductivity constant. This describes how well heat flows between the bodies.</param>
    /// <returns>The amount of heat in joules that would be exchanged between the bodies.</returns>
    /// <example>A positive value indicates heat transfer from a hot c2 to a cold c1.</example>
    [PublicAPI]
    public static float ConductHeatQuery(this HeatContainer c1, HeatContainer c2, float deltaTime, float k)
    {
        return ConductHeatQuery(c1, c2.Temperature, deltaTime, k);
    }

    /// <summary>
    /// Changes the temperature of a <see cref="HeatContainer"/> to a target temperature by
    /// adding or removing the necessary amount of heat.
    /// </summary>
    /// <param name="c">The <see cref="HeatContainer"/> to change the temperature of.</param>
    /// <param name="targetTemp">The desired temperature to reach.</param>
    /// <returns>The amount of heat in joules that was transferred to or from the <see cref="HeatContainer"/>
    /// to reach the target temperature.</returns>
    /// <example>A positive value indicates heat must be added to reach the target temperature.</example>
    [PublicAPI]
    public static float ConductHeatToTemp(this HeatContainer c, float targetTemp)
    {
        var dQ = ConductHeatToTempQuery(c, targetTemp);
        c.Temperature = targetTemp;
        return dQ;
    }

    /// <summary>
    /// Determines the amount of heat that must be transferred to or from a <see cref="HeatContainer"/>
    /// to reach a target temperature.
    /// </summary>
    /// <param name="c">The <see cref="HeatContainer"/> to query.</param>
    /// <param name="targetTemp">The desired temperature to reach.</param>
    /// <returns>The amount of heat in joules that must be transferred to or from the <see cref="HeatContainer"/>
    /// to reach the target temperature.</returns>
    /// <example>A positive value indicates heat must be added to reach the target temperature.</example>
    [PublicAPI]
    public static float ConductHeatToTempQuery(this HeatContainer c, float targetTemp)
    {
        return (targetTemp - c.Temperature) * c.HeatCapacity;
    }
}
