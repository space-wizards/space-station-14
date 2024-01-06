using System.Linq;
using System.Numerics;
using Content.Shared.Doors.Components;
using Content.Shared.Tag;
using Content.Shared.Turnstile.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Turnstile.Systems;

public sealed class SharedTurnstileSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming GameTiming = default!;
    [Dependency] protected readonly SharedPhysicsSystem PhysicsSystem = default!;
    [Dependency] protected readonly SharedAppearanceSystem AppearanceSystem = default!;
    [Dependency] protected readonly SharedTransformSystem XformSystem = default!;
    [Dependency] protected readonly TagSystem Tags = default!;


    private EntityQuery<TransformComponent> _xformQuery;

    /// <summary>
    ///     A body must have an intersection percentage larger than this in order to be considered as colliding with a
    ///     turnstile. Used for understanding when a mob has finished passing through.
    /// </summary>
    public const float IntersectPercentage = 0.2f;

    /// <summary>
    ///     A set of turnstiles that are currently rotating.
    /// </summary>
    private readonly HashSet<Entity<TurnstileComponent>> _activeTurnstiles = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TurnstileComponent, ComponentInit>(OnComponentInit);
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
        AppearanceSystem.SetData(ent, DoorVisuals.State, turnstile.State);
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
    private void HandleCollide(Entity<TurnstileComponent> ent, ref StartCollideEvent args)
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
            var facingDirection = GetFacingDirection(ent.Owner, turnstile);
            var directionOfContact = GetDirectionOfContact(ent.Owner, args.OtherEntity);
            if (facingDirection == directionOfContact)
            {
                // Admit the entity.
                turnstile.CurrentlyAdmittingEntity = args.OtherEntity;
                SetState(ent.Owner, TurnstileState.Rotating);
            }
        }
    }

    /// <summary>
    ///     Makes a turnstile proceed to the next state.
    /// </summary>
    private void NextState(Entity<TurnstileComponent> ent, TimeSpan time)
    {
        var door = ent.Comp;
        door.NextStateChange = null;

        SetState(ent.Owner, TurnstileState.Idle);
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

    private Direction GetFacingDirection(EntityUid uid, TurnstileComponent? turnstile = null)
    {
        if (!Resolve(uid, ref turnstile, false))
            return Direction.Invalid;

        var xform = _xformQuery.GetComponent(uid);
        return xform.LocalRotation.ToWorldVec().GetDir();
    }

    private Direction GetDirectionOfContact(EntityUid uid, EntityUid other)
    {
        return (XformSystem.GetWorldPosition(other) - XformSystem.GetWorldPosition(uid)).GetDir();
    }

    private void PreventCollision(EntityUid uid, TurnstileComponent component, PreventCollideEvent args)
    {
        // Allow currently-admitted entity to pass through.
        if (component.CurrentlyAdmittingEntity == args.OtherEntity)
        {
            args.Cancelled = true;
        }
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
                turnstile.CurrentlyAdmittingEntity = EntityUid.Invalid;
                break;
        }

        turnstile.State = state;
        Dirty(uid, turnstile);
        RaiseLocalEvent(uid, new TurnstileEvents.TurnstileStateChangedEvent(state));
        AppearanceSystem.SetData(uid, TurnstileVisuals.State, turnstile.State);
    }
}
