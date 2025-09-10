using Content.Server.Explosion.EntitySystems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.NewEffects.Transform;

namespace Content.Server.EntityEffects.Effects.Transform;

public sealed partial class ExplosionEntityEffectSystem : SharedExplosionEntityEffectSystem
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
