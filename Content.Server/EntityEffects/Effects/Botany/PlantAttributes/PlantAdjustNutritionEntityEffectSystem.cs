using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

namespace Content.Server.EntityEffects.Effects.Botany.PlantAttributes;

public sealed partial class PlantAdjustNutritionEntityEffectSystem : EntityEffectSystem<PlantHolderComponent, PlantAdjustNutrition>
{
    [Dependency] private PlantHolderSystem _plantHolder = default!;

    protected override void Effect(Entity<PlantHolderComponent> entity, PlantAdjustNutrition effect, EntityEffectData data)
    {
        _plantHolder.AdjustNutrient(entity, effect.Amount, entity);
    }
}
