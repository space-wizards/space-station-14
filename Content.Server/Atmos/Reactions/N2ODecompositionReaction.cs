using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Reactions;

/// <summary>
///     Decomposes Nitrous Oxide into Nitrogen and Oxygen.
/// </summary>
[UsedImplicitly]
public sealed partial class N2ODecompositionReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem)
    {
        var cacheN2O = mixture.GetMoles(Gas.NitrousOxide);
        var cacheNitrogen = mixture.GetMoles(Gas.Nitrogen);
        var cacheOxygen = mixture.GetMoles(Gas.Oxygen);

        var burnedFuel = cacheN2O * Atmospherics.N2ODecompositionRate * (mixture.Temperature - Atmospherics.N2ODecompositionMinScaleTemp) * (mixture.Temperature - Atmospherics.N2ODecompositionMaxScaleTemp) / Atmospherics.N2ODecompositionScaleDivisor;

        if (burnedFuel <= 0 || cacheN2O - burnedFuel < 0) return ReactionResult.NoReaction;

        mixture.AdjustMoles(Gas.NitrousOxide, cacheN2O -= burnedFuel);
        mixture.AdjustMoles(Gas.Nitrogen, cacheNitrogen += burnedFuel);
        mixture.AdjustMoles(Gas.Oxygen, cacheOxygen += burnedFuel / 2);

        var heatCap = atmosphereSystem.GetHeatCapacity(mixture);
        var energyReleased = Atmospherics.N2ODecompositionEnergy * burnedFuel;
        if (heatCap > Atmospherics.MinimumHeatCapacity)
            mixture.Temperature = (mixture.Temperature * heatCap + energyReleased) / heatCap;

        return ReactionResult.Reacting;
    }
}
