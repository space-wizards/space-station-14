using Content.Shared.Item;
using Content.Shared.Magic.Components;
using Content.Shared.Physics;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using System.Linq;

namespace Content.Shared.Magic.Systems;

public sealed class AnimateSpellSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AnimateComponent, ComponentStartup>(OnAnimate);
        SubscribeLocalEvent<AnimateComponent, GettingPickedUpAttemptEvent>(OnPickupAttempt);
    }

    private void OnAnimate(EntityUid uid, AnimateComponent comp, ComponentStartup args)
    {
        // Physics bullshittery necessary for object to behave properly

        if (!TryComp<FixturesComponent>(uid, out var fixtures) || !TryComp<PhysicsComponent>(uid, out var physics))
            return;

        var xform = Transform(uid);
        var fixture = fixtures.Fixtures.First();

        _transform.Unanchor(uid); // If left anchored they are effectively stuck/immobile and not a threat
        _physics.SetCanCollide(uid, true, true, false, fixtures, physics);
        _physics.SetCollisionMask(uid, fixture.Key, fixture.Value, (int)CollisionGroup.FlyingMobMask, fixtures, physics);
        _physics.SetCollisionLayer(uid, fixture.Key, fixture.Value, (int)CollisionGroup.FlyingMobLayer, fixtures, physics);
        _physics.SetBodyType(uid, BodyType.KinematicController, fixtures, physics, xform);
        _physics.SetBodyStatus(uid, physics, BodyStatus.InAir, true);
        _physics.SetFixedRotation(uid, false, true, fixtures, physics);
        _physics.SetHard(uid, fixture.Value, true, fixtures);
        _container.AttachParentToContainerOrGrid((uid, xform)); // Items animated inside inventory now exit, they can't be picked up and so can't escape otherwise

        var ev = new AnimateSpellEvent();
        RaiseLocalEvent(ref ev);
    }

    private void OnPickupAttempt(EntityUid uid, AnimateComponent comp, GettingPickedUpAttemptEvent args)
    {
        args.Cancel();
    }
}

[ByRefEvent]
public readonly record struct AnimateSpellEvent;
