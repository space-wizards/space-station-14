using Content.Server.Atmos.Reactions;
using Content.Shared.Atmos;
using JetBrains.Annotations;
using System.Collections;
using System.Linq;

namespace Content.Server.Atmos.EntitySystems;

public sealed class GenericGasReactionSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;

    /// <summary>
    ///     Return a reaction rate (in units reactants per second) for a given reaction. Based on the
    ///     Arrhenius equation (https://en.wikipedia.org/wiki/Arrhenius_equation).
    ///
    ///     This means that most reactions scale exponentially above the MinimumTemperatureRequirement.
    /// </summary>
    private float ReactionRate(GasReactionPrototype reaction, GasMixture mix, float dE)
    {
        float temp = mix.Temperature;

        // Gas reactions have a MinimumEnergyRequirement which is in spirit activiation energy (Ea),
        // but no reactions define it. So we have to calculate one to use. One way is to assume that
        // Ea = 10*R*MinimumTemperatureRequirement such that Ea >> RT.
        float TScaleFactor = 10;
        float Ea = TScaleFactor*Atmospherics.R*reaction.MinimumTemperatureRequirement + dE;

        // To compute initial rate coefficient A, assume that at temp = min temp we return 1/10.
        float RateScaleFactor = 10; // not necessarily the same as TScaleFactor! Don't get confused!
        float A = MathF.Exp(TScaleFactor) / RateScaleFactor;

        return reaction.RateMultiplier*A*MathF.Exp(-Ea/(Atmospherics.R*temp));
    }

    /// <summary>
    ///     Run all of the reactions given on the given gas mixture located in the given container.
    /// </summary>
    public ReactionResult ReactAll(IEnumerable<GasReactionPrototype> reactions, GasMixture mix, IGasMixtureHolder? holder)
    {
        // It is possible for reactions to change the specific heat capacity, so we need to save initial
        // internal energy so that we can conserve energy at the end
        float initialE = _atmosphere.GetThermalEnergy(mix);
        float reactionE = 0; // heat added by reaction enthalpy
        foreach (var reaction in reactions)
        {
            float rate = 1f; // rate of this reaction
            int reactants = 0;

            // Reactions that have a maximum temperature really don't make physical sense since increasing
            // kinetic energy always increases reaction rate. But begrudgingly implement this anyway.
            if (mix.Temperature > reaction.MaximumTemperatureRequirement)
                continue;

            // Add concentration-dependent reaction rate
            // For 1A + 2B -> 3C, the concentration-dependence is [A]^1 * [B]^2
            float nTotal = mix.TotalMoles;
            if (nTotal < Atmospherics.GasMinMoles)
                continue;

            foreach (var (reactant, num) in reaction.Reactants)
            {
                rate *= MathF.Pow(mix.GetMoles(reactant)/nTotal, num);
                reactants++;
            }

            // No reactants; this is not a generic reaction.
            if (reactants == 0)
                continue;

            // Sum catalysts
            float catalystEnergy = 0;
            foreach (var (catalyst, dE) in reaction.Catalysts)
            {
                catalystEnergy += dE;
            }

            // Now apply temperature-dependent reaction rate scaling
            rate *= ReactionRate(reaction, mix, catalystEnergy);

            // Nothing to do
            if (rate <= 0)
                continue;

            // Pass to check the maximum rate, limited by the minimum available
            // reactant to avoid going negative
            float rateLim = rate;
            foreach (var (reactant, num) in reaction.Reactants)
            {
                rateLim = MathF.Min(mix.GetMoles(reactant)/num, rateLim);
            }
            rate = rateLim;

            // Go through and remove all the reactants
            foreach (var (reactant, num) in reaction.Reactants)
            {
                mix.AdjustMoles(reactant, -num*rate);
            }

            // Go through and add products
            foreach (var (product, num) in reaction.Products)
            {
                mix.AdjustMoles(product, num*rate);
            }

            // Add heat from the reaction
            if (reaction.Enthalpy != 0)
            {
                reactionE += reaction.Enthalpy/_atmosphere.HeatScale * rate;
                if (reaction.Enthalpy > 0)
                    mix.ReactionResults[GasReaction.Fire] += rate;
            }
        }

        float newHeatCapacity = _atmosphere.GetHeatCapacity(mix, true);
        mix.Temperature = (initialE + reactionE)/newHeatCapacity;
        if (reactionE > 0)
        {
            var location = holder as TileAtmosphere;
            if (location != null)
            {
                if (mix.Temperature > Atmospherics.FireMinimumTemperatureToExist)
                {
                    _atmosphere.HotspotExpose(location.GridIndex, location.GridIndices, mix.Temperature, mix.Volume);
                }
            }
        }
        return ReactionResult.Reacting;
    }
}
