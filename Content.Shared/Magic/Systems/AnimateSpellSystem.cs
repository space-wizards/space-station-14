using Content.Shared.Item;
using Content.Shared.Magic.Components;
using Content.Shared.Magic.Events;
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
    [Dependency] private readonly SharedMagicSystem _magic = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AnimateSpellEvent>(OnAnimateSpell);
    }

    private void OnAnimateSpell(AnimateSpellEvent ev)
    {
        if (ev.Handled || !_magic.PassesSpellPrerequisites(ev.Action, ev.Performer))
        {
            return;
        }

        if (TryComp<ItemComponent>(ev.Target, out var item))
        {
            if (item.Size == "Small" || item.Size == "Tiny")
            {
                return;
            }
        }

        ev.Handled = true;

        EntityManager.RemoveComponents(ev.Target, ev.ToRemove);
        EntityManager.AddComponents(ev.Target, ev.ToAdd, false);

        // Physics bullshittery necessary for object to behave properly

        if (!TryComp<FixturesComponent>(ev.Target, out var fixtures) || !TryComp<PhysicsComponent>(ev.Target, out var physics))
        {
            return;
        }

        var xform = Transform(ev.Target);
        var fixture = fixtures.Fixtures.First();

        _transform.Unanchor(ev.Target); // If left anchored they are effectively stuck/immobile and not a threat
        _physics.SetCanCollide(ev.Target, true, true, false, fixtures, physics);
        _physics.SetCollisionMask(ev.Target, fixture.Key, fixture.Value, (int)CollisionGroup.FlyingMobMask, fixtures, physics);
        _physics.SetCollisionLayer(ev.Target, fixture.Key, fixture.Value, (int)CollisionGroup.FlyingMobLayer, fixtures, physics);
        _physics.SetBodyType(ev.Target, BodyType.KinematicController, fixtures, physics, xform);
        _physics.SetBodyStatus(ev.Target, physics, BodyStatus.InAir, true);
        _physics.SetFixedRotation(ev.Target, false, true, fixtures, physics);
        _physics.SetHard(ev.Target, fixture.Value, true, fixtures);
        _container.AttachParentToContainerOrGrid((ev.Target, xform)); // Items animated inside inventory now exit, they can't be picked up and so can't escape otherwise
    }
}
