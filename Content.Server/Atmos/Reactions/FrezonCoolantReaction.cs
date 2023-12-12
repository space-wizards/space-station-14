using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Reactions;

/// <summary>
///     Takes in nitrogen and frezon and cools down the surrounding area.
/// </summary>
[UsedImplicitly]
public sealed partial class FrezonCoolantReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem)
    {
        var oldHeatCapacity = atmosphereSystem.GetHeatCapacity(mixture);
        var temperature = mixture.Temperature;

        var energyModifier = 1f;
        var scale = (temperature - Atmospherics.FrezonCoolLowerTemperature) /
                    (Atmospherics.FrezonCoolMidTemperature - Atmospherics.FrezonCoolLowerTemperature);

        if (scale > 1f)
        {
            // Scale energy but not frezon usage if we're in a very, very hot place
            energyModifier = Math.Min(scale, Atmospherics.FrezonCoolMaximumEnergyModifier);
            scale = 1f;
        }

        if (scale <= 0)
            return ReactionResult.NoReaction;

        var initialNit = mixture.GetMoles(Gas.Nitrogen);
        var initialFrezon = mixture.GetMoles(Gas.Frezon);

        var burnRate = initialFrezon * scale / Atmospherics.FrezonCoolRateModifier;

        var energyReleased = 0f;
        if (burnRate > Atmospherics.MinimumHeatCapacity)
        {
            var nitAmt = Math.Min(burnRate * Atmospherics.FrezonNitrogenCoolRatio, initialNit);
            var frezonAmt = Math.Min(burnRate, initialFrezon);
            mixture.AdjustMoles(Gas.Nitrogen, -nitAmt);
            mixture.AdjustMoles(Gas.Frezon, -frezonAmt);
            mixture.AdjustMoles(Gas.NitrousOxide, nitAmt + frezonAmt);
            energyReleased = burnRate * Atmospherics.FrezonCoolEnergyReleased * energyModifier;
        }

        energyReleased /= atmosphereSystem.HeatScale; // adjust energy to make sure speedup doesn't cause mega temperature rise
        if (energyReleased >= 0f)
            return ReactionResult.NoReaction;

        var newHeatCapacity = atmosphereSystem.GetHeatCapacity(mixture);
        if (newHeatCapacity > Atmospherics.MinimumHeatCapacity)
            mixture.Temperature = (temperature * oldHeatCapacity + energyReleased) / newHeatCapacity;

        return ReactionResult.Reacting;
    }
}
