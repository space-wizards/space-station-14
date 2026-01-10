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
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<PlantVisualsComponent, PlantComponent, PlantHarvestComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out _, out var plant, out var harvest, out var sprite))
        {
            UpdateSprite((uid, plant), harvest, sprite);
        }
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

    private void UpdateSprite(Entity<PlantComponent> ent, PlantHarvestComponent harvest, SpriteComponent sprite)
    {
        string state;

        var dead = _plantHolder.IsDead(ent.Owner);
        var harvestReady = harvest.ReadyForHarvest;
        var growthStage = _plantSystem.GetGrowthStageValue(ent.AsNullable());

        if (dead)
            state = "dead";
        else if (harvestReady)
            state = "harvest";
        else
            state = $"stage-{growthStage}";

        SpriteSystem.LayerSetVisible((ent.Owner, sprite), PlantLayers.Plant, true);
        SpriteSystem.LayerSetRsiState((ent.Owner, sprite), PlantLayers.Plant, state);
    }
}

public enum PlantLayers : byte
{
    Plant
}
