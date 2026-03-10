using Content.Server.Explosion.EntitySystems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects;
using Content.Shared.Explosion.Components;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
/// Makes this entity explode using its <see cref="ExplosiveComponent"/>.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class ExplodeEntityEffectSystem : EntityEffectSystem<ExplosiveComponent, Explode>
{
    [Dependency] private readonly ExplosionSystem _explosion = default!;

    protected override void Effect(Entity<ExplosiveComponent> entity, ref EntityEffectEvent<Explode> args)
    {
        _explosion.TriggerExplosive(entity, entity, args.Effect.Delete, args.Effect.Intensity, args.Effect.Radius, args.User);
    }
}
