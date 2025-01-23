using System.Numerics;
using Content.Shared.Movement.Components;
using Content.Shared.Physics;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Movement.Systems;

public abstract class SharedMobCollisionSystem : EntitySystem
{
    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _xformSystem = default!;

    protected EntityQuery<MobCollisionComponent> MobQuery;
    protected EntityQuery<PhysicsComponent> PhysicsQuery;

    public override void Initialize()
    {
        base.Initialize();
        MobQuery = GetEntityQuery<MobCollisionComponent>();
        PhysicsQuery = GetEntityQuery<PhysicsComponent>();
        SubscribeAllEvent<MobCollisionMessage>(OnCollision);
        SubscribeLocalEvent<MobCollisionComponent, ComponentStartup>(OnCollisionStartup);
    }

    private void OnCollisionStartup(Entity<MobCollisionComponent> ent, ref ComponentStartup args)
    {
        _fixtures.TryCreateFixture(ent.Owner,
            ent.Comp.Shape,
            "mob_collision",
            hard: false,
            collisionLayer: (int) CollisionGroup.MidImpassable,
            collisionMask: (int) CollisionGroup.MidImpassable);
    }

    private void OnCollision(MobCollisionMessage msg, EntitySessionEventArgs args)
    {
        var player = args.SenderSession.AttachedEntity;

        if (!MobQuery.HasComp(player))
            return;

        // TODO: Validation
        MoveMob(player.Value, msg.Direction);
    }

    protected void MoveMob(EntityUid uid, Vector2 direction)
    {
        _physics.ApplyLinearImpulse(uid, direction);

        return;
        // TODO: If we do the localpos method then shapecast it.
        // TODO: Also try updatebeforesolve + using linearimpulse = mass or w/e
        var xform = Transform(uid);
        _xformSystem.SetLocalPosition(uid, xform.LocalPosition + direction);
    }

    protected void HandleCollisions(Entity<MobCollisionComponent, PhysicsComponent> entity, float frameTime)
    {
        var physics = entity.Comp2;

        if (physics.ContactCount == 0)
            return;

        var xform = Transform(entity.Owner);
        var (worldPos, worldRot) = _xformSystem.GetWorldPositionRotation(entity.Owner);
        var ourTransform = new Transform(worldPos, worldRot);
        var contacts = _physics.GetContacts(entity.Owner);
        var direction = Vector2.Zero;

        while (contacts.MoveNext(out var contact))
        {
            if (!contact.IsTouching)
                continue;

            var other = contact.OtherEnt(entity.Owner);

            if (!MobQuery.TryComp(other, out var otherComp))
                continue;

            // TODO: Get overlap amount
            var otherTransform = _physics.GetPhysicsTransform(other);
            var diff = ourTransform.Position - otherTransform.Position;
            var penDepth = MathF.Max(0f, 0.7f - diff.LengthSquared());

            // Sum the strengths so we get pushes back the same amount (impulse-wise, ignoring prediction).
            var mobMovement = penDepth * diff.Normalized() * (entity.Comp1.Strength + otherComp.Strength);

            // Need the push strength proportional to penetration depth.
            direction += mobMovement;
        }

        if (direction == Vector2.Zero)
            return;

        var parentAngle = worldRot - xform.LocalRotation;
        // var localDir = (-parentAngle).RotateVec(direction);
        RaiseCollisionEvent(entity.Owner, direction);
    }

    protected abstract void RaiseCollisionEvent(EntityUid uid, Vector2 direction);

    [Serializable, NetSerializable]
    protected sealed class MobCollisionMessage : EntityEventArgs
    {
        public Vector2 Direction;
    }
}
