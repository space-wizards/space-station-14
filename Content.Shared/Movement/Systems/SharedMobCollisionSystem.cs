using System.Numerics;
using Content.Shared.Buckle.Components;
using Content.Shared.CCVar;
using Content.Shared.Movement.Components;
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
    [Dependency] private   readonly MovementSpeedModifierSystem _moveMod = default!;
    [Dependency] protected readonly SharedPhysicsSystem Physics = default!;
    [Dependency] private   readonly SharedTransformSystem _xformSystem = default!;

    protected EntityQuery<MobCollisionComponent> MobQuery;
    protected EntityQuery<PhysicsComponent> PhysicsQuery;

    private float _pushingCap;
    private float _pushingDotProduct;

    public const float BufferTime = 0.2f;

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
        SubscribeLocalEvent<MobCollisionComponent, RefreshMovementSpeedModifiersEvent>(OnMoveModifier);

        UpdatesBefore.Add(typeof(SharedPhysicsSystem));
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
            comp.HandledThisTick = false;

            if (!comp.Colliding)
                continue;

            comp.BufferAccumulator -= frameTime;
            DirtyField(uid, comp, nameof(MobCollisionComponent.BufferAccumulator));

            if (comp.BufferAccumulator <= 0f)
            {
                SetColliding((uid, comp), false);
            }
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
        if (entity.Comp.SpeedModifier.Equals(1f))
            return;

        if (value)
        {
            entity.Comp.BufferAccumulator = BufferTime;
            DirtyField(entity.Owner, entity.Comp, nameof(MobCollisionComponent.BufferAccumulator));
        }

        if (entity.Comp.Colliding == value)
            return;

        entity.Comp.Colliding = value;
        DirtyField(entity.Owner, entity.Comp, nameof(MobCollisionComponent.Colliding));
        _moveMod.RefreshMovementSpeedModifiers(entity.Owner);
    }

    private void OnCollision(MobCollisionMessage msg, EntitySessionEventArgs args)
    {
        var player = args.SenderSession.AttachedEntity;

        if (!MobQuery.TryComp(player, out var comp))
            return;

        var xform = Transform(player.Value);

        // If not parented directly to a grid then fail it.
        if (xform.ParentUid != xform.GridUid && xform.ParentUid != xform.MapUid)
            return;

        var direction = msg.Direction;

        if (direction.Length() > _pushingCap)
        {
            direction = direction.Normalized() * _pushingCap;
        }

        MoveMob((player.Value, comp, xform), direction);
    }

    protected void MoveMob(Entity<MobCollisionComponent, TransformComponent> entity, Vector2 direction)
    {
        // Wake it so we don't clip into a wall.
        Physics.WakeBody(entity.Owner);
        var xform = entity.Comp2;

        // Alternative though needs tweaks to mob movement code.
        // Physics.ApplyLinearImpulse(entity.Owner, direction);
        _xformSystem.SetLocalPosition(entity.Owner, xform.LocalPosition + direction);
        SetColliding(entity, true);
    }

    protected bool HandleCollisions(Entity<MobCollisionComponent, PhysicsComponent> entity, float frameTime)
    {
        var physics = entity.Comp2;

        if (physics.ContactCount == 0)
            return false;

        if (entity.Comp1.HandledThisTick)
            return true;

        entity.Comp1.HandledThisTick = true;
        var ourVelocity = entity.Comp2.LinearVelocity;

        if (ourVelocity == Vector2.Zero && !CfgManager.GetCVar(CCVars.MovementPushingStatic))
            return false;

        var xform = Transform(entity.Owner);

        if (xform.ParentUid != xform.GridUid && xform.ParentUid != xform.MapUid)
            return false;

        var (worldPos, worldRot) = _xformSystem.GetWorldPositionRotation(xform);
        var ourTransform = new Transform(worldPos, worldRot);
        var contacts = Physics.GetContacts(entity.Owner);
        var direction = Vector2.Zero;
        var contactCount = 0;

        while (contacts.MoveNext(out var contact))
        {
            if (!contact.IsTouching)
                continue;

            var ourFixture = contact.OurFixture(entity.Owner);

            if (ourFixture.Id != entity.Comp1.FixtureId)
                continue;

            var other = contact.OtherEnt(entity.Owner);

            if (!MobQuery.TryComp(other, out var otherComp) || !PhysicsQuery.TryComp(other, out var otherPhysics))
                continue;

            var velocityProduct = Vector2.Dot(ourVelocity, otherPhysics.LinearVelocity);

            if (velocityProduct < _pushingDotProduct)
            {
                continue;
            }

            // TODO: More robust overlap detection.
            var otherTransform = Physics.GetPhysicsTransform(other);
            var diff = ourTransform.Position - otherTransform.Position;

            // 0.7 for 0.35 + 0.35 for mob bounds (see TODO above).
            // Clamp so we don't get a heap of penetration depth and suddenly lurch other mobs.
            // This is also so we don't have to trigger the speed-cap above.
            // Maybe we just do speedcap and dump this? Though it's less configurable and the cap is just there for cheaters.
            var penDepth = Math.Clamp(0.7f - diff.Length(), 0f, 0.40f);

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

        var parentAngle = worldRot - xform.LocalRotation;
        direction *= frameTime;
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
