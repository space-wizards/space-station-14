using System.Numerics;
using Content.Shared.CCVar;
using Content.Shared.Movement.Components;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Movement.Systems;

public abstract class SharedMobCollisionSystem : EntitySystem
{
    [Dependency] protected readonly IConfigurationManager CfgManager = default!;
    [Dependency] private   readonly MovementSpeedModifierSystem _moveMod = default!;
    [Dependency] protected readonly SharedPhysicsSystem Physics = default!;
    [Dependency] private   readonly SharedTransformSystem _xformSystem = default!;

    protected EntityQuery<MobCollisionComponent> MobQuery;
    protected EntityQuery<PhysicsComponent> PhysicsQuery;

    /// <summary>
    /// <see cref="CCVars.MovementPushingCap"/>
    /// </summary>
    private float _pushingCap;

    /// <summary>
    /// <see cref="CCVars.MovementPushingVelocityProduct"/>
    /// </summary>
    private float _pushingDotProduct;

    /// <summary>
    /// <see cref="CCVars.MovementMinimumPush"/>
    /// </summary>
    private float _minimumPushSquared = 0.01f;

    /// <summary>
    /// Time after we stop colliding with another mob before adjusting the movespeedmodifier.
    /// This is required so if we stop colliding for a frame we don't fully reset and get jerky movement.
    /// </summary>
    public const float BufferTime = 0.2f;

    public override void Initialize()
    {
        base.Initialize();

        UpdatePushCap();
        Subs.CVar(CfgManager, CVars.NetTickrate, _ => UpdatePushCap());
        Subs.CVar(CfgManager, CCVars.MovementMinimumPush, val => _minimumPushSquared = val * val, true);
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
            if (!comp.Colliding)
                continue;

            comp.BufferAccumulator -= frameTime;
            DirtyField(uid, comp, nameof(MobCollisionComponent.BufferAccumulator));
            var direction = comp.Direction;

            if (comp.BufferAccumulator <= 0f)
            {
                SetColliding((uid, comp), false);
            }
            // Apply the mob collision; if it's too low ignore it (e.g. if mob friction would overcome it).
            // This is so we don't spam velocity changes every tick. It's not that expensive for physics but
            // avoids the networking side.
            else if (direction != Vector2.Zero && PhysicsQuery.TryComp(uid, out var physics))
            {
                DebugTools.Assert(direction.LengthSquared() > _minimumPushSquared);

                if (direction.Length() > _pushingCap)
                {
                    direction = direction.Normalized() * _pushingCap;
                }

                Physics.ApplyLinearImpulse(uid, direction * physics.Mass, body: physics);
                comp.Direction = Vector2.Zero;
                DirtyField(uid, comp, nameof(MobCollisionComponent.Direction));
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
        if (value)
        {
            entity.Comp.BufferAccumulator = BufferTime;
            DirtyField(entity.Owner, entity.Comp, nameof(MobCollisionComponent.BufferAccumulator));
        }

        if (entity.Comp.Colliding == value)
            return;

        entity.Comp.Colliding = value;
        DirtyField(entity.Owner, entity.Comp, nameof(MobCollisionComponent.Colliding));

        if (entity.Comp.SpeedModifier.Equals(1f))
            return;

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

        MoveMob((player.Value, comp, xform), direction);
    }

    protected void MoveMob(Entity<MobCollisionComponent, TransformComponent> entity, Vector2 direction)
    {
        // Length too short to do anything.
        if (direction.LengthSquared() < _minimumPushSquared)
        {
            direction = Vector2.Zero;
        }

        SetColliding(entity, true);

        if (direction == entity.Comp1.Direction)
            return;

        entity.Comp1.Direction = direction;
        DirtyField(entity.Owner, entity.Comp1, nameof(MobCollisionComponent.Direction));
    }

    protected bool HandleCollisions(Entity<MobCollisionComponent, PhysicsComponent> entity, float frameTime)
    {
        var physics = entity.Comp2;

        if (physics.ContactCount == 0)
            return false;

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

        direction *= frameTime;
        RaiseCollisionEvent(entity.Owner, direction);
        return true;
    }

    protected abstract void RaiseCollisionEvent(EntityUid uid, Vector2 direction);

    [Serializable, NetSerializable]
    protected sealed class MobCollisionMessage : EntityEventArgs
    {
        public Vector2 Direction;
    }
}
