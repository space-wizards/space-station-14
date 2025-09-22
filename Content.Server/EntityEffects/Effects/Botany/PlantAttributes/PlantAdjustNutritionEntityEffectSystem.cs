using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.NewEffects.Botany.PlantAttributes;

namespace Content.Server.EntityEffects.Effects.Botany.PlantAttributes;

public sealed partial class PlantAdjustNutritionEntityEffectSystem : EntityEffectSystem<PlantHolderComponent, Shared.EntityEffects.Effects.Botany.PlantAttributes.PlantAdjustNutrition>
{
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;

    protected override void Effect(Entity<PlantHolderComponent> entity, ref EntityEffectEvent<Shared.EntityEffects.Effects.Botany.PlantAttributes.PlantAdjustNutrition> args)
    {
        _plantHolder.AdjustNutrient(entity, args.Effect.Amount, entity);
    }
}
