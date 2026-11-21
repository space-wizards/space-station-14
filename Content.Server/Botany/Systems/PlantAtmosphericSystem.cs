using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;
using Content.Shared.Botany.Systems;
using Content.Shared.Temperature.Components;

namespace Content.Server.Botany.Systems;

public sealed partial class PlantAtmosphericSystem : SharedPlantAtmosphericSystem
{
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private PlantHolderSystem _plantHolder = default!;

    [Dependency] private EntityQuery<PlantHolderComponent> _holderQuery;
    [Dependency] private EntityQuery<TemperatureComponent> _temperatureQuery;

    [SubscribeLocalEvent]
    private void OnPlantGrow(Entity<PlantAtmosphericComponent> ent, ref PlantGrowEvent args)
    {
        if (!_holderQuery.TryComp(ent.Owner, out var holder))
            return;

        if (_temperatureQuery.TryComp(ent.Owner, out var temperature)
            && (temperature.Temperature < ent.Comp.LowHeatTolerance || temperature.Temperature > ent.Comp.HighHeatTolerance))
        {
            _plantHolder.AdjustsHealth((ent.Owner, holder), -ent.Comp.HeatToleranceDamage);
            holder.ImproperHeat = true;
        }
        else
            holder.ImproperHeat = false;

        var environment = _atmosphere.GetContainingMixture(ent.Owner, true, true) ?? GasMixture.SpaceGas;
        var pressure = environment.Pressure;
        if (pressure < ent.Comp.LowPressureTolerance || pressure > ent.Comp.HighPressureTolerance)
        {
            _plantHolder.AdjustsHealth((ent.Owner, holder), -ent.Comp.PressureToleranceDamage);
            holder.ImproperPressure = true;
        }
        else
            holder.ImproperPressure = false;

        Dirty(ent);
    }
}
