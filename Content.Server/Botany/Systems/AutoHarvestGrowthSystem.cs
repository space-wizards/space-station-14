using Content.Server.Botany.Components;

namespace Content.Server.Botany.Systems;
public sealed class AutoHarvestGrowthSystem : PlantGrowthSystem
{
    [Dependency] private readonly PlantHolderSystem _plantHolderSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AutoHarvestGrowthComponent, OnPlantGrowEvent>(OnPlantGrow);
    }

    private void OnPlantGrow(EntityUid uid, AutoHarvestGrowthComponent component, OnPlantGrowEvent args)
    {
        PlantHolderComponent? holder = null;
        Resolve<PlantHolderComponent>(uid, ref holder);

        if (holder == null || holder.Seed == null || holder.Dead || !holder.Harvest)
            return;

        _plantHolderSystem.AutoHarvest(uid, holder);
    }
}
