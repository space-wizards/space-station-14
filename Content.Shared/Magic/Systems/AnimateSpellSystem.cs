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
        SubscribeLocalEvent<AnimateComponent, MapInitEvent>(OnAnimate);
    }

    private void OnAnimate(Entity<AnimateComponent> ent, ref MapInitEvent args)
    {
        // Physics bullshittery necessary for object to behave properly

        if (!TryComp<FixturesComponent>(ent, out var fixtures) || !TryComp<PhysicsComponent>(ent, out var physics))
            return;

        var xform = Transform(ent);
        var fixture = fixtures.Fixtures.First();

        _transform.Unanchor(ent); // If left anchored they are effectively stuck/immobile and not a threat
        _physics.SetCanCollide(ent, true, true, false, fixtures, physics);
        _physics.SetCollisionMask(ent, fixture.Key, fixture.Value, (int)CollisionGroup.FlyingMobMask, fixtures, physics);
        _physics.SetCollisionLayer(ent, fixture.Key, fixture.Value, (int)CollisionGroup.FlyingMobLayer, fixtures, physics);
        _physics.SetBodyType(ent, BodyType.KinematicController, fixtures, physics, xform);
        _physics.SetBodyStatus(ent, physics, BodyStatus.InAir, true);
        _physics.SetFixedRotation(ent, false, true, fixtures, physics);
        _physics.SetHard(ent, fixture.Value, true, fixtures);
        _container.AttachParentToContainerOrGrid((ent, xform)); // Items animated inside inventory now exit, they can't be picked up and so can't escape otherwise

        var ev = new AnimateSpellEvent();
        RaiseLocalEvent(ent, ref ev);
    }
}

[ByRefEvent]
public readonly record struct AnimateSpellEvent;
