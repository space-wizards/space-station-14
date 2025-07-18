using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Atmos.Reactions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class TritiumFireReaction : IGasReactionEffect
    {
        private static readonly EntProtoId RadiationPulseProtoId = new("TritiumReactionRadiationPulse");

        private static readonly Color WeakestRadiationPulseColor = new Color(26, 77, 255);
        private static readonly Color StrongestRadiationPulseColor = new Color(179, 26, 255);

        // Well we don't have a method for this, so..
        private static Color LerpColor(Color current, Color goal, float fraction)
        {
            var aRGB = new Span<float>([current.R, current.G, current.B]);
            var bRGB = new Span<float>([goal.R, goal.G, goal.B]);

            NumericsHelpers.Sub(bRGB, aRGB);
            NumericsHelpers.Multiply(bRGB, fraction);
            NumericsHelpers.Add(bRGB, aRGB);

            var c = aRGB.ToArray();
            return new Color(c[0], c[1], c[2]);
        }

        public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale, EntityUid? holderUid)
        {
            var energyReleased = 0f;
            var oldHeatCapacity = atmosphereSystem.GetHeatCapacity(mixture, true);
            var temperature = mixture.Temperature;
            var location = holder as TileAtmosphere;
            mixture.ReactionResults[(byte)GasReaction.Fire] = 0f;
            var burnedFuel = 0f;
            var initialTrit = mixture.GetMoles(Gas.Tritium);

            if (mixture.GetMoles(Gas.Oxygen) < initialTrit ||
                Atmospherics.MinimumTritiumOxyburnEnergy > (temperature * oldHeatCapacity * heatScale))
            {
                burnedFuel = mixture.GetMoles(Gas.Oxygen) / Atmospherics.TritiumBurnOxyFactor;
                if (burnedFuel > initialTrit)
                    burnedFuel = initialTrit;

                mixture.AdjustMoles(Gas.Tritium, -burnedFuel);
            }
            else
            {
                burnedFuel = initialTrit;
                mixture.SetMoles(Gas.Tritium, mixture.GetMoles(Gas.Tritium) * (1 - 1 / Atmospherics.TritiumBurnTritFactor));
                mixture.AdjustMoles(Gas.Oxygen, -mixture.GetMoles(Gas.Tritium));
                energyReleased += (Atmospherics.FireHydrogenEnergyReleased * burnedFuel * (Atmospherics.TritiumBurnTritFactor - 1));
            }

            if (burnedFuel > 0)
            {
                energyReleased += (Atmospherics.FireHydrogenEnergyReleased * burnedFuel);

                // Conservation of mass is important.
                mixture.AdjustMoles(Gas.WaterVapor, burnedFuel);

                mixture.ReactionResults[(byte)GasReaction.Fire] += burnedFuel;
            }

            energyReleased /= heatScale; // adjust energy to make sure speedup doesn't cause mega temperature rise
            if (energyReleased > 0)
            {
                var newHeatCapacity = atmosphereSystem.GetHeatCapacity(mixture, true);
                if (newHeatCapacity > Atmospherics.MinimumHeatCapacity)
                    mixture.Temperature = ((temperature * oldHeatCapacity + energyReleased) / newHeatCapacity);

                // ATMOS Radiation pulse here!
                if (energyReleased > Atmospherics.TritiumMinimumEnergyForRadiation &&
                     atmosphereSystem.TryGetMixtureHolderCoordinates(holder, holderUid, out var holderCoordinates))
                {
                    var radiationEntity = atmosphereSystem.EnsureMixtureEntity(
                        mixture,
                        (byte)GasReactionEntity.TritiumRadiation,
                        RadiationPulseProtoId,
                        holderCoordinates.Value);

                    atmosphereSystem.RefreshEntityTimedDespawn(radiationEntity, 2);

                    var fullRadiation = (energyReleased + Atmospherics.TritiumMinimumEnergyForRadiation) / Atmospherics.TritiumRadiationFactor;
                    var radiation = MathF.Min(Atmospherics.MaxTritiumRadiation, fullRadiation);

                    // the light emitted from tritium-combustion is very, very loosely based off of cherenkov radiation (its because its blue)
                    // tihs is to signal to people that "OH FUCK THERE'S TRIT HERE.."
                    atmosphereSystem.AdjustRadiationPulse(radiationEntity,
                        radiation,
                        LerpColor(WeakestRadiationPulseColor, StrongestRadiationPulseColor, radiation / Atmospherics.MaxTritiumRadiation),
                        fullRadiation / Atmospherics.MaxTritiumRadiation);
                }
            }

            if (location != null)
            {
                temperature = mixture.Temperature;
                if (temperature > Atmospherics.FireMinimumTemperatureToExist)
                {
                    atmosphereSystem.HotspotExpose(location, temperature, mixture.Volume);
                }
            }

            return mixture.ReactionResults[(byte)GasReaction.Fire] != 0 ? ReactionResult.Reacting : ReactionResult.NoReaction;
        }
    }
}
