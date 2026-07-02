using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Shared.EntityEffects;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
/// Polymorphs this entity into another entity.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PolymorphEntityEffectSystem : EntityEffectSystem<PolymorphableComponent, Shared.EntityEffects.Effects.Polymorph>
{
    [Dependency] private PolymorphSystem _polymorph = default!;

     protected override void Effect(Entity<PolymorphableComponent> entity, Shared.EntityEffects.Effects.Polymorph effect, EntityEffectData data)
    {
        _polymorph.PolymorphEntity(entity, effect.Prototype);
    }
}
