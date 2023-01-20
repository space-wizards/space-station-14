using Content.Shared.DragDrop;
using Content.Shared.Movement;
using Content.Shared.Movement.Events;

namespace Content.Shared.Climbing;

public abstract class SharedClimbSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ClimbingComponent, UpdateCanMoveEvent>(HandleMoveAttempt);
        SubscribeLocalEvent<ClimbableComponent, CanDragDropOnEvent>(OnCanDragDropOn);
    }

    private static void HandleMoveAttempt(EntityUid uid, ClimbingComponent component, UpdateCanMoveEvent args)
    {
        if (component.LifeStage > ComponentLifeStage.Running)
            return;

        if (component.OwnerIsTransitioning)
            args.Cancel();
    }

    protected virtual void OnCanDragDropOn(EntityUid uid, ClimbableComponent component, CanDragDropOnEvent args)
    {
        args.CanDrop = HasComp<ClimbingComponent>(args.Dragged);
    }
}
