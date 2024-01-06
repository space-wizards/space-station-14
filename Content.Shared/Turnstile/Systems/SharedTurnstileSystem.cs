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
    [Dependency] protected readonly SharedTransformSystem XformSystem = default!;
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
        SubscribeLocalEvent<TurnstileComponent, PreventCollideEvent>(PreventCollision);

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

        SetCollidable(ent, true, turnstile);
        AppearanceSystem.SetData(ent, TurnstileVisuals.State, turnstile.State);
    }

    protected virtual void HandleCollide(Entity<TurnstileComponent> ent, ref StartCollideEvent args)
    {

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
            var stillCollidingWithAdmitted = GetIsCollidingWithAdmitted(ent.Owner);
            if (!stillCollidingWithAdmitted)
            {
                turnstile.NextStateChange = null;
                SetState(ent.Owner, TurnstileState.Idle);

                // Remove preventcollision comp
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

    private bool GetIsCollidingWithAdmitted(EntityUid uid, TurnstileComponent? turnstile = null, PhysicsComponent? physics = null, PreventCollideComponent? preventCollide = null)
    {
        if (!Resolve(uid, ref physics, ref turnstile, ref preventCollide, false))
            return false;

        var turnstileAABB = EntityLookupSystem.GetWorldAABB(uid);

        foreach (var otherPhysics in PhysicsSystem.GetCollidingEntities(Transform(uid).MapID, turnstileAABB))
        {
            if (otherPhysics == physics)
                continue;

            if (otherPhysics.Owner == preventCollide.Uid)
                return true;
        }

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

    protected Direction GetFacingDirection(EntityUid uid, TurnstileComponent? turnstile = null)
    {
        if (!Resolve(uid, ref turnstile, false))
            return Direction.Invalid;

        var xform = _xformQuery.GetComponent(uid);
        return xform.LocalRotation.GetDir();
    }

    protected Direction GetDirectionOfContact(EntityUid uid, EntityUid other)
    {
        var xform = _xformQuery.GetComponent(uid);
        var xformOther = _xformQuery.GetComponent(other);
        return (xform.LocalPosition - xformOther.LocalPosition).GetDir();
    }

    private void PreventCollision(Entity<TurnstileComponent> ent, ref PreventCollideEvent args)
    {/*
        var turnstile = ent.Comp;
        // Allow currently-admitted entity to pass through.
        if (turnstile.CurrentlyAdmittingEntity == args.OtherEntity)
        {
            args.Cancelled = true;
        }*/
    }


    private void SetCollidable(
        EntityUid uid,
        bool collidable,
        TurnstileComponent? turnstile = null,
        PhysicsComponent? physics = null)
    {
        if (!Resolve(uid, ref turnstile))
            return;

        if (Resolve(uid, ref physics, false))
            PhysicsSystem.SetCanCollide(uid, collidable, body: physics);
    }

    protected void SetState(EntityUid uid, TurnstileState state, TurnstileComponent? turnstile = null)
    {
        if (!Resolve(uid, ref turnstile))
            return;

        // If no change, return to avoid firing a new TurnstileStateChangedEvent.
        if (state == turnstile.State)
            return;

        switch (state)
        {
            case TurnstileState.Rotating:
                _activeTurnstiles.Add((uid, turnstile));
                turnstile.NextStateChange = GameTiming.CurTime + turnstile.TurnstileTurnTime;
                break;

            case TurnstileState.Idle:
                _activeTurnstiles.Remove((uid, turnstile));
                break;
        }

        turnstile.State = state;
        Dirty(uid, turnstile);
        RaiseLocalEvent(uid, new TurnstileEvents.TurnstileStateChangedEvent(state));
        AppearanceSystem.SetData(uid, TurnstileVisuals.State, turnstile.State);
    }
}
