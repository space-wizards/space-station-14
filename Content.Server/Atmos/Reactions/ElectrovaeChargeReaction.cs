using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Reactions;

/// <summary>
///     Chemical Equation: H₄N₂₂O₁₁ + Heat → H₄N₂₂O₁₁⁺
///     This reaction converts electrovae to charged electrovae using thermal energy
///     Higher temperatures increase efficiency (Arrhenius)
/// </summary>
[UsedImplicitly]
public sealed partial class ElectrovaeChargeReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        if (holder is not TileAtmosphere tileAtmos)
            return ReactionResult.NoReaction;

        // Minimum efficiency (0-1) below which the reaction does not proceed.
        const float minimumEfficiency = 0.01f;
        // Fraction of electrovae converted per unit efficiency per reaction cycle.
        const float chargeRateMultiplier = 0.1f;

        var initialE = mixture.GetMoles(Gas.Electrovae);
        var efficiency = CalculateHeatBasedEfficiency(mixture.Temperature);
        if (efficiency < minimumEfficiency)
            return ReactionResult.NoReaction;

        // Scale the reaction by efficiency and available electrovae
        var chargeAmount = Math.Min(initialE * efficiency * chargeRateMultiplier, initialE);

        mixture.AdjustMoles(Gas.Electrovae, -chargeAmount);
        mixture.AdjustMoles(Gas.ChargedElectrovae, chargeAmount);

        // Expose the tile to charged electrovae - AtmosphereSystem will handle the rest
        var intensity = Math.Min(mixture.GetMoles(Gas.ChargedElectrovae) / Atmospherics.ChargedElectrovaeIntensityDivisor, 1f);
        atmosphereSystem.ChargedElectrovaeExpose(tileAtmos.GridIndex, tileAtmos, intensity);

        // Consume some heat energy during the charging process
        const float energyPerMole = 5000f; // Energy consumed per mole charged, in joules
        atmosphereSystem.AddHeat(mixture, -chargeAmount * energyPerMole);

        return ReactionResult.Reacting;
    }

    /// <summary>
    /// Calculate charging efficiency based on temperature
    /// </summary>
    private static float CalculateHeatBasedEfficiency(float temperature)
    {
        // Efficient above 1508K, peaks around 1984K+
        const float minTemp = 1508f; // Minimum temperature for reaction to occur
        const float maxTemp = 1984f; // Maximum temperature for reaction to occur
        const float temperatureExponent = 1.2f; // Exponent for temperature scaling

        if (temperature < minTemp)
            return 0f;

        if (temperature >= maxTemp)
            return 1f;

        var normalized = (temperature - minTemp) / (maxTemp - minTemp);
        return MathF.Pow(normalized, temperatureExponent);
    }
}
