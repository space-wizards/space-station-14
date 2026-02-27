using Content.Server.Explosion.EntitySystems;
using Content.Shared.EntityEffects;
using ExplosionEffect = Content.Shared.EntityEffects.Effects.Transform.Explosion;

namespace Content.Server.EntityEffects.Effects.Transform;

/// <summary>
/// Creates an explosion at this entity's position.
/// Intensity is modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class ExplosionEntityEffectSystem : EntityEffectSystem<TransformComponent, ExplosionEffect>
{
    [Dependency] private readonly ExplosionSystem _explosion = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<ExplosionEffect> args)
    {
        var intensity = MathF.Min(args.Effect.IntensityPerUnit * args.Scale, args.Effect.MaxTotalIntensity);

        _explosion.QueueExplosion(
            entity,
            args.Effect.ExplosionType,
            intensity,
            args.Effect.IntensitySlope,
            args.Effect.MaxIntensity,
            args.Effect.TileBreakScale);
    }
}
