using Content.Server.Atmos.EntitySystems;
using Content.Server.Botany.Components;
using Content.Shared.Atmos;

namespace Content.Server.Botany.Systems;
public sealed class PressureGrowthSystem : PlantGrowthSystem
{
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolderSystem = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PressureGrowthComponent, OnPlantGrowEvent>(OnPlantGrow);
    }

    private void OnPlantGrow(EntityUid uid, PressureGrowthComponent component, OnPlantGrowEvent args)
    {
        PlantHolderComponent? holder = null;
        Resolve<PlantHolderComponent>(uid, ref holder);

        if (holder == null || holder.Seed == null || holder.Dead)
            return;

        var environment = _atmosphere.GetContainingMixture(uid, true, true) ?? GasMixture.SpaceGas;
        var pressure = environment.Pressure;
        if (pressure < component.LowPressureTolerance || pressure > component.HighPressureTolerance)
        {
            holder.Health -= _random.Next(1, 3);
            holder.ImproperPressure = true;
            if (holder.DrawWarnings)
                holder.UpdateSpriteAfterUpdate = true;
        }
        else
        {
            holder.ImproperPressure = false;
        }
    }
}
