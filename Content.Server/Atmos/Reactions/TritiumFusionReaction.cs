using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Robust.Shared.Maths;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Reactions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class TritiumFusionReaction : IGasReactionEffect
    {
        public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem)
        {
            var energyReleased = 0f;
            var oldHeatCapacity = atmosphereSystem.GetHeatCapacity(mixture);
            var temperature = mixture.Temperature;
            var location = holder as TileAtmosphere;
            mixture.ReactionResults[GasReaction.Fire] = 0f;
            var burnedFuel = 0f;
            var burnedFuelBase = 0f;
            var initialTrit = mixture.GetMoles(Gas.Tritium);

            if (initialTrit < 0.02f) // don't even bother doing the reaction, if this happened somehow
            {
                return ReactionResult.NoReaction;
            }
            else
            {
                // In order to even qualify for a nuclear reaction, we need to calculate how much tritium is above the energy barrier to react.
                // Unlike other reactions, this will be done with real math (and some tweaked constants) to make Atmos's life harder (or easier).
                // We will not be considering nuclear cross sections.

                // The actual energy level we need to compare to:
                //           1    Z1*Z2                                      10000x downscale
                // E_coul = --- * ----- ≈ 970 keV (classical, ~= 11.3 GK; too much) ==> 97 eV (SS14, ~= 1.12MK)
                //          4πε     r                                                 = Atmospherics.TritiumFusionIgnitionTemperature
                //             0

                // Find out how much tritium can react in 1 iteration
                burnedFuelBase = Math.Max(initialTrit
                // Find out how much tritium is ABOVE the fusion ignition temperature, x (multiply by maxwell boltzmann CDF)
                //                                        2
                //                x        2    x       -x
                // P = 1 - (tanh(---) - √(---) --- exp(-----))
                //               K*T       π    T      2 T^2
                // where K is a factor chosen to approximate erf(x/(sqrt(2)T)) best
                                      * (1f - (MathF.Tanh(Atmospherics.TritiumFusionIgnitionTemperature / 1.1753f / temperature)
                                              - ( 0.7978845608028654f * (Atmospherics.TritiumFusionIgnitionTemperature / temperature)
                                                  * MathF.Exp((-(Atmospherics.TritiumFusionIgnitionTemperature
                                                                 * Atmospherics.TritiumFusionIgnitionTemperature)) / 
                                                              (2f * temperature * temperature)
                                      )))), 0f);

                // Quantize our burn
                burnedFuel = burnedFuelBase > 0.02f ? Math.Max(0.02f, burnedFuelBase / Atmospherics.TritiumFusionFactor) : 0f;

                // Do we need this math? Yes, absolutely. Nuclear fusion is complicated and the next step up from "ooo molar reactions" is
                // actual thermodynamics. This equation, by the way, also allows nuclear fusion to happen at way lower temps, say, 200KK.
            }
            if (burnedFuel > 0f)
            {
                // Reaction modeled here:
                //  3    3    4       1
                //  1T + 1T = 2He + 2 0n + Q;
                energyReleased += (Atmospherics.TritiumFusionEnergyReleased * burnedFuel);

                // Conservation of mass is important.
                mixture.AdjustMoles(Gas.Tritium, -burnedFuel);
                mixture.AdjustMoles(Gas.Helium4, burnedFuel / 2);
                // neutrons handled below

                mixture.ReactionResults[GasReaction.Fire] += burnedFuel * 10; // fusion is, uhh... wack
                //mixture.ReactionResults[GasReaction.Nuclear] += burnedFuel;
            }
            
            /*var radiation = Atmospherics.TritiumFusionRadiationScale * burnedFuel; // TODO ATMOS: implement radiation support
            if (holder != null)
            {
                if (radiation > 0.25)
                    holder.SetRadiation(true, radiation);
                else
                    holder.SetRadiation(false, 0f);
            }*/

            if (energyReleased > 0)
            {

                var newHeatCapacity = atmosphereSystem.GetHeatCapacity(mixture);
                if (newHeatCapacity > Atmospherics.MinimumHeatCapacity)
                    mixture.Temperature = ((temperature * oldHeatCapacity + energyReleased) / newHeatCapacity);
            }

            if (location != null)
            {
                temperature = mixture.Temperature;
                if (temperature > Atmospherics.FusionMinimumTemperatureToExist)
                {
                    atmosphereSystem.HotspotExpose(location.GridIndex, location.GridIndices, temperature, mixture.Volume);
                }
            }

            return mixture.ReactionResults[GasReaction.Fire] != 0 ? ReactionResult.Reacting : ReactionResult.NoReaction;
        }
    }
}
