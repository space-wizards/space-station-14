using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Shared.EntityEffects;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
/// Polymorphs this entity into another entity.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PolymorphEntityEffectSystem : EntityEffectSystem<MetaDataComponent, Shared.EntityEffects.Effects.Polymorph>
{
    [Dependency] private PolymorphSystem _polymorph = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<Shared.EntityEffects.Effects.Polymorph> args)
    {
        if (!args.Effect.Force && !HasComp<PolymorphableComponent>(entity))
            return;

        _polymorph.PolymorphEntity(entity, args.Effect.Prototype);
    }
}
