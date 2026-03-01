using Content.Client.Botany.Components;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;
using Robust.Client.GameObjects;

namespace Content.Client.Botany;

public sealed class PlantVisualizerSystem : VisualizerSystem<PlantVisualsComponent>
{
    [Dependency] private readonly PlantSystem _plantSystem = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlantVisualsComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<PlantVisualsComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<PlantComponent, AfterAutoHandleStateEvent>(OnPlantState);
        SubscribeLocalEvent<PlantHarvestComponent, AfterAutoHandleStateEvent>(OnHarvestState);
        SubscribeLocalEvent<PlantHolderComponent, AfterAutoHandleStateEvent>(OnHolderState);
    }

    private void OnComponentInit(EntityUid uid, PlantVisualsComponent component, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        // Ensure they always render above the tray sprite.
        SpriteSystem.SetDrawDepth((uid, sprite), (int)DrawDepth.SmallObjects);
        SpriteSystem.LayerMapReserve((uid, sprite), PlantLayers.Plant);
        SpriteSystem.LayerSetVisible((uid, sprite), PlantLayers.Plant, false);
    }

    private void OnComponentStartup(Entity<PlantVisualsComponent> ent, ref ComponentStartup args)
    {
        UpdateSprite(ent.Owner);
    }

    private void OnPlantState(Entity<PlantComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateSprite(ent.Owner);
    }

    private void OnHarvestState(Entity<PlantHarvestComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateSprite(ent.Owner);
    }

    private void OnHolderState(Entity<PlantHolderComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateSprite(ent.Owner);
    }

    private void UpdateSprite(EntityUid plantUid)
    {
        if (!HasComp<PlantVisualsComponent>(plantUid)
            || !TryComp<PlantHarvestComponent>(plantUid, out var harvest)
            || !TryComp<SpriteComponent>(plantUid, out var sprite))
        {
            return;
        }

        string state;

        var dead = _plantHolder.IsDead(plantUid);
        var harvestReady = harvest.ReadyForHarvest;
        var growthStage = _plantSystem.GetGrowthStageValue(plantUid);

        if (dead)
            state = "dead";
        else if (harvestReady)
            state = "harvest";
        else
            state = $"stage-{growthStage}";

        var layer = SpriteSystem.LayerMapReserve((plantUid, sprite), PlantLayers.Plant);
        SpriteSystem.LayerSetVisible((plantUid, sprite), layer, true);
        SpriteSystem.LayerSetRsiState((plantUid, sprite), layer, state);
    }
}

public enum PlantLayers : byte
{
    Plant
}
