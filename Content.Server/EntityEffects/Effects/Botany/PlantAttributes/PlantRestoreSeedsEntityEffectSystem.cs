using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Server.Popups;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

namespace Content.Server.EntityEffects.Effects.Botany.PlantAttributes;

public sealed partial class PlantRestoreSeedsEntityEffectSystem : EntityEffectSystem<PlantHolderComponent, PlantRestoreSeeds>
{
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    protected override void Effect(Entity<PlantHolderComponent> entity, ref EntityEffectEvent<PlantRestoreSeeds> args)
    {
        if (entity.Comp.Seed == null || entity.Comp.Dead || entity.Comp.Seed.Immutable)
            return;

        if (!entity.Comp.Seed.Seedless)
            return;

        _plantHolder.EnsureUniqueSeed(entity, entity.Comp);
        _popup.PopupEntity(Loc.GetString("botany-plant-seedsrestored"), entity);
        entity.Comp.Seed.Seedless = false;
    }
}
