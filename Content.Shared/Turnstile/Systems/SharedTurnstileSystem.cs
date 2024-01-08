using System.Linq;
using Content.Shared.Physics;
using Content.Shared.Pulling.Components;
using Content.Shared.Tag;
using Content.Shared.Turnstile.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Turnstile.Systems;

public abstract class SharedTurnstileSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private EntityQuery<TransformComponent> _xformQuery;

    /// <summary>
    ///     A set of turnstiles that are currently rotating.
    /// </summary>
    private readonly HashSet<Entity<TurnstileComponent>> _activeTurnstiles = new();

    public override void Initialize()
    {
        base.Initialize();

        _xformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<TurnstileComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<TurnstileComponent, ComponentRemove>(OnRemove);

        SubscribeLocalEvent<TurnstileComponent, StartCollideEvent>(HandleCollide);
        SubscribeLocalEvent<TurnstileComponent, PreventCollideEvent>(HandlePreventCollide);
    }

    private void HandlePreventCollide(Entity<TurnstileComponent> ent, ref PreventCollideEvent args)
    {
        // If Turnstile is rotating, allow entities the admitted entity is pulling to come through with it.
        var turnstile = ent.Comp;
        if (turnstile.State != TurnstileState.Rotating || !TryComp<PreventCollideComponent>(ent, out var preventCollide))
            return;

        if (TryComp<SharedPullerComponent>(preventCollide.Uid, out var puller))
            args.Cancelled |= puller.Pulling == args.OtherEntity;
    }

    private void OnComponentInit(Entity<TurnstileComponent> ent, ref ComponentInit args)
    {
        var turnstile = ent.Comp;

        // No turnstile should be spawned in the rotating state, as that requires a mob to be passing through.
        if (turnstile.State == TurnstileState.Rotating)
            turnstile.State = TurnstileState.Idle;

        SetCollidable(ent, true);
        _appearanceSystem.SetData(ent, TurnstileVisuals.State, turnstile.State);
    }


    protected virtual void HandleCollide(Entity<TurnstileComponent> ent, ref StartCollideEvent args)
    {
        // If the colliding entity cannot open doors by bumping into them, then it can't turn the turnstile either.
        if (!_tags.HasTag(args.OtherEntity, "DoorBumpOpener"))
            return;

        // Check the contact normal against our direction.
        // For simplicity, we always want a mob to pass from the "back" to the "front" of the turnstile.
        // That allows unanchored turnstiles to be dragged and rotated as needed, and will admit passage in the
        // direction that they are pulled in.
        var turnstile = ent.Comp;
        if (turnstile.State == TurnstileState.Idle)
        {
            var facingDirection = GetFacingDirection(ent);
            var directionOfContact = GetDirectionOfContact(ent, args.OtherEntity);
            if (facingDirection == directionOfContact)
            {
                // Admit the entity.
                var comp = EnsureComp<PreventCollideComponent>(ent);
                comp.Uid = args.OtherEntity;

                // Play sound of turning
                _audio.PlayPredicted(turnstile.TurnSound, ent, args.OtherEntity, AudioParams.Default.WithVolume(-3));

                // We have to set collidable to false for one frame, otherwise the client does not
                // predict the removal of the contacts properly, and instead thinks the mob is colliding when it isn't.
                SetCollidable(ent, false);
                SetState(ent, TurnstileState.Rotating);
            }
            else
            {
                // Reject the entity, play sound
                _audio.PlayPredicted(turnstile.BumpSound, ent, args.OtherEntity, AudioParams.Default.WithVolume(-3));
            }
        }
        else
        {
            // Reject the entity, play sound
            _audio.PlayPredicted(turnstile.BumpSound, ent, args.OtherEntity, AudioParams.Default.WithVolume(-3));
        }
    }

    /// <summary>
    ///     Turnstiles stop rotating once their admitted entity has passed through.
    /// </summary>
    private void StopRotatingIfEntityPassed(Entity<TurnstileComponent> ent)
    {
        var turnstile = ent.Comp;

        if (turnstile.State != TurnstileState.Rotating)
            return;

        // Check and see if we're still colliding with the admitted entity.
        var stillCollidingWithAdmitted = GetIsCollidingWithAdmitted(ent);
        if (stillCollidingWithAdmitted)
            return;

        SetState(ent, TurnstileState.Idle);
        RemComp<PreventCollideComponent>(ent);
    }

    private void OnRemove(Entity<TurnstileComponent> turnstile, ref ComponentRemove args)
    {
        _activeTurnstiles.Remove(turnstile);
    }

    private bool GetIsCollidingWithAdmitted(Entity<TurnstileComponent, PreventCollideComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2))
            return false;

        var turnstilexform = _xformQuery.GetComponent(ent);
        var otherxform = _xformQuery.GetComponent(ent.Comp2.Uid);

        var turnstileAABB = _entityLookupSystem.GetAABBNoContainer(ent, turnstilexform.LocalPosition, turnstilexform.LocalRotation);
        var otherAABB = _entityLookupSystem.GetAABBNoContainer(ent, otherxform.LocalPosition, otherxform.LocalRotation);

        bool isColliding = turnstileAABB.Intersects(otherAABB);

        // Is the entity pulling something else through too? If so, the turnstile should still be spinning to allow this object through.
        if (TryComp<SharedPullerComponent>(ent.Comp2.Uid, out var puller))
        {
            if (puller.Pulling != null)
            {
                otherxform = _xformQuery.GetComponent(puller.Pulling.Value);
                otherAABB = _entityLookupSystem.GetAABBNoContainer(ent, otherxform.LocalPosition, otherxform.LocalRotation);
                isColliding |= turnstileAABB.Intersects(otherAABB);
            }
        }

        return isColliding;
    }

    /// <summary>
    ///     Iterate over active turnstiles and finish the rotation-admission cycle when mobs leave them.
    /// </summary>
    public override void Update(float frameTime)
    {
        foreach (var ent in _activeTurnstiles.ToList())
        {
            SetCollidable(ent, true);
            var turnstile = ent.Comp;
            if (turnstile.Deleted)
            {
                _activeTurnstiles.Remove(ent);
                continue;
            }

            if (Paused(ent))
                continue;

            StopRotatingIfEntityPassed(ent);
        }
    }

    protected Direction GetFacingDirection(Entity<TurnstileComponent> ent)
    {
        var xform = _xformQuery.GetComponent(ent);
        return xform.LocalRotation.GetDir();
    }

    protected Direction GetDirectionOfContact(EntityUid uid, EntityUid other)
    {
        var xform = _xformQuery.GetComponent(uid);
        var xformOther = _xformQuery.GetComponent(other);
        return (xform.LocalPosition - xformOther.LocalPosition).GetDir();
    }

    private void SetCollidable(Entity<TurnstileComponent, PhysicsComponent?> ent, bool collidable)
    {
        if (!Resolve(ent, ref ent.Comp2))
            return;

        _physicsSystem.SetCanCollide(ent, collidable, body: ent.Comp2);
    }

    protected void SetState(Entity<TurnstileComponent> ent, TurnstileState state)
    {
        // If no change, return.
        var turnstile = ent.Comp;
        if (state == turnstile.State)
            return;

        switch (state)
        {
            case TurnstileState.Rotating:
                _activeTurnstiles.Add((ent, turnstile));
                break;

            case TurnstileState.Idle:
                _activeTurnstiles.Remove((ent, turnstile));
                break;
        }

        turnstile.State = state;
        Dirty(ent, turnstile);
        _appearanceSystem.SetData(ent, TurnstileVisuals.State, turnstile.State);
    }
}
