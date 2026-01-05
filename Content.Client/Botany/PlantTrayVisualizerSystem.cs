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
            UpdateTrayWarnings(uid, tray, appearance);
        }
    }

    private void UpdateTrayWarnings(EntityUid trayUid, PlantTrayComponent tray, AppearanceComponent appearance)
    {
        if (!tray.DrawWarnings)
            return;

        var water = tray.WaterLevel <= tray.MaxWaterLevel * 0.1f;
        var nutrition = tray.NutritionLevel <= tray.MaxNutritionLevel * 0.1f;
        var alert = _plantTray.GetWeedThreshold(trayUid);
        var health = false;
        var harvest = false;

        if (_plantTray.TryGetPlant(trayUid, out var plantUid))
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
        _appearance.SetData(trayUid, PlantTrayVisuals.HealthLight, health, appearance);
        _appearance.SetData(trayUid, PlantTrayVisuals.WaterLight, water, appearance);
        _appearance.SetData(trayUid, PlantTrayVisuals.NutritionLight, nutrition, appearance);
        _appearance.SetData(trayUid, PlantTrayVisuals.AlertLight, alert, appearance);
        _appearance.SetData(trayUid, PlantTrayVisuals.HarvestLight, harvest, appearance);
    }
}
