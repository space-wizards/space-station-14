using Content.Shared.Destructible;

namespace Content.Shared.EntityEffects.Effects.Transform;

/// <summary>
/// Destroys the entity.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class DestroyEntityEntityEffectSystem : EntityEffectSystem<TransformComponent, DestroyEntity>
{
    [Dependency] private SharedDestructibleSystem _destructible = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<DestroyEntity> args)
    {
        _destructible.DestroyEntity(entity);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class DestroyEntity : EntityEffectBase<DestroyEntity>;
