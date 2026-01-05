using Content.Client.Botany.Components;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;
using Robust.Client.GameObjects;

namespace Content.Client.Botany;

public sealed class PlantVisualizerSystem : VisualizerSystem<PlantVisualsComponent>
{
    [Dependency] private readonly PlantSystem _plantSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlantVisualsComponent, ComponentInit>(OnComponentInit);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<PlantVisualsComponent, PlantComponent, PlantHolderComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out _, out var plant, out var holder, out var sprite))
        {
            UpdateSprite((uid, plant), holder, sprite);
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

    private void UpdateSprite(Entity<PlantComponent> ent, PlantHolderComponent holder, SpriteComponent sprite)
    {
        string state;

        if (holder.Dead)
            state = "dead";
        else if (TryComp<PlantHarvestComponent>(ent.Owner, out var harvest) && harvest.ReadyForHarvest)
            state = "harvest";
        else if (holder.Age < ent.Comp.Maturation)
            state = $"stage-{Math.Max(1, _plantSystem.GetGrowthStageValue(ent.AsNullable()))}";
        else
            state = $"stage-{ent.Comp.GrowthStages}";

        SpriteSystem.LayerSetVisible((ent.Owner, sprite), PlantLayers.Plant, true);
        SpriteSystem.LayerSetRsiState((ent.Owner, sprite), PlantLayers.Plant, state);
    }
}

public enum PlantLayers : byte
{
    Plant
}
