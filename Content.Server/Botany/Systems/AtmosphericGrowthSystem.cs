using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;
using Content.Shared.Botany.Systems;

namespace Content.Server.Botany.Systems;

public sealed class AtmosphericGrowthSystem : SharedAtmosphericGrowthSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AtmosphericGrowthComponent, OnPlantGrowEvent>(OnPlantGrow);
    }

    private void OnPlantGrow(Entity<AtmosphericGrowthComponent> ent, ref OnPlantGrowEvent args)
    {
        if (!TryComp<PlantHolderComponent>(ent.Owner, out var holder))
            return;

        var environment = _atmosphere.GetContainingMixture(ent.Owner, true, true) ?? GasMixture.SpaceGas;
        if (environment.Temperature < ent.Comp.LowHeatTolerance || environment.Temperature > ent.Comp.HighHeatTolerance)
        {
            _plantHolder.AdjustsHealth(ent.Owner, -ent.Comp.HeatToleranceDamage);
            holder.ImproperHeat = true;
        }
        else
        {
            holder.ImproperHeat = false;
        }

        var pressure = environment.Pressure;
        if (pressure < ent.Comp.LowPressureTolerance || pressure > ent.Comp.HighPressureTolerance)
        {
            _plantHolder.AdjustsHealth(ent.Owner, -ent.Comp.PressureToleranceDamage);
            holder.ImproperPressure = true;
        }
        else
        {
            holder.ImproperPressure = false;
        }

        Dirty(ent);
    }
}
