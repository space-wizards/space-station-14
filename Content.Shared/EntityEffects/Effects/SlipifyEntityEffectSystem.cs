using Content.Shared.Physics;
using Content.Shared.Slippery;
using Content.Shared.StepTrigger.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.EntityEffects.Effects;

public sealed partial class SlipifyEntityEffectSystem : EntityEffectSystem<FixturesComponent, Slipify>
{
    [Dependency] private readonly CollisionWakeSystem _collisionWake = default!;
    [Dependency] private readonly FixtureSystem _fixture = default!;

    protected override void Effect(Entity<FixturesComponent> entity, ref EntityEffectEvent<Slipify> args)
    {
        EnsureComp<SlipperyComponent>(entity, out var slippery);
        slippery.SlipData = args.Effect.Slippery;

        Dirty(entity, slippery);

        EnsureComp<StepTriggerComponent>(entity);

        // TODO: This is fucking cursed but it's worked fine for now. Someone else needs to make this not hot ass.
        var shape = entity.Comp.Fixtures["fix1"].Shape;
        _fixture.TryCreateFixture(entity, shape, "slips", 1, false, (int)CollisionGroup.SlipLayer, manager: entity.Comp);

        // Need to disable collision wake so that mobs can collide with and slip on it
        EnsureComp<CollisionWakeComponent>(entity, out var collisionWake);
        _collisionWake.SetEnabled(entity, false, collisionWake);
    }
}

public sealed partial class Slipify : EntityEffectBase<Slipify>
{
    [DataField]
    public SlipperyEffectEntry Slippery = new();
}
