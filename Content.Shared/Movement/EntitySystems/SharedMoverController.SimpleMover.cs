using System.Diagnostics.CodeAnalysis;
using Content.Shared.Audio;
using Content.Shared.Maps;
using Content.Shared.MobState.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Pulling.Components;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared.Movement.EntitySystems;

public abstract partial class SharedMoverController
{
    protected Angle GetParentGridAngle(TransformComponent xform, SimpleMoverComponent mover)
    {
        if (!_mapManager.TryGetGrid(xform.GridUid, out var grid))
            return mover.LastGridAngle;

        return grid.WorldRotation;
    }

    /// <summary>
    ///     Movement while considering actionblockers, weightlessness, etc.
    /// </summary>
    protected void HandleSimpleMovement(
        SimpleMoverComponent mover,
        PhysicsComponent physicsComponent,
        TransformComponent xform,
        float frameTime)
    {
        DebugTools.Assert(!UsedMobMovement.ContainsKey(mover.Owner));

        if (!UseMobMovement(mover, physicsComponent))
        {
            UsedMobMovement[mover.Owner] = false;
            return;
        }

        UsedMobMovement[mover.Owner] = true;
        var weightless = mover.Owner.IsWeightless(physicsComponent, mapManager: _mapManager, entityManager: EntityManager);
        var (walkDir, sprintDir) = GetSimpleVelocityInput(mover);
        bool touching = true;

        // Handle wall-pushes.
        if (weightless)
        {
            // No gravity: is our entity touching anything?
            touching = xform.GridUid != null || IsAroundCollider(_physics, xform, mover, physicsComponent);

            if (!touching)
            {
                if (xform.GridUid != null)
                    mover.LastGridAngle = GetParentGridAngle(xform, mover);
            }
        }

        // Regular movement.
        // Target velocity.
        // This is relative to the map / grid we're on.
        var total = walkDir * mover.CurrentWalkSpeed + sprintDir * mover.CurrentSprintSpeed;
        var parentRotation = GetParentGridAngle(xform, mover);
        var worldTotal = _relativeMovement ? parentRotation.RotateVec(total) : total;

        DebugTools.Assert(MathHelper.CloseToPercent(total.Length, worldTotal.Length));

        var velocity = physicsComponent.LinearVelocity;
        float friction;
        float weightlessModifier;
        float accel;

        if (weightless)
        {
            if (worldTotal != Vector2.Zero && touching)
                friction = _weightlessFrictionVelocity;
            else
                friction = _weightlessFrictionVelocityNoInput;

            weightlessModifier = _mobWeightlessModifier;
            accel = _mobWeightlessAcceleration;
        }
        else
        {
            friction = _frictionVelocity;
            weightlessModifier = 1f;
            accel = _mobAcceleration;
        }

        Friction(frameTime, friction, ref velocity);

        if (xform.GridUid != EntityUid.Invalid)
            mover.LastGridAngle = parentRotation;

        if (worldTotal != Vector2.Zero)
        {
            // This should have its event run during island solver soooo
            xform.DeferUpdates = true;
            xform.LocalRotation = xform.GridUid != EntityUid.Invalid
                ? total.ToWorldAngle()
                : worldTotal.ToWorldAngle();
            xform.DeferUpdates = false;

            if (TryGetSound(mover, xform, out var variation, out var sound))
            {
                SoundSystem.Play(sound,
                    GetSoundPlayers(mover.Owner),
                    mover.Owner, AudioHelpers.WithVariation(variation).WithVolume(FootstepVolume));
            }
        }

        worldTotal *= weightlessModifier;

        if (touching)
            Accelerate(ref velocity, in worldTotal, accel, frameTime);

        _physics.SetLinearVelocity(physicsComponent, velocity);
    }

    private void Friction(float frameTime, float friction, ref Vector2 velocity)
    {
        var speed = velocity.Length;

        if (speed < _minimumFrictionSpeed) return;

        var drop = 0f;

        var control = MathF.Max(_stopSpeed, speed);
        drop += control * friction * frameTime;

        var newSpeed = MathF.Max(0f, speed - drop);

        if (newSpeed.Equals(speed)) return;

        newSpeed /= speed;
        velocity *= newSpeed;
    }

    private void Accelerate(ref Vector2 currentVelocity, in Vector2 velocity, float accel, float frameTime)
    {
        var wishDir = velocity != Vector2.Zero ? velocity.Normalized : Vector2.Zero;
        var wishSpeed = velocity.Length;

        var currentSpeed = Vector2.Dot(currentVelocity, wishDir);
        var addSpeed = wishSpeed - currentSpeed;

        if (addSpeed <= 0f) return;

        var accelSpeed = accel * frameTime * wishSpeed;
        accelSpeed = MathF.Min(accelSpeed, addSpeed);

        currentVelocity += wishDir * accelSpeed;
    }

    public bool UseMobMovement(EntityUid uid)
    {
        return UsedMobMovement.TryGetValue(uid, out var used) && used;
    }

    protected bool UseMobMovement(SimpleMoverComponent mover, PhysicsComponent body)
    {
        return mover.CanMove &&
               body.BodyStatus == BodyStatus.OnGround &&
               HasComp<MobStateComponent>(body.Owner) &&
               // If we're being pulled then don't mess with our velocity.
               (!TryComp(body.Owner, out SharedPullableComponent? pullable) || !pullable.BeingPulled);
    }

    /// <summary>
    ///     Used for weightlessness to determine if we are near a wall.
    /// </summary>
    private bool IsAroundCollider(SharedPhysicsSystem broadPhaseSystem, TransformComponent transform, SimpleMoverComponent mover, IPhysBody collider)
    {
        var enlargedAABB = collider.GetWorldAABB().Enlarged(mover.GrabRange);

        foreach (var otherCollider in broadPhaseSystem.GetCollidingEntities(transform.MapID, enlargedAABB))
        {
            if (otherCollider == collider) continue; // Don't try to push off of yourself!

            // Only allow pushing off of anchored things that have collision.
            if (otherCollider.BodyType != BodyType.Static ||
                !otherCollider.CanCollide ||
                ((collider.CollisionMask & otherCollider.CollisionLayer) == 0 &&
                (otherCollider.CollisionMask & collider.CollisionLayer) == 0) ||
                (TryComp(otherCollider.Owner, out SharedPullableComponent? pullable) && pullable.BeingPulled))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    // TODO: Predicted audio moment.
    protected abstract Filter GetSoundPlayers(EntityUid mover);

    protected abstract bool CanSound();
}
