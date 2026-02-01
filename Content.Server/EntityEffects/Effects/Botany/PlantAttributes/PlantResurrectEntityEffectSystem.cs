using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

namespace Content.Server.EntityEffects.Effects.Botany.PlantAttributes;
/// <summary>
/// Plant Entityeffect that revives it from a dead state.
/// </summary>
public sealed partial class PlantResurrectEntityEffectSystem : EntityEffectSystem<PlantHolderComponent, PlantResurrect>
{
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    protected override void Effect(Entity<PlantHolderComponent> entity, ref EntityEffectEvent<PlantResurrect> args)
    {
        if (entity.Comp.Seed == null)
            return;

        entity.Comp.Dead = false;

        if (entity.Comp.Health <= 0) //so that it doesn't immediately die again
        {
            entity.Comp.Health = 1;
            if (args.Effect.ReviveSeedless)
                entity.Comp.Seed.Seedless = true; // revival makes plant sterile
        }
        _plantHolder.CheckHealth(entity, entity.Comp);
        entity.Comp.UpdateSpriteAfterUpdate = true;
    }
}
