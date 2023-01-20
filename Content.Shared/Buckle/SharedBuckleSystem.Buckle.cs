using Content.Shared.Buckle.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Events;
using Content.Shared.Standing;
using Content.Shared.Throwing;
using Content.Shared.Vehicle.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics.Events;

namespace Content.Shared.Buckle;

public abstract partial class SharedBuckleSystem
{
    private void InitializeBuckle()
    {
        SubscribeLocalEvent<BuckleComponent, PreventCollideEvent>(PreventCollision);
        SubscribeLocalEvent<BuckleComponent, DownAttemptEvent>(HandleDown);
        SubscribeLocalEvent<BuckleComponent, StandAttemptEvent>(HandleStand);
        SubscribeLocalEvent<BuckleComponent, ThrowPushbackAttemptEvent>(HandleThrowPushback);
        SubscribeLocalEvent<BuckleComponent, UpdateCanMoveEvent>(HandleMove);
        SubscribeLocalEvent<BuckleComponent, ChangeDirectionAttemptEvent>(OnBuckleChangeDirectionAttempt);
    }

    private void PreventCollision(EntityUid uid, BuckleComponent component, ref PreventCollideEvent args)
    {
        if (args.BodyB.Owner != component.LastEntityBuckledTo)
            return;

        if (component.Buckled || component.DontCollide)
            args.Cancelled = true;
    }

    private void HandleDown(EntityUid uid, BuckleComponent component, DownAttemptEvent args)
    {
        if (component.Buckled)
            args.Cancel();
    }

    private void HandleStand(EntityUid uid, BuckleComponent component, StandAttemptEvent args)
    {
        if (component.Buckled)
            args.Cancel();
    }

    private void HandleThrowPushback(EntityUid uid, BuckleComponent component, ThrowPushbackAttemptEvent args)
    {
        if (component.Buckled)
            args.Cancel();
    }

    private void HandleMove(EntityUid uid, BuckleComponent component, UpdateCanMoveEvent args)
    {
        if (component.LifeStage > ComponentLifeStage.Running)
            return;

        if (component.Buckled &&
            !HasComp<VehicleComponent>(Transform(uid).ParentUid)) // buckle+vehicle shitcode
            args.Cancel();
    }

    private void OnBuckleChangeDirectionAttempt(EntityUid uid, BuckleComponent component, ChangeDirectionAttemptEvent args)
    {
        if (component.Buckled)
            args.Cancel();
    }

    public bool IsBuckled(EntityUid uid, BuckleComponent? component = null)
    {
        return Resolve(uid, ref component, false) && component.Buckled;
    }

    /// <summary>
    ///     Reattaches this entity to the strap, modifying its position and rotation.
    /// </summary>
    /// <param name="buckleId">The entity to reattach.</param>
    /// <param name="strap">The strap to reattach to.</param>
    /// <param name="buckle">The buckle component of the entity to reattach.</param>
    public void ReAttach(EntityUid buckleId, StrapComponent strap, BuckleComponent? buckle = null)
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
