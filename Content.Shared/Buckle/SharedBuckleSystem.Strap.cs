using Content.Shared.Buckle.Components;
using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Robust.Shared.GameStates;

namespace Content.Shared.Buckle;

public abstract partial class SharedBuckleSystem
{
    [Dependency] private readonly SharedInteractionSystem _interactions = default!;

    private void InitializeStrap()
    {
        SubscribeLocalEvent<StrapComponent, MoveEvent>(OnStrapRotate);
        SubscribeLocalEvent<StrapComponent, ComponentHandleState>(OnStrapHandleState);
        SubscribeLocalEvent<StrapComponent, CanDragDropOnEvent>(OnStrapCanDragDropOn);
    }

    private void OnStrapHandleState(EntityUid uid, StrapComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not StrapComponentState state)
            return;

        component.Position = state.Position;
        component.BuckleOffsetUnclamped = state.BuckleOffsetClamped;
        component.BuckledEntities.Clear();
        component.BuckledEntities.UnionWith(state.BuckledEntities);
        component.MaxBuckleDistance = state.MaxBuckleDistance;
    }

    private void OnStrapRotate(EntityUid uid, StrapComponent component, ref MoveEvent args)
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
            if (!EntityManager.TryGetComponent(buckledEntity, out BuckleComponent? buckled))
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

    protected bool StrapCanDragDropOn(
        EntityUid strapId,
        EntityUid user,
        EntityUid target,
        EntityUid buckleId,
        StrapComponent? strap = null,
        BuckleComponent? buckle = null)
    {
        if (!Resolve(strapId, ref strap, false) ||
            !Resolve(buckleId, ref buckle, false))
        {
            return false;
        }

        bool Ignored(EntityUid entity) => entity == user || entity == buckleId || entity == target;

        return _interactions.InRangeUnobstructed(target, buckleId, buckle.Range, predicate: Ignored);
    }

    private void OnStrapCanDragDropOn(EntityUid uid, StrapComponent strap, CanDragDropOnEvent args)
    {
        args.CanDrop = StrapCanDragDropOn(args.Target, args.User, args.Target, args.Dragged, strap);
        args.Handled = true;
    }
}
