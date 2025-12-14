using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

namespace Content.Server.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that mutates plant to lose health with time.
/// </summary>
public sealed partial class PlantPhalanximineEntityEffectSystem : EntityEffectSystem<PlantHolderComponent, PlantPhalanximine>
{
    protected override void Effect(Entity<PlantHolderComponent> entity, ref EntityEffectEvent<PlantPhalanximine> args)
    {
        if (entity.Comp.Seed == null || entity.Comp.Dead || entity.Comp.Seed.Immutable)
            return;

        if (TryComp<PlantTraitsComponent>(entity, out var traits))
            traits.Viable = true;
    }
}
