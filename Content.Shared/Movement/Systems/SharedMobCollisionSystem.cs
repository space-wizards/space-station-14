using System.Numerics;
using Content.Shared.Movement.Components;
using Content.Shared.Physics;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Movement.Systems;

public abstract class SharedMobCollisionSystem : EntitySystem
{
    [Dependency] private   readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _moveSpeed = default!;
    [Dependency] protected readonly SharedPhysicsSystem Physics = default!;
    [Dependency] private   readonly SharedTransformSystem _xformSystem = default!;

    protected EntityQuery<MobCollisionComponent> MobQuery;
    protected EntityQuery<PhysicsComponent> PhysicsQuery;

    public override void Initialize()
    {
        base.Initialize();
        MobQuery = GetEntityQuery<MobCollisionComponent>();
        PhysicsQuery = GetEntityQuery<PhysicsComponent>();
        SubscribeAllEvent<MobCollisionMessage>(OnCollision);
        SubscribeAllEvent<MobCollisionToggleMessage>(OnCollisionToggle);
        SubscribeLocalEvent<MobCollisionComponent, ComponentStartup>(OnCollisionStartup);
        SubscribeLocalEvent<MobCollisionComponent, RefreshMovementSpeedModifiersEvent>(OnMoveSpeed);

        UpdatesAfter.Add(typeof(SharedPhysicsSystem));
    }

    private void OnCollisionToggle(MobCollisionToggleMessage msg, EntitySessionEventArgs args)
    {
        var player = args.SenderSession.AttachedEntity;

        if (!MobQuery.TryComp(player, out var comp))
            return;

        SetColliding((player.Value, comp), value: msg.Enabled, update: false);
        _moveSpeed.RefreshMovementSpeedModifiers(player.Value);
    }

    private void OnCollision(MobCollisionMessage msg, EntitySessionEventArgs args)
    {
        var player = args.SenderSession.AttachedEntity;

        if (!MobQuery.TryComp(player, out var comp))
            return;

        // TODO: Validation
        MoveMob((player.Value, comp), msg.Direction);
    }

    private void OnMoveSpeed(Entity<MobCollisionComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!ent.Comp.Colliding)
            return;

        args.ModifySpeed(0.25f);
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

    protected void SetColliding(Entity<MobCollisionComponent> entity, bool value, bool update = false)
    {
        if (entity.Comp.Colliding == value)
            return;

        _moveSpeed.RefreshMovementSpeedModifiers(entity.Owner);
        entity.Comp.Colliding = value;
        Dirty(entity);

        if (!update)
            return;

        if (IoCManager.Resolve<INetManager>().IsClient)
        {
            RaisePredictiveEvent(new MobCollisionToggleMessage()
            {
                Enabled = value,
            });
        }
        else
        {
            RaiseLocalEvent(entity.Owner, new MobCollisionToggleMessage()
            {
                Enabled = value,
            });
        }
    }

    protected void MoveMob(Entity<MobCollisionComponent> entity, Vector2 direction)
    {
        // TODO: If we do the localpos method then shapecast it.
        // TODO: Also try updatebeforesolve + using linearimpulse = mass or w/e
        var xform = Transform(entity.Owner);
        _xformSystem.SetLocalPosition(entity.Owner, xform.LocalPosition + direction);
    }

    protected bool HandleCollisions(Entity<MobCollisionComponent, PhysicsComponent> entity, float frameTime)
    {
        var physics = entity.Comp2;

        if (physics.LinearVelocity == Vector2.Zero || physics.ContactCount == 0)
            return false;

        var xform = Transform(entity.Owner);
        var (worldPos, worldRot) = _xformSystem.GetWorldPositionRotation(entity.Owner);
        var ourTransform = new Transform(worldPos, worldRot);
        var contacts = Physics.GetContacts(entity.Owner);
        var direction = Vector2.Zero;

        while (contacts.MoveNext(out var contact))
        {
            if (!contact.IsTouching)
                continue;

            var other = contact.OtherEnt(entity.Owner);

            if (!MobQuery.TryComp(other, out var otherComp))
                continue;

            // TODO: Get overlap amount
            var otherTransform = Physics.GetPhysicsTransform(other);
            var diff = ourTransform.Position - otherTransform.Position;
            var penDepth = MathF.Max(0f, 0.6f - diff.Length());

            var mobMovement = penDepth * diff.Normalized() * (entity.Comp1.Strength + otherComp.Strength) * frameTime;

            // Need the push strength proportional to penetration depth.
            direction += mobMovement;
        }

        if (direction == Vector2.Zero)
        {
            return false;
        }

        entity.Comp1.EndAccumulator = MobCollisionComponent.BufferTime;
        var parentAngle = worldRot - xform.LocalRotation;
        var localDir = (-parentAngle).RotateVec(direction);
        RaiseCollisionEvent(entity.Owner, localDir);

        return true;
    }

    protected abstract void RaiseCollisionEvent(EntityUid uid, Vector2 direction);

    [Serializable, NetSerializable]
    protected sealed class MobCollisionMessage : EntityEventArgs
    {
        public Vector2 Direction;
    }

    [Serializable, NetSerializable]
    protected sealed class MobCollisionToggleMessage : EntityEventArgs
    {
        public bool Enabled;
    }
}
