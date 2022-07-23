using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Reactions;

[UsedImplicitly]
public sealed class FreonCoolantReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem)
    {
        var oldHeatCapacity = atmosphereSystem.GetHeatCapacity(mixture);
        var temperature = mixture.Temperature;

        var scale = 5 * (temperature - Atmospherics.FreonCoolLowerTemperature) /
                    (Atmospherics.FreonCoolUpperTemperature - Atmospherics.FreonCoolLowerTemperature);

        scale = Math.Min(scale, 5f);

        if (scale <= 0)
            return ReactionResult.NoReaction;

        var initialOxy = mixture.GetMoles(Gas.Oxygen);
        var initialFreon = mixture.GetMoles(Gas.Freon);

        var burnRate = initialFreon * scale / Atmospherics.FreonCoolRateModifier;

        var energyReleased = 0f;
        if (burnRate > Atmospherics.MinimumHeatCapacity)
        {
            var oxyAmt = Math.Min(burnRate * Atmospherics.FreonOxygenCoolRatio, initialOxy);
            var freonAmt = Math.Min(burnRate, initialFreon);
            mixture.AdjustMoles(Gas.Oxygen, -oxyAmt);
            mixture.AdjustMoles(Gas.Freon, -freonAmt);
            mixture.AdjustMoles(Gas.CarbonDioxide, oxyAmt + freonAmt);
            energyReleased = burnRate * Atmospherics.FreonCoolEnergyReleased;
        }

        if (energyReleased >= 0f)
            return ReactionResult.NoReaction;

        var newHeatCapacity = atmosphereSystem.GetHeatCapacity(mixture);
        if (newHeatCapacity > Atmospherics.MinimumHeatCapacity)
            mixture.Temperature = (temperature * oldHeatCapacity + energyReleased) / newHeatCapacity;

        return ReactionResult.Reacting;
    }
}
