using Content.Shared.DragDrop;
using Content.Shared.Movement;
using Content.Shared.Movement.Events;

namespace Content.Shared.Climbing;

public abstract class SharedClimbSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SharedClimbingComponent, UpdateCanMoveEvent>(HandleMoveAttempt);
        SubscribeLocalEvent<SharedClimbableComponent, CanDragDropOnEvent>(OnCanDragDropOn);
    }

    private static void HandleMoveAttempt(EntityUid uid, SharedClimbingComponent component, UpdateCanMoveEvent args)
    {
        if (component.LifeStage > ComponentLifeStage.Running)
            return;

        if (component.OwnerIsTransitioning)
            args.Cancel();
    }

    protected virtual void OnCanDragDropOn(EntityUid uid, SharedClimbableComponent component, CanDragDropOnEvent args)
    {
        args.CanDrop = HasComp<SharedClimbingComponent>(args.Dragged);
    }
}
