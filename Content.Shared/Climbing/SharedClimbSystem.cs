using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Movement.Events;
using Robust.Shared.Serialization;

namespace Content.Shared.Climbing;

public abstract partial class SharedClimbSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ClimbingComponent, UpdateCanMoveEvent>(HandleMoveAttempt);
    }

    private static void HandleMoveAttempt(EntityUid uid, ClimbingComponent component, UpdateCanMoveEvent args)
    {
        if (component.LifeStage > ComponentLifeStage.Running)
            return;

        if (component.OwnerIsTransitioning)
            args.Cancel();
    }

    protected virtual void OnCanDragDropOn(EntityUid uid, ClimbableComponent component, ref CanDropTargetEvent args)
    {
        args.CanDrop = HasComp<ClimbingComponent>(args.Dragged);
    }

    [Serializable, NetSerializable]
    protected sealed partial class ClimbDoAfterEvent : SimpleDoAfterEvent
    {
    }
}
