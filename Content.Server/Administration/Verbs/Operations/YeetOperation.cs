using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Server.Administration.Verbs.Operations;

public sealed partial class AdminOperationSystem
{
    [SubscribeLocalEvent]
    private void OnYeet(Entity<PhysicsComponent> entity, ref AdminOperationEvent<YeetOperation> args)
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

public sealed partial class YeetOperation : AdminOperationBase<YeetOperation>;
