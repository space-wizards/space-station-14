using Content.Shared.Chemistry.Components;
using Content.Shared.DragDrop;
using Content.Shared.Fluids.Components;

namespace Content.Shared.Fluids;

public abstract class SharedPuddleSystem : EntitySystem
{
    /// <summary>
    /// The lowest threshold to be considered for puddle sprite states as well as slipperiness of a puddle.
    /// </summary>
    public const float LowThreshold = 0.3f;

    public const float MediumThreshold = 0.6f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RefillableSolutionComponent, CanDragEvent>(OnRefillableCanDrag);
        SubscribeLocalEvent<RefillableSolutionComponent, CanDropDraggedEvent>(OnRefillableCanDropDragged);
        SubscribeLocalEvent<DrainableSolutionComponent, CanDropTargetEvent>(OnDrainCanDropTarget);
    }

    private void OnRefillableCanDrag(EntityUid uid, RefillableSolutionComponent component, ref CanDragEvent args)
    {
        args.Handled = true;
    }

    private void OnDrainCanDropTarget(EntityUid uid, DrainableSolutionComponent component, ref CanDropTargetEvent args)
    {
        if (HasComp<RefillableSolutionComponent>(args.Dragged))
        {
            args.CanDrop = true;
            args.Handled = true;
        }
    }

    private void OnRefillableCanDropDragged(EntityUid uid, RefillableSolutionComponent component, ref CanDropDraggedEvent args)
    {
        if (!HasComp<DrainableSolutionComponent>(args.Target) && !HasComp<DrainComponent>(args.Target))
            return;

        args.CanDrop = true;
        args.Handled = true;
    }
}
