using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Reactions;

/// <summary>
///     Produces electrovae from water vapor and nitrous oxide;
///     Chemical Equation: 11N₂O + 2H₂O → H₄N₂₂O₁₁ + O₂
///     (H₄N₂₂O₁₁ = Electrovae), and yes, O₂ is created;
///     So avoid having electricity nearby if you don't want it rapidly changing to charged electrovae and EMPing everything.
///     This reaction is temperature-dependent, with lower temperatures increasing efficiency.
///     Nitrogen acts as both a catalyst (enabling the reaction) and a limiter (preventing runaway production), is not consumed.
/// </summary>
[UsedImplicitly]
public sealed partial class ElectrovaeProductionReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        var initialN2 = mixture.GetMoles(Gas.Nitrogen);
        var initialN2O = mixture.GetMoles(Gas.NitrousOxide);
        var initialH2O = mixture.GetMoles(Gas.WaterVapor);

        const float h2oRatio = 2f;
        const float n2oRatio = 11f;

        // The amount of catalyst determines the MAXIMUM amount of reaction that can occur.
        // 6% is the optimal catalyst proportion. We calculate a catalyst factor.
        var totalMoles = initialN2 + initialH2O + initialN2O;
        if (totalMoles < 0.01f)
            return ReactionResult.NoReaction;

        var catalystProportion = initialN2 / totalMoles;
        var catalystFactor = Math.Clamp(catalystProportion / Atmospherics.ElectrovaeProductionNitrogenRatio, 0f, 1f);

        // Find the limiting reactant, respecting the 2:11 ratio.
        var maxSetsByH2O = initialH2O / h2oRatio;
        var maxSetsByN2O = initialN2O / n2oRatio;
        var possibleSets = Math.Min(maxSetsByH2O, maxSetsByN2O);

        // Apply catalyst limit: The reaction cannot proceed faster than the catalyst allows.
        possibleSets *= catalystFactor;

        // INVERSE ARRHENIUS: Yield is highest at low temperatures and drops to zero as temperature increases.
        var efficiency = CalculateInverseArrheniusEfficiency(mixture.Temperature);
        if (efficiency < 0.01f)
            return ReactionResult.NoReaction;

        // Scale the reaction by the inverse thermal efficiency.
        var reactionSets = possibleSets * efficiency;

        var h2oUsed = reactionSets * h2oRatio;
        var n2oUsed = reactionSets * n2oRatio;
        var electrovaeProduced = reactionSets;

        mixture.AdjustMoles(Gas.NitrousOxide, -n2oUsed);
        mixture.AdjustMoles(Gas.WaterVapor, -h2oUsed);
        mixture.AdjustMoles(Gas.Electrovae, electrovaeProduced);
        mixture.AdjustMoles(Gas.Oxygen, electrovaeProduced);

        return (reactionSets > 0.01f) ? ReactionResult.Reacting : ReactionResult.NoReaction;
    }

    private static float CalculateInverseArrheniusEfficiency(float temperature)
    {
        var maxTemp = Atmospherics.ElectrovaeProductionMaxTemperature;
        if (temperature >= maxTemp)
            return 0f;

        var minTemp = Atmospherics.ElectrovaeProductionMinTemperature;

        // Normalize the temperature to a 0-1 range, where 1 is max efficiency (coldest)
        // and 0 is no efficiency (hottest within range).
        var normalizedEfficiency = 1 - (temperature - minTemp) / (maxTemp - minTemp);

        normalizedEfficiency = MathF.Pow(normalizedEfficiency, Atmospherics.ElectrovaeProductionTemperatureExponent);

        return Math.Clamp(normalizedEfficiency, 0f, 1f);
    }
}
