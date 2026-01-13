using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

namespace Content.Server.EntityEffects.Effects.Botany.PlantAttributes;

public sealed partial class PlantAdjustWaterEntityEffectSystem : EntityEffectSystem<PlantHolderComponent, PlantAdjustWater>
{
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;

    protected override void Effect(Entity<PlantHolderComponent> entity, ref EntityEffectEvent<PlantAdjustWater> args)
    {
        _plantHolder.AdjustWater(entity, args.Effect.Amount, entity.Comp);
    }
}
