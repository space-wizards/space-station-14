using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Slippery;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Friction;

/// <summary>
/// This handles the bulldozing of tileFriction when we want to
/// </summary>
public sealed class TileFrictionBulldozerSystem : VirtualController
{

    [Dependency] private   readonly FixtureSystem _fixtures = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TileFrictionOverwrittenComponent, MoverTileDefEvent>(OnMoverTileDef);
        SubscribeLocalEvent<TileFrictionBulldozerComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<TileFrictionBulldozerComponent, EndCollideEvent>(OnEndCollide);
    }

    private void OnMoverTileDef(Entity<TileFrictionOverwrittenComponent> entity, ref MoverTileDefEvent args)
    {
        if (args.Handled || !CalculateTileFriction(entity))
            return;

        args.Friction = entity.Comp.Friction;
        args.MobFriction = entity.Comp.MobFriction;
        args.MobAcceleration = entity.Comp.MobAcceleration;
    }

    private void OnStartCollide(Entity<TileFrictionBulldozerComponent> entity, ref StartCollideEvent args)
    {
        var uid = args.OtherEntity;

        if (!TryComp<PhysicsComponent>(uid, out var physics))
            return;

        EnsureComp<TileFrictionOverwrittenComponent>(uid, out var tile);
    }

    private void OnEndCollide(Entity<TileFrictionBulldozerComponent> entity, ref EndCollideEvent args)
    {
        var uid = args.OtherEntity;

        if (!TryComp<PhysicsComponent>(uid, out var physics) || !TryComp<TileFrictionOverwrittenComponent>(uid, out var tile))
            return;

        if (!CalculateTileFriction((uid, tile), physics))
            RemComp<TileFrictionOverwrittenComponent>(uid);
    }

    private bool CalculateTileFriction(Entity<TileFrictionOverwrittenComponent> entity, PhysicsComponent? physics = null, EntityUid? ignore = null)
    {
        var friction = (0.0f, 0);
        var mobFriction = (0.0f, 0);
        var acceleration = (0.0f, 0);

        var contacts = PhysicsSystem.GetContacts(entity.Owner);
        var transform = PhysicsSystem.GetPhysicsTransform(entity.Owner);

        while (contacts.MoveNext(out var contact))
        {
            var other = contact.OtherEnt(entity);

            if (other == ignore || !TryComp<TileFrictionBulldozerComponent>(other, out var overrider))
                continue;

            var otherFixture = contact.OtherFixture(entity.Owner);
            var otherTransform = PhysicsSystem.GetPhysicsTransform(other);

            if (!_fixtures.TestPoint(otherFixture.Item2.Shape, otherTransform, transform.Position))
                continue;

            if (overrider.Friction.HasValue)
            {
                friction.Item1 += overrider.Friction.Value;
                friction.Item2 ++;
            }

            if (overrider.MobFriction.HasValue)
            {
                mobFriction.Item1 += overrider.MobFriction.Value;
                mobFriction.Item2 ++;
            }

            if (overrider.MobAcceleration.HasValue)
            {
                acceleration.Item1 += overrider.MobAcceleration.Value;
                acceleration.Item2 ++;
            }
        }

        if (friction.Item2 == 0 && mobFriction.Item2 == 0 && acceleration.Item2 == 0)
            return false;

        entity.Comp.Friction = friction.Item1 / friction.Item2;
        entity.Comp.MobFriction = mobFriction.Item1 / mobFriction.Item2;
        entity.Comp.MobAcceleration = acceleration.Item1 / acceleration.Item2;
        return true;
    }
}
