using Content.Shared.EntityEffects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Server.EntityEffects.Effects.Smite;

public sealed partial class PinballEntityEffectSystem : EntityEffectSystem<PhysicsComponent, Pinball>
{
    [Dependency] private FixtureSystem _fixtures = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    protected override void Effect(Entity<PhysicsComponent> entity, ref EntityEffectEvent<Pinball> args)
    {
        if (!TryComp<FixturesComponent>(entity, out var fixtures))
            return;

        _transform.Unanchor(entity);
        _physics.SetBodyType(entity, BodyType.Dynamic, fixtures, entity.Comp);
        _physics.SetBodyStatus(entity, entity.Comp, BodyStatus.InAir);
        _physics.WakeBody(entity, manager: fixtures, body: entity.Comp);

        foreach (var fixture in fixtures.Fixtures.Values)
        {
            if (fixture.Hard)
                _physics.SetRestitution(entity, fixture, 1.1f, false, fixtures);
        }

        _fixtures.FixtureUpdate(entity, manager: fixtures, body: entity.Comp);
        _physics.SetLinearVelocity(entity, _random.NextVector2(1.5f, 1.5f), manager: fixtures, body: entity.Comp);
        _physics.SetAngularVelocity(entity, MathF.PI * 12, manager: fixtures, body: entity.Comp);
        _physics.SetLinearDamping(entity, entity.Comp, 0f);
        _physics.SetAngularDamping(entity, entity.Comp, 0f);
    }
}

public sealed partial class Pinball : EntityEffectBase<Pinball>;
