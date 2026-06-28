using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Reactions;

/// <summary>
///     Produces electrovae from water vapor and nitrous oxide;
///     Chemical Equation: 11N₂O + 2H₂O → H₄N₂₂O₁₁ + O₂
///     (H₄N₂₂O₁₁ = Electrovae), and yes, O₂ is created;
///     So avoid having electrovae at high temperature if you don't want it rapidly changing to charged electrovae and discharging.
///     This reaction is temperature-dependent, with higher temperatures increasing efficiency.
///     Nitrogen acts as both a catalyst (enabling the reaction) and a limiter (preventing runaway production), is not consumed.
///     Carbon dioxide acts as an inhibitor, reducing or preventing the reaction when present.
/// </summary>
[UsedImplicitly]
public sealed partial class ElectrovaeProductionReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        var initialN2 = mixture.GetMoles(Gas.Nitrogen);
        var initialN2O = mixture.GetMoles(Gas.NitrousOxide);
        var initialH2O = mixture.GetMoles(Gas.WaterVapor);
        var initialCO2 = mixture.GetMoles(Gas.CarbonDioxide);

        // Ratio of water vapor consumed per reaction set.
        const float h2oRatio = 2f;
        // Ratio of nitrous oxide consumed per reaction set.
        const float n2oRatio = 11f;
        // Minimum combined efficiency (0-1) below which the reaction does not proceed.
        const float minimumEfficiency = 0.01f;
        // CO2 proportion at which the inhibition factor reaches 50%. Higher values = less sensitive.
        const float inhibitorSensitivity = 0.05f;

        // The amount of catalyst determines the MAXIMUM amount of reaction that can occur.
        // 6% is the optimal catalyst proportion. We calculate a catalyst factor.
        var totalMoles = mixture.TotalMoles;

        var catalystProportion = initialN2 / totalMoles;
        var catalystFactor = Math.Clamp(catalystProportion / Atmospherics.ElectrovaeProductionNitrogenRatio, 0f, 1f);

        // CO2 acts as an inhibitor, higher concentrations reduce reaction rate
        var inhibitorProportion = initialCO2 / totalMoles;
        var inhibitorFactor = Math.Clamp(1f - inhibitorProportion / inhibitorSensitivity, 0f, 1f);
        if (catalystFactor * inhibitorFactor < minimumEfficiency)
            return ReactionResult.NoReaction;

        // Find the limiting reactant, respecting the 2:11 ratio.
        var maxSetsByH2O = initialH2O / h2oRatio;
        var maxSetsByN2O = initialN2O / n2oRatio;
        var possibleSets = Math.Min(maxSetsByH2O, maxSetsByN2O);

        // Apply catalyst limit: The reaction cannot proceed faster than the catalyst allows.
        possibleSets *= catalystFactor;
        possibleSets *= inhibitorFactor;

        // Arrhenius: Yield is highest at high temperatures and drops to zero as temperature decreases.
        var arrheniusEfficiency = CalculateArrheniusEfficiency(mixture.Temperature);
        if (arrheniusEfficiency * catalystFactor * inhibitorFactor < minimumEfficiency)
            return ReactionResult.NoReaction;

        // Scale the reaction by the thermal efficiency.
        var reactionSets = possibleSets * arrheniusEfficiency;

        var h2oUsed = reactionSets * h2oRatio;
        var n2oUsed = reactionSets * n2oRatio;
        var electrovaeProduced = reactionSets;

        mixture.AdjustMoles(Gas.NitrousOxide, -n2oUsed);
        mixture.AdjustMoles(Gas.WaterVapor, -h2oUsed);
        mixture.AdjustMoles(Gas.Electrovae, electrovaeProduced);
        mixture.AdjustMoles(Gas.Oxygen, electrovaeProduced);

        return reactionSets > 0 ? ReactionResult.Reacting : ReactionResult.NoReaction;
    }

    private static float CalculateArrheniusEfficiency(float temperature)
    {
        var minTemp = Atmospherics.ElectrovaeProductionMinTemperature;
        if (temperature < minTemp)
            return 0f;

        var maxTemp = Atmospherics.ElectrovaeProductionMaxTemperature;
        if (temperature >= maxTemp)
            return 1f;

        var normalizedEfficiency = (temperature - minTemp) / (maxTemp - minTemp);

        normalizedEfficiency = MathF.Pow(normalizedEfficiency, Atmospherics.ElectrovaeProductionTemperatureExponent);

        return Math.Clamp(normalizedEfficiency, 0f, 1f);
    }
}
