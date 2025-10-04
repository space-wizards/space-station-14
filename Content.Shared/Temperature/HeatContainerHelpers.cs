using JetBrains.Annotations;

namespace Content.Shared.Temperature;

/// <summary>
/// Class containing helper methods for working with <see cref="HeatContainer"/>s.
/// Use these classes instead of implementing your own heat transfer logic.
/// </summary>
public static class HeatContainerHelpers
{
    /// <summary>
    /// Adds or removes heat energy from the container.
    /// Positive values add heat, negative values remove heat.
    /// </summary>
    /// <param name="c">The <see cref="HeatContainer"/> to add or remove energy.</param>
    /// <param name="dQ">The energy in joules to remove.</param>
    [PublicAPI]
    public static void ChangeHeat(this HeatContainer c, float dQ)
    {
        c.Temperature = c.ChangeHeatQuery(dQ);
    }

    /// <summary>
    /// Calculates the resulting temperature of the container after adding or removing heat energy.
    /// Positive values add heat, negative values remove heat. This method doesn't change the container's state.
    /// </summary>
    /// <param name="c">The <see cref="HeatContainer"/> to query.</param>
    /// <param name="dQ">The energy in joules to add or remove.</param>
    /// <returns>The resulting temperature after the heat change.</returns>
    [PublicAPI]
    public static float ChangeHeatQuery(this HeatContainer c, float dQ)
    {
        return c.Temperature + dQ / c.HeatCapacity;
    }

    /// <summary>
    /// Determines the amount of heat energy that must be transferred between two containers
    /// to bring them to thermal equilibrium.
    /// </summary>
    /// <param name="cA">The first <see cref="HeatContainer"/> to exchange heat.</param>
    /// <param name="cB">The second <see cref="HeatContainer"/> to exchange heat with.</param>
    /// <returns>The amount of heat in joules that is needed
    /// to bring the containers to thermal equilibrium.</returns>
    /// <example>A positive value indicates heat transfer from a hot cA to a cold cB.</example>
    [PublicAPI]
    public static float FullyExchangeHeatQuery(this HeatContainer cA, HeatContainer cB)
    {
        /*
         The solution is derived from the following facts:
         1. Let Q be the amount of heat energy transferred from cA to cB.
         2. T_A > T_B, so heat will flow from cA to cB.
         3. The energy lost by T_A is equal to Q = C_A * (T_A_initial - T_A_final)
         4. The energy gained by T_B is equal to Q = C_B * (T_B_final - T_B_initial)
         5. Energy is conserved. So T_A_final and T_B_final can be expressed as:
            T_A_final = T_A_initial - Q / C_A
            T_B_final = T_B_initial + Q / C_B
         6. At thermal equilibrium, T_A_final = T_B_final.
         7. Solve for Q.
         */
        return (cB.Temperature - cA.Temperature) *
               ((cA.HeatCapacity * cB.HeatCapacity) / (cA.HeatCapacity + cB.HeatCapacity));
    }

    /// <summary>
    /// Brings two <see cref="HeatContainer"/>s to thermal equilibrium by exchanging heat.
    /// </summary>
    /// <param name="cA">The first <see cref="HeatContainer"/> to exchange heat.</param>
    /// <param name="cB">The second <see cref="HeatContainer"/> to exchange heat with.</param>
    /// <returns>The amount of heat in joules that is exchanged between the two containers.</returns>
    /// <example>A positive value indicates heat transfer from a hot cA to a cold cB.</example>
    [PublicAPI]
    public static float FullyExchangeHeat(this HeatContainer cA, HeatContainer cB)
    {
        var q = FullyExchangeHeatQuery(cA, cB);
        cA.ChangeHeat(q);
        cB.ChangeHeat(-q);
        return q;
    }

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
        return k * (temp - c.Temperature) * deltaTime;
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
    [PublicAPI]
    public static float ConductHeatToTemp(this HeatContainer c, float targetTemp)
    {
        c.Temperature = targetTemp;
        return ConductHeatToTempQuery(c, targetTemp);
    }

    /// <summary>
    /// Determines the amount of heat that must be transferred to or from a <see cref="HeatContainer"/>
    /// to reach a target temperature.
    /// </summary>
    /// <param name="c">The <see cref="HeatContainer"/> to query.</param>
    /// <param name="targetTemp">The desired temperature to reach.</param>
    /// <returns>The amount of heat in joules that must be transferred to or from the <see cref="HeatContainer"/>
    /// to reach the target temperature.</returns>
    [PublicAPI]
    public static float ConductHeatToTempQuery(this HeatContainer c, float targetTemp)
    {
        return (targetTemp - c.Temperature) * c.HeatCapacity;
    }

    /// <summary>
    /// Changes the heat capacity of a <see cref="HeatContainer"/> without altering its thermal energy.
    /// Adjusts the temperature accordingly to maintain the same internal energy.
    /// </summary>
    /// <param name="c">The <see cref="HeatContainer"/> to modify.</param>
    /// <param name="newHeatCapacity">The new heat capacity to set.</param>
    [PublicAPI]
    public static void ChangeHeatCapacity(this HeatContainer c, float newHeatCapacity)
    {
        var currentEnergy = c.InternalEnergy;
        c.HeatCapacity = newHeatCapacity;
        c.Temperature = currentEnergy / c.HeatCapacity;
    }
}
