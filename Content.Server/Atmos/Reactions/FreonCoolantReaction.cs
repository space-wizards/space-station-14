using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Reactions;

/// <summary>
///     Takes in nitrogen and freon and cools down the surrounding area.
/// </summary>
[UsedImplicitly]
public sealed class FreonCoolantReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem)
    {
        var oldHeatCapacity = atmosphereSystem.GetHeatCapacity(mixture);
        var temperature = mixture.Temperature;

        var energyModifier = 1f;
        var scale = (temperature - Atmospherics.FreonCoolLowerTemperature) /
                    (Atmospherics.FreonCoolMidTemperature - Atmospherics.FreonCoolLowerTemperature);

        if (scale > 1f)
        {
            // Scale energy but not freon usage if we're in a very, very hot place
            energyModifier = Math.Min(scale, Atmospherics.FreonCoolMaximumEnergyModifier);
            scale = 1f;
        }

        if (scale <= 0)
            return ReactionResult.NoReaction;

        var initialNit = mixture.GetMoles(Gas.Nitrogen);
        var initialFreon = mixture.GetMoles(Gas.Freon);

        var burnRate = initialFreon * scale / Atmospherics.FreonCoolRateModifier;

        var energyReleased = 0f;
        if (burnRate > Atmospherics.MinimumHeatCapacity)
        {
            var nitAmt = Math.Min(burnRate * Atmospherics.FreonNitrogenCoolRatio, initialNit);
            var freonAmt = Math.Min(burnRate, initialFreon);
            mixture.AdjustMoles(Gas.Nitrogen, -nitAmt);
            mixture.AdjustMoles(Gas.Freon, -freonAmt);
            // TODO nitrous oxide
            mixture.AdjustMoles(Gas.CarbonDioxide, nitAmt + freonAmt);
            energyReleased = burnRate * Atmospherics.FreonCoolEnergyReleased * energyModifier;
        }

        if (energyReleased >= 0f)
            return ReactionResult.NoReaction;

        var newHeatCapacity = atmosphereSystem.GetHeatCapacity(mixture);
        if (newHeatCapacity > Atmospherics.MinimumHeatCapacity)
            mixture.Temperature = (temperature * oldHeatCapacity + energyReleased) / newHeatCapacity;

        return ReactionResult.Reacting;
    }
}
