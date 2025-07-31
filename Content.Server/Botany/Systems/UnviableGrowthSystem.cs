using Content.Server.Botany.Components;

namespace Content.Server.Botany.Systems;
public sealed class UnviableGrowthSystem : PlantGrowthSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<UnviableGrowthComponent, OnPlantGrowEvent>(OnPlantGrow);
    }

    private void OnPlantGrow(EntityUid uid, UnviableGrowthComponent component, OnPlantGrowEvent args)
    {
        PlantHolderComponent? holder = null;
        Resolve<PlantHolderComponent>(uid, ref holder);

        if (holder == null || holder.Seed == null || holder.Dead)
            return;

        holder.Health -= 6 * _random.Next(1, 3) * HydroponicsSpeedMultiplier;
    }
}
