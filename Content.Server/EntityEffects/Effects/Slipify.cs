using Content.Server.Atmos.EntitySystems;
using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Content.Shared.Physics;
using Content.Shared.Slippery;
using Content.Shared.StepTrigger.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
///     Makes a mob slippery.
/// </summary>
public sealed partial class Slipify : EntityEffect
{
    public override void Effect(EntityEffectBaseArgs args)
    {
        var _fixtureSystem = args.EntityManager.System<FixtureSystem>();
        var _colWakeSystem = args.EntityManager.System<CollisionWakeSystem>();
        var slippery = args.EntityManager.EnsureComponent<SlipperyComponent>(args.TargetEntity);
        args.EntityManager.Dirty(args.TargetEntity, slippery);
        args.EntityManager.EnsureComponent<StepTriggerComponent>(args.TargetEntity);
        // Need a fixture with a slip layer in order to actually do the slipping
        var fixtures = args.EntityManager.EnsureComponent<FixturesComponent>(args.TargetEntity);
        var body = args.EntityManager.EnsureComponent<PhysicsComponent>(args.TargetEntity);
        var shape = fixtures.Fixtures["fix1"].Shape;
        _fixtureSystem.TryCreateFixture(args.TargetEntity, shape, "slips", 1, false, (int)CollisionGroup.SlipLayer, manager: fixtures, body: body);
        // Need to disable collision wake so that mobs can collide with and slip on it
        var collisionWake = args.EntityManager.EnsureComponent<CollisionWakeComponent>(args.TargetEntity);
        _colWakeSystem.SetEnabled(args.TargetEntity, false, collisionWake);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        throw new NotImplementedException();
    }
}
