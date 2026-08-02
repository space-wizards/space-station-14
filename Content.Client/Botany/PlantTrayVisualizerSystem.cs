using Content.Client.Botany.Components;
using Content.Shared.Botany;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;
using Robust.Client.GameObjects;

namespace Content.Client.Botany;

public sealed partial class PlantTrayVisualizerSystem : VisualizerSystem<PlantTrayVisualsComponent>
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private PlantTraySystem _plantTray = default!;
    [Dependency] private PlantHolderSystem _plantHolder = default!;

    /// <summary>
    /// Defers appearance writes until after network state application and deduplicates multiple state events per frame.
    /// </summary>
    private readonly HashSet<EntityUid> _pendingTrayUpdates = [];

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var uid in _pendingTrayUpdates)
        {
            UpdateTrayWarnings(uid);
        }

        _pendingTrayUpdates.Clear();
    }

    [SubscribeLocalEvent]
    private void OnComponentStartup(Entity<PlantTrayVisualsComponent> ent, ref ComponentStartup args)
    {
        QueueTrayWarnings(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnPlantTrayState(Entity<PlantTrayComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        QueueTrayWarnings(ent.Owner);
    }

    private void QueueTrayWarnings(EntityUid uid)
    {
        _pendingTrayUpdates.Add(uid);
    }

    public void UpdateTrayWarnings(Entity<PlantTrayComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (!ent.Comp.DrawWarnings)
            return;

        var water = _plantTray.GetWaterThreshold(ent.AsNullable());
        var nutrition = _plantTray.GetNutrientThreshold(ent.AsNullable());
        var alert = _plantTray.GetWeedThreshold(ent.AsNullable())
                    || _plantTray.GetToxinThreshold(ent.AsNullable())
                    || _plantTray.GetPestThreshold(ent.AsNullable());
        var health = false;
        var harvest = false;

        if (_plantTray.TryGetPlant(ent.AsNullable(), out var plantUid))
        {
            if (TryComp<PlantHolderComponent>(plantUid, out var plantHolder))
            {
                alert |= plantHolder.ImproperHeat
                         || plantHolder.ImproperPressure
                         || plantHolder.MissingGas;

                health = _plantHolder.GetHealthThreshold(plantUid.Value);
            }

            if (TryComp<PlantHarvestComponent>(plantUid, out var plantHarvest))
                harvest = plantHarvest.ReadyForHarvest;
        }

        // These are appearance keys consumed by the prototype's <see cref="GenericVisualizerComponent"/>.
        _appearance.SetData(ent.Owner, PlantTrayVisuals.HealthLight, health);
        _appearance.SetData(ent.Owner, PlantTrayVisuals.WaterLight, water);
        _appearance.SetData(ent.Owner, PlantTrayVisuals.NutritionLight, nutrition);
        _appearance.SetData(ent.Owner, PlantTrayVisuals.AlertLight, alert);
        _appearance.SetData(ent.Owner, PlantTrayVisuals.HarvestLight, harvest);
    }
}
