using Content.Client.Interactable;
using Content.Shared.Climbing;
using Content.Shared.DragDrop;
using Robust.Shared.GameStates;

namespace Content.Client.Movement.Systems;

public sealed class ClimbSystem : SharedClimbSystem
{
    [Dependency] private readonly InteractionSystem _interactionSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ClimbingComponent, ComponentHandleState>(OnClimbingState);
    }

    private static void OnClimbingState(EntityUid uid, ClimbingComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not ClimbingComponent.ClimbModeComponentState climbModeState)
            return;

        component.IsClimbing = climbModeState.Climbing;
        component.OwnerIsTransitioning = climbModeState.IsTransitioning;
    }

    protected override void OnCanDragDropOn(EntityUid uid, ClimbableComponent component, CanDragDropOnEvent args)
    {
        base.OnCanDragDropOn(uid, component, args);

        if (!args.CanDrop)
            return;

        var user = args.User;
        var target = args.Target;
        var dragged = args.Dragged;
        bool Ignored(EntityUid entity) => entity == target || entity == user || entity == dragged;

        args.CanDrop = _interactionSystem.InRangeUnobstructed(user, target, component.Range, predicate: Ignored)
                       && _interactionSystem.InRangeUnobstructed(user, dragged, component.Range, predicate: Ignored);
        args.Handled = true;
    }
}
