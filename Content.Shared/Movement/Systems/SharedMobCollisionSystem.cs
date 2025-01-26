using System.Numerics;
using Content.Shared.CCVar;
using Content.Shared.Movement.Components;
using Content.Shared.Physics;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Movement.Systems;

public abstract class SharedMobCollisionSystem : EntitySystem
{
    [Dependency] protected readonly IConfigurationManager CfgManager = default!;
    [Dependency] private   readonly FixtureSystem _fixtures = default!;
    [Dependency] private   readonly MovementSpeedModifierSystem _moveMod = default!;
    [Dependency] protected readonly SharedPhysicsSystem Physics = default!;
    [Dependency] private   readonly SharedTransformSystem _xformSystem = default!;

    protected EntityQuery<MobCollisionComponent> MobQuery;
    protected EntityQuery<PhysicsComponent> PhysicsQuery;

    private float _pushingCap;
    private float _pushingDotProduct;

    public override void Initialize()
    {
        base.Initialize();

        UpdatePushCap();
        Subs.CVar(CfgManager, CVars.NetTickrate, _ => UpdatePushCap());
        Subs.CVar(CfgManager, CCVars.MovementPushingCap, _ => UpdatePushCap());
        Subs.CVar(CfgManager, CCVars.MovementPushingVelocityProduct,
            value =>
            {
                _pushingDotProduct = value;
            }, true);

        MobQuery = GetEntityQuery<MobCollisionComponent>();
        PhysicsQuery = GetEntityQuery<PhysicsComponent>();
        SubscribeAllEvent<MobCollisionMessage>(OnCollision);
        SubscribeLocalEvent<MobCollisionComponent, ComponentStartup>(OnCollisionStartup);
        SubscribeLocalEvent<MobCollisionComponent, RefreshMovementSpeedModifiersEvent>(OnMoveModifier);

        UpdatesAfter.Add(typeof(SharedPhysicsSystem));
    }

    private void UpdatePushCap()
    {
        _pushingCap = (1f / CfgManager.GetCVar(CVars.NetTickrate)) * CfgManager.GetCVar(CCVars.MovementPushingCap);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = AllEntityQuery<MobCollisionComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            SetColliding((uid, comp), false);
        }
    }

    private void OnMoveModifier(Entity<MobCollisionComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!ent.Comp.Colliding)
            return;

        args.ModifySpeed(ent.Comp.SpeedModifier);
    }

    private void SetColliding(Entity<MobCollisionComponent> entity, bool value)
    {
        if (entity.Comp.Colliding == value)
            return;

        entity.Comp.Colliding = value;
        Dirty(entity);
        _moveMod.RefreshMovementSpeedModifiers(entity.Owner);
    }

    private void OnCollision(MobCollisionMessage msg, EntitySessionEventArgs args)
    {
        var player = args.SenderSession.AttachedEntity;

        if (!MobQuery.TryComp(player, out var comp))
            return;

        var direction = msg.Direction;

        if (direction.Length() > _pushingCap)
        {
            direction = direction.Normalized() * _pushingCap;
        }

        MoveMob((player.Value, comp), direction);
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

    protected void MoveMob(Entity<MobCollisionComponent> entity, Vector2 direction)
    {
        var xform = Transform(entity.Owner);

        // TODO: Raycast to the specified spot so we don't clip into a wall.
        // TODO: Does wakebody break this???
        Physics.WakeBody(entity.Owner);

        _xformSystem.SetLocalPosition(entity.Owner, xform.LocalPosition + direction);
        SetColliding(entity, true);
    }

    protected bool HandleCollisions(Entity<MobCollisionComponent, PhysicsComponent> entity, float frameTime)
    {
        var physics = entity.Comp2;

        // TODO: Dot product check

        if (physics.ContactCount == 0)
            return false;

        var xform = Transform(entity.Owner);
        var (worldPos, worldRot) = _xformSystem.GetWorldPositionRotation(entity.Owner);
        var ourTransform = new Transform(worldPos, worldRot);
        var contacts = Physics.GetContacts(entity.Owner);
        var direction = Vector2.Zero;
        var contactCount = 0;
        var ourVelocity = entity.Comp2.LinearVelocity;

        if (!CfgManager.GetCVar(CCVars.MovementPushingStatic) && ourVelocity == Vector2.Zero)
            return false;

        while (contacts.MoveNext(out var contact))
        {
            if (!contact.IsTouching)
                continue;

            var other = contact.OtherEnt(entity.Owner);

            if (!MobQuery.TryComp(other, out var otherComp) || !PhysicsQuery.TryComp(other, out var otherPhysics))
                continue;

            var velocityProduct = Vector2.Dot(ourVelocity, otherPhysics.LinearVelocity);

            if (velocityProduct < _pushingDotProduct)
            {
                continue;
            }

            // TODO: Get overlap amount
            var otherTransform = Physics.GetPhysicsTransform(other);
            var diff = ourTransform.Position - otherTransform.Position;
            var penDepth = MathF.Max(0f, 0.6f - diff.Length());

            penDepth = MathF.Pow(penDepth, 1f);

            // Sum the strengths so we get pushes back the same amount (impulse-wise, ignoring prediction).
            var mobMovement = penDepth * diff.Normalized() * (entity.Comp1.Strength + otherComp.Strength);

            // Need the push strength proportional to penetration depth.
            direction += mobMovement;
            contactCount++;
        }

        if (direction == Vector2.Zero)
        {
            return contactCount > 0;
        }

        direction *= frameTime;
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
}
