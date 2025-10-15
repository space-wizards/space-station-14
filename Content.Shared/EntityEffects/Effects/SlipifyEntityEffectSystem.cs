using System.Linq;
using Content.Shared.Physics;
using Content.Shared.Slippery;
using Content.Shared.StepTrigger.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// This effect permanently creates a slippery fixture for this entity and then makes this entity slippery like soap.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
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

        if (entity.Comp.Fixtures.FirstOrDefault(x => x.Value.Hard).Value.Shape is not { } shape)
            return;

        _fixture.TryCreateFixture(entity, shape, "slips", 1, false, (int)CollisionGroup.SlipLayer, manager: entity.Comp);

        // Need to disable collision wake so that mobs can collide with and slip on it
        EnsureComp<CollisionWakeComponent>(entity, out var collisionWake);
        _collisionWake.SetEnabled(entity, false, collisionWake);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class Slipify : EntityEffectBase<Slipify>
{
    [DataField]
    public SlipperyEffectEntry Slippery = new();
}
