using Content.Shared.Interaction.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Random.Helpers;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.EntityEffects.Effects.Smite;

/// <summary>
/// Launches this entity as a spinning dynamic physics body.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T, TEffect}"/>
public sealed partial class FlingEntityEffectSystem : EntityEffectSystem<PhysicsComponent, Fling>
{
    [Dependency] private FixtureSystem _fixtures = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    protected override void Effect(Entity<PhysicsComponent> entity, ref EntityEffectEvent<Fling> args)
    {
        if (!TryComp<FixturesComponent>(entity, out var fixtures))
            return;

        // Input movers must stop using mob movement, otherwise the mover controller asserts on dynamic bodies.
        if (HasComp<InputMoverComponent>(entity))
            EnsureComp<BlockMovementComponent>(entity);

        var fling = args.Effect;

        _transform.Unanchor(entity);
        _physics.SetBodyType(entity, BodyType.Dynamic, fixtures, entity.Comp);
        _physics.SetBodyStatus(entity, entity.Comp, BodyStatus.InAir);
        _physics.WakeBody(entity, manager: fixtures, body: entity.Comp);

        foreach (var fixture in fixtures.Fixtures.Values)
        {
            if (fling.NonSolid)
                _physics.SetHard(entity, fixture, false, fixtures);

            if (fling.Restitution.HasValue && fixture.Hard)
                _physics.SetRestitution(entity, fixture, fling.Restitution.Value, false, fixtures);
        }

        if (fling.Restitution.HasValue)
            _fixtures.FixtureUpdate(entity, manager: fixtures, body: entity.Comp);

        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(entity));
        _physics.SetLinearVelocity(entity, random.NextVector2(fling.Speed, fling.Speed), manager: fixtures, body: entity.Comp);
        _physics.SetAngularVelocity(entity, fling.AngularVelocity, manager: fixtures, body: entity.Comp);
        _physics.SetLinearDamping(entity, entity.Comp, 0f);
        _physics.SetAngularDamping(entity, entity.Comp, 0f);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class Fling : EntityEffectBase<Fling>
{
    /// <summary>
    /// Maximum magnitude of the random linear velocity applied on launch.
    /// </summary>
    [DataField]
    public float Speed = 1.5f;

    /// <summary>
    /// Angular velocity applied on launch in radians per second.
    /// </summary>
    [DataField]
    public float AngularVelocity = MathF.PI * 12f;

    /// <summary>
    /// Restitution applied to hard fixtures, making the entity bounce.
    /// </summary>
    [DataField]
    public float? Restitution = 1.1f;

    /// <summary>
    /// Make all fixtures non-solid so the entity passes through obstacles.
    /// </summary>
    [DataField]
    public bool NonSolid;
}
