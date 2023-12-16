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

        var burnedFuel = cacheN2O * Atmospherics.N2ODecompositionRate * (mixture.Temperature - Atmospherics.N2ODecompositionMinScaleTemp) * (mixture.Temperature - Atmospherics.N2ODecompositionMaxScaleTemp) / Atmospherics.N2ODecompositionScaleDivisor;

        //if (burnedFuel <= 0 || cacheN2O - burnedFuel < 0) return ReactionResult.NoReaction;

        mixture.AdjustMoles(Gas.NitrousOxide, burnedFuel * Atmospherics.N2ODecompositionRate);
        mixture.AdjustMoles(Gas.Nitrogen, -burnedFuel * Atmospherics.N2ODecompositionRate);
        mixture.AdjustMoles(Gas.Oxygen, (-burnedFuel / 2) * Atmospherics.N2ODecompositionRate);

        return ReactionResult.Reacting;
    }
}
