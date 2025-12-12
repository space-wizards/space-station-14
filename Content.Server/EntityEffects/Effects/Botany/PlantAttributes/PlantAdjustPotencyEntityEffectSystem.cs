using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

namespace Content.Server.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that sets plant potency.
/// </summary>
public sealed partial class PlantAdjustPotencyEntityEffectSystem : EntityEffectSystem<PlantHolderComponent, PlantAdjustPotency>
{
    [Dependency] private readonly PlantTraitsSystem _plantTraits = default!;

    protected override void Effect(Entity<PlantHolderComponent> entity, ref EntityEffectEvent<PlantAdjustPotency> args)
    {
        if (entity.Comp.Seed == null || entity.Comp.Dead)
            return;

        if (!TryComp<PlantTraitsComponent>(entity, out var traits))
            return;

        _plantTraits.AdjustPotency((entity.Owner, traits), args.Effect.Amount);
    }
}
