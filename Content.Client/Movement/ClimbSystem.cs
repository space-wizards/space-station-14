using Content.Client.Movement.Components;
using Content.Shared.Climbing;
using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Robust.Shared.GameStates;

namespace Content.Client.Movement;

public sealed class ClimbSystem : SharedClimbSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ClimbingComponent, ComponentHandleState>(OnClimbingState);
    }

    private static void OnClimbingState(EntityUid uid, ClimbingComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not SharedClimbingComponent.ClimbModeComponentState climbModeState)
        {
            return;
        }

        component.IsClimbing = climbModeState.Climbing;
        component.OwnerIsTransitioning = climbModeState.IsTransitioning;
    }

    protected override void OnCanDragDropOn(EntityUid uid, SharedClimbableComponent component, CanDragDropOnEvent args)
    {
        base.OnCanDragDropOn(uid, component, args);

        if (!args.Handled)
            return;

        var user = args.User;
        var target = args.Target;
        var dragged = args.Dragged;
        bool Ignored(EntityUid entity) => entity == target || entity == user || entity == dragged;

        var sys = Get<SharedInteractionSystem>();

        args.Handled = sys.InRangeUnobstructed(user, target, component.Range, predicate: Ignored)
                       && sys.InRangeUnobstructed(user, dragged, component.Range, predicate: Ignored);
    }
}
