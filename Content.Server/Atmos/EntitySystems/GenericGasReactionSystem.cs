using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Content.Server.Atmos.Reactions;
using Content.Shared.Atmos.Reactions;
using Content.Shared.Atmos;

namespace Content.Server.Atmos.EntitySystems;

public sealed class GenericGasReactionSystem : EntitySystem
{
    // Extremely high temperatures cause numerical instability with exp(-Ea/RT), so cap it.
    const float MaxTemperature = 1500f;

    // Convergence relative tolerance criteria. Reducing this causes the solver to reject solutions
    // that don't conserve reltol * TotalMoles mass and fall back to the backup solver.
    const float reltol = 1e-1f;

    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;

    /// <summary>
    /// Return a reaction rate (in units reactants per second) for a given reaction. Based on the
    /// Arrhenius equation (https://en.wikipedia.org/wiki/Arrhenius_equation).
    ///
    /// This means that most reactions scale exponentially above the MinimumTemperatureRequirement.
    /// </summary>
    /// <remarks>See this basic calculator (https://www.desmos.com/calculator/db9t8ophwm) for a visualization of how
    /// changing the parameters affects the curve.</remarks>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private float ReactionRate(GasReactionPrototype reaction, GasMixture mix, float dE)
    {
        var temp = mix.Temperature;

        // Gas reactions have a MinimumEnergyRequirement which is in spirit activation energy (Ea),
        // but no reactions define it. So we have to calculate one to use. One way is to assume that
        // Ea = 10 * R * MinimumTemperatureRequirement such that Ea >> RT.
        const float TScaleFactor = 7f;
        var Ea = TScaleFactor * Atmospherics.R * reaction.MinimumTemperatureRequirement + dE;

        // To compute initial rate coefficient A, assume that at temp = min temp we return 1/10.
        const float RateScaleFactor = 0.1f; // not necessarily the same as TScaleFactor! Don't get confused!
        var A = MathF.Exp(TScaleFactor) / RateScaleFactor;

        // Prevent divide by zero
        if (temp < Atmospherics.TCMB)
            return 0;

        return A * MathF.Exp(-Ea / (Atmospherics.R * Math.Min(temp, MaxTemperature)));
    }

    /// <summary>
    ///     Run all of the reactions given on the given gas mixture located in the given container.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public ReactionResult ReactAll(IEnumerable<GasReactionPrototype> reactions,
        GasMixture mix,
        IGasMixtureHolder? holder)
    {
        var nTotal = mix.TotalMoles;
        // Guard against very small amounts of gas in mixture
        if (nTotal < Atmospherics.GasMinMoles)
            return ReactionResult.NoReaction;

        // Guard against volume div/0.
        // Realistically, GasMixtures should never have this low of a volume.
        // Debug.Assert(mix.Volume > Atmospherics.GasMinVolumeForReactions);

        foreach (var reaction in reactions)
        {
            // Check if this is a generic YAML reaction (has reactants)
            if (reaction.Reactants.Count == 0)
                continue;

            // TODO ATMOS PLAYTEST: Determine if people like the realistic reaction rate behavior
            // where reactions still occur (albeit very slowly) below the minimum temperature requirement.
            // if (mix.Temperature < reaction.MinimumTemperatureRequirement)
            //     continue;

            // Set up stoichiometry vector. If there are 3 gases A, B, and C, then A + B -> C results in a stoichiometry
            // vector of [-1, -1, 1].
            var S = new float[Atmospherics.AdjustedNumberOfGases];
            foreach (var (reactant, num) in reaction.Reactants)
            {
                S[(int)reactant] = -num;
            }

            foreach (var (product, num) in reaction.Products)
            {
                S[(int)product] = num;
            }

            // Add concentration-dependent reaction rate
            // For 1A + 2B -> 3C, the concentration-dependence is [A]^1 * [B]^2
            var rate = 1f; // rate of this reaction
            foreach (var (reactant, num) in reaction.Reactants)
            {
                // TODO ATMOS: nTotal is not truly correct here as reaction rate should be dependant on
                // concentration, not the mole fraction. Using mix.Volume here is more accurate but causes nothing
                // to react because the resulting concentrations are so low, which leads to an effectively zero rate.
                // However for the sake of getting this Working:tm: we'll leave it as is for now.
                var concentration = mix.GetMoles(reactant) / nTotal;
                rate *= MathF.Pow(concentration, num);
            }

            // Sum catalysts
            float catalystEnergy = 0;
            foreach (var (catalyst, dE) in reaction.Catalysts)
            {
                var concentration = mix.GetMoles(catalyst) / nTotal;
                catalystEnergy += dE * concentration;
            }

            // Now apply temperature-dependent reaction rate scaling
            rate *= ReactionRate(reaction, mix, catalystEnergy);

            // Nothing to do
            if (rate <= 0)
                continue;

            var dC = new float[Atmospherics.AdjustedNumberOfGases]; // change in concentration

            // Go through and remove all the reactants
            // If any of the reactants were zero, then the code above would have already set
            // rate to zero, so we don't have to check that again here.
            foreach (var (reactant, num) in reaction.Reactants)
            {
                dC[(int)reactant] = -num * rate;
            }

            // Go through and add products
            foreach (var (product, num) in reaction.Products)
            {
                dC[(int)product] = num * rate;
            }

            // Check for conservation of mass. If we have A + B -> C, then we need to check that d/dt[A] + d/dt[B] =
            // -d/dt[C], i.e. d/dt[A] + d/dt[B] + d/dt[C] = 0 (under some epsilon). This epsilon is computed
            // automatically from a "relative tolerance" (reltol) constant.
            float residualMoles = 0;
            for (var i = 0; i < dC.Length; i++)
            {
                residualMoles += dC[i] * Math.Abs(S[i]);
            }
            var Nreltol = mix.TotalMoles * reltol;
            if (residualMoles > Nreltol)
            {
                Log.Error($"GenericGasReaction {reaction.ID} did not converge to a safe solution, residual {residualMoles} > {Nreltol}, mix {mix.ToPrettyString()}." +
                          $" Falling back to extent of reaction method.");

                // Reaction has diverged. Switch over to applying a different method via the Extent of Reaction.
                // See https://en.wikipedia.org/wiki/Extent_of_reaction
                // Limit by whichever reactant would run out first.
                var dXiMax = float.MaxValue;
                foreach (var (reactant, num) in reaction.Reactants)
                {
                    var available = mix.GetMoles(reactant);
                    Debug.Assert(num > 0);
                    dXiMax = MathF.Min(dXiMax, available / num);
                }

                // Determine the actual extent of reaction.
                var dXi = MathF.Min(rate, dXiMax);
                if (dXi <= 0f)
                    continue;

                foreach (var (reactant, num) in reaction.Reactants)
                {
                    mix.AdjustMoles(reactant, -num * dXi);
                }

                // Go through and add products
                foreach (var (product, num) in reaction.Products)
                {
                    mix.AdjustMoles(product, num * dXi);
                }
            }
            else
            {
                for (var i = 0; i < dC.Length; i++)
                {
                    mix.AdjustMoles(i, dC[i]); // TODO: fancier SIMD/numerics helper method
                }
            }

            // Add heat from the reaction
            if (reaction.Enthalpy != 0)
            {
                _atmosphere.AddHeat(mix, reaction.Enthalpy / _atmosphere.HeatScale * rate);
                if (reaction.Enthalpy > 0)
                {
                    mix.ReactionResults[(byte)GasReaction.Fire] += rate;
                    if (holder is TileAtmosphere location)
                    {
                        if (mix.Temperature > Atmospherics.FireMinimumTemperatureToExist)
                        {
                            _atmosphere.HotspotExpose(location.GridIndex,
                                location.GridIndices,
                                mix.Temperature,
                                mix.Volume);
                        }
                    }
                }
            }
        }

        return ReactionResult.Reacting;
    }
}
