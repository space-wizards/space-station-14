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
        var initialE = mixture.GetMoles(Gas.Electrovae);
        if (initialE < 0.01f || holder is not TileAtmosphere tileAtmos)
            return ReactionResult.NoReaction;

        var efficiency = CalculateHeatBasedEfficiency(mixture.Temperature);
        if (efficiency < 0.01f)
            return ReactionResult.NoReaction;

        // Scale the reaction by efficiency and available electrovae
        var chargeAmount = Math.Min(initialE * efficiency * 0.1f, initialE);

        mixture.AdjustMoles(Gas.Electrovae, -chargeAmount);
        mixture.AdjustMoles(Gas.ChargedElectrovae, chargeAmount);

        // Expose the tile to charged electrovae - AtmosphereSystem will handle the rest
        var intensity = Math.Min(mixture.GetMoles(Gas.ChargedElectrovae) / 2f, 1f);
        atmosphereSystem.ChargedElectrovaeExpose(tileAtmos, intensity);

        // Consume some heat energy during the charging process
        var energyConsumed = chargeAmount * 5000f; // 5kJ per mole
        var oldHeatCapacity = atmosphereSystem.GetHeatCapacity(mixture, true);
        if (oldHeatCapacity > Atmospherics.MinimumHeatCapacity)
            mixture.Temperature = Math.Max(mixture.Temperature - energyConsumed / oldHeatCapacity, Atmospherics.TCMB);

        return ReactionResult.Reacting;
    }

    /// <summary>
    /// Calculate charging efficiency based on temperature
    /// </summary>
    private static float CalculateHeatBasedEfficiency(float temperature)
    {
        // Efficient above 1508K, peaks around 1984K+
        const float minTemp = 1508f;
        const float maxTemp = 1984f;

        if (temperature < minTemp)
            return 0f;

        if (temperature >= maxTemp)
            return 1f;

        var normalized = (temperature - minTemp) / (maxTemp - minTemp);
        return MathF.Pow(normalized, 1.2f);
    }
}
