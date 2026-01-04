using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;

namespace Content.Server.EntityEffects.Effects.Botany;

/// <summary>
/// Entity effect that adds or removes a plant trait.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantChangeTraitsEntityEffectSystem : EntityEffectSystem<PlantComponent, PlantChangeTraits>
{
    [Dependency] private readonly PlantTraitsSystem _plantTraits = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantChangeTraits> args)
    {
        // if (_plantHolder.IsDead(entity.Owner))
        //     return;

        // var trait = args.Effect.Trait;
        // if (args.Effect.Remove)
        //     _plantTraits.DelTrait(entity.Owner, trait);
        // else
        //     _plantTraits.AddTrait(entity.Owner, trait);
    }
}
