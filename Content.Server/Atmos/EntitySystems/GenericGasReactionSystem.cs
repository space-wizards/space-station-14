using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Content.Server.Atmos.Reactions;
using Content.Shared.Atmos.Reactions;
using Content.Shared.Atmos;

namespace Content.Server.Atmos.EntitySystems;

public sealed class GenericGasReactionSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;

    /// <summary>
    /// Return a reaction rate (in units reactants per second) for a given reaction. Based on the
    /// Arrhenius equation (https://en.wikipedia.org/wiki/Arrhenius_equation).
    ///
    /// This means that most reactions scale exponentially above the MinimumTemperatureRequirement.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private float ReactionRate(GasReactionPrototype reaction, GasMixture mix, float dE)
    {
        var temp = mix.Temperature;

        // Gas reactions have a MinimumEnergyRequirement which is in spirit activation energy (Ea),
        // but no reactions define it. So we have to calculate one to use. One way is to assume that
        // Ea = 10 * R * MinimumTemperatureRequirement such that Ea >> RT.
        const float TScaleFactor = 10;
        var Ea = TScaleFactor * Atmospherics.R * reaction.MinimumTemperatureRequirement + dE;

        // To compute initial rate coefficient A, assume that at temp = min temp we return 1/10.
        const float RateScaleFactor = 10; // not necessarily the same as TScaleFactor! Don't get confused!
        var A = MathF.Exp(TScaleFactor) / RateScaleFactor;

        // Prevent divide by zero
        if (temp < Atmospherics.TCMB)
            return 0;

        return A * MathF.Exp(-Ea / (Atmospherics.R * temp));
    }

    /// <summary>
    ///     Run all of the reactions given on the given gas mixture located in the given container.
    /// </summary>
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
        Debug.Assert(mix.Volume > Atmospherics.GasMinVolumeForReactions);

        foreach (var reaction in reactions)
        {
            // Check if this is a generic YAML reaction (has reactants)
            if (reaction.Reactants.Count == 0)
                continue;

            // Add concentration-dependent reaction rate
            // For 1A + 2B -> 3C, the concentration-dependence is [A]^1 * [B]^2
            var rate = 1f; // rate of this reaction
            foreach (var (reactant, num) in reaction.Reactants)
            {
                var concentration = mix.GetMoles(reactant) / mix.Volume;
                rate *= MathF.Pow(concentration, num);
            }

            // Sum catalysts
            float catalystEnergy = 0;
            foreach (var (catalyst, dE) in reaction.Catalysts)
            {
                var concentration = mix.GetMoles(catalyst) / mix.Volume;
                catalystEnergy += dE * concentration;
            }

            // Now apply temperature-dependent reaction rate scaling
            rate *= ReactionRate(reaction, mix, catalystEnergy);

            // Nothing to do
            if (rate <= 0)
                continue;

            // Go through and remove all the reactants
            // If any of the reactants were zero, then the code above would have already set
            // rate to zero, so we don't have to check that again here.
            foreach (var (reactant, num) in reaction.Reactants)
            {
                mix.AdjustMoles(reactant, -num * rate);
            }

            // Go through and add products
            foreach (var (product, num) in reaction.Products)
            {
                mix.AdjustMoles(product, num * rate);
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
