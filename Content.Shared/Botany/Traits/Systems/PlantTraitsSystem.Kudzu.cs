using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;
using Content.Shared.Botany.Systems;
using Content.Shared.Botany.Traits.Components;

namespace Content.Shared.Botany.Traits.Systems;

/// <inheritdoc cref="PlantTraitKudzuComponent"/>
public sealed partial class PlantTraitKudzuSystem : EntitySystem
{
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly PlantTraySystem _plantTray = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlantTraitKudzuComponent, OnPlantGrowEvent>(OnPlantGrow);
    }

    private void OnPlantGrow(Entity<PlantTraitKudzuComponent> ent, ref OnPlantGrowEvent args)
    {
        var trayUid = GetEntity(args.Tray);
        if (!TryComp<PlantTrayComponent>(trayUid, out var trayComp))
            return;

        if (trayComp is { WaterLevel: > 10, NutritionLevel: > 5 })
            _plantTray.AdjustWeed(trayUid, ent.Comp.WeedGrowthAmount);

        // Handle kudzu transformation.
        if (trayComp.WeedLevel >= ent.Comp.WeedLevelThreshold)
        {
            EntityManager.PredictedSpawn(ent.Comp.KudzuPrototype, _transform.GetMapCoordinates(ent.Owner));
            RemComp<PlantTraitKudzuComponent>(ent.Owner);
            _plantHolder.Die(ent.Owner);
        }
    }
}
