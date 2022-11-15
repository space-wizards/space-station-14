using Content.Shared.Buckle.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Events;
using Content.Shared.Standing;
using Content.Shared.Throwing;
using Content.Shared.Vehicle.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Shared.Buckle;

public abstract class SharedBuckleSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming GameTiming = default!;

    [Dependency] private readonly StandingStateSystem _standing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharedStrapComponent, MoveEvent>(OnStrapRotate);

        SubscribeLocalEvent<SharedBuckleComponent, PreventCollideEvent>(PreventCollision);
        SubscribeLocalEvent<SharedBuckleComponent, DownAttemptEvent>(HandleDown);
        SubscribeLocalEvent<SharedBuckleComponent, StandAttemptEvent>(HandleStand);
        SubscribeLocalEvent<SharedBuckleComponent, ThrowPushbackAttemptEvent>(HandleThrowPushback);
        SubscribeLocalEvent<SharedBuckleComponent, UpdateCanMoveEvent>(HandleMove);
        SubscribeLocalEvent<SharedBuckleComponent, ChangeDirectionAttemptEvent>(OnBuckleChangeDirectionAttempt);
    }

    private void OnStrapRotate(EntityUid uid, SharedStrapComponent component, ref MoveEvent args)
    {
        // TODO: This looks dirty af.
        // On rotation of a strap, reattach all buckled entities.
        // This fixes buckle offsets and draw depths.
        // This is mega cursed. Please somebody save me from Mr Buckle's wild ride.
        // Oh god I'm back here again. Send help.

        // Consider a chair that has a player strapped to it. Then the client receives a new server state, showing
        // that the player entity has moved elsewhere, and the chair has rotated. If the client applies the player
        // state, then the chairs transform comp state, and then the buckle state. The transform state will
        // forcefully teleport the player back to the chair (client-side only). This causes even more issues if the
        // chair was teleporting in from nullspace after having left PVS.
        //
        // One option is to just never trigger re-buckles during state application.
        // another is to.. just not do this? Like wtf is this code. But I CBF with buckle atm.

        if (GameTiming.ApplyingState || args.NewRotation == args.OldRotation)
            return;

        foreach (var buckledEntity in component.BuckledEntities)
        {
            if (!EntityManager.TryGetComponent(buckledEntity, out SharedBuckleComponent? buckled))
            {
                continue;
            }

            if (!buckled.Buckled || buckled.LastEntityBuckledTo != uid)
            {
                Logger.Error($"A moving strap entity {ToPrettyString(uid)} attempted to re-parent an entity that does not 'belong' to it {ToPrettyString(buckledEntity)}");
                continue;
            }

            ReAttach(buckledEntity, component, buckle: buckled);
            Dirty(buckled);
        }
    }

    public bool IsBuckled(EntityUid uid, SharedBuckleComponent? component = null)
    {
        return Resolve(uid, ref component, false) && component.Buckled;
    }

    private void OnBuckleChangeDirectionAttempt(EntityUid uid, SharedBuckleComponent component, ChangeDirectionAttemptEvent args)
    {
        if (component.Buckled)
            args.Cancel();
    }

    private void HandleMove(EntityUid uid, SharedBuckleComponent component, UpdateCanMoveEvent args)
    {
        if (component.LifeStage > ComponentLifeStage.Running)
            return;

        if (component.Buckled &&
            !HasComp<VehicleComponent>(Transform(uid).ParentUid)) // buckle+vehicle shitcode
            args.Cancel();
    }

    private void HandleStand(EntityUid uid, SharedBuckleComponent component, StandAttemptEvent args)
    {
        if (component.Buckled)
        {
            args.Cancel();
        }
    }

    private void HandleDown(EntityUid uid, SharedBuckleComponent component, DownAttemptEvent args)
    {
        if (component.Buckled)
        {
            args.Cancel();
        }
    }

    private void HandleThrowPushback(EntityUid uid, SharedBuckleComponent component, ThrowPushbackAttemptEvent args)
    {
        if (!component.Buckled) return;
        args.Cancel();
    }

    private void PreventCollision(EntityUid uid, SharedBuckleComponent component, ref PreventCollideEvent args)
    {
        if (args.BodyB.Owner != component.LastEntityBuckledTo)
            return;

        if (component.Buckled || component.DontCollide)
        {
            args.Cancelled = true;
        }
    }

    /// <summary>
    ///     Reattaches this entity to the strap, modifying its position and rotation.
    /// </summary>
    /// <param name="buckleId">The entity to reattach.</param>
    /// <param name="strap">The strap to reattach to.</param>
    /// <param name="buckle">The buckle component of the entity to reattach.</param>
    public void ReAttach(EntityUid buckleId, SharedStrapComponent strap, SharedBuckleComponent? buckle = null)
    {
        if (!Resolve(buckleId, ref buckle, false))
            return;

        var ownTransform = Transform(buckleId);
        var strapTransform = Transform(strap.Owner);

        ownTransform.Coordinates = new EntityCoordinates(strapTransform.Owner, strap.BuckleOffset);

        // Buckle subscribes to move for <reasons> so this might fail.
        // TODO: Make buckle not do that.
        if (ownTransform.ParentUid != strapTransform.Owner)
            return;

        ownTransform.LocalRotation = Angle.Zero;

        switch (strap.Position)
        {
            case StrapPosition.None:
                break;
            case StrapPosition.Stand:
                _standing.Stand(buckleId);
                break;
            case StrapPosition.Down:
                _standing.Down(buckleId, false, false);
                break;
        }
    }
}
