using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Pulling.Components;
using Content.Shared.Turnstile.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Turnstile.Systems;

public sealed class TurnstileSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TurnstileComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<TurnstileComponent, StartCollideEvent>(HandleCollide);
        SubscribeLocalEvent<TurnstileComponent, PreventCollideEvent>(HandlePreventCollide);
    }

    private void HandlePreventCollide(Entity<TurnstileComponent> ent, ref PreventCollideEvent args)
    {
        // If Turnstile is rotating, allow entities the admitted entity is pulling to come through with it.
        if (ent.Comp.State != TurnstileState.Rotating || !TryComp<PreventCollideComponent>(ent, out var preventCollide))
            return;

        if (TryComp<SharedPullerComponent>(preventCollide.Uid, out var puller))
            args.Cancelled |= puller.Pulling == args.OtherEntity;
    }

    private void OnComponentInit(Entity<TurnstileComponent> ent, ref ComponentInit args)
    {
        // No turnstile should be spawned in the rotating state, as that requires a mob to be passing through.
        if (ent.Comp.State == TurnstileState.Rotating)
            ent.Comp.State = TurnstileState.Idle;

        _appearanceSystem.SetData(ent, TurnstileVisuals.State, ent.Comp.State);
    }


    private void HandleCollide(Entity<TurnstileComponent> ent, ref StartCollideEvent args)
    {
        // The colliding entity needs to be a mob.
        if (!HasComp<MobStateComponent>(args.OtherEntity))
            return;

        if (!TryComp<TransformComponent>(ent, out var xform))
            return;

        // Check the contact normal against our direction.
        // For simplicity, we always want a mob to pass from the "back" to the "front" of the turnstile.
        // That allows unanchored turnstiles to be dragged and rotated as needed, and will admit passage in the
        // direction that they are pulled in.
        if (ent.Comp.State == TurnstileState.Idle)
        {
            var facingDirection = xform.LocalRotation.GetDir();
            var directionOfContact = GetDirectionOfContact(xform, args.OtherEntity);
            if (facingDirection == directionOfContact)
            {
                // Admit the entity.
                var comp = EnsureComp<PreventCollideComponent>(ent);
                comp.Uid = args.OtherEntity;

                // Play sound of turning
                _audio.PlayPredicted(ent.Comp.TurnSound, ent, args.OtherEntity, AudioParams.Default.WithVolume(-3));

                // We have to set collidable to false for one frame, otherwise the client does not
                // predict the removal of the contacts properly, and instead thinks the mob is colliding when it isn't.
                SetCollidable((ent, ent.Comp, null), false);
                SetState((ent, ent.Comp), TurnstileState.Rotating);
            }
            else
            {
                // Reject the entity, play sound
                _audio.PlayPredicted(ent.Comp.BumpSound, ent, args.OtherEntity, AudioParams.Default.WithVolume(-3));
            }
        }
        else
        {
            // Reject the entity, play sound
            _audio.PlayPredicted(ent.Comp.BumpSound, ent, args.OtherEntity, AudioParams.Default.WithVolume(-3));
        }
    }

    /// <summary>
    ///     Turnstiles stop rotating once their admitted entity has passed through.
    /// </summary>
    private void StopRotatingIfEntityPassed(Entity<TurnstileComponent> ent)
    {
        if (ent.Comp.State != TurnstileState.Rotating)
            return;

        // Check and see if we're still colliding with the admitted entity.
        var stillCollidingWithAdmitted = GetIsCollidingWithAdmitted(ent);
        if (stillCollidingWithAdmitted)
            return;

        SetState(ent, TurnstileState.Idle);
        RemComp<PreventCollideComponent>(ent);
    }

    private bool GetIsCollidingWithAdmitted(EntityUid uid, TurnstileComponent? turnstile = null, TransformComponent? xform = null, PreventCollideComponent? preventCollide = null)
    {
        if (!Resolve(uid, ref turnstile) || !Resolve(uid, ref xform) || !Resolve(uid, ref preventCollide))
            return false;

        if (!TryComp<TransformComponent>(preventCollide.Uid, out var otherxform))
            return false;

        var turnstileAABB = _entityLookupSystem.GetAABBNoContainer(uid, xform.LocalPosition, xform.LocalRotation);
        var otherAABB = _entityLookupSystem.GetAABBNoContainer(uid, otherxform.LocalPosition, otherxform.LocalRotation);

        bool isColliding = turnstileAABB.Intersects(otherAABB);

        // Is the entity pulling something else through too? If so, the turnstile should still be spinning to allow this object through.
        if (TryComp<SharedPullerComponent>(preventCollide.Uid, out var puller))
        {
            if (puller.Pulling != null)
            {
                if (TryComp<TransformComponent>(puller.Pulling.Value, out otherxform))
                {
                    otherAABB = _entityLookupSystem.GetAABBNoContainer(uid, otherxform.LocalPosition, otherxform.LocalRotation);
                    isColliding |= turnstileAABB.Intersects(otherAABB);
                }
            }
        }

        return isColliding;
    }

    /// <summary>
    ///     Iterate over active turnstiles and finish the rotation-admission cycle when mobs leave them.
    /// </summary>
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<TurnstileComponent, ActiveTurnstileMarkerComponent>();
        while (query.MoveNext(out var uid, out var turnstile, out _))
        {
            if (Paused(uid))
                continue;

            SetCollidable(uid, true);
            StopRotatingIfEntityPassed((uid, turnstile));
        }
    }

    private Direction GetDirectionOfContact(TransformComponent xform, EntityUid other)
    {
        if (!TryComp<TransformComponent>(other, out var xformOther))
            return Direction.Invalid;
        return (xform.LocalPosition - xformOther.LocalPosition).GetDir();
    }

    private void SetCollidable(Entity<TurnstileComponent?, PhysicsComponent?> ent, bool collidable)
    {
        if (!Resolve(ent, ref ent.Comp1) || !Resolve(ent, ref ent.Comp2))
            return;

        _physicsSystem.SetCanCollide(ent, collidable, body: ent.Comp2);
    }

    private void SetState(Entity<TurnstileComponent> ent, TurnstileState state)
    {
        // If no change, return.
        if (state == ent.Comp.State)
            return;

        switch (state)
        {
            case TurnstileState.Rotating:
                EnsureComp<ActiveTurnstileMarkerComponent>(ent);
                break;

            case TurnstileState.Idle:
                RemComp<ActiveTurnstileMarkerComponent>(ent);
                break;
        }

        ent.Comp.State = state;
        Dirty(ent, ent.Comp);
        _appearanceSystem.SetData(ent, TurnstileVisuals.State, ent.Comp.State);
    }
}
