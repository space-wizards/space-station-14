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
    [Dependency] private ExplosionSystem _explosion = default!;

    protected override void Effect(Entity<TransformComponent> entity, ExplosionEffect effect, EntityEffectData data)
    {
        var intensity = MathF.Min(effect.IntensityPerUnit * data.Scale, effect.MaxTotalIntensity);

        _explosion.QueueExplosion(
            entity,
            effect.ExplosionType,
            intensity,
            effect.IntensitySlope,
            effect.MaxIntensity,
            effect.TileBreakScale);
    }
}
