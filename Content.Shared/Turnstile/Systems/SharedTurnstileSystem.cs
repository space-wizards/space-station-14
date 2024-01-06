using System.Linq;
using System.Numerics;
using Content.Shared.Doors.Components;
using Content.Shared.Physics;
using Content.Shared.Tag;
using Content.Shared.Turnstile.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Turnstile.Systems;

public abstract class SharedTurnstileSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming GameTiming = default!;
    [Dependency] protected readonly SharedPhysicsSystem PhysicsSystem = default!;
    [Dependency] protected readonly SharedAppearanceSystem AppearanceSystem = default!;
    [Dependency] protected readonly EntityLookupSystem EntityLookupSystem = default!;
    [Dependency] protected readonly SharedBroadphaseSystem Broadphase = default!;
    [Dependency] protected readonly TagSystem Tags = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;

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
        SubscribeLocalEvent<TurnstileComponent, AfterAutoHandleStateEvent>(OnHandleState);
    }

    private void OnHandleState(Entity<TurnstileComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        var turnstile = ent.Comp;
        if (turnstile.NextStateChange == null)
            _activeTurnstiles.Remove(ent);
        else
            _activeTurnstiles.Add(ent);

        RaiseLocalEvent(ent, new TurnstileEvents.TurnstileStateChangedEvent(turnstile.State));
        AppearanceSystem.SetData(ent, TurnstileVisuals.State, turnstile.State);
    }

    private void OnComponentInit(Entity<TurnstileComponent> ent, ref ComponentInit args)
    {
        var turnstile = ent.Comp;

        // No turnstile should be spawned in the rotating state, as that requires a mob to be passing through.
        if (turnstile.State == TurnstileState.Rotating)
            turnstile.State = TurnstileState.Idle;

        SetCollidable(ent, true);
        AppearanceSystem.SetData(ent, TurnstileVisuals.State, turnstile.State);
    }


    protected virtual void HandleCollide(Entity<TurnstileComponent> ent, ref StartCollideEvent args)
    {
        // If the colliding entity cannot open doors by bumping into them, then it can't turn the turnstile either.
        if (!Tags.HasTag(args.OtherEntity, "DoorBumpOpener"))
            return;

        // Check the contact normal against our direction.
        // For simplicity, we always want a mob to pass from the "back" to the "front" of the turnstile.
        // That allows unanchored turnstiles to be dragged and rotated as needed, and will admit passage in the
        // direction that they are pulled in.
        var turnstile = ent.Comp;
        if (turnstile.State == TurnstileState.Idle)
        {
            var facingDirection = GetFacingDirection(ent);
            var directionOfContact = GetDirectionOfContact(ent.Owner, args.OtherEntity);
            if (facingDirection == directionOfContact)
            {
                // Admit the entity.
                var comp = EnsureComp<PreventCollideComponent>(ent);
                comp.Uid = args.OtherEntity;

                // Play sound of turning
                Audio.PlayPvs(turnstile.TurnSound, ent.Owner, AudioParams.Default.WithVolume(-3));

                // We have to set collidable to false for one frame, otherwise the client does not
                // predict the removal of the contacts properly, and instead thinks the mob is colliding when it isn't.
                SetCollidable(ent, false);
                SetState(ent, TurnstileState.Rotating);
            }
            else
            {
                // Reject the entity, play sound with cooldown
                Audio.PlayPvs(turnstile.BumpSound, ent.Owner, AudioParams.Default.WithVolume(-3));
            }
        }
        else
        {
            // Reject the entity, play sound with cooldown
            Audio.PlayPvs(turnstile.BumpSound, ent.Owner, AudioParams.Default.WithVolume(-3));
        }
    }

    /// <summary>
    ///     Makes a turnstile proceed to the next state.
    /// </summary>
    private void NextState(Entity<TurnstileComponent> ent, TimeSpan time)
    {
        var turnstile = ent.Comp;

        if (turnstile.State == TurnstileState.Rotating)
        {
            // Check and see if we're still colliding with the admitted entity.
            var stillCollidingWithAdmitted = GetIsCollidingWithAdmitted(ent);
            if (!stillCollidingWithAdmitted)
            {
                turnstile.NextStateChange = null;
                SetState(ent, TurnstileState.Idle);
                RemComp<PreventCollideComponent>(ent.Owner);
            }
            else
            {
                turnstile.NextStateChange = GameTiming.CurTime + turnstile.TurnstileTurnTime;
            }
        }
    }

    private void OnRemove(Entity<TurnstileComponent> turnstile, ref ComponentRemove args)
    {
        _activeTurnstiles.Remove(turnstile);
    }

    private bool GetIsCollidingWithAdmitted(Entity<TurnstileComponent, PreventCollideComponent?> ent)
    {
        if (ent.Comp2 == null)
            return false;

        var turnstileAABB = EntityLookupSystem.GetWorldAABB(ent.Owner);
        var otherAABB = EntityLookupSystem.GetWorldAABB(ent.Comp2!.Uid);

        if (turnstileAABB.Intersects(otherAABB))
            return true;

        return false;
    }

    /// <summary>
    ///     Iterate over active turnstiles and progress them to the next state if they need to be updated.
    /// </summary>
    public override void Update(float frameTime)
    {
        var time = GameTiming.CurTime;
        foreach (var ent in _activeTurnstiles.ToList())
        {
            SetCollidable(ent, true);
            var turnstile = ent.Comp;
            if (turnstile.Deleted || turnstile.NextStateChange == null)
            {
                _activeTurnstiles.Remove(ent);
                continue;
            }

            if (Paused(ent))
                continue;

            if (turnstile.NextStateChange.Value < time)
                NextState(ent, time);
        }
    }

    protected Direction GetFacingDirection(Entity<TurnstileComponent> ent)
    {
        var xform = _xformQuery.GetComponent(ent.Owner);
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
        PhysicsSystem.SetCanCollide(ent.Owner, collidable, body: ent.Comp2);
    }

    protected void SetState(Entity<TurnstileComponent> ent, TurnstileState state)
    {
        if (ent.Comp == null)
            return;

        // If no change, return to avoid firing a new TurnstileStateChangedEvent.
        var turnstile = ent.Comp;
        if (state == turnstile.State)
            return;

        switch (state)
        {
            case TurnstileState.Rotating:
                _activeTurnstiles.Add((ent.Owner, turnstile));
                turnstile.NextStateChange = GameTiming.CurTime + turnstile.TurnstileTurnTime;
                break;

            case TurnstileState.Idle:
                _activeTurnstiles.Remove((ent.Owner, turnstile));
                break;
        }

        turnstile.State = state;
        Dirty(ent.Owner, turnstile);
        RaiseLocalEvent(ent.Owner, new TurnstileEvents.TurnstileStateChangedEvent(state));
        AppearanceSystem.SetData(ent.Owner, TurnstileVisuals.State, turnstile.State);
    }
}
