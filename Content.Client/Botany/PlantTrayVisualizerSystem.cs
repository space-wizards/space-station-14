using Content.Client.Botany.Components;
using Content.Shared.Botany;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;
using Robust.Client.GameObjects;

namespace Content.Client.Botany;

public sealed class PlantTrayVisualizerSystem : VisualizerSystem<PlantTrayVisualsComponent>
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly PlantTraySystem _plantTray = default!;
    [Dependency] private readonly WeedPestGrowthSystem _weedPestGrowth = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<PlantTrayVisualsComponent, PlantTrayComponent, AppearanceComponent>();
        while (query.MoveNext(out var uid, out _, out var tray, out var appearance))
        {
            UpdateTrayWarnings((uid, tray), appearance);
        }
    }

    private void UpdateTrayWarnings(Entity<PlantTrayComponent> ent, AppearanceComponent appearance)
    {
        if (!ent.Comp.DrawWarnings)
            return;

        var water = ent.Comp.WaterLevel <= ent.Comp.MaxWaterLevel * 0.1f;
        var nutrition = ent.Comp.NutritionLevel <= ent.Comp.MaxNutritionLevel * 0.1f;
        var alert = _plantTray.GetWeedThreshold(ent.Owner);
        var health = false;
        var harvest = false;

        if (_plantTray.TryGetPlant(ent.AsNullable(), out var plantUid))
        {
            if (TryComp<PlantHolderComponent>(plantUid, out var plantHolder))
            {
                alert |= _weedPestGrowth.GetPestThreshold(plantUid.Value)
                         || _plantHolder.GetToxinsThreshold(plantUid.Value)
                         || plantHolder.ImproperHeat
                         || plantHolder.ImproperPressure
                         || plantHolder.MissingGas;

                health = _plantHolder.GetHealthThreshold(plantUid.Value);
            }

            if (TryComp<PlantHarvestComponent>(plantUid, out var plantHarvest))
                harvest = plantHarvest.ReadyForHarvest;
        }

        // These are appearance keys consumed by the prototype's <see cref="GenericVisualizer"/>.
        _appearance.SetData(ent.Owner, PlantTrayVisuals.HealthLight, health, appearance);
        _appearance.SetData(ent.Owner, PlantTrayVisuals.WaterLight, water, appearance);
        _appearance.SetData(ent.Owner, PlantTrayVisuals.NutritionLight, nutrition, appearance);
        _appearance.SetData(ent.Owner, PlantTrayVisuals.AlertLight, alert, appearance);
        _appearance.SetData(ent.Owner, PlantTrayVisuals.HarvestLight, harvest, appearance);
    }
}
