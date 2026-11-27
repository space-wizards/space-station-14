using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;
using Content.Shared.Botany.Systems;

namespace Content.Server.Botany.Systems;

public sealed partial class PlantAtmosphericSystem : SharedPlantAtmosphericSystem
{
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private PlantHolderSystem _plantHolder = default!;

    [Dependency] private EntityQuery<PlantHolderComponent> _holderQuery;


    /// <summary>
    /// Calculates the damage that a plant should take due to improper temperature in a given environment
    /// </summary>
    private static float CalculatePlantTemperatureDamage(Entity<PlantAtmosphericComponent> ent, GasMixture environment)
    {
        var tempThresholdDiff = 0f;
        if (environment.Temperature < ent.Comp.LowHeatTolerance)
        {
            tempThresholdDiff = ent.Comp.LowHeatTolerance - environment.Temperature;
        }
        else if (environment.Temperature > ent.Comp.HighHeatTolerance)
        {
            tempThresholdDiff = environment.Temperature - ent.Comp.HighHeatTolerance;
        }

        if (tempThresholdDiff > 0)
        {
            //Take HeatToleranceDamage at HeatToleranceDifference degrees above or below the threshold, increasing as
            //the differential increases. A decrease in steepness will increase damage taken at higher differentials
           return (float) (ent.Comp.HeatToleranceDamage *
                  Math.Log(ent.Comp.HeatToleranceInvScaling * tempThresholdDiff + 1) /
                  Math.Log(ent.Comp.HeatToleranceInvScaling * ent.Comp.HeatToleranceDifference + 1));
        }

        return 0f;
    }

    /// <summary>
    /// Calculates the damage that a plant should take due to improper pressure in a given environment
    /// </summary>
    private static float CalculatePlantPressureDamage(Entity<PlantAtmosphericComponent> ent, GasMixture environment)
    {
        var pressureThresholdDiff = 0f;

        if (environment.Pressure < ent.Comp.LowPressureTolerance)
        {
            pressureThresholdDiff = ent.Comp.LowPressureTolerance - environment.Pressure;
        }
        else if (environment.Pressure > ent.Comp.HighPressureTolerance)
        {
            pressureThresholdDiff = environment.Pressure - ent.Comp.HighPressureTolerance;
        }

        if (pressureThresholdDiff > 0)
        {
            //Take PressureToleranceDamage at PressureToleranceDifference kPA above or below the threshold, increasing
            //as the differential increases. A decrease in steepness will increase damage taken at higher differentials
            return (float) (ent.Comp.PressureToleranceDamage *
                            Math.Log(ent.Comp.PressureToleranceInvScaling * pressureThresholdDiff + 1) /
                            Math.Log(ent.Comp.PressureToleranceInvScaling * ent.Comp.PressureToleranceDifference + 1));
        }

        return 0f;
    }

    [SubscribeLocalEvent]
    private void OnPlantGrow(Entity<PlantAtmosphericComponent> ent, ref PlantGrowEvent args)
    {
        if (!_holderQuery.TryComp(ent.Owner, out var holder))
            return;

        var environment = _atmosphere.GetContainingMixture(ent.Owner, true, true) ?? GasMixture.SpaceGas;

        var tempDamage = CalculatePlantTemperatureDamage(ent, environment);
        if (tempDamage > 0)
        {
            holder.ImproperHeat = true;
            _plantHolder.AdjustsHealth((ent.Owner, holder), -tempDamage);
        }
        else
            holder.ImproperHeat = false;


        var pressureDamage = CalculatePlantPressureDamage(ent, environment);
        if (pressureDamage > 0)
        {
            holder.ImproperPressure = true;
            _plantHolder.AdjustsHealth((ent.Owner, holder), -pressureDamage);
        }
        else
            holder.ImproperPressure = false;

        Dirty(ent);
    }
}
