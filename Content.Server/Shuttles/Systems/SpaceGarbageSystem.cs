using Content.Server.Shuttles.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Server.Shuttles.Systems;

/// <summary>
///     Deletes anything with <see cref="SpaceGarbageComponent"/> that has a cross-grid collision with a static body.
/// </summary>
public sealed class SpaceGarbageSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpaceGarbageComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<SpaceGarbageComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<SpaceGarbageComponent, StartCollideEvent>(OnCollide);
    }

    private void OnComponentInit(EntityUid uid, SpaceGarbageComponent component, ComponentInit args)
    {
        // StartCollideEvents can accumulate before TransformSystem has had a
        // chance to adequately assign a proper GridUid, if one is available.
        // Delay those until everything is ready.
        if (TryComp(uid, out PhysicsComponent? physicsComponent))
            _physicsSystem.SetCanCollide(physicsComponent, false);
    }

    private void OnComponentStartup(EntityUid uid, SpaceGarbageComponent component, ComponentStartup args)
    {
        if (TryComp(uid, out PhysicsComponent? physicsComponent))
            _physicsSystem.SetCanCollide(physicsComponent, true);
    }

    private void OnCollide(EntityUid uid, SpaceGarbageComponent component, ref StartCollideEvent args)
    {
        if (args.OtherFixture.Body.BodyType != BodyType.Static) return;

        var ourXform = Transform(args.OurFixture.Body.Owner);
        var otherXform = Transform(args.OtherFixture.Body.Owner);

        if (ourXform.GridUid == otherXform.GridUid) return;

        QueueDel(uid);
    }
}
