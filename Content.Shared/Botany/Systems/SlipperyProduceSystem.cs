using Content.Shared.Botany.Components;
using Content.Shared.Slippery;
using Content.Shared.StepTrigger.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Botany.Systems;

/// <summary>
/// Makes produce slippery!
/// </summary>
public sealed class SlipperyProduceSystem : EntitySystem
{
    [Dependency] private readonly CollisionWakeSystem _wake = default!;
    [Dependency] private readonly FixtureSystem _fixture = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlipperyProduceComponent, PlantCopyTraitsEvent>(OnCopyTraits);
        SubscribeLocalEvent<SlipperyProduceComponent, ProduceCreatedEvent>(OnProduceCreated);
    }

    // TODO: mutation

    private void OnCopyTraits(Entity<SlipperyProduceComponent> ent, ref PlantCopyTraitsEvent args)
    {
        EnsureComp<SlipperyProduceComponent>(args.Plant);
    }

    private void OnProduceCreated(Entity<SlipperyProduceComponent> ent, ref ProduceCreatedEvent args)
    {
        var uid = args.Produce;
        var slippery = EnsureComp<SlipperyComponent>(uid);
        Dirty(uid, slippery);
        EnsureComp<StepTriggerComponent>(uid);
        // Need a fixture with a slip layer in order to actually do the slipping
        var fixtures = EnsureComp<FixturesComponent>(uid);
        var body = EnsureComp<PhysicsComponent>(uid);
        var shape = fixtures.Fixtures["fix1"].Shape;
        _fixture.TryCreateFixture(uid, shape, "slips", 1, false, (int) CollisionGroup.SlipLayer, manager: fixtures, body: body);
        // Need to disable collision wake so that mobs can collide with and slip on it
        var wake = EnsureComp<CollisionWakeComponent>(uid);
        _wake.SetEnabled(uid, false, wake);
    }
}
