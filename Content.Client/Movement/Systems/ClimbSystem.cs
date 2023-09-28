using Content.Client.Interactable;
using Content.Shared.Climbing;
using Content.Shared.DragDrop;

namespace Content.Client.Movement.Systems;

public sealed class ClimbSystem : SharedClimbSystem
{
    [Dependency] private readonly InteractionSystem _interactionSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ClimbableComponent, CanDropTargetEvent>(OnCanDragDropOn);
    }

    protected override void OnCanDragDropOn(EntityUid uid, ClimbableComponent component, ref CanDropTargetEvent args)
    {
        base.OnCanDragDropOn(uid, component, ref args);

        if (!args.CanDrop)
            return;

        var user = args.User;
        var target = uid;
        var dragged = args.Dragged;
        bool Ignored(EntityUid entity) => entity == target || entity == user || entity == dragged;

        args.CanDrop = _interactionSystem.InRangeUnobstructed(user, target, component.Range, predicate: Ignored)
                       && _interactionSystem.InRangeUnobstructed(user, dragged, component.Range, predicate: Ignored);
        args.Handled = true;
    }
}
