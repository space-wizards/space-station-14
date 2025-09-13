using System.Diagnostics.CodeAnalysis;
using Content.Shared.ActionBlocker;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DragDrop;
using Content.Shared.FixedPoint;
using Content.Shared.Item;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.Fluids.EntitySystems;

/// <summary>
/// Handles drag and drop of various solutions.
/// </summary>
/// <remarks>
/// The thing dragged always "gives" its reagents away for consistent UX.
/// </remarks>
/// <seealso cref="DumpableSolutionComponent" />
/// <seealso cref="DrainableSolutionComponent" />
/// <seealso cref="RefillableSolutionComponent" />
public sealed class SolutionDumpingSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly OpenableSystem _openable = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solContainer = default!;

    private EntityQuery<DumpableSolutionComponent> _dumpQuery;
    private EntityQuery<ItemComponent> _itemQuery;
    private EntityQuery<RefillableSolutionComponent> _refillableQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DrainableSolutionComponent, CanDragEvent>(OnDrainableCanDrag);
        SubscribeLocalEvent<RefillableSolutionComponent, CanDragEvent>(OnRefillableCanDrag);
        SubscribeLocalEvent<DrainableSolutionComponent, CanDropDraggedEvent>(OnDrainableCanDragDropped);
        SubscribeLocalEvent<RefillableSolutionComponent, CanDropDraggedEvent>(OnRefillableCanDropDragged);

        SubscribeLocalEvent<RefillableSolutionComponent, DragDropDraggedEvent>(OnRefillableDragged);
        SubscribeLocalEvent<DrainableSolutionComponent, DragDropDraggedEvent>(OnDrainableDragged);

        // We use queries for these since CanDropDraggedEvent gets called pretty rapidly
        _itemQuery = GetEntityQuery<ItemComponent>();
        _refillableQuery = GetEntityQuery<RefillableSolutionComponent>();
        _dumpQuery = GetEntityQuery<DumpableSolutionComponent>();
    }

    private void OnDrainableCanDrag(Entity<DrainableSolutionComponent> ent, ref CanDragEvent args)
    {
        if (_itemQuery.HasComp(ent))
            args.Handled = true;
    }

    private void OnRefillableCanDrag(Entity<RefillableSolutionComponent> ent, ref CanDragEvent args)
    {
        args.Handled = true;
    }

    private void OnDrainableCanDragDropped(Entity<DrainableSolutionComponent> ent, ref CanDropDraggedEvent args)
    {
        // Easily drawn-from thing can be dragged onto easily refillable thing.
        if (!_refillableQuery.HasComp(args.Target))
            return;

        args.CanDrop = true;
        args.Handled = true;
    }

    private void OnRefillableCanDropDragged(Entity<RefillableSolutionComponent> entity, ref CanDropDraggedEvent args)
    {
        // Easily refillable things can be dragged onto (and dumped into) easy-to-dump-into things,
        if (!_dumpQuery.HasComp(args.Target))
            return;

        args.CanDrop = true;
        args.Handled = true;
    }

    private void OnDrainableDragged(Entity<DrainableSolutionComponent> sourceContainer, ref DragDropDraggedEvent args)
    {
        // We only allow dragging drainable solutions which are items onto refillable solutions
        if (!_refillableQuery.TryComp(args.Target, out var targetRefillComp)
            || !_itemQuery.HasComp(sourceContainer))
            return;

        // Bail if target is refillable but doesn't have a solution
        if (!_solContainer.TryGetRefillableSolution((args.Target, targetRefillComp),
                out var targetSolEnt,
                out var targetSol))
            return;

        // Check openness, hands, source being empty, and target being full.
        if (!DragInteractionChecks(args,
                sourceContainer,
                sourceContainer.Comp.Solution,
                targetSol,
                out var sourceEnt))
            return;

        _solContainer.TryAddSolution(targetSolEnt.Value,
            _solContainer.SplitSolution(sourceEnt.Value, targetSol.AvailableVolume));

        _audio.PlayPredicted(AbsorbentComponent.DefaultTransferSound, args.Target, args.User);
    }

    private void OnRefillableDragged(Entity<RefillableSolutionComponent> sourceContainer, ref DragDropDraggedEvent args)
    {
        // We only allow dragging refillable solutions onto DumpableSolutions
        if (!_dumpQuery.TryComp(args.Target, out var targetDumpComp))
            return;

        // Target has DumpableSolution but we couldn't get its solution. Oh well.
        if (!_solContainer.TryGetDumpableSolution((args.Target, targetDumpComp, null),
                out var targetSolEnt,
                out var targetSol))
            return;

        // Check openness, hands, source being empty, and target being full.
        if (!DragInteractionChecks(args,
                sourceContainer,
                sourceContainer.Comp.Solution,
                targetSol,
                out var sourceSolEnt))
            return;

        if (targetDumpComp.Unlimited)
        {
            // Unlimited means we're dumping into an infinite buffer, so we
            // have to be careful that we don't trigger any reactions. This
            // means SolutionContainerSystem.AddSolution can't be used!
            targetSol.AddSolution(
                _solContainer.SplitSolution(sourceSolEnt.Value, sourceSolEnt.Value.Comp.Solution.Volume),
                _protoMan);
            // Solution.AddSolution doesn't dirty targetSol for us
            Dirty(targetSolEnt.Value);
        }
        else
        {
            _solContainer.TryAddSolution(targetSolEnt.Value,
                _solContainer.SplitSolution(sourceSolEnt.Value, targetSol.AvailableVolume));
        }

        _audio.PlayPredicted(AbsorbentComponent.DefaultTransferSound, args.Target, args.User);
    }

    // Common checks between dragging handlers.
    // Yes I realize this is an obtuse method signature but it's just how it worked out.
    private bool DragInteractionChecks(DragDropDraggedEvent args,
        EntityUid sourceContainer,
        string sourceSolutionName,
        Solution targetSol,
        [NotNullWhen(true)] out Entity<SolutionComponent>? sourceSolEnt)
    {
        sourceSolEnt = null;
        if (!_actionBlocker.CanComplexInteract(args.User))
        {
            _popup.PopupClient(Loc.GetString("mopping-system-no-hands"), args.User, args.User);
            return false;
        }

        if (!_solContainer.TryGetSolution(sourceContainer, sourceSolutionName, out sourceSolEnt)
            || sourceSolEnt.Value.Comp.Solution.Volume == FixedPoint2.Zero)
        {
            _popup.PopupClient(Loc.GetString("mopping-system-empty", ("used", sourceContainer)),
                sourceContainer,
                args.User);
            return false;
        }

        if (targetSol.AvailableVolume == FixedPoint2.Zero)
        {
            _popup.PopupClient(Loc.GetString("mopping-system-full", ("used", args.Target)), args.Target, args.User);
            return false;
        }

        // Both things need to be open.
        return _openable.IsOpen(sourceContainer, args.User, predicted: true)
               && _openable.IsOpen(args.Target, args.User, predicted: true);
    }
}
