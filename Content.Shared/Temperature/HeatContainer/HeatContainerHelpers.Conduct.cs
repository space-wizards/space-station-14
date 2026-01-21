using JetBrains.Annotations;

namespace Content.Shared.Temperature.HeatContainer;

public static partial class HeatContainerHelpers
{
    /// <summary>
    /// Conducts heat between a <see cref="HeatContainer"/> and some body with a different temperature,
    /// given some constant thermal conductance g and a small time delta.
    /// </summary>
    /// <param name="c">The <see cref="HeatContainer"/> to conduct heat to.</param>
    /// <param name="temp">The temperature of the second object that we are conducting heat with, in kelvin.</param>
    /// <param name="deltaTime">
    /// The amount of time that the heat is allowed to conduct, in seconds.
    /// This value should be small such that deltaTime &lt;&lt; C / g where C is the heat capacity of the container.
    /// If you need to simulate a larger time step split it into several smaller ones.
    /// </param>
    /// <param name="g">The thermal conductance in watt per kelvin. This describes how well heat flows between the bodies.</param>
    /// <returns>The amount of heat in joules that was added to the heat container.</returns>
    /// <example>A positive value indicates heat transfer from a hot body to a cold heat container c.</example>
    /// <remarks>
    /// This performs a single step using the Euler method for solving the Fourier heat equation
    /// \frac{dQ}{dt} = g \Delta T.
    /// If we need more precision in the future consider using a higher order integration scheme.
    /// If we need support for larger time steps in the future consider adding a method to split the time delta into several
    /// integration steps with adaptive step size.
    /// </remarks>
    [PublicAPI]
    public static float ConductHeat(this ref HeatContainer c, float temp, float deltaTime, float g)
    {
        var dQ = c.ConductHeatQuery(temp, deltaTime, g);
        c.AddHeat(dQ);
        return dQ;
    }

    /// <summary>
    /// Conducts heat between two <see cref="HeatContainer"/>s,
    /// given some constant thermal conductance g and a small time delta.
    /// </summary>
    /// <param name="cA">The first <see cref="HeatContainer"/> to conduct heat to.</param>
    /// <param name="cB">The second <see cref="HeatContainer"/> to conduct heat to.</param>
    /// <param name="deltaTime">
    /// The amount of time that the heat is allowed to conduct, in seconds.
    /// This value should be small such that deltaTime &lt;&lt; C / g where C is the heat capacity of the containers.
    /// If you need to simulate a larger time step split it into several smaller ones.
    /// </param>
    /// <param name="g">The thermal conductance in watt per kelvin. This describes how well heat flows between the bodies.</param>
    /// <returns>The amount of heat in joules that is exchanged between the bodies.</returns>
    /// <example>A positive value indicates heat transfer from a hot cB to a cold cA.</example>
    /// <remarks>
    /// This performs a single step using the Euler method for solving the Fourier heat equation
    /// \frac{dQ}{dt} = g \Delta T.
    /// If we need more precision in the future consider using a higher order integration scheme.
    /// If we need support for larger time steps in the future consider adding a method to split the time delta into several
    /// integration steps with adaptive step size.
    /// </remarks>
    [PublicAPI]
    public static float ConductHeat(this ref HeatContainer cA, ref HeatContainer cB, float deltaTime, float g)
    {
        var dQ = ConductHeatQuery(ref cA, cB.Temperature, deltaTime, g);
        cA.AddHeat(dQ);
        cB.AddHeat(-dQ);
        return dQ;
    }

    /// <summary>
    /// Calculates the amount of heat that would be conducted between a <see cref="HeatContainer"/> and some body with a different temperature,
    /// given some constant thermal conductance g and a small time delta.
    /// </summary>
    /// <param name="c">The <see cref="HeatContainer"/> to conduct heat to.</param>
    /// <param name="temp">The temperature of the second object that we are conducting heat with, in kelvin.</param>
    /// <param name="deltaTime">
    /// The amount of time that the heat is allowed to conduct, in seconds.
    /// This value should be small such that deltaTime &lt;&lt; C / g where C is the heat capacity of the container.
    /// If you need to simulate a larger time step split it into several smaller ones.
    /// </param>
    /// <param name="g">The thermal conductance in watt per kelvin. This describes how well heat flows between the bodies.</param>
    /// <returns>The amount of heat in joules that would be exchanged between the bodies.</returns>
    /// <example>A positive value indicates heat transfer from a hot body to a cold heat container c.</example>
    /// <remarks>
    /// This performs a single step using the Euler method for solving the Fourier heat equation
    /// \frac{dQ}{dt} = g \Delta T.
    /// If we need more precision in the future consider using a higher order integration scheme.
    /// If we need support for larger time steps in the future consider adding a method to split the time delta into several
    /// integration steps with adaptive step size.
    /// </remarks>
    [PublicAPI]
    public static float ConductHeatQuery(this ref HeatContainer c, float temp, float deltaTime, float g)
    {
        var dQ = g * (temp - c.Temperature) * deltaTime;
        var dQMax = Math.Abs(ConductHeatToTempQuery(ref c, temp));

        // Clamp the transferred heat amount in case we are overshooting the equilibrium temperature because our time step was too large.
        return Math.Clamp(dQ, -dQMax, dQMax);
    }

    /// <summary>
    /// Calculates the amount of heat that would be conducted between two <see cref="HeatContainer"/>s,
    /// given some conductivity constant k and a time delta. Does not modify the containers.
    /// </summary>
    /// <param name="c1">The first <see cref="HeatContainer"/> to conduct heat to.</param>
    /// <param name="c2">The second <see cref="HeatContainer"/> to conduct heat to.</param>
    /// <param name="deltaTime">
    /// The amount of time that the heat is allowed to conduct, in seconds.
    /// This value should be small such that deltaTime &lt;&lt; C / g where C is the heat capacity of the container.
    /// If you need to simulate a larger time step split it into several smaller ones.
    /// </param>
    /// <param name="g">The thermal conductance in watt per kelvin. This describes how well heat flows between the bodies.</param>
    /// <returns>The amount of heat in joules that would be exchanged between the bodies.</returns>
    /// <example>A positive value indicates heat transfer from a hot c2 to a cold c1.</example>
    /// <remarks>
    /// This performs a single step using the Euler method for solving the Fourier heat equation
    /// \frac{dQ}{dt} = g \Delta T.
    /// If we need more precision in the future consider using a higher order integration scheme.
    /// If we need support for larger time steps in the future consider adding a method to split the time delta into several
    /// integration steps with adaptive step size.
    /// </remarks>
    [PublicAPI]
    public static float ConductHeatQuery(this ref HeatContainer c1, ref HeatContainer c2, float deltaTime, float g)
    {
        return ConductHeatQuery(ref c1, c2.Temperature, deltaTime, g);
    }

    /// <summary>
    /// Changes the temperature of a <see cref="HeatContainer"/> to a target temperature by
    /// adding or removing the necessary amount of heat.
    /// </summary>
    /// <param name="c">The <see cref="HeatContainer"/> to change the temperature of.</param>
    /// <param name="targetTemp">The desired temperature to reach.</param>
    /// <returns>The amount of heat in joules that was transferred to or from the <see cref="HeatContainer"/>
    /// to reach the target temperature.</returns>
    /// <example>A positive value indicates heat must be added to the container to reach the target temperature.</example>
    [PublicAPI]
    public static float ConductHeatToTemp(this ref HeatContainer c, float targetTemp)
    {
        var dQ = ConductHeatToTempQuery(ref c, targetTemp);
        c.Temperature = targetTemp;
        return dQ;
    }

    /// <summary>
    /// Determines the amount of heat that must be transferred to or from a <see cref="HeatContainer"/>
    /// to reach a target temperature. Does not modify the heat container.
    /// </summary>
    /// <param name="c">The <see cref="HeatContainer"/> to query.</param>
    /// <param name="targetTemp">The desired temperature to reach.</param>
    /// <returns>The amount of heat in joules that must be transferred to or from the <see cref="HeatContainer"/>
    /// to reach the target temperature.</returns>
    /// <example>A positive value indicates heat must be added to the container to reach the target temperature.</example>
    [PublicAPI]
    public static float ConductHeatToTempQuery(this ref HeatContainer c, float targetTemp)
    {
        return (targetTemp - c.Temperature) * c.HeatCapacity;
    }
}
