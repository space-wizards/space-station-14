using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;
using Robust.Shared.Random;

namespace Content.Server.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that enhances plant longevity and endurance.
/// </summary>
public sealed partial class PlantDiethylamineEntityEffectSystem : EntityEffectSystem<PlantHolderComponent, PlantDiethylamine>
{
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void Effect(Entity<PlantHolderComponent> entity, ref EntityEffectEvent<PlantDiethylamine> args)
    {
        if (entity.Comp.Seed == null || entity.Comp.Dead || entity.Comp.Seed.Immutable)
            return;

        if(!TryComp<PlantTraitsComponent>(entity, out var traits))
            return;

        if (_random.Prob(0.1f))
            traits.Lifespan++;

        if (_random.Prob(0.1f))
            traits.Endurance++;
    }
}
