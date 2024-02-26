using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.DragDrop;
using Content.Shared.Fluids.Components;
using Content.Shared.Movement.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared.Fluids;

public abstract class SharedPuddleSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

    /// <summary>
    /// The lowest threshold to be considered for puddle sprite states as well as slipperiness of a puddle.
    /// </summary>
    public const float LowThreshold = 0.3f;

    public const float MediumThreshold = 0.6f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RefillableSolutionComponent, CanDragEvent>(OnRefillableCanDrag);
        SubscribeLocalEvent<DumpableSolutionComponent, CanDropTargetEvent>(OnDumpCanDropTarget);
        SubscribeLocalEvent<DrainableSolutionComponent, CanDropTargetEvent>(OnDrainCanDropTarget);
        SubscribeLocalEvent<RefillableSolutionComponent, CanDropDraggedEvent>(OnRefillableCanDropDragged);
        SubscribeLocalEvent<PuddleComponent, GetFootstepSoundEvent>(OnGetFootstepSound);
    }

    private void OnRefillableCanDrag(Entity<RefillableSolutionComponent> entity, ref CanDragEvent args)
    {
        args.Handled = true;
    }

    private void OnDumpCanDropTarget(Entity<DumpableSolutionComponent> entity, ref CanDropTargetEvent args)
    {
        if (HasComp<DrainableSolutionComponent>(args.Dragged))
        {
            args.CanDrop = true;
            args.Handled = true;
        }
    }

    private void OnDrainCanDropTarget(Entity<DrainableSolutionComponent> entity, ref CanDropTargetEvent args)
    {
        if (HasComp<RefillableSolutionComponent>(args.Dragged))
        {
            args.CanDrop = true;
            args.Handled = true;
        }
    }

    private void OnRefillableCanDropDragged(Entity<RefillableSolutionComponent> entity, ref CanDropDraggedEvent args)
    {
        if (!HasComp<DrainableSolutionComponent>(args.Target) && !HasComp<DumpableSolutionComponent>(args.Target))
            return;

        args.CanDrop = true;
        args.Handled = true;
    }

    private void OnGetFootstepSound(Entity<PuddleComponent> entity, ref GetFootstepSoundEvent args)
    {
        if (!_solutionContainerSystem.ResolveSolution(entity.Owner, entity.Comp.SolutionName, ref entity.Comp.Solution,
                out var solution))
            return;

        var reagentId = solution.GetPrimaryReagentId();
        if (!string.IsNullOrWhiteSpace(reagentId?.Prototype)
            && _prototypeManager.TryIndex(reagentId.Value.Prototype, out ReagentPrototype? proto))
        {
            args.Sound = proto.FootstepSound;
        }
    }
}
