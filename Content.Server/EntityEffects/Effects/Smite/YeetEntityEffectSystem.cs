using Content.Shared.EntityEffects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Server.EntityEffects.Effects.Smite;

public sealed partial class YeetEntityEffectSystem : EntityEffectSystem<PhysicsComponent, Yeet>
{
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    protected override void Effect(Entity<PhysicsComponent> entity, ref EntityEffectEvent<Yeet> args)
    {
        if (!TryComp<FixturesComponent>(entity, out var fixtures))
            return;

        _transform.Unanchor(entity);
        _physics.SetBodyType(entity, BodyType.Dynamic, body: entity.Comp);
        _physics.SetBodyStatus(entity, entity.Comp, BodyStatus.InAir);
        _physics.WakeBody(entity, manager: fixtures, body: entity.Comp);

        foreach (var fixture in fixtures.Fixtures.Values)
        {
            _physics.SetHard(entity, fixture, false, fixtures);
        }

        _physics.SetLinearVelocity(entity, _random.NextVector2(8f, 8f), manager: fixtures, body: entity.Comp);
        _physics.SetAngularVelocity(entity, MathF.PI * 12, manager: fixtures, body: entity.Comp);
        _physics.SetLinearDamping(entity, entity.Comp, 0f);
        _physics.SetAngularDamping(entity, entity.Comp, 0f);
    }
}

public sealed partial class Yeet : EntityEffectBase<Yeet>;
