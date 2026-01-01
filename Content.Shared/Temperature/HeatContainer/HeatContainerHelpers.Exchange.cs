using JetBrains.Annotations;

namespace Content.Shared.Temperature.HeatContainer;

public static partial class HeatContainerHelpers
{
    #region 2-Body Exchange

    /// <summary>
    /// Determines the amount of heat energy that must be transferred between two heat containers
    /// to bring them into thermal equilibrium.
    /// Does not modify the containers.
    /// </summary>
    /// <param name="cA">The first <see cref="HeatContainer"/> to exchange heat.</param>
    /// <param name="cB">The second <see cref="HeatContainer"/> to exchange heat with.</param>
    /// <returns>The amount of heat in joules that is needed
    /// to bring the containers to thermal equilibrium.</returns>
    /// <example>A positive value indicates heat transfer from a hot cA to a cold cB.</example>
    [PublicAPI]
    public static float EquilibriumHeatQuery(this ref HeatContainer cA, ref HeatContainer cB)
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
        return (cA.Temperature - cB.Temperature) *
               (cA.HeatCapacity * cB.HeatCapacity / (cA.HeatCapacity + cB.HeatCapacity));
    }

    /// <summary>
    /// Determines the resulting temperature if two heat containers are brought into thermal equilibrium.
    /// Does not modify the containers.
    /// </summary>
    /// <param name="cA">The first <see cref="HeatContainer"/> to exchange heat.</param>
    /// <param name="cB">The second <see cref="HeatContainer"/> to exchange heat with.</param>
    /// <returns>The resulting equilibrium temperature both containers will be at.</returns>
    [PublicAPI]
    public static float EquilibriumTemperatureQuery(this ref HeatContainer cA, ref HeatContainer cB)
    {
        // Insert the above solution for Q into T_A_final = T_A_initial - Q / C_A and rearrange the result.
        return (cA.HeatCapacity * cA.Temperature - cB.HeatCapacity * cB.Temperature) / (cA.HeatCapacity + cB.HeatCapacity);
    }

    /// <summary>
    /// Brings two <see cref="HeatContainer"/>s into thermal equilibrium by exchanging heat.
    /// </summary>
    /// <param name="cA">The first <see cref="HeatContainer"/> to exchange heat.</param>
    /// <param name="cB">The second <see cref="HeatContainer"/> to exchange heat with.</param>
    [PublicAPI]
    public static void Equilibrate(this ref HeatContainer cA, ref HeatContainer cB)
    {
        var tFinal = EquilibriumTemperatureQuery(ref cA, ref cB);
        cA.Temperature = tFinal;
        cB.Temperature = tFinal;
    }

    /// <summary>
    /// Brings two <see cref="HeatContainer"/>s into thermal equilibrium by exchanging heat.
    /// </summary>
    /// <param name="cA">The first <see cref="HeatContainer"/> to exchange heat.</param>
    /// <param name="cB">The second <see cref="HeatContainer"/> to exchange heat with.</param>
    /// <param name="dQ">The amount of heat in joules that was transferred from container A to B.</param>
    [PublicAPI]
    public static void Equilibrate(this ref HeatContainer cA, ref HeatContainer cB, out float dQ)
    {
        var tInitialA = cA.Temperature;
        var tFinal = EquilibriumTemperatureQuery(ref cA, ref cB);
        cA.Temperature = tFinal;
        cB.Temperature = tFinal;
        dQ = (tInitialA - tFinal) / cA.HeatCapacity;
    }

    #endregion

    #region N-Body Exchange

    /// <summary>
    /// Brings an array of <see cref="HeatContainer"/>s into thermal equilibrium by exchanging heat.
    /// </summary>
    /// <param name="cN">The array of <see cref="HeatContainer"/>s to bring into thermal equilibrium.</param>
    [PublicAPI]
    public static void Equilibrate(this HeatContainer[] cN)
    {
        var tF = cN.EquilibriumTemperatureQuery();
        for (var i = 0; i < cN.Length; i++)
        {
            cN[i].Temperature = tF;
        }
    }

    /// <summary>
    /// Brings a <see cref="HeatContainer"/> into thermal equilibrium
    /// with an array of other <see cref="HeatContainer"/>s by exchanging heat.
    /// </summary>
    /// <param name="cA">The first <see cref="HeatContainer"/> to bring into thermal equilibrium.</param>
    /// <param name="cN">The array of <see cref="HeatContainer"/>s to bring into thermal equilibrium.</param>
    [PublicAPI]
    public static void Equilibrate(this ref HeatContainer cA, HeatContainer[] cN)
    {
        var tF = cA.EquilibriumTemperatureQuery(cN);

        cA.Temperature = tF;
        for (var i = 0; i < cN.Length; i++)
        {
            cN[i].Temperature = tF;
        }
    }

    /// <summary>
    /// Determines the final temperature of an array of <see cref="HeatContainer"/>s
    /// when they are brought into thermal equilibrium. Does not modify the containers.
    /// </summary>
    /// <param name="cN">The array of <see cref="HeatContainer"/>s to bring into thermal equilibrium.</param>
    /// <returns>The temperature of all <see cref="HeatContainer"/>s involved after reaching thermal equilibrium.</returns>
    [PublicAPI]
    public static float EquilibriumTemperatureQuery(this HeatContainer[] cN)
    {
        /*
        The solution is derived via the following:

        1. In thermal equilibrium all bodies have the same temperature T_f.

        2. Heat exchange for each body is defined by the equation \Delta Q_n = C_n \Delta T_n = C_n (T_f - T_n)
        where C_n is the heat capacity and \Delta T_n the change in temperature of the n-th body.

        3. Heat energy must be conserved, so the sum of all heat changes must equal zero.
        Therefore, \sum_{n=1}^{N} Q_n = 0.

        4. Substitute and expand.
        \sum_{n=1}^{N} C_n (T_f - T_n) = 0.

        5. Unroll and expand.
        C_1(T_f - T_1) + C_2(T_f - T_2) + ... + C_n(T_f - T_n) = 0
        C_1 T_f - C_1 T_1 + C_2 T_f - C_2 T_2 + ... + C_n T_f - C_n T_n = 0

        6. Group like terms.
        T_f(C_1 + C_2 + ... + C_n) - (C_1 T_1 + C_2 T_2 + ... + C_n T_n) = 0

        7. Solve.
        T_f(C_1 + C_2 + ... + C_n) = (C_1 T_1 + C_2 T_2 + ... + C_n T_n)
        T_f = \frac{C_1 T_1 + C_2 T_2 + ... + C_n T_n}{C_1 + C_2 + ... + C_n}

        8. Summation.
        T_f = \frac{\sum(C_n T_n)}{\sum(C_n)}
        */

        var numerator = 0f;
        var denominator = 0f;

        foreach (var c in cN)
        {
            numerator += c.HeatCapacity * c.Temperature;
            denominator += c.HeatCapacity;
        }

        return numerator / denominator;
    }

    /// <summary>
    /// Determines the final temperature of an array of <see cref="HeatContainer"/>s
    /// when they are brought into thermal equilibrium. Does not modify the containers.
    /// </summary>
    /// <param name="cN">The array of <see cref="HeatContainer"/>s to bring into thermal equilibrium.</param>
    /// <param name="dQ">The amount of heat in joules that was added to each container
    /// to reach thermal equilibrium.</param>
    /// <returns>The temperature of all <see cref="HeatContainer"/>s involved after reaching thermal equilibrium.</returns>
    [PublicAPI]
    public static float EquilibriumTemperatureQuery(this HeatContainer[] cN, out float[] dQ)
    {
        /*
        For finding the total heat exchanged during the equalization between a group of bodies
        take the difference of the internal energy before and after the exchange.

        dQ = C * (T_f - T_i) for each container
        */

        var tF = cN.EquilibriumTemperatureQuery();
        dQ = new float[cN.Length];

        for (var i = 0; i < cN.Length; i++)
        {
            dQ[i] = cN[i].HeatCapacity * (tF - cN[i].Temperature);
        }

        return tF;
    }

    /// <summary>
    /// Determines the final temperature of a <see cref="HeatContainer"/> when it is brought into thermal equilibrium
    /// with an array of other <see cref="HeatContainer"/>s. Does not modify the containers.
    /// </summary>
    /// <param name="cA">The first <see cref="HeatContainer"/> to bring into thermal equilibrium.</param>
    /// <param name="cN">The array of <see cref="HeatContainer"/>s to bring into thermal equilibrium.</param>
    /// <returns>The temperature of all <see cref="HeatContainer"/>s involved after reaching thermal equilibrium.</returns>
    [PublicAPI]
    public static float EquilibriumTemperatureQuery(this ref HeatContainer cA, HeatContainer[] cN)
    {
        var cAll = new HeatContainer[cN.Length + 1];
        cAll[0] = cA;
        cN.CopyTo(cAll, 1);

        return cAll.EquilibriumTemperatureQuery();
    }

    #endregion
}
